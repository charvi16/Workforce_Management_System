using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Infrastructure.Data;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly WmsDbContext _dbContext;

    public HealthController(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            success = true,
            message = "WMS API is running",
            data = new { service = "WMS.API", status = "Healthy" },
            errors = Array.Empty<string>()
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> Database()
    {
        var canConnect = await _dbContext.Database.CanConnectAsync();

        return Ok(new
        {
            success = canConnect,
            message = canConnect ? "SQL Server connection is healthy" : "SQL Server connection failed",
            data = new { canConnect },
            errors = Array.Empty<string>()
        });
    }
}
