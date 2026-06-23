using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Clients;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ClientsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ClientsController(IClientService clientService, IAuditLogService auditLogService, ILogger<ClientsController> logger, IWebHostEnvironment environment)
    {
        _clientService = clientService;
        _auditLogService = auditLogService;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var clients = await _clientService.GetAllAsync(CurrentRole, CurrentEmployeeId, search, status, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ClientDto>>.Ok(clients, "Clients retrieved successfully."));
    }

    [HttpGet("{clientId:int}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public async Task<IActionResult> GetById(int clientId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.GetByIdAsync(clientId, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ClientDto>.Ok(client, "Client retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<ClientDto>.Fail("Client lookup failed.", ex.Message));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ClientRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.CreateAsync(request, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Create", client, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { clientId = client.ClientId }, ApiResponse<ClientDto>.Ok(client, "Client created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClientDto>.Fail("Client creation failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create client. Inner exception: {InnerException}", ex.InnerException?.Message);
            var details = _environment.IsDevelopment() ? ex.InnerException?.Message ?? ex.Message : "A database error occurred while creating the client.";
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<ClientDto>.Fail("Failed to create client", details));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating client.");
            var details = _environment.IsDevelopment() ? ex.Message : "An unexpected error occurred while creating the client.";
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<ClientDto>.Fail("Failed to create client", details));
        }
    }

    [HttpPut("{clientId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int clientId, ClientRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.UpdateAsync(clientId, request, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Update", client, cancellationToken);
            return Ok(ApiResponse<ClientDto>.Ok(client, "Client updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClientDto>.Fail("Client update failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update client {ClientId}. Inner exception: {InnerException}", clientId, ex.InnerException?.Message);
            var details = _environment.IsDevelopment() ? ex.InnerException?.Message ?? ex.Message : "A database error occurred while updating the client.";
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<ClientDto>.Fail("Failed to update client", details));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating client {ClientId}.", clientId);
            var details = _environment.IsDevelopment() ? ex.Message : "An unexpected error occurred while updating the client.";
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<ClientDto>.Fail("Failed to update client", details));
        }
    }

    [HttpDelete("{clientId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int clientId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientService.DeactivateAsync(clientId, CurrentEmployeeId, cancellationToken);
            await TryLogAuditAsync("Deactivate", client, cancellationToken);
            return Ok(ApiResponse<ClientDto>.Ok(client, "Client deactivated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ClientDto>.Fail("Client deactivation failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    private string? CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;

    private async Task TryLogAuditAsync(string action, ClientDto client, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.LogAsync(CurrentEmployeeId, CurrentUsername, action, "Client", client.ClientId.ToString(), client.ClientName, ClientIp, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client {Action} succeeded but audit logging failed for client {ClientId}.", action, client.ClientId);
        }
    }
}
