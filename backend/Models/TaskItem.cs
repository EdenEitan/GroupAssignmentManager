namespace backend.Models;

public class TaskItem
{
    // generated when the task is created. Cannot be changed later.
    // uniqueness will also be enforced by the database.
    public Guid Id { get; }

    // The project this task belongs to. Cannot be changed later.
    public Guid ProjectId { get; }

    public string Title { get; set; }



    // optional task details....
    public string? Description { get; set; }

    // the assigned user ID. Null = unassigned.
    public Guid? AssigneeId { get; set; }

    
    public DateTime? Deadline { get; set; }

    public string Status { get; set; } = "Open";

    public string Priority { get; set; }

    public bool NeedsHelp { get; set; } = false;

    // IDs of users who requested this task.
    // the list starts empty.
    public List<Guid> AssignmentRequests { get; } = new();

    // used to detect conflicting updates.
    // the server will increase it after each successful update.
    public int Version { get; set; } = 1;

    // input validation will be done on the server
    // when handling the request, before creating the task.
    public TaskItem(
        Guid projectId,
        string title,
        string priority = "Medium",
        string? description = null,
        DateTime? deadline = null)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = title;
        Priority = priority;
        Description = description;
        Deadline = deadline;
    }
}