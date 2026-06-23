namespace WMS.Application.DTOs.Projects;

public class ProjectDto
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public int? ClientId { get; set; }

    public string? ClientName { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int MembersCount { get; set; }
}

