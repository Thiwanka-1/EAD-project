// =============================================
// File: EvOwnersController.cs
// Description: Handles all EV owner-related logic for the EV Charging app.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EvCharge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvOwnersController : ControllerBase
    {
        private readonly EvOwnerRepository _repo;

        public EvOwnersController(IConfiguration config)
        {
            _repo = new EvOwnerRepository(config);
        }

        // 🔹 GET ALL (Backoffice only)
        [HttpGet]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult<List<EvOwner>>> GetAll() =>
            await _repo.GetAllAsync();

        // 🔹 GET BY NIC (Owner can see self, Backoffice can see any)
        [HttpGet("{nic}")]
        [Authorize(Roles = "Backoffice,Owner")]
        public async Task<ActionResult<EvOwner>> GetByNic(string nic)
        {
            var owner = await _repo.GetByNicAsync(nic);
            if (owner == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != nic) return Forbid(); // Owner can only view self
            }

            return owner;
        }

        // 🔹 UPDATE (Owner = self, Backoffice = any)
        [HttpPut("{nic}")]
        [Authorize(Roles = "Backoffice,Owner")]
        public async Task<ActionResult> Update(string nic, EvOwner updated)
        {
            var existing = await _repo.GetByNicAsync(nic);
            if (existing == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != nic) return Forbid(); // Owner can only update self
            }

            updated.NIC = nic;
            updated.PasswordHash = existing.PasswordHash; // do not overwrite password here
            await _repo.UpdateAsync(nic, updated);
            return NoContent();
        }

        // 🔹 DELETE (Owner = self, Backoffice = any)
        [HttpDelete("{nic}")]
        [Authorize(Roles = "Backoffice,Owner")]
        public async Task<ActionResult> Delete(string nic)
        {
            var existing = await _repo.GetByNicAsync(nic);
            if (existing == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != nic) return Forbid(); // Owner can only delete self
            }

            await _repo.DeleteAsync(nic);
            return NoContent();
        }

        // 🔹 Change Active Status
        [HttpPatch("{nic}/status")]
        [Authorize(Roles = "Backoffice,Owner")]
        public async Task<ActionResult> ChangeStatus(string nic, [FromQuery] bool isActive)
        {
            var existing = await _repo.GetByNicAsync(nic);
            if (existing == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != nic) return Forbid();

                // Owners can only deactivate themselves
                if (isActive) return Forbid();
            }

            // Backoffice can deactivate/reactivate any account
            existing.IsActive = isActive;
            await _repo.UpdateAsync(nic, existing);

            return Ok(new { message = $"Owner {nic} active status set to {isActive}" });
        }


        public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

private static string Hash(string plain)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plain));
    return Convert.ToBase64String(bytes);
}

[HttpPatch("{nic}/password")]
[Authorize(Roles = "Backoffice,Owner")]
public async Task<ActionResult> ChangePassword(string nic, [FromBody] ChangePasswordRequest req)
{
    var existing = await _repo.GetByNicAsync(nic);
    if (existing == null) return NotFound();

    if (User.IsInRole("Owner"))
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (subject != nic) return Forbid(); // owner can change only own password
    }

    if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
        return BadRequest("Passwords required.");

    var currentHash = existing.PasswordHash ?? "";
    if (!string.Equals(currentHash, Hash(req.CurrentPassword)))
        return Unauthorized(new { message = "Incorrect current password." });

    existing.PasswordHash = Hash(req.NewPassword);
    await _repo.UpdateAsync(nic, existing);
    return Ok(new { message = "Password updated." });
}

    }
}
