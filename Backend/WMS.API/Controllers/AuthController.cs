using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Auth;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Register), ApiResponse<RegisterResponseDto>.Ok(response, "User registered successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RegisterResponseDto>.Fail("Registration failed.", ex.Message));
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Login failed.", ex.Message));
        }
    }
}
