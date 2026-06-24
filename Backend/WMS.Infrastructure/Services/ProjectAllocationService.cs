using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Employees;
using WMS.Application.DTOs.ProjectAllocations;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class ProjectAllocationService : IProjectAllocationService
{
    private readonly WmsDbContext _dbContext;

    public ProjectAllocationService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProjectAllocationDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var query = BuildAccessibleQuery(currentUserRole, currentEmployee);

        var projected = query.Select(a => new AllocationRow
        {
            AllocationId = a.AllocationId,
            EmpId = a.EmpId,
            EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
            ProjectId = a.ProjectId,
            ProjectName = a.Project.ProjectName,
            ClientName = a.Project.Client != null ? a.Project.Client.ClientName : null,
            AssignedOn = a.AssignedOn,
            RoleInProject = a.RoleInProject,
            AllocationPercentage = a.AllocationPercentage,
            Status = a.Status,
            CreatedOn = a.CreatedOn,
            CreatedBy = a.CreatedBy,
            UpdatedOn = a.UpdatedOn,
            UpdatedBy = a.UpdatedBy
        });

        var totalCount = await projected.CountAsync(cancellationToken);
        var rows = await projected
            .OrderByDescending(a => a.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectAllocationDto>
        {
            Items = rows.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAssignableEmployeesAsync(string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!IsAdminRole(currentUserRole) && !IsManagerRole(currentUserRole))
        {
            throw new InvalidOperationException("You do not have permission to assign employees to projects.");
        }

        var query = _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Role)
            .Where(e => e.Status == EmployeeStatus.Active)
            .AsQueryable();

        if (IsManagerRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
            if (currentEmployee is null)
            {
                throw new InvalidOperationException("Current employee not found.");
            }

            query = query.Where(e =>
                e.DepartmentId == currentEmployee.DepartmentId &&
                e.Role.RoleName == nameof(UserRole.Employee));
        }

        return await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                Username = e.Username,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                Gender = (int)e.Gender,
                GenderName = e.Gender.ToString(),
                DOB = e.DOB,
                DOJ = e.DOJ,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.DepartmentName,
                RoleId = e.RoleId,
                RoleName = e.Role.RoleName,
                Status = (int)e.Status,
                StatusName = e.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<PagedResult<ProjectAllocationDto>> GetByProjectAsync(int projectId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return GetFilteredAsync(a => a.ProjectId == projectId, currentUserRole, currentEmployeeId, pageNumber, pageSize, cancellationToken);
    }

    public Task<PagedResult<ProjectAllocationDto>> GetByEmployeeAsync(int employeeId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return GetFilteredAsync(a => a.EmpId == employeeId, currentUserRole, currentEmployeeId, pageNumber, pageSize, cancellationToken);
    }

    public async Task<ProjectAllocationDto> CreateAsync(ProjectAllocationRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!IsAdminRole(currentUserRole) && !IsManagerRole(currentUserRole))
        {
            throw new InvalidOperationException("You do not have permission to assign employees to projects.");
        }

        var employee = await _dbContext.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmpId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException("Employee must be active.");
        }

        var project = await _dbContext.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        var projectStatus = ResolveProjectStatus(project);
        if (projectStatus is "Completed" or "Cancelled")
        {
            throw new InvalidOperationException("You cannot assign employees to completed or cancelled projects.");
        }

        if (!IsAdminRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
            if (currentEmployee is null)
            {
                throw new InvalidOperationException("Current employee not found.");
            }

            if (employee.DepartmentId != currentEmployee.DepartmentId || !string.Equals(employee.Role.RoleName, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Managers can only assign team employees from their own department.");
            }
        }

        if (request.AllocationPercentage.HasValue && (request.AllocationPercentage < 1 || request.AllocationPercentage > 100))
        {
            throw new InvalidOperationException("Allocation percentage must be between 1 and 100.");
        }

        if (project.StartDate.HasValue && request.AssignedOn.Date < project.StartDate.Value.Date)
        {
            throw new InvalidOperationException("Assigned on date cannot be before the project start date.");
        }

        if (project.EndDate.HasValue && request.AssignedOn.Date > project.EndDate.Value.Date)
        {
            throw new InvalidOperationException("Assigned on date cannot be after the project end date.");
        }

        var duplicateExists = await _dbContext.EmployeeProjectAllocations.AnyAsync(a =>
            a.Status &&
            a.EmpId == request.EmpId &&
            a.ProjectId == request.ProjectId,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("This employee is already assigned to the selected project.");
        }

        var allocation = new EmployeeProjectAllocation
        {
            EmpId = request.EmpId,
            ProjectId = request.ProjectId,
            AssignedOn = request.AssignedOn.Date,
            RoleInProject = string.IsNullOrWhiteSpace(request.RoleInProject) ? null : request.RoleInProject.Trim(),
            AllocationPercentage = request.AllocationPercentage,
            Status = request.Status,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = currentEmployeeId,
            UpdatedBy = null,
            UpdatedOn = null
        };

        _dbContext.EmployeeProjectAllocations.Add(allocation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(allocation.AllocationId, currentUserRole, currentEmployeeId, cancellationToken);
    }

    public async Task<ProjectAllocationDto> UpdateAsync(int allocationId, ProjectAllocationRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!IsAdminRole(currentUserRole) && !IsManagerRole(currentUserRole))
        {
            throw new InvalidOperationException("You do not have permission to update allocations.");
        }

        var allocation = await _dbContext.EmployeeProjectAllocations
            .Include(a => a.Employee)
            .Include(a => a.Project)
            .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(a => a.AllocationId == allocationId, cancellationToken)
            ?? throw new InvalidOperationException("Allocation not found.");

        var employee = await _dbContext.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmpId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException("Employee must be active.");
        }

        var projectStatus = ResolveProjectStatus(project);
        if (projectStatus is "Completed" or "Cancelled")
        {
            throw new InvalidOperationException("You cannot assign employees to completed or cancelled projects.");
        }

        if (!IsAdminRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
            if (currentEmployee is null || employee.DepartmentId != currentEmployee.DepartmentId || !string.Equals(employee.Role.RoleName, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Managers can only update allocations for team employees.");
            }
        }

        if (request.AllocationPercentage.HasValue && (request.AllocationPercentage < 1 || request.AllocationPercentage > 100))
        {
            throw new InvalidOperationException("Allocation percentage must be between 1 and 100.");
        }

        if (project.StartDate.HasValue && request.AssignedOn.Date < project.StartDate.Value.Date)
        {
            throw new InvalidOperationException("Assigned on date cannot be before the project start date.");
        }

        if (project.EndDate.HasValue && request.AssignedOn.Date > project.EndDate.Value.Date)
        {
            throw new InvalidOperationException("Assigned on date cannot be after the project end date.");
        }

        var duplicateExists = await _dbContext.EmployeeProjectAllocations.AnyAsync(a =>
            a.Status &&
            a.AllocationId != allocationId &&
            a.EmpId == request.EmpId &&
            a.ProjectId == request.ProjectId,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("This employee is already assigned to the selected project.");
        }

        allocation.EmpId = request.EmpId;
        allocation.ProjectId = request.ProjectId;
        allocation.AssignedOn = request.AssignedOn.Date;
        allocation.RoleInProject = string.IsNullOrWhiteSpace(request.RoleInProject) ? null : request.RoleInProject.Trim();
        allocation.AllocationPercentage = request.AllocationPercentage;
        allocation.Status = request.Status;
        allocation.UpdatedOn = DateTime.UtcNow;
        allocation.UpdatedBy = currentEmployeeId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(allocationId, currentUserRole, currentEmployeeId, cancellationToken);
    }

    public async Task<ProjectAllocationDto> DeleteAsync(int allocationId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!IsAdminRole(currentUserRole) && !IsManagerRole(currentUserRole))
        {
            throw new InvalidOperationException("You do not have permission to deactivate allocations.");
        }

        var allocation = await _dbContext.EmployeeProjectAllocations
            .Include(a => a.Employee)
            .Include(a => a.Project)
            .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(a => a.AllocationId == allocationId, cancellationToken)
            ?? throw new InvalidOperationException("Allocation not found.");

        allocation.Status = false;
        allocation.UpdatedOn = DateTime.UtcNow;
        allocation.UpdatedBy = currentEmployeeId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(new AllocationRow
        {
            AllocationId = allocation.AllocationId,
            EmpId = allocation.EmpId,
            EmployeeName = allocation.Employee.FirstName + " " + allocation.Employee.LastName,
            ProjectId = allocation.ProjectId,
            ProjectName = allocation.Project.ProjectName,
            ClientName = allocation.Project.Client?.ClientName,
            AssignedOn = allocation.AssignedOn,
            RoleInProject = allocation.RoleInProject,
            AllocationPercentage = allocation.AllocationPercentage,
            Status = allocation.Status,
            CreatedOn = allocation.CreatedOn,
            CreatedBy = allocation.CreatedBy,
            UpdatedOn = allocation.UpdatedOn,
            UpdatedBy = allocation.UpdatedBy
        });
    }

    private async Task<ProjectAllocationDto> GetByIdAsync(int allocationId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken)
    {
        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var query = BuildAccessibleQuery(currentUserRole, currentEmployee)
            .Where(a => a.AllocationId == allocationId);

        var row = await query.Select(a => new AllocationRow
        {
            AllocationId = a.AllocationId,
            EmpId = a.EmpId,
            EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
            ProjectId = a.ProjectId,
            ProjectName = a.Project.ProjectName,
            ClientName = a.Project.Client != null ? a.Project.Client.ClientName : null,
            AssignedOn = a.AssignedOn,
            RoleInProject = a.RoleInProject,
            AllocationPercentage = a.AllocationPercentage,
            Status = a.Status,
            CreatedOn = a.CreatedOn,
            CreatedBy = a.CreatedBy,
            UpdatedOn = a.UpdatedOn,
            UpdatedBy = a.UpdatedBy
        }).FirstOrDefaultAsync(cancellationToken);

        return row is null ? throw new InvalidOperationException("Allocation not found.") : MapToDto(row);
    }

    private async Task<PagedResult<ProjectAllocationDto>> GetFilteredAsync(Expression<Func<EmployeeProjectAllocation, bool>> predicate, string currentUserRole, int currentEmployeeId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var query = BuildAccessibleQuery(currentUserRole, currentEmployee).Where(predicate);

        var projected = query.Select(a => new AllocationRow
        {
            AllocationId = a.AllocationId,
            EmpId = a.EmpId,
            EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
            ProjectId = a.ProjectId,
            ProjectName = a.Project.ProjectName,
            ClientName = a.Project.Client != null ? a.Project.Client.ClientName : null,
            AssignedOn = a.AssignedOn,
            RoleInProject = a.RoleInProject,
            AllocationPercentage = a.AllocationPercentage,
            Status = a.Status,
            CreatedOn = a.CreatedOn,
            CreatedBy = a.CreatedBy,
            UpdatedOn = a.UpdatedOn,
            UpdatedBy = a.UpdatedBy
        });

        var totalCount = await projected.CountAsync(cancellationToken);
        var rows = await projected
            .OrderByDescending(a => a.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectAllocationDto>
        {
            Items = rows.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private IQueryable<EmployeeProjectAllocation> BuildAccessibleQuery(string currentUserRole, Employee? currentEmployee)
    {
        var query = _dbContext.EmployeeProjectAllocations
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.Department)
            .Include(a => a.Project)
            .ThenInclude(p => p.Client)
            .AsQueryable();

        if (IsAdminRole(currentUserRole))
        {
            return query;
        }

        if (currentEmployee is null)
        {
            return query.Where(a => false);
        }

        if (IsEmployeeRole(currentUserRole))
        {
            return query.Where(a => a.EmpId == currentEmployee.EmployeeId);
        }

        return query.Where(a => a.EmpId == currentEmployee.EmployeeId || (a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee)));
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private static string ResolveProjectStatus(Project project)
    {
        var status = project.Status.Trim();
        if (IsTerminalStatus(status))
        {
            return status;
        }

        var today = DateTime.UtcNow.Date;
        if (project.StartDate.HasValue && project.StartDate.Value.Date > today)
        {
            return "Planned";
        }

        if (project.EndDate.HasValue && project.EndDate.Value.Date < today)
        {
            return "Delayed";
        }

        return "Active";
    }

    private static bool IsTerminalStatus(string status) =>
        string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "OnHold", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "planned" => "Planned",
            "active" => "Active",
            "onhold" or "on hold" => "OnHold",
            "completed" => "Completed",
            "cancelled" or "canceled" => "Cancelled",
            "delayed" => "Delayed",
            _ => throw new InvalidOperationException("Invalid project status selected.")
        };
    }

    private static bool IsAdminRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);

    private static bool IsManagerRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);

    private static bool IsEmployeeRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase);

    private static ProjectAllocationDto MapToDto(AllocationRow row)
    {
        return new ProjectAllocationDto
        {
            AllocationId = row.AllocationId,
            EmpId = row.EmpId,
            EmployeeName = row.EmployeeName,
            ProjectId = row.ProjectId,
            ProjectName = row.ProjectName,
            ClientName = row.ClientName,
            AssignedOn = row.AssignedOn,
            RoleInProject = row.RoleInProject,
            AllocationPercentage = row.AllocationPercentage,
            Status = row.Status,
            CreatedOn = row.CreatedOn,
            CreatedBy = row.CreatedBy,
            UpdatedOn = row.UpdatedOn,
            UpdatedBy = row.UpdatedBy
        };
    }

    private sealed class AllocationRow
    {
        public int AllocationId { get; set; }
        public int EmpId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public DateTime AssignedOn { get; set; }
        public string? RoleInProject { get; set; }
        public int? AllocationPercentage { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
