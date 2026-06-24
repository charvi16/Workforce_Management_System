using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Announcements;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly WmsDbContext _dbContext;

    public AnnouncementService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AnnouncementDto>> GetAllAsync(string currentUserRole, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Announcements.AsNoTracking();

        if (!IsAdminRole(currentUserRole))
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(a =>
                a.IsActive
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value.Date >= today)
                && (a.TargetRole == null || a.TargetRole == string.Empty || a.TargetRole.ToLower() == currentUserRole.ToLower()));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title,
                Message = a.Message,
                CreatedBy = a.CreatedBy,
                CreatedByName = (a.Creator.FirstName + " " + a.Creator.LastName).Trim(),
                CreatedOn = a.CreatedOn,
                UpdatedOn = a.UpdatedOn,
                IsActive = a.IsActive,
                TargetRole = a.TargetRole,
                ExpiryDate = a.ExpiryDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AnnouncementDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<AnnouncementDto> GetByIdAsync(int announcementId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = _dbContext.Announcements
            .AsNoTracking()
            .Where(a => a.AnnouncementId == announcementId);

        if (!IsAdminRole(currentUserRole))
        {
            query = query.Where(a =>
                a.IsActive
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value.Date >= today)
                && (a.TargetRole == null || a.TargetRole == string.Empty || a.TargetRole.ToLower() == currentUserRole.ToLower()));
        }

        var announcement = await query
            .Select(a => new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title,
                Message = a.Message,
                CreatedBy = a.CreatedBy,
                CreatedByName = (a.Creator.FirstName + " " + a.Creator.LastName).Trim(),
                CreatedOn = a.CreatedOn,
                UpdatedOn = a.UpdatedOn,
                IsActive = a.IsActive,
                TargetRole = a.TargetRole,
                ExpiryDate = a.ExpiryDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        return announcement ?? throw new InvalidOperationException("Announcement not found.");
    }

    public async Task<AnnouncementDto> CreateAsync(AnnouncementRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureEmployeeExistsAsync(currentEmployeeId, cancellationToken);

        var announcement = new Announcement
        {
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            CreatedBy = currentEmployeeId,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = null,
            IsActive = request.IsActive,
            TargetRole = NormalizeTargetRole(request.TargetRole),
            ExpiryDate = request.ExpiryDate
        };

        _dbContext.Announcements.Add(announcement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(announcement.AnnouncementId, nameof(UserRole.Admin), cancellationToken);
    }

    public async Task<AnnouncementDto> UpdateAsync(int announcementId, AnnouncementRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(a => a.AnnouncementId == announcementId, cancellationToken)
            ?? throw new InvalidOperationException("Announcement not found.");

        announcement.Title = request.Title.Trim();
        announcement.Message = request.Message.Trim();
        announcement.TargetRole = NormalizeTargetRole(request.TargetRole);
        announcement.ExpiryDate = request.ExpiryDate;
        announcement.IsActive = request.IsActive;
        announcement.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(announcement);
    }

    public async Task<AnnouncementDto> DeactivateAsync(int announcementId, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var announcement = await _dbContext.Announcements.FirstOrDefaultAsync(a => a.AnnouncementId == announcementId, cancellationToken)
            ?? throw new InvalidOperationException("Announcement not found.");

        announcement.IsActive = false;
        announcement.UpdatedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(announcement);
    }

    private async Task EnsureEmployeeExistsAsync(int employeeId, CancellationToken cancellationToken)
    {
        if (employeeId <= 0 || !await _dbContext.Employees.AnyAsync(e => e.EmployeeId == employeeId, cancellationToken))
        {
            throw new InvalidOperationException("No employee profile is linked to this login.");
        }
    }

    private static void ValidateRequest(AnnouncementRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Announcement title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Announcement message is required.");
        }
    }

    private static string? NormalizeTargetRole(string? targetRole)
    {
        if (string.IsNullOrWhiteSpace(targetRole))
        {
            return null;
        }

        var normalized = targetRole.Trim();
        if (!Enum.TryParse<UserRole>(normalized, true, out var role))
        {
            throw new InvalidOperationException("Invalid announcement target role.");
        }

        return role.ToString();
    }

    private static bool IsAdminRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);

    private static AnnouncementDto MapToDto(Announcement announcement) =>
        new()
        {
            AnnouncementId = announcement.AnnouncementId,
            Title = announcement.Title,
            Message = announcement.Message,
            CreatedBy = announcement.CreatedBy,
            CreatedOn = announcement.CreatedOn,
            UpdatedOn = announcement.UpdatedOn,
            IsActive = announcement.IsActive,
            TargetRole = announcement.TargetRole,
            ExpiryDate = announcement.ExpiryDate
        };
}
