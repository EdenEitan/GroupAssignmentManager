using backend.Contracts;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace backend.Controllers;
[ApiController, Route("api/projects")]
public class ProjectsController(ProjectRepository projects, UserRepository users, TaskRepository tasks) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> All([FromQuery] Guid? memberId) => Ok(await projects.All(memberId));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) =>
        await projects.Get(id) is { } project ? Ok(project) : NotFound("Project not found.");
    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName) || string.IsNullOrWhiteSpace(request.CourseName))
            return BadRequest("Project and course names are required.");
        if (await users.Get(request.CreatorId) is null) return BadRequest("Creator does not exist.");
        var project = new Project { ProjectName = request.ProjectName.Trim(),
            CourseName = request.CourseName.Trim(), Description = request.Description,
            Deadline = request.Deadline, MemberIds = new() { request.CreatorId } };
        await projects.Add(project);
        return CreatedAtAction(nameof(Get), new { id = project.ProjectId }, project);
    }
    [HttpPut("{id:guid}/details")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectRequest request) =>
        await projects.Update(id, request.Description, request.Deadline) is { } project
            ? Ok(project) : NotFound("Project not found.");
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, AddProjectMemberRequest request)
    {
        if (await projects.Get(id) is null) return NotFound("Project not found.");
        if (await users.Get(request.UserId) is null) return BadRequest("User does not exist.");
        var project = await projects.AddMember(id, request.UserId);
        return project is null ? Conflict("Already a member.") : Ok(project);
    }
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        if (await projects.Get(id) is null) return NotFound("Project not found.");
        if (await tasks.HasMemberWork(id, userId)) return Conflict("Reassign tasks and clear requests first.");
        return await projects.RemoveMember(id, userId) is null ? NotFound("Member not found.") : NoContent();
    }
}
