using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }
}
