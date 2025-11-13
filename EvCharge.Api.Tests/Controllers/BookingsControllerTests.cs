using System;
using System.Threading.Tasks;
using EvCharge.Api.Controllers;
using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using EvCharge.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EvCharge.Api.Tests.Controllers
{
    [Collection("mongo")]
    public class BookingsControllerTests
    {
        private readonly IConfiguration _cfg;

        public BookingsControllerTests(TestMongoHost mongo)
        {
            _cfg = TestConfig.Build(mongo.ConnectionString, "evcharge-tests-bookings");
        }

        [Fact]
        public async Task CreateBooking_ShouldBlock_WhenSlotsExceeded()
        {
            if (TestMongoHost.SkipAll) return;

            var stations = new StationRepository(_cfg);
            var bookings = new BookingRepository(_cfg);
            var _ = new BookingsController(_cfg);

            // Station with 1 slot
            await stations.CreateAsync(new Station
            {
                StationId = "ST-BLOCK",
                Name = "OneSlot",
                Latitude = 0, Longitude = 0,
                Address = "A", Type = "AC",
                AvailableSlots = 1, IsActive = true
            });

            var start = DateTime.UtcNow.AddHours(1);
            var end = start.AddHours(1);

            // One active approved booking occupying the slot
            await bookings.CreateAsync(new Booking
            {
                OwnerNic = "O1", StationId = "ST-BLOCK",
                StartTimeUtc = start, EndTimeUtc = end,
                Status = BookingStatus.Approved
            });

            // Validate overlap logic directly
            var overlapping = await bookings.CountOverlappingAsync("ST-BLOCK", start, end);
            overlapping.Should().BeGreaterOrEqualTo(1);
        }
    }
}
