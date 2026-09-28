using backend.Contracts;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
namespace backend.Controllers;
[ApiController, Route("api/tasks")]
public class TasksController(TaskRepository tasks, ProjectRepository projects, TaskService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All([FromQuery] Guid projectId, [FromQuery] Guid? assigneeId,
        [FromQuery] bool unassignedOnly = false, [FromQuery] bool needsHelp = false, [FromQuery] string? status = null)
    {
        if (projectId == Guid.Empty) return BadRequest("projectId is required.");
        if (unassignedOnly && assigneeId is not null) return BadRequest("Choose one assignee filter.");
        var items = await tasks.All(projectId);
        return Ok(items.Where(x => (assigneeId is null || x.AssigneeId == assigneeId)
            && (!unassignedOnly || x.AssigneeId is null) && (!needsHelp || x.NeedsHelp)
            && (status is null || x.Status == status)));
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) =>
        await tasks.Get(id) is { } task ? Ok(task) : NotFound("Task not found.");
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        if (request.ProjectId == Guid.Empty || await projects.Get(request.ProjectId) is null)
            return BadRequest("Project does not exist.");
        if (string.IsNullOrWhiteSpace(request.Title) || !TaskService.ValidPriority(request.Priority))
            return BadRequest("Provide a title and Low, Medium, or High priority.");
        var task = new TaskItem { ProjectId = request.ProjectId, Title = request.Title.Trim(),
            Description = request.Description, Deadline = request.Deadline, Priority = request.Priority };
        await tasks.Add(task);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }
    private IActionResult Respond(ChangeResult result) => result.Code switch
    {
        200 => Ok(result.Task),
        404 => NotFound(result.Error),
        409 => Conflict(result.Error),
        _ => BadRequest(result.Error)
    };
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request) =>
        Respond(await service.Details(id, request.Version, request.Title, request.Description, request.Deadline, request.Priority));
    [HttpPost("{id:guid}/request-assignment")]
    public async Task<IActionResult> RequestAssignment(Guid id, RequestTaskAssignmentRequest request) =>
        Respond(await service.Request(id, request.Version, request.UserId));
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignTaskRequest request) =>
        Respond(await service.Assign(id, request.Version, request.UserId));
    [HttpPut("{id:guid}/unassign")]
    public async Task<IActionResult> Unassign(Guid id, TaskVersionRequest request) =>
        Respond(await service.Unassign(id, request.Version));
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, UpdateTaskStatusRequest request) =>
        Respond(await service.Status(id, request.Version, request.Status));
    [HttpPut("{id:guid}/help")]
    public async Task<IActionResult> Help(Guid id, UpdateTaskHelpRequest request) =>
        Respond(await service.Help(id, request.Version, request.NeedsHelp));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int version)
    {
        if (version < 1) return BadRequest("A positive version is required.");
        if ((await tasks.Delete(id, version)).DeletedCount > 0) return NoContent();
        return await tasks.Get(id) is null ? NotFound("Task not found.") : Conflict("Task changed. Refresh it and try again.");
    }
}
