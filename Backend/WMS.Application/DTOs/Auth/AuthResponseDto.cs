namespace WMS.Application.DTOs.Auth;

public class AuthResponseDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
