using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Leave
{
    [Key]
    public int LeaveId { get; set; }

    public int EmpId { get; set; }

    [Required]
    public LeaveType LeaveType { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public Employee Employee { get; set; } = null!;

    public Employee? Approver { get; set; }
}
