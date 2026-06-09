using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.DTOs.Auth;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Security;

namespace WMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly WmsDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(WmsDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        if (await _dbContext.UserLogins.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken);
        if (role is null)
        {
            throw new InvalidOperationException("Invalid role selected.");
        }

        var user = new UserLogin
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            RoleId = role.RoleId
        };

        _dbContext.UserLogins.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = role.RoleName
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var user = await _dbContext.UserLogins
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLogin = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(GetExpiryMinutes());
        var token = CreateJwtToken(user, expiresAtUtc);

        return new AuthResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role.RoleName,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private string CreateJwtToken(UserLogin user, DateTime expiresAtUtc)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpiryMinutes()
    {
        return int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60;
    }
}
