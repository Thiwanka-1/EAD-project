// =============================================
// File: UserRepository.cs
// Description: Provides data access methods for users, including add, update, and fetch operations.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using MongoDB.Driver;

namespace EvCharge.Api.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(IConfiguration config)
        {
            var client = new MongoClient(config["Mongo:ConnectionString"]);
            var database = client.GetDatabase(config["Mongo:DatabaseName"]);
            _users = database.GetCollection<User>("Users");
        }

        public async Task<List<User>> GetAllAsync() =>
            await _users.Find(_ => true).ToListAsync();

        public async Task<User?> GetByIdAsync(string id) =>
            await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetByUsernameAsync(string username) =>
            await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

        public async Task CreateAsync(User user) =>
            await _users.InsertOneAsync(user);

        public async Task UpdateAsync(string id, User updated) =>
            await _users.ReplaceOneAsync(u => u.Id == id, updated);

        public async Task DeleteAsync(string id) =>
            await _users.DeleteOneAsync(u => u.Id == id);
    }
}
