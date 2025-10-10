// =============================================
// File: StationRepository.cs
// Description: Provides data access methods for charging stations, including add, update, and fetch operations.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using MongoDB.Driver;

namespace EvCharge.Api.Repositories
{
    public class StationRepository
    {
        private readonly IMongoCollection<Station> _stations;

        public StationRepository(IConfiguration config)
        {
            var client = new MongoClient(config["Mongo:ConnectionString"]);
            var db = client.GetDatabase(config["Mongo:DatabaseName"]);
            _stations = db.GetCollection<Station>("Stations");
        }

        public async Task<List<Station>> GetAllAsync() =>
            await _stations.Find(_ => true).ToListAsync();

        public async Task<Station?> GetByIdAsync(string stationId) =>
            await _stations.Find(s => s.StationId == stationId).FirstOrDefaultAsync();

        public async Task<bool> ExistsAsync(string stationId) =>
            await _stations.Find(s => s.StationId == stationId).AnyAsync();

        public async Task CreateAsync(Station s) =>
            await _stations.InsertOneAsync(s);

        public async Task UpdateAsync(string stationId, Station s) =>
            await _stations.ReplaceOneAsync(x => x.StationId == stationId, s);

        public async Task DeleteAsync(string stationId) =>
            await _stations.DeleteOneAsync(s => s.StationId == stationId);

        public async Task SetActiveAsync(string stationId, bool isActive) =>
            await _stations.UpdateOneAsync(s => s.StationId == stationId,
                Builders<Station>.Update.Set(x => x.IsActive, isActive));

        public async Task SetAvailableSlotsAsync(string stationId, int slots) =>
            await _stations.UpdateOneAsync(s => s.StationId == stationId,
                Builders<Station>.Update.Set(x => x.AvailableSlots, slots));

        public async Task AddOperatorAsync(string stationId, string userId)
        {
            var update = Builders<Station>.Update.AddToSet(s => s.OperatorUserIds, userId);
            await _stations.UpdateOneAsync(s => s.StationId == stationId, update);
        }

        public async Task RemoveOperatorAsync(string stationId, string userId)
        {
            var update = Builders<Station>.Update.Pull(s => s.OperatorUserIds, userId);
            await _stations.UpdateOneAsync(s => s.StationId == stationId, update);
        }
    }
}
