using backend.Models;
using backend.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    // Temporary storage. Will be replaced with a database.
    private static readonly List<Project> Projects = new();

    // GET /api/projects
    // Optional filter: projects that include a specific user.
    [HttpGet]
    public IActionResult GetProjects([FromQuery] Guid? memberId)
    {
        IEnumerable<Project> result = Projects;

        if (memberId.HasValue)
        {
            result = result.Where(
                project => project.MemberIds.Contains(memberId.Value));
        }

        return Ok(result.ToList());
    }

    // GET /api/projects/{id}
    [HttpGet("{id}")]
    public IActionResult GetProjectById([FromRoute] Guid id)
    {
        Project? project = Projects.FirstOrDefault(
            p => p.ProjectId == id);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        return Ok(project);
    }

    // POST /api/projects
    [HttpPost]
    public IActionResult CreateProject(
        [FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName) ||
            string.IsNullOrWhiteSpace(request.CourseName))
        {
            return BadRequest(
                "Project name and course name are required.");
        }

        if (request.CreatorId == Guid.Empty)
        {
            return BadRequest("Creator ID is required.");
        }

        // TODO: Check that the creator exists.

        Project project = new Project(
            projectName: request.ProjectName.Trim(),
            courseName: request.CourseName.Trim(),
            description: request.Description,
            deadline: request.Deadline);

        // Add the creator as the first member.
        project.MemberIds.Add(request.CreatorId);

        Projects.Add(project);

        return CreatedAtAction(
            nameof(GetProjectById),
            new { id = project.ProjectId },
            project);
    }

    // PUT /api/projects/{id}/details
    // Replace the editable project details.
    [HttpPut("{id}/details")]
    public IActionResult UpdateProject(
        [FromRoute] Guid id,
        [FromBody] UpdateProjectRequest request)
    {
        Project? project = Projects.FirstOrDefault(
            p => p.ProjectId == id);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        project.Description = request.Description;
        project.Deadline = request.Deadline;

        return Ok(project);
    }

    // POST /api/projects/{id}/members
    [HttpPost("{id}/members")]
    public IActionResult AddMember(
        [FromRoute] Guid id,
        [FromBody] AddProjectMemberRequest request)
    {
        Project? project = Projects.FirstOrDefault(
            p => p.ProjectId == id);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        if (request.UserId == Guid.Empty)
        {
            return BadRequest("User ID is required.");
        }

        // TODO: Check that the user exists.

        if (project.MemberIds.Contains(request.UserId))
        {
            return Conflict("User is already a project member.");
        }

        project.MemberIds.Add(request.UserId);

        return Ok(project);
    }

    // DELETE /api/projects/{id}/members/{userId}
    [HttpDelete("{id}/members/{userId}")]
    public IActionResult RemoveMember(
        [FromRoute] Guid id,
        [FromRoute] Guid userId)
    {
        Project? project = Projects.FirstOrDefault(
            p => p.ProjectId == id);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        if (!project.MemberIds.Contains(userId))
        {
            return NotFound("User is not a project member.");
        }

        // TODO: Handle assigned tasks and assignment requests
        // before allowing a member to leave the project.

        project.MemberIds.Remove(userId);

        return NoContent();
    }
}