using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WMS.Application.Common;
using WMS.Application.DTOs.Departments;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly WmsDbContext _dbContext;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(WmsDbContext dbContext, ILogger<DepartmentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResult<DepartmentDto>> SearchAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var stopwatch = Stopwatch.StartNew();

        var query = _dbContext.Departments
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{EscapeLikePattern(term)}%";
            query = query.Where(d =>
                EF.Functions.Like(d.DepartmentName, likeTerm, @"\") ||
                (d.Description != null && EF.Functions.Like(d.Description, likeTerm, @"\")));
        }

        _logger.LogInformation(
            "Department search filters: search={Search}, page={PageNumber}, size={PageSize}",
            search,
            pageNumber,
            pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var departments = await query
            .OrderBy(d => d.DepartmentName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                EmployeeCount = d.Employees.Count
            })
            .ToListAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Department search completed in {ElapsedMs}ms, total={TotalCount}, returned={ReturnedCount}",
            stopwatch.ElapsedMilliseconds,
            totalCount,
            departments.Count);

        return new PagedResult<DepartmentDto>
        {
            Items = departments,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                EmployeeCount = d.Employees.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .Where(d => d.DepartmentId == departmentId)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                EmployeeCount = d.Employees.Count
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var departmentName = NormalizeDepartmentName(request.DepartmentName);

        if (await _dbContext.Departments.AnyAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower(), cancellationToken))
        {
            throw new InvalidOperationException("Department already exists.");
        }

        var department = new Department
        {
            DepartmentName = departmentName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(department.DepartmentId, cancellationToken);
    }

    public async Task<DepartmentDto> UpdateAsync(int departmentId, DepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");

        var departmentName = NormalizeDepartmentName(request.DepartmentName);

        if (await _dbContext.Departments.AnyAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower() && d.DepartmentId != departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Department already exists.");
        }

        department.DepartmentName = departmentName;
        department.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(departmentId, cancellationToken);
    }

    public async Task DeleteAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");

        if (await _dbContext.Employees.AnyAsync(e => e.DepartmentId == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete a department that has assigned employees.");
        }

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }

    private static string NormalizeDepartmentName(string? departmentName)
    {
        var normalized = departmentName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Department name is required.");
        }

        return normalized;
    }
}
