using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class UserLogin
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public DateTime? LastLogin { get; set; }

    public Role Role { get; set; } = null!;
}
