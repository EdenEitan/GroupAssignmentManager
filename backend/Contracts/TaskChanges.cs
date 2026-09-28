namespace backend.Contracts;
// Every mutation carries the version the browser last saw.
public record UpdateTaskRequest(int Version, string Title, string? Description, DateTime? Deadline, string Priority);
public record AssignTaskRequest(int Version, Guid UserId);
public record RequestTaskAssignmentRequest(int Version, Guid UserId);
public record UpdateTaskStatusRequest(int Version, string Status);
public record UpdateTaskHelpRequest(int Version, bool NeedsHelp);
public record TaskVersionRequest(int Version);
