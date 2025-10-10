using EvCharge.Api.Domain;
using EvCharge.Api.DTOs;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCharge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly BookingRepository _repo;
        private readonly StationRepository _stationRepo;

        public BookingsController(IConfiguration config)
        {
            _repo = new BookingRepository(config);
            _stationRepo = new StationRepository(config);
        }

        // ---- Helpers ----
        private string? UserSubject() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static bool WithinSevenDays(DateTime startUtc)
            => startUtc <= DateTime.UtcNow.AddDays(7);

        private static bool HasTwelveHoursLead(DateTime startUtc)
            => (startUtc - DateTime.UtcNow) >= TimeSpan.FromHours(12);

        private static bool ValidWindow(DateTime startUtc, DateTime endUtc)
            => endUtc > startUtc; // (optional: enforce max duration, e.g., <= 12h)

        private static string NewQr() => Guid.NewGuid().ToString("N");

        private bool IsBackoffice => User.IsInRole("Backoffice");
        private bool IsOperator => User.IsInRole("Operator");
        private bool IsOwner => User.IsInRole("Owner");

        private async Task<bool> OperatorOwnsStation(string stationId)
        {
            if (!IsOperator) return false;
            var userId = UserSubject()!;
            var st = await _stationRepo.GetByIdAsync(stationId);
            return st != null && st.OperatorUserIds.Contains(userId);
        }

        // ---- Queries ----

        // Backoffice: all bookings
        [HttpGet]
        [Authorize(Roles = "Backoffice")]
        public Task<List<Booking>> GetAll() => _repo.GetAllAsync();

        // Owner: my bookings (all)
        [HttpGet("my")]
        [Authorize(Roles = "Owner")]
        public Task<List<Booking>> GetMine()
        {
            var nic = UserSubject()!;
            return _repo.GetByOwnerAsync(nic);
        }

        // Owner: my upcoming
        [HttpGet("my/upcoming")]
        [Authorize(Roles = "Owner")]
        public Task<List<Booking>> GetMyUpcoming()
        {
            var nic = UserSubject()!;
            return _repo.GetUpcomingByOwnerAsync(nic);
        }

        // Owner: my history
        [HttpGet("my/history")]
        [Authorize(Roles = "Owner")]
        public Task<List<Booking>> GetMyHistory()
        {
            var nic = UserSubject()!;
            return _repo.GetHistoryByOwnerAsync(nic);
        }

        // Operator/Backoffice: station bookings
        [HttpGet("station/{stationId}")]
        [Authorize(Roles = "Operator,Backoffice")]
        public async Task<ActionResult<List<Booking>>> GetForStation(string stationId)
        {
            if (IsOperator && !await OperatorOwnsStation(stationId))
                return Forbid();

            return await _repo.GetByStationAsync(stationId);
        }

        // Details
        [HttpGet("{id}")]
        [Authorize(Roles = "Backoffice,Operator,Owner")]
        public async Task<ActionResult<Booking>> GetById(string id)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();

            if (IsOwner && b.OwnerNic != UserSubject()) return Forbid();
            if (IsOperator && !await OperatorOwnsStation(b.StationId)) return Forbid();

            return b;
        }

        // ---- Create / Update / Cancel ----

        // Owner creates booking (7-day rule + overlap prevention)
        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<ActionResult> Create(BookingCreateRequest req)
        {
            if (!ValidWindow(req.StartTimeUtc, req.EndTimeUtc)) return BadRequest("Invalid time window.");
            if (!WithinSevenDays(req.StartTimeUtc)) return BadRequest("Start time must be within 7 days.");
            var st = await _stationRepo.GetByIdAsync(req.StationId);
            if (st == null || !st.IsActive) return BadRequest("Station not available.");

            var overlapping = await _repo.CountOverlappingAsync(req.StationId, req.StartTimeUtc, req.EndTimeUtc);
            if (overlapping >= st.AvailableSlots) return Conflict(new { message = "No slots available in this time window." });

            var nic = UserSubject()!;
            var b = new Booking
            {
                OwnerNic = nic,
                StationId = req.StationId,
                StartTimeUtc = req.StartTimeUtc,
                EndTimeUtc = req.EndTimeUtc,
                Status = BookingStatus.Pending,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            await _repo.CreateAsync(b);
            return CreatedAtAction(nameof(GetById), new { id = b.Id }, b);
        }

        // Owner updates booking (>= 12h before; overlap check)
        [HttpPut("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<ActionResult> Update(string id, BookingUpdateRequest req)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();
            if (b.OwnerNic != UserSubject()) return Forbid();

            if (!BookingStatus.OwnerCanChange.Contains(b.Status))
                return Conflict(new { message = $"Cannot update when status is {b.Status}." });

            if (!HasTwelveHoursLead(b.StartTimeUtc))
                return Conflict(new { message = "Updates must be at least 12 hours before start." });

            if (!ValidWindow(req.StartTimeUtc, req.EndTimeUtc))
                return BadRequest("Invalid time window.");
            if (!WithinSevenDays(req.StartTimeUtc))
                return BadRequest("Start time must be within 7 days.");

            var st = await _stationRepo.GetByIdAsync(b.StationId);
            if (st == null || !st.IsActive) return BadRequest("Station not available.");

            var overlapping = await _repo.CountOverlappingAsync(b.StationId, req.StartTimeUtc, req.EndTimeUtc, excludeBookingId: b.Id);
            if (overlapping >= st.AvailableSlots) return Conflict(new { message = "No slots available in the new time window." });

            b.StartTimeUtc = req.StartTimeUtc;
            b.EndTimeUtc = req.EndTimeUtc;
            b.UpdatedUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(b);
            return NoContent();
        }

        // Owner cancel (>= 12h); Backoffice can cancel anytime
        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner,Backoffice")]
        public async Task<ActionResult> Cancel(string id)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();

            if (IsOwner && b.OwnerNic != UserSubject()) return Forbid();
            if (IsOwner && !HasTwelveHoursLead(b.StartTimeUtc))
                return Conflict(new { message = "Cancellations must be at least 12 hours before start." });

            if (b.Status is BookingStatus.Completed or BookingStatus.Cancelled)
                return Conflict(new { message = $"Already {b.Status}." });

            b.Status = BookingStatus.Cancelled;
            b.UpdatedUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(b);
            return NoContent();
        }

        // ---- Approve / Reject ----

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Backoffice,Operator")]
        public async Task<ActionResult> ApproveOrReject(string id, [FromBody] ApproveRequest req)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();

            if (IsOperator && !await OperatorOwnsStation(b.StationId))
                return Forbid();

            if (b.Status is not BookingStatus.Pending && b.Status is not BookingStatus.Approved)
                return Conflict(new { message = $"Cannot approve/reject when status is {b.Status}." });

            if (req.Approve)
            {
                var st = await _stationRepo.GetByIdAsync(b.StationId);
                if (st == null || !st.IsActive) return Conflict(new { message = "Station not available." });

                var overlapping = await _repo.CountOverlappingAsync(b.StationId, b.StartTimeUtc, b.EndTimeUtc, excludeBookingId: b.Id);
                if (overlapping >= st.AvailableSlots) return Conflict(new { message = "No slots available at this time." });

                b.Status = BookingStatus.Approved;
                b.QrCode = b.QrCode ?? NewQr(); // generate if first approval
            }
            else
            {
                b.Status = BookingStatus.Rejected;
                b.RejectionReason = string.IsNullOrWhiteSpace(req.Reason) ? "Rejected" : req.Reason;
            }

            b.UpdatedUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(b);
            return Ok(new { id = b.Id, b.Status, b.QrCode, b.RejectionReason });
        }

        // ---- Operator: Start / Complete ----

        [HttpPatch("{id}/start")]
        [Authorize(Roles = "Operator")]
        public async Task<ActionResult> StartSession(string id, [FromBody] StartRequest req)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();
            if (!await OperatorOwnsStation(b.StationId)) return Forbid();

            if (b.Status != BookingStatus.Approved)
                return Conflict(new { message = "Only approved bookings can be started." });

            if (string.IsNullOrWhiteSpace(b.QrCode) || b.QrCode != req.QrCode)
                return Unauthorized(new { message = "Invalid QR code." });

            b.Status = BookingStatus.InProgress;
            b.UpdatedUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(b);
            return Ok(new { message = "Charging session started." });
        }

        [HttpPatch("{id}/complete")]
        [Authorize(Roles = "Operator")]
        public async Task<ActionResult> CompleteSession(string id)
        {
            var b = await _repo.GetByIdAsync(id);
            if (b == null) return NotFound();
            if (!await OperatorOwnsStation(b.StationId)) return Forbid();

            if (b.Status != BookingStatus.InProgress)
                return Conflict(new { message = "Only in-progress bookings can be completed." });

            b.Status = BookingStatus.Completed;
            b.UpdatedUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(b);
            return Ok(new { message = "Charging session completed." });
        }


        [HttpGet("station/{stationId}/availability")]
[Authorize(Roles = "Owner,Backoffice,Operator")]
public async Task<ActionResult<object>> GetAvailability(
    string stationId,
    [FromQuery] string dateLocal,      // e.g. "2025-10-13"
    [FromQuery] int tzOffsetMinutes    // e.g. Colombo = +330
)
{
    var station = await _stationRepo.GetByIdAsync(stationId);
    if (station == null || !station.IsActive)
        return NotFound("Station not available.");

    // 1) Parse local calendar day string robustly
    //    We accept "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd".
    if (!TryParseYmd(dateLocal, out var ymd))
        return BadRequest("date must be yyyy-MM-dd (or yyyy.MM.dd / yyyy/MM/dd)");

    // local day start/end (no timezone yet)
    var localStart = new DateTime(ymd.Year, ymd.Month, ymd.Day, 0, 0, 0, DateTimeKind.Unspecified);
    var localEnd   = localStart.AddDays(1);

    // 2) Convert the local window to UTC using client offset
    //    local = UTC + offset  =>  UTC = local - offset
    var startUtc = localStart - TimeSpan.FromMinutes(tzOffsetMinutes);
    var endUtc   = localEnd   - TimeSpan.FromMinutes(tzOffsetMinutes);

    // 3) Pull active bookings that overlap this UTC window
    var bookings = (await _repo.GetByStationAsync(stationId))
        .Where(b =>
            BookingStatus.ActiveStatuses.Contains(b.Status) &&
            b.StartTimeUtc < endUtc &&
            b.EndTimeUtc   > startUtc
        )
        .ToList();

    // 4) Build 30-min LOCAL slots across the requested day; compare in UTC
    var availability = new List<object>();
    for (var tLocal = localStart; tLocal < localEnd; tLocal = tLocal.AddMinutes(30))
    {
        var slotStartUtc = tLocal - TimeSpan.FromMinutes(tzOffsetMinutes);
        var slotEndUtc   = slotStartUtc.AddMinutes(30);

        // Overlap rule
        var overlapping = bookings.Count(b => b.StartTimeUtc < slotEndUtc && b.EndTimeUtc > slotStartUtc);
        var freeSlots   = Math.Max(0, station.AvailableSlots - overlapping);

        availability.Add(new
        {
            time = tLocal.ToString("HH:mm"), // label in LOCAL time
            availableSlots = freeSlots
        });
    }

    return Ok(new
    {
        stationId,
        date = $"{ymd:yyyy-MM-dd}",
        tzOffsetMinutes,
        availability
    });
}

// helper at bottom of controller
private static bool TryParseYmd(string s, out DateOnly ymd)
{
    var fmts = new[] { "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd" };
    foreach (var fmt in fmts)
        if (DateOnly.TryParseExact(s, fmt, out ymd))
            return true;
    ymd = default;
    return false;
}



    }
}
