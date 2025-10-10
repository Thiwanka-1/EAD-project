// =============================================
// File: BookingRepository.cs
// Description: Provides data access methods for bookings, including add, update, and fetch operations.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using MongoDB.Driver;

namespace EvCharge.Api.Repositories
{
    public class BookingRepository
    {
        private readonly IMongoCollection<Booking> _bookings;

        public BookingRepository(IConfiguration config)
        {
            var client = new MongoClient(config["Mongo:ConnectionString"]);
            var db = client.GetDatabase(config["Mongo:DatabaseName"]);
            _bookings = db.GetCollection<Booking>("Bookings");
        }

        public Task<List<Booking>> GetAllAsync() =>
            _bookings.Find(_ => true).SortByDescending(b => b.CreatedUtc).ToListAsync();

        public Task<Booking?> GetByIdAsync(string id) =>
            _bookings.Find(b => b.Id == id).FirstOrDefaultAsync() as Task<Booking?>;

        public Task<List<Booking>> GetByOwnerAsync(string ownerNic) =>
            _bookings.Find(b => b.OwnerNic == ownerNic).SortByDescending(b => b.StartTimeUtc).ToListAsync();

        public Task<List<Booking>> GetByStationAsync(string stationId) =>
            _bookings.Find(b => b.StationId == stationId).SortByDescending(b => b.StartTimeUtc).ToListAsync();

        public Task CreateAsync(Booking b) => _bookings.InsertOneAsync(b);

        public Task UpdateAsync(Booking b) =>
            _bookings.ReplaceOneAsync(x => x.Id == b.Id, b);

        public Task DeleteAsync(string id) =>
            _bookings.DeleteOneAsync(b => b.Id == id);

        // ---- Overlap & Active checks ----

        public async Task<int> CountOverlappingAsync(string stationId, DateTime startUtc, DateTime endUtc, string? excludeBookingId = null)
        {
            var filter =
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId) &
                Builders<Booking>.Filter.In(b => b.Status, BookingStatus.ActiveStatuses) &
                Builders<Booking>.Filter.Lt(b => b.StartTimeUtc, endUtc) &
                Builders<Booking>.Filter.Gt(b => b.EndTimeUtc, startUtc);

            if (!string.IsNullOrEmpty(excludeBookingId))
                filter &= Builders<Booking>.Filter.Ne(b => b.Id, excludeBookingId);

            return (int)await _bookings.CountDocumentsAsync(filter);
        }

        public async Task<bool> HasActiveBookingsAsync(string stationId)
        {
            var filter =
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId) &
                Builders<Booking>.Filter.In(b => b.Status, BookingStatus.ActiveStatuses);
            return await _bookings.Find(filter).AnyAsync();
        }

        // ---- Views for Owner ----

        // upcoming = any not-ended booking in future (or now)
        public Task<List<Booking>> GetUpcomingByOwnerAsync(string ownerNic) =>
            _bookings.Find(b => b.OwnerNic == ownerNic && b.StartTimeUtc >= DateTime.UtcNow &&
                                (b.Status == BookingStatus.Pending ||
                                 b.Status == BookingStatus.Approved ||
                                 b.Status == BookingStatus.InProgress))
                     .SortBy(b => b.StartTimeUtc)
                     .ToListAsync();

        // history = completed or cancelled, or ended in the past
        public Task<List<Booking>> GetHistoryByOwnerAsync(string ownerNic) =>
            _bookings.Find(b => b.OwnerNic == ownerNic &&
                               (b.Status == BookingStatus.Completed ||
                                b.Status == BookingStatus.Cancelled ||
                                b.EndTimeUtc < DateTime.UtcNow))
                     .SortByDescending(b => b.StartTimeUtc)
                     .ToListAsync();
    }
}
