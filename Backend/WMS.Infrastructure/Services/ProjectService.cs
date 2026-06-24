using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Projects;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Planned",
        "Active",
        "OnHold",
        "Completed",
        "Cancelled",
        "Delayed"
    };

    private readonly WmsDbContext _dbContext;

    public ProjectService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProjectDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, string? search, int? clientId, string? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var accessibleProjectIds = GetAccessibleProjectIdsQuery(currentUserRole, currentEmployee);

        var query = _dbContext.Projects
            .AsNoTracking()
            .Where(p => IsAdminRole(currentUserRole) || IsManagerRole(currentUserRole) || accessibleProjectIds.Contains(p.ProjectId));

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{EscapeLikePattern(term)}%";
            query = query.Where(p =>
                EF.Functions.Like(p.ProjectName, likeTerm, @"\") ||
                EF.Functions.Like(p.Client!.ClientName, likeTerm, @"\"));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = ApplyStatusFilter(query, NormalizeStatus(status));
        }

        var projected = query.Select(p => new ProjectRow
        {
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectName,
            ClientId = p.ClientId,
            ClientName = p.Client != null ? p.Client.ClientName : null,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            MembersCount = p.EmployeeAllocations.Count(a => a.Status)
        });

        var totalCount = await projected.CountAsync(cancellationToken);
        var items = await projected
            .OrderBy(p => p.ProjectName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ProjectDto> GetByIdAsync(int projectId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var accessibleProjectIds = GetAccessibleProjectIdsQuery(currentUserRole, currentEmployee);

        var project = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId && (IsAdminRole(currentUserRole) || IsManagerRole(currentUserRole) || accessibleProjectIds.Contains(p.ProjectId)))
            .Select(p => new ProjectRow
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.ClientName : null,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                MembersCount = p.EmployeeAllocations.Count(a => a.Status)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return project is null ? throw new InvalidOperationException("Project not found.") : MapToDto(project);
    }

    public Task<PagedResult<ProjectDto>> GetByClientAsync(int clientId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return GetAllAsync(currentUserRole, currentEmployeeId, null, clientId, null, pageNumber, pageSize, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(ProjectRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        await ValidateClientAsync(request.ClientId, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var project = new Project
        {
            ProjectName = request.ProjectName.Trim(),
            ClientId = request.ClientId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ResolveStatusForStorage(request.Status, request.StartDate, request.EndDate)
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncProjectMembersAsync(project, request.MemberIds, currentEmployeeId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(project.ProjectId, nameof(UserRole.Admin), currentEmployeeId, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(int projectId, ProjectRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        await ValidateClientAsync(request.ClientId, cancellationToken);

        project.ProjectName = request.ProjectName.Trim();
        project.ClientId = request.ClientId;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = ResolveStatusForStorage(request.Status, request.StartDate, request.EndDate);

        await SyncProjectMembersAsync(project, request.MemberIds, currentEmployeeId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(projectId, nameof(UserRole.Admin), currentEmployeeId, cancellationToken);
    }

    public async Task<ProjectDto> UpdateStatusAsync(int projectId, ProjectStatusUpdateDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!AllowedStatuses.Contains(request.Status))
        {
            throw new InvalidOperationException("Invalid project status selected.");
        }

        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        if (!IsAdminRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
            if (currentEmployee is null)
            {
                throw new InvalidOperationException("Current employee not found.");
            }

            var accessibleProjectIds = GetAccessibleProjectIdsQuery(currentUserRole, currentEmployee);
            var canUpdate = await accessibleProjectIds.AnyAsync(id => id == projectId, cancellationToken);
            if (!canUpdate)
            {
                throw new InvalidOperationException("You do not have permission to update this project.");
            }
        }

        project.Status = ResolveStatusForStorage(request.Status, project.StartDate, project.EndDate);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(projectId, currentUserRole, currentEmployeeId, cancellationToken);
    }

    public async Task<ProjectDto> CancelAsync(int projectId, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        project.Status = "Cancelled";
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(projectId, nameof(UserRole.Admin), currentEmployeeId, cancellationToken);
    }

    private async Task ValidateClientAsync(int? clientId, CancellationToken cancellationToken)
    {
        if (clientId.HasValue && !await _dbContext.Clients.AnyAsync(c => c.ClientId == clientId.Value, cancellationToken))
        {
            throw new InvalidOperationException("Client not found.");
        }
    }

    private async Task SyncProjectMembersAsync(Project project, IReadOnlyCollection<int> memberIds, int currentEmployeeId, CancellationToken cancellationToken)
    {
        var selectedMemberIds = memberIds
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        var allocations = await _dbContext.EmployeeProjectAllocations
            .Where(a => a.ProjectId == project.ProjectId)
            .ToListAsync(cancellationToken);

        foreach (var activeAllocation in allocations.Where(a => a.Status && !selectedMemberIds.Contains(a.EmpId)))
        {
            activeAllocation.Status = false;
            activeAllocation.UpdatedOn = DateTime.UtcNow;
            activeAllocation.UpdatedBy = currentEmployeeId;
        }

        if (selectedMemberIds.Count == 0)
        {
            return;
        }

        var activeEmployeeIds = await _dbContext.Employees
            .Where(e => selectedMemberIds.Contains(e.EmployeeId) && e.Status == EmployeeStatus.Active)
            .Select(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        var missingEmployeeIds = selectedMemberIds.Except(activeEmployeeIds).ToList();
        if (missingEmployeeIds.Count > 0)
        {
            throw new InvalidOperationException("One or more selected project members are not active employees.");
        }

        var assignedOn = DefaultAssignedOnDate(project);
        foreach (var memberId in selectedMemberIds)
        {
            var existingActive = allocations.FirstOrDefault(a => a.EmpId == memberId && a.Status);
            if (existingActive is not null)
            {
                continue;
            }

            _dbContext.EmployeeProjectAllocations.Add(new EmployeeProjectAllocation
            {
                EmpId = memberId,
                ProjectId = project.ProjectId,
                AssignedOn = assignedOn,
                Status = true,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = currentEmployeeId
            });
        }
    }

    private static DateTime DefaultAssignedOnDate(Project project)
    {
        var today = DateTime.UtcNow.Date;
        if (project.StartDate.HasValue && today < project.StartDate.Value.Date)
        {
            return project.StartDate.Value.Date;
        }

        if (project.EndDate.HasValue && today > project.EndDate.Value.Date)
        {
            return project.EndDate.Value.Date;
        }

        return today;
    }

    private IQueryable<int> GetAccessibleProjectIdsQuery(string currentUserRole, Employee? currentEmployee)
    {
        if (IsAdminRole(currentUserRole))
        {
            return _dbContext.Projects.Select(p => p.ProjectId);
        }

        if (currentEmployee is null)
        {
            return _dbContext.Projects.Where(p => false).Select(p => p.ProjectId);
        }

        return _dbContext.EmployeeProjectAllocations
            .AsNoTracking()
            .Where(a => a.Status && (
                a.EmpId == currentEmployee.EmployeeId ||
                (IsManagerRole(currentUserRole) && a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee))))
            .Select(a => a.ProjectId)
            .Distinct();
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private static IQueryable<Project> ApplyStatusFilter(IQueryable<Project> query, string status)
    {
        var today = DateTime.UtcNow.Date;
        return status.ToLowerInvariant() switch
        {
            "planned" => query.Where(p =>
                p.Status == "Planned" ||
                (p.Status != "Completed" && p.Status != "Cancelled" && p.Status != "OnHold" && p.StartDate.HasValue && p.StartDate.Value.Date > today)),
            "active" => query.Where(p =>
                p.Status == "Active" ||
                (p.Status != "Completed" && p.Status != "Cancelled" && p.Status != "OnHold" &&
                 (!p.StartDate.HasValue || p.StartDate.Value.Date <= today) &&
                 (!p.EndDate.HasValue || p.EndDate.Value.Date >= today))),
            "onhold" => query.Where(p => p.Status == "OnHold"),
            "completed" => query.Where(p => p.Status == "Completed"),
            "cancelled" => query.Where(p => p.Status == "Cancelled"),
            "delayed" => query.Where(p =>
                p.Status == "Delayed" ||
                (p.Status != "Completed" && p.Status != "Cancelled" && p.Status != "OnHold" && p.EndDate.HasValue && p.EndDate.Value.Date < today)),
            _ => query
        };
    }

    private static string ResolveStatusForStorage(string status, DateTime? startDate, DateTime? endDate)
    {
        var normalized = NormalizeStatus(status);
        if (normalized is "Completed" or "Cancelled" or "OnHold" or "Delayed")
        {
            return normalized;
        }

        var today = DateTime.UtcNow.Date;
        if (startDate.HasValue && startDate.Value.Date > today)
        {
            return "Planned";
        }

        if (endDate.HasValue && endDate.Value.Date < today)
        {
            return "Delayed";
        }

        return "Active";
    }

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

    private static bool IsTerminalStatus(string status) =>
        string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "OnHold", StringComparison.OrdinalIgnoreCase);

    private static bool IsAdminRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);

    private static bool IsManagerRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");

    private static ProjectDto MapToDto(ProjectRow project)
    {
        return new ProjectDto
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            ClientId = project.ClientId,
            ClientName = project.ClientName,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = ResolveStatusForDisplay(project.Status, project.StartDate, project.EndDate),
            MembersCount = project.MembersCount
        };
    }

    private static string ResolveStatusForDisplay(string status, DateTime? startDate, DateTime? endDate)
    {
        var normalized = NormalizeStatus(status);
        if (normalized is "Completed" or "Cancelled" or "OnHold")
        {
            return normalized;
        }

        var today = DateTime.UtcNow.Date;
        if (startDate.HasValue && startDate.Value.Date > today)
        {
            return "Planned";
        }

        if (endDate.HasValue && endDate.Value.Date < today)
        {
            return "Delayed";
        }

        return "Active";
    }

    private sealed class ProjectRow
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MembersCount { get; set; }
    }
}
