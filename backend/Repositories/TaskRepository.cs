using backend.Models;
using MongoDB.Driver;
namespace backend.Repositories;
public class TaskRepository(IMongoDatabase database)
{
    private readonly IMongoCollection<TaskItem> tasks = database.GetCollection<TaskItem>("tasks");
    public Task<List<TaskItem>> All(Guid projectId) => tasks.Find(x => x.ProjectId == projectId).ToListAsync();
    public Task<TaskItem?> Get(Guid id) => tasks.Find(x => x.Id == id).FirstOrDefaultAsync();
    public Task Add(TaskItem task) => tasks.InsertOneAsync(task);
    // Filter and update run atomically inside MongoDB. Only one writer can win a version.
    public Task<TaskItem?> Change(Guid id, int version, FilterDefinition<TaskItem> condition, UpdateDefinition<TaskItem> changes)
    {
        var f = Builders<TaskItem>.Filter;
        var filter = f.Eq(x => x.Id, id) & f.Eq(x => x.Version, version) & condition;
        return tasks.FindOneAndUpdateAsync(filter,
            Builders<TaskItem>.Update.Combine(changes, Builders<TaskItem>.Update.Inc(x => x.Version, 1)),
            new FindOneAndUpdateOptions<TaskItem> { ReturnDocument = ReturnDocument.After });
    }
    public Task<DeleteResult> Delete(Guid id, int version) => tasks.DeleteOneAsync(x => x.Id == id && x.Version == version);
    public Task<bool> HasMemberWork(Guid projectId, Guid userId) => tasks.Find(
        x => x.ProjectId == projectId && (x.AssigneeId == userId || x.AssignmentRequests.Contains(userId)))
        .AnyAsync();
}
