using backend.Models;
using MongoDB.Driver;
namespace backend.Repositories;
public class UserRepository(IMongoDatabase database)
{
    private readonly IMongoCollection<User> users = database.GetCollection<User>("users");
    public Task<List<User>> All() => users.Find(_ => true).ToListAsync();
    public Task<User?> Get(Guid id) => users.Find(x => x.UserId == id).FirstOrDefaultAsync();
    public Task<User?> GetByEmail(string email) => users.Find(x => x.Email == email).FirstOrDefaultAsync();
    public Task Add(User user) => users.InsertOneAsync(user);
}
