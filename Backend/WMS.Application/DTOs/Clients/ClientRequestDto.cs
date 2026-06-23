using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Clients;

public class ClientRequestDto
{
    [Required, MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    public string? ClientAddress { get; set; }

    [MaxLength(15)]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Client phone number can contain digits, +, spaces, hyphens, and parentheses only.")]
    public string? ClientPhoneNumber { get; set; }

    [MaxLength(100)]
    public string? ClientLocation { get; set; }

    public bool Status { get; set; } = true;
}
