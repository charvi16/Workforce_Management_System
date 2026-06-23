using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Leaves;

public class ReviewLeaveRequestDto
{
    [Required]
    public bool IsApproved { get; set; }
}
