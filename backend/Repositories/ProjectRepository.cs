using backend.Models;
using MongoDB.Driver;
namespace backend.Repositories;
public class ProjectRepository(IMongoDatabase database)
{
    private readonly IMongoCollection<Project> projects = database.GetCollection<Project>("projects");
    public Task<List<Project>> All(Guid? memberId) => projects.Find(
        memberId is null ? Builders<Project>.Filter.Empty : Builders<Project>.Filter.AnyEq(x => x.MemberIds, memberId.Value)).ToListAsync();
    public Task<Project?> Get(Guid id) => projects.Find(x => x.ProjectId == id).FirstOrDefaultAsync();
    public Task Add(Project project) => projects.InsertOneAsync(project);
    public Task<Project?> AddMember(Guid id, Guid memberId) => projects.FindOneAndUpdateAsync(
        x => x.ProjectId == id && !x.MemberIds.Contains(memberId),
        Builders<Project>.Update.AddToSet(x => x.MemberIds, memberId),
        new FindOneAndUpdateOptions<Project> { ReturnDocument = ReturnDocument.After });
    public Task<Project?> RemoveMember(Guid id, Guid memberId) => projects.FindOneAndUpdateAsync(
        x => x.ProjectId == id && x.MemberIds.Contains(memberId),
        Builders<Project>.Update.Pull(x => x.MemberIds, memberId),
        new FindOneAndUpdateOptions<Project> { ReturnDocument = ReturnDocument.After });
    public Task<Project?> Update(Guid id, string? description, DateTime? deadline) => projects.FindOneAndUpdateAsync(
        x => x.ProjectId == id,
        Builders<Project>.Update.Set(x => x.Description, description).Set(x => x.Deadline, deadline),
        new FindOneAndUpdateOptions<Project> { ReturnDocument = ReturnDocument.After });
}
