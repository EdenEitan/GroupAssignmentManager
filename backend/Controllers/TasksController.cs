using backend.Models;
using backend.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    // Temporary storage. Will be replaced with a database.
    private static readonly List<TaskItem> Tasks = new();

    // GET /api/tasks
    // Optional filters: projectId, assigneeId, unassignedOnly.
    [HttpGet]
    public IActionResult GetTasks(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] bool unassignedOnly = false)
    {
        if (unassignedOnly && assigneeId.HasValue)
        {
            return BadRequest(
                "Choose either an assignee or unassigned tasks.");
        }

        IEnumerable<TaskItem> result = Tasks;

        if (projectId.HasValue)
        {
            result = result.Where(
                task => task.ProjectId == projectId.Value);
        }

        if (assigneeId.HasValue)
        {
            result = result.Where(
                task => task.AssigneeId == assigneeId.Value);
        }

        if (unassignedOnly)
        {
            result = result.Where(
                task => task.AssigneeId == null);
        }

        return Ok(result.ToList());
    }

    // GET /api/tasks/{id}
    [HttpGet("{id}")]
    public IActionResult GetTaskById([FromRoute] Guid id)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        return Ok(task);
    }

    // POST /api/tasks
    [HttpPost]
    public IActionResult CreateTask(
        [FromBody] CreateTaskRequest request)
    {
        if (request.ProjectId == Guid.Empty)
        {
            return BadRequest("Project ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        if (!IsValidPriority(request.Priority))
        {
            return BadRequest(
                "Priority must be Low, Medium, or High.");
        }

        // TODO: Check that the project exists.

        TaskItem task = new TaskItem(
            projectId: request.ProjectId,
            title: request.Title.Trim(),
            priority: request.Priority,
            description: request.Description,
            deadline: request.Deadline);

        Tasks.Add(task);

        return CreatedAtAction(
            nameof(GetTaskById),
            new { id = task.Id },
            task);
    }

    // PUT /api/tasks/{id}
    // Replace the editable task details.
    [HttpPut("{id}")]
    public IActionResult UpdateTask(
        [FromRoute] Guid id,
        [FromBody] UpdateTaskRequest request)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Title is required.");
        }

        if (!IsValidPriority(request.Priority))
        {
            return BadRequest(
                "Priority must be Low, Medium, or High.");
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Deadline = request.Deadline;
        task.Priority = request.Priority;

        return Ok(task);
    }

    // POST /api/tasks/{id}/request-assignment
    // Ask to take an unassigned task.
    [HttpPost("{id}/request-assignment")]
    public IActionResult RequestAssignment(
        [FromRoute] Guid id,
        [FromBody] RequestTaskAssignmentRequest request)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (request.UserId == Guid.Empty)
        {
            return BadRequest("User ID is required.");
        }

        // TODO: Check that the user belongs to the project.

        if (task.AssigneeId != null)
        {
            return Conflict("Task is already assigned.");
        }

        if (task.Status == "Completed")
        {
            return Conflict("Task is already completed.");
        }

        if (task.AssignmentRequests.Contains(request.UserId))
        {
            return Conflict("User already requested this task.");
        }

        task.AssignmentRequests.Add(request.UserId);

        return Ok(task);
    }

    // PUT /api/tasks/{id}/assign
    // Assign a task or transfer it to another user.
    [HttpPut("{id}/assign")]
    public IActionResult AssignTask(
        [FromRoute] Guid id,
        [FromBody] AssignTaskRequest request)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (request.UserId == Guid.Empty)
        {
            return BadRequest("User ID is required.");
        }

        // TODO: Check that the user belongs to the project.

        if (task.Status == "Completed")
        {
            return Conflict("Reopen the task before assigning it.");
        }

        // An unassigned task requires a previous assignment request.
        if (task.AssigneeId == null &&
            !task.AssignmentRequests.Contains(request.UserId))
        {
            return BadRequest("This user did not request the task.");
        }

        task.AssigneeId = request.UserId;
        task.Status = "In Progress";
        task.AssignmentRequests.Clear();

        return Ok(task);
    }

    // PUT /api/tasks/{id}/status
    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateTaskStatusRequest request)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (request.Status != "Open" &&
            request.Status != "In Progress" &&
            request.Status != "Completed")
        {
            return BadRequest(
                "Status must be Open, In Progress, or Completed.");
        }

        task.Status = request.Status;

        return Ok(task);
    }

    // PUT /api/tasks/{id}/help
    [HttpPut("{id}/help")]
    public IActionResult UpdateHelpStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateTaskHelpRequest request)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (!request.NeedsHelp.HasValue)
        {
            return BadRequest("NeedsHelp is required.");
        }

        task.NeedsHelp = request.NeedsHelp.Value;

        return Ok(task);
    }

    // DELETE /api/tasks/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteTask([FromRoute] Guid id)
    {
        TaskItem? task = Tasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        Tasks.Remove(task);

        // The deletion succeeded. No response body is needed.
        return NoContent();
    }

    // A private helper, not an API endpoint.
    private static bool IsValidPriority(string? priority)
    {
        return priority == "Low" ||
               priority == "Medium" ||
               priority == "High";
    }
}