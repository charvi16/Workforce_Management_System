using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WMS.Application.Common;
using WMS.Application.DTOs.Employees;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Security;

namespace WMS.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly WmsDbContext _dbContext;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository employeeRepository, WmsDbContext dbContext, ILogger<EmployeeService> logger)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<EmployeeDto>> SearchAsync(string? search, int? departmentId, int? roleId, string? role, string? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var stopwatch = Stopwatch.StartNew();

        var query = _dbContext.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{EscapeLikePattern(term)}%";
            var searchStatus = ParseStatus(term);
            var employeeId = int.TryParse(term, out var parsedEmployeeId)
                ? parsedEmployeeId
                : (int?)null;

            query = query.Where(e =>
                EF.Functions.Like(e.Username, likeTerm, @"\") ||
                EF.Functions.Like(e.FirstName, likeTerm, @"\") ||
                EF.Functions.Like(e.LastName, likeTerm, @"\") ||
                EF.Functions.Like(e.FirstName + " " + e.LastName, likeTerm, @"\") ||
                EF.Functions.Like(e.LastName + " " + e.FirstName, likeTerm, @"\") ||
                EF.Functions.Like(e.Email, likeTerm, @"\") ||
                EF.Functions.Like(e.PhoneNumber, likeTerm, @"\") ||
                EF.Functions.Like(e.Department.DepartmentName, likeTerm, @"\") ||
                EF.Functions.Like(e.Role.RoleName, likeTerm, @"\") ||
                (employeeId.HasValue && e.EmployeeId == employeeId.Value) ||
                (searchStatus.HasValue && e.Status == searchStatus.Value));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(e => e.RoleId == roleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleTerm = role.Trim();
            query = query.Where(e => e.Role.RoleName == roleTerm);
        }

        var statusFilter = ParseStatusFilter(status);
        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        _logger.LogInformation(
            "Employee search filters: search={Search}, departmentId={DepartmentId}, roleId={RoleId}, role={Role}, status={Status}, page={PageNumber}, size={PageSize}",
            search,
            departmentId,
            roleId,
            role,
            status,
            pageNumber,
            pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var employees = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        stopwatch.Stop();
        _logger.LogInformation(
            "Employee search completed in {ElapsedMs}ms, total={TotalCount}, returned={ReturnedCount}",
            stopwatch.ElapsedMilliseconds,
            totalCount,
            employees.Count);

        return new PagedResult<EmployeeDto>
        {
            Items = employees,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        return MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);
        await ValidateReferencesAsync(request.DepartmentId, request.RoleId, cancellationToken);

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _employeeRepository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Employee email already exists.");
        }

        if (await _employeeRepository.UsernameExistsAsync(username, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Employee username already exists.");
        }

        if (await _dbContext.UserLogins.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new InvalidOperationException("Login username already exists.");
        }

        var employee = new Employee();
        Apply(employee, request);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.UserLogins.Add(new UserLogin
        {
            Username = employee.Username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            EmployeeId = employee.EmployeeId,
            RoleId = employee.RoleId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(employee.EmployeeId, cancellationToken);
    }

    public async Task<EmployeeDto> UpdateAsync(int employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        await ValidateReferencesAsync(request.DepartmentId, request.RoleId, cancellationToken);

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _employeeRepository.EmailExistsAsync(email, employeeId, cancellationToken))
        {
            throw new InvalidOperationException("Employee email already exists.");
        }

        if (await _employeeRepository.UsernameExistsAsync(username, employeeId, cancellationToken))
        {
            throw new InvalidOperationException("Employee username already exists.");
        }

        if (await _dbContext.UserLogins.AnyAsync(u => u.Username == username && u.EmployeeId != employeeId, cancellationToken))
        {
            throw new InvalidOperationException("Login username already exists.");
        }

        Apply(employee, request);
        employee.UpdatedOn = DateTime.UtcNow;

        var login = await _dbContext.UserLogins.FirstOrDefaultAsync(u => u.EmployeeId == employeeId, cancellationToken);
        if (login is not null)
        {
            login.Username = employee.Username;
            login.RoleId = employee.RoleId;
        }

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    public async Task DeleteAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        var login = await _dbContext.UserLogins.FirstOrDefaultAsync(u => u.EmployeeId == employeeId, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (login is not null)
        {
            _dbContext.UserLogins.Remove(login);
        }

        _employeeRepository.Delete(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EmployeeDto> AssignDepartmentAsync(int employeeId, int departmentId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        if (!await _dbContext.Departments.AnyAsync(d => d.DepartmentId == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid department selected.");
        }

        employee.DepartmentId = departmentId;
        employee.UpdatedOn = DateTime.UtcNow;
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    public async Task<EmployeeDto> AssignRoleAsync(int employeeId, int roleId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        if (!await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid role selected.");
        }

        employee.RoleId = roleId;
        employee.UpdatedOn = DateTime.UtcNow;

        var login = await _dbContext.UserLogins.FirstOrDefaultAsync(u => u.EmployeeId == employeeId, cancellationToken);
        if (login is not null)
        {
            login.RoleId = roleId;
        }

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    private async Task<Employee> GetEmployeeOrThrowAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");
    }

    private async Task ValidateReferencesAsync(int departmentId, int roleId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Departments.AnyAsync(d => d.DepartmentId == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid department selected.");
        }

        if (!await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid role selected.");
        }
    }

    private static void ValidateCreateRequest(CreateEmployeeRequestDto request)
    {
        ValidateCommonRequest(request.Username, request.FirstName, request.LastName, request.Email, request.PhoneNumber);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Password is required for new employees.");
        }

        if (request.Password.Trim().Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }
    }

    private static void ValidateUpdateRequest(UpdateEmployeeRequestDto request)
    {
        ValidateCommonRequest(request.Username, request.FirstName, request.LastName, request.Email, request.PhoneNumber);
    }

    private static void ValidateCommonRequest(string username, string firstName, string lastName, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new InvalidOperationException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new InvalidOperationException("Phone number is required.");
        }
    }

    private static void Apply(Employee employee, CreateEmployeeRequestDto request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Username = request.Username.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber.Trim();
        employee.Gender = (Gender)request.Gender;
        employee.DOB = request.DOB;
        employee.DOJ = request.DOJ;
        employee.DepartmentId = request.DepartmentId;
        employee.RoleId = request.RoleId;
        employee.Status = (EmployeeStatus)request.Status;
    }

    private static void Apply(Employee employee, UpdateEmployeeRequestDto request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Username = request.Username.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber.Trim();
        employee.Gender = (Gender)request.Gender;
        employee.DOB = request.DOB;
        employee.DOJ = request.DOJ;
        employee.DepartmentId = request.DepartmentId;
        employee.RoleId = request.RoleId;
        employee.Status = (EmployeeStatus)request.Status;
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            Username = employee.Username,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Gender = (int)employee.Gender,
            GenderName = employee.Gender.ToString(),
            DOB = employee.DOB,
            DOJ = employee.DOJ,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department.DepartmentName,
            RoleId = employee.RoleId,
            RoleName = employee.Role.RoleName,
            Status = (int)employee.Status,
            StatusName = employee.Status.ToString()
        };
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }

    private static EmployeeStatus? ParseStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => EmployeeStatus.Active,
            "inactive" => EmployeeStatus.Inactive,
            _ => null
        };
    }

    private static EmployeeStatus? ParseStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "active" or "1" => EmployeeStatus.Active,
            "false" or "inactive" or "2" => EmployeeStatus.Inactive,
            _ => null
        };
    }
}
