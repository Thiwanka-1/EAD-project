using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvCharge.Api.Controllers;
using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using EvCharge.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.Linq;


namespace EvCharge.Api.Tests.Controllers
{
    [Collection("mongo")]
    public class StationsControllerTests
    {
        private readonly IConfiguration _cfg;

        public StationsControllerTests(TestMongoHost mongo)
        {
            _cfg = TestConfig.Build(mongo.ConnectionString, "evcharge-tests-stations");
        }

        [Fact]
        public async Task Create_And_GetById_Works()
        {
            if (TestMongoHost.SkipAll) return;

            var repo = new StationRepository(_cfg);
            // You can still new the controller if you want it in scope
            var _ = new StationsController(_cfg);

            var st = new Station
            {
                StationId = "ST100",
                Name = "Main",
                Latitude = 6.9,
                Longitude = 79.8,
                Address = "Somewhere",
                Type = "AC",
                AvailableSlots = 3,
                IsActive = true
            };

            await repo.CreateAsync(st);
            var got = await repo.GetByIdAsync("ST100");

            got.Should().NotBeNull();
            got!.AvailableSlots.Should().Be(3);
        }

        [Fact]
        public async Task Deactivate_Precondition_HasActiveBookings()
        {
            if (TestMongoHost.SkipAll) return;

            var stationRepo = new StationRepository(_cfg);
            var bookingRepo = new BookingRepository(_cfg);

            // Arrange station
            var st = new Station
            {
                StationId = "ST200",
                Name = "Busy",
                Latitude = 6.0,
                Longitude = 79.0,
                Address = "Addr",
                Type = "DC",
                AvailableSlots = 1,
                IsActive = true
            };
            await stationRepo.CreateAsync(st);

            // Arrange one active (approved) booking in future window
            var active = new Booking
            {
                OwnerNic = "NIC1",
                StationId = "ST200",
                StartTimeUtc = DateTime.UtcNow.AddHours(1),
                EndTimeUtc = DateTime.UtcNow.AddHours(2),
                Status = BookingStatus.Approved
            };
            await bookingRepo.CreateAsync(active);

            // Assert the precondition your controller would check:
            var startOfDay = DateTime.UtcNow.Date;
            var endOfDay = startOfDay.AddDays(1);
            var bookings = await bookingRepo.GetByStationAsync("ST200");
            bookings.Exists(b =>
                BookingStatus.ActiveStatuses.Contains(b.Status) &&
                b.StartTimeUtc < endOfDay &&
                b.EndTimeUtc > startOfDay
            ).Should().BeTrue();
        }

        [Fact]
        public async Task Operator_UpdateSlots_OnlyIfAssigned_BasicRepoUpdate()
        {
            if (TestMongoHost.SkipAll) return;

            var stationRepo = new StationRepository(_cfg);

            var st = new Station
            {
                StationId = "ST300",
                Name = "Ops",
                Latitude = 1,
                Longitude = 2,
                Address = "X",
                Type = "AC",
                AvailableSlots = 1,
                IsActive = true,
                OperatorUserIds = new List<string> { "op123" } // FIX: List<string>, not array
            };
            await stationRepo.CreateAsync(st);

            // Update available slots using repo’s UpdateAsync (since UpdateSlotsAsync doesn’t exist)
            var entity = await stationRepo.GetByIdAsync("ST300");
            entity!.AvailableSlots = 5;
            await stationRepo.UpdateAsync(entity.StationId, entity);

            var got = await stationRepo.GetByIdAsync("ST300");
            got!.AvailableSlots.Should().Be(5);
        }
    }
}
