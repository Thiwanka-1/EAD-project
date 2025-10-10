// =============================================
// File: EVOwnerRepository.cs
// Description: Provides data access methods for EV owners, including add, update, and fetch operations.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using MongoDB.Driver;

namespace EvCharge.Api.Repositories
{
    public class EvOwnerRepository
    {
        private readonly IMongoCollection<EvOwner> _owners;

        public EvOwnerRepository(IConfiguration config)
        {
            var client = new MongoClient(config["Mongo:ConnectionString"]);
            var database = client.GetDatabase(config["Mongo:DatabaseName"]);
            _owners = database.GetCollection<EvOwner>("EvOwners");
        }

        public async Task<List<EvOwner>> GetAllAsync() =>
            await _owners.Find(_ => true).ToListAsync();

        public async Task<EvOwner?> GetByNicAsync(string nic) =>
            await _owners.Find(o => o.NIC == nic).FirstOrDefaultAsync();

        public async Task CreateAsync(EvOwner owner) =>
            await _owners.InsertOneAsync(owner);

        public async Task UpdateAsync(string nic, EvOwner updated) =>
            await _owners.ReplaceOneAsync(o => o.NIC == nic, updated);

        public async Task DeleteAsync(string nic) =>
            await _owners.DeleteOneAsync(o => o.NIC == nic);
    }
}
