using backend.Models;
using backend.Repositories;
using MongoDB.Driver;
namespace backend.Services;
public record ChangeResult(TaskItem? Task, string? Error = null, int Code = 200);
public class TaskService(TaskRepository tasks, ProjectRepository projects)
{
    private static readonly FilterDefinition<TaskItem> Any = Builders<TaskItem>.Filter.Empty;
    private static readonly UpdateDefinitionBuilder<TaskItem> U = Builders<TaskItem>.Update;
    private static readonly FilterDefinitionBuilder<TaskItem> F = Builders<TaskItem>.Filter;
    public async Task<ChangeResult> Change(Guid id, int version, FilterDefinition<TaskItem> condition,
        UpdateDefinition<TaskItem> update, Func<TaskItem, string?>? rule = null)
    {
        if (version < 1) return new(null, "A positive version is required.", 400);
        var current = await tasks.Get(id);
        if (current is null) return new(null, "Task not found.", 404);
        if (current.Version != version) return new(null, "Task changed. Refresh it and try again.", 409);
        var problem = rule?.Invoke(current);
        if (problem is not null) return new(null, problem, 400);
        var saved = await tasks.Change(id, version, condition, update);
        return saved is null ? new(null, "Task changed. Refresh it and try again.", 409) : new(saved);
    }
    public Task<ChangeResult> Details(Guid id, int version, string title, string? description, DateTime? deadline, string priority) =>
        Change(id, version, Any,
            U.Set(x => x.Title, title?.Trim() ?? "").Set(x => x.Description, description)
             .Set(x => x.Deadline, deadline).Set(x => x.Priority, priority),
            _ => string.IsNullOrWhiteSpace(title) || !ValidPriority(priority) ? "Provide a title and Low, Medium, or High priority." : null);
    public async Task<ChangeResult> Request(Guid id, int version, Guid userId)
    {
        var task = await tasks.Get(id);
        if (task is null) return new(null, "Task not found.", 404);
        var project = await projects.Get(task.ProjectId);
        if (userId == Guid.Empty || project is null || !project.MemberIds.Contains(userId))
            return new(null, "The requester must belong to this project.", 400);
        return await Change(id, version, F.Eq(x => x.AssigneeId, null) & F.Ne(x => x.Status, "Completed")
            & F.Not(F.AnyEq(x => x.AssignmentRequests, userId)),
            U.AddToSet(x => x.AssignmentRequests, userId),
            x => x.AssigneeId is not null || x.Status == "Completed" || x.AssignmentRequests.Contains(userId)
                ? "The task is assigned, completed, or already requested by this member." : null);
    }
    public async Task<ChangeResult> Assign(Guid id, int version, Guid userId)
    {
        var task = await tasks.Get(id);
        if (task is null) return new(null, "Task not found.", 404);
        var project = await projects.Get(task.ProjectId);
        if (userId == Guid.Empty || project is null || !project.MemberIds.Contains(userId))
            return new(null, "Assignee must belong to this project.", 400);
        // A free task requires a request; an assigned task can be transferred to any member.
        var allowed = F.Ne(x => x.Status, "Completed") &
            (F.Ne(x => x.AssigneeId, null) | F.AnyEq(x => x.AssignmentRequests, userId));
        return await Change(id, version, allowed,
            U.Set(x => x.AssigneeId, userId).Set(x => x.Status, "In Progress")
             .Set(x => x.AssignmentRequests, new List<Guid>()),
            x => x.Status == "Completed" || (x.AssigneeId is null && !x.AssignmentRequests.Contains(userId))
                ? "Reopen the task or ask the member to request this free task first." : null);
    }
    public Task<ChangeResult> Status(Guid id, int version, string status) => Change(id, version,
        status == "In Progress" ? F.Ne(x => x.AssigneeId, null) : Any,
        U.Set(x => x.Status, status),
        x => status is not ("Open" or "In Progress" or "Completed") ||
            (status == "In Progress" && x.AssigneeId is null)
            ? "Choose a valid status; In Progress requires an assignee." : null);
    public Task<ChangeResult> Help(Guid id, int version, bool needsHelp) =>
        Change(id, version, Any, U.Set(x => x.NeedsHelp, needsHelp));
    public Task<ChangeResult> Unassign(Guid id, int version) =>
        Change(id, version, Any, U.Set(x => x.AssigneeId, null).Set(x => x.Status, "Open")
            .Set(x => x.AssignmentRequests, new List<Guid>()));
    public static bool ValidPriority(string? priority) => priority is "Low" or "Medium" or "High";
}
