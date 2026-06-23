using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.DTOs.Reports;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Employee")]
[Route("api/v1/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> Attendance([FromBody] AttendanceReportRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var pdf = await _reportService.GenerateAttendanceReportAsync(request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return File(pdf, "application/pdf", "attendance-report.pdf");
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                success = false,
                message = "Crystal Reports integration is not configured yet.",
                details = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = "Report generation failed.",
                errors = new[] { ex.Message }
            });
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
