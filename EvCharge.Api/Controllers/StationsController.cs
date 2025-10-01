using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCharge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly StationRepository _repo;
        private readonly BookingRepository _bookingRepo;
        private readonly UserRepository _userRepo;

        public StationsController(IConfiguration config)
        {
            _repo = new StationRepository(config);
            _bookingRepo = new BookingRepository(config);
            _userRepo = new UserRepository(config);
        }

        // ---- Helpers ----
        private static bool ValidType(string t) => t is "AC" or "DC";
        private static bool ValidLat(double lat) => lat >= -90 && lat <= 90;
        private static bool ValidLng(double lng) => lng >= -180 && lng <= 180;
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat/2)*Math.Sin(dLat/2) +
                    Math.Cos(lat1 * Math.PI/180.0) * Math.Cos(lat2 * Math.PI/180.0) *
                    Math.Sin(dLon/2)*Math.Sin(dLon/2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            return R * c;
        }

        private async Task<bool> IsOperatorAssignedTo(string userId, string stationId)
        {
            var station = await _repo.GetByIdAsync(stationId);
            return station != null && station.OperatorUserIds.Contains(userId);
        }

        // ---- Query: list / details / nearby ----

        // View stations: Backoffice, Operator, Owner (all can see)
        [HttpGet]
        [Authorize(Roles = "Backoffice,Operator,Owner")]
        public async Task<ActionResult<List<Station>>> GetAll() =>
            await _repo.GetAllAsync();

        [HttpGet("{stationId}")]
        [Authorize(Roles = "Backoffice,Operator,Owner")]
        public async Task<ActionResult<Station>> GetById(string stationId)
        {
            var st = await _repo.GetByIdAsync(stationId);
            if (st == null) return NotFound();
            return st;
        }

        // Nearby stations for mobile maps
        [HttpGet("near")]
        [Authorize(Roles = "Backoffice,Operator,Owner")]
        public async Task<ActionResult<List<Station>>> GetNearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm = 10)
        {
            if (!ValidLat(lat) || !ValidLng(lng) || radiusKm <= 0) return BadRequest("Invalid coordinates or radius");

            var all = await _repo.GetAllAsync();
            var result = all
                .Where(s => s.IsActive)
                .Where(s => HaversineKm(lat, lng, s.Latitude, s.Longitude) <= radiusKm)
                .ToList();

            return result;
        }

        // ---- Create / Update / Delete ----

        // Create station: Backoffice only
        [HttpPost]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> Create(Station s)
        {
            if (string.IsNullOrWhiteSpace(s.StationId)) return BadRequest("StationId is required");
            if (await _repo.ExistsAsync(s.StationId)) return Conflict("StationId already exists");
            if (!ValidType(s.Type)) return BadRequest("Type must be 'AC' or 'DC'");
            if (!ValidLat(s.Latitude) || !ValidLng(s.Longitude)) return BadRequest("Invalid latitude/longitude");
            if (s.AvailableSlots < 0) return BadRequest("AvailableSlots cannot be negative");

            s.OperatorUserIds = s.OperatorUserIds?.Distinct().ToList() ?? new List<string>();

            await _repo.CreateAsync(s);
            return CreatedAtAction(nameof(GetById), new { stationId = s.StationId }, s);
        }

        // Full update: Backoffice only
        [HttpPut("{stationId}")]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> Update(string stationId, Station updated)
        {
            var existing = await _repo.GetByIdAsync(stationId);
            if (existing == null) return NotFound();

            if (!ValidType(updated.Type)) return BadRequest("Type must be 'AC' or 'DC'");
            if (!ValidLat(updated.Latitude) || !ValidLng(updated.Longitude)) return BadRequest("Invalid latitude/longitude");
            if (updated.AvailableSlots < 0) return BadRequest("AvailableSlots cannot be negative");

            updated.StationId = stationId;
            // keep operator assignments unless explicitly provided
            if (updated.OperatorUserIds == null || updated.OperatorUserIds.Count == 0)
                updated.OperatorUserIds = existing.OperatorUserIds;

            await _repo.UpdateAsync(stationId, updated);
            return NoContent();
        }

        // Delete station: Backoffice only (OPTIONAL – often we keep stations and mark inactive)
        [HttpDelete("{stationId}")]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> Delete(string stationId)
        {
            var existing = await _repo.GetByIdAsync(stationId);
            if (existing == null) return NotFound();

            // Prevent delete if active bookings exist
            if (await _bookingRepo.HasActiveBookingsAsync(stationId))
                return Conflict(new { message = "Cannot delete: station has active bookings." });

            await _repo.DeleteAsync(stationId);
            return NoContent();
        }

        // Activate/Deactivate: Backoffice only; block deactivation if active bookings
        [HttpPatch("{stationId}/status")]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> SetStatus(string stationId, [FromQuery] bool isActive)
        {
            var existing = await _repo.GetByIdAsync(stationId);
            if (existing == null) return NotFound();

            if (!isActive) // deactivating
            {
                if (await _bookingRepo.HasActiveBookingsAsync(stationId))
                    return Conflict(new { message = "Cannot deactivate: station has active bookings." });
            }

            await _repo.SetActiveAsync(stationId, isActive);
            return Ok(new { message = $"Station {stationId} active status set to {isActive}" });
        }

        // ---- Operator abilities ----

        // Operator updates ONLY AvailableSlots on their assigned station
        [HttpPatch("{stationId}/slots")]
        [Authorize(Roles = "Operator,Backoffice")]
        public async Task<ActionResult> UpdateAvailableSlots(string stationId, [FromQuery] int availableSlots)
        {
            if (availableSlots < 0) return BadRequest("AvailableSlots cannot be negative");

            var existing = await _repo.GetByIdAsync(stationId);
            if (existing == null) return NotFound();

            if (User.IsInRole("Operator"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var assigned = await IsOperatorAssignedTo(userId, stationId);
                if (!assigned) return Forbid(); // operator can only update their station
            }

            await _repo.SetAvailableSlotsAsync(stationId, availableSlots);
            return Ok(new { message = $"Station {stationId} AvailableSlots set to {availableSlots}" });
        }

        // ---- Assign/Remove operators (Backoffice only) ----

        [HttpPost("{stationId}/assign-operator")]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> AssignOperator(string stationId, [FromQuery] string userId)
        {
            var station = await _repo.GetByIdAsync(stationId);
            if (station == null) return NotFound("Station not found");

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound("User not found");
            if (user.Role != "Operator") return BadRequest("User is not an Operator");

            await _repo.AddOperatorAsync(stationId, userId);
            return Ok(new { message = $"Operator {userId} assigned to station {stationId}" });
        }

        [HttpPost("{stationId}/remove-operator")]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> RemoveOperator(string stationId, [FromQuery] string userId)
        {
            var station = await _repo.GetByIdAsync(stationId);
            if (station == null) return NotFound("Station not found");

            await _repo.RemoveOperatorAsync(stationId, userId);
            return Ok(new { message = $"Operator {userId} removed from station {stationId}" });
        }
    }
}
