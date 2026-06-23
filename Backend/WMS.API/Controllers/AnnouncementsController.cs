using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Announcements;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Employee")]
[Route("api/v1/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AnnouncementsController> _logger;

    public AnnouncementsController(IAnnouncementService announcementService, IAuditLogService auditLogService, ILogger<AnnouncementsController> logger)
    {
        _announcementService = announcementService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var announcements = await _announcementService.GetAllAsync(CurrentRole, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<AnnouncementDto>>.Ok(announcements, "Announcements retrieved successfully."));
    }

    [HttpGet("{announcementId:int}")]
    public async Task<IActionResult> GetById(int announcementId, CancellationToken cancellationToken)
    {
        try
        {
            var announcement = await _announcementService.GetByIdAsync(announcementId, CurrentRole, cancellationToken);
            return Ok(ApiResponse<AnnouncementDto>.Ok(announcement, "Announcement retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<AnnouncementDto>.Fail("Announcement lookup failed.", ex.Message));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var announcement = await _announcementService.CreateAsync(request, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Create", announcement, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { announcementId = announcement.AnnouncementId }, ApiResponse<AnnouncementDto>.Ok(announcement, "Announcement created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AnnouncementDto>.Fail("Announcement creation failed.", ex.Message));
        }
    }

    [HttpPut("{announcementId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int announcementId, AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var announcement = await _announcementService.UpdateAsync(announcementId, request, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Update", announcement, cancellationToken);
            return Ok(ApiResponse<AnnouncementDto>.Ok(announcement, "Announcement updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AnnouncementDto>.Fail("Announcement update failed.", ex.Message));
        }
    }

    [HttpDelete("{announcementId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int announcementId, CancellationToken cancellationToken)
    {
        try
        {
            var announcement = await _announcementService.DeactivateAsync(announcementId, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Deactivate", announcement, cancellationToken);
            return Ok(ApiResponse<AnnouncementDto>.Ok(announcement, "Announcement deactivated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AnnouncementDto>.Fail("Announcement deactivation failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    private string? CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;

    private async Task TryLogAuditAsync(string action, AnnouncementDto announcement, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.LogAsync(CurrentEmployeeId, CurrentUsername, action, "Announcement", announcement.AnnouncementId.ToString(), announcement.Title, ClientIp, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Announcement {Action} succeeded but audit logging failed for announcement {AnnouncementId}.", action, announcement.AnnouncementId);
        }
    }
}
