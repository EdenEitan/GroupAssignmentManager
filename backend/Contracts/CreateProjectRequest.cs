namespace backend.Contracts;

public class CreateProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }

    // The creator will be added as the first project member.
    public Guid CreatorId { get; set; }
}