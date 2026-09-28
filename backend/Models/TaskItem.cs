using MongoDB.Bson.Serialization.Attributes;
namespace backend.Models;
public class TaskItem
{
    [BsonId] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? Deadline { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public bool NeedsHelp { get; set; }
    public List<Guid> AssignmentRequests { get; set; } = new();
    public int Version { get; set; } = 1;
}
