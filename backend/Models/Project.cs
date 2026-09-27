namespace backend.Models;

public class Project
{
    // set when the user is created. Cannot be changed later.
    // next i will set uniqunes check with the DB. 
    public Guid ProjectId { get; }

    // Name changes are not supported for now.
    public string ProjectName { get; }

    public string CourseName { get;  }

    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }

    // IDs of the users who belong to this project.
    public List<Guid> MemberIds { get; } = new();

    public Project(string projectName, string courseName , string? description = null,
        DateTime? deadline = null)
    {
        ProjectId  = Guid.NewGuid();
        ProjectName = projectName ; 
        CourseName = courseName;
        Description = description;
        Deadline = deadline;

    }
}