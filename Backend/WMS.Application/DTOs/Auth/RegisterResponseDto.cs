namespace WMS.Application.DTOs.Auth;

public class RegisterResponseDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
