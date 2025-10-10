// =============================================
// File: UserController.cs
// Description: Handles all Backoffice and operator-related logic for the EV Charging app.
// Date: 10/10/2025
// =============================================

using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EvCharge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _repo;

        public UsersController(IConfiguration config)
        {
            _repo = new UserRepository(config);
        }

        // Hash password using SHA256 (simple demo)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // 🔹 GET ALL (Backoffice only)
        [HttpGet]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult<List<User>>> GetAll() =>
            await _repo.GetAllAsync();

        // 🔹 GET BY ID (Backoffice = any, Operator = only self)
        [HttpGet("{id}")]
        [Authorize(Roles = "Backoffice,Operator")]
        public async Task<ActionResult<User>> GetById(string id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (User.IsInRole("Operator"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != id) return Forbid(); // Operator can only view self
            }

            return user;
        }

        // 🔹 CREATE (Backoffice only)
        [HttpPost]
        [Authorize(Roles = "Backoffice")]
        public async Task<ActionResult> Create(User user)
        {
            // enforce valid role
            if (user.Role != "Backoffice" && user.Role != "Operator")
                return BadRequest("Role must be either 'Backoffice' or 'Operator'");

            // hash password before save
            user.PasswordHash = HashPassword(user.PasswordHash);

            await _repo.CreateAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // 🔹 UPDATE (Backoffice = any, Operator = only self)
        [HttpPut("{id}")]
        [Authorize(Roles = "Backoffice,Operator")]
        public async Task<ActionResult> Update(string id, User updated)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (User.IsInRole("Operator"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != id) return Forbid(); // Operator can only update self
            }

            updated.Id = id;

            if (!string.IsNullOrWhiteSpace(updated.PasswordHash))
                updated.PasswordHash = HashPassword(updated.PasswordHash);
            else
                updated.PasswordHash = existing.PasswordHash; // keep old password

            await _repo.UpdateAsync(id, updated);
            return NoContent();
        }

        // 🔹 DELETE (Backoffice = any, Operator = only self)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Backoffice,Operator")]
        public async Task<ActionResult> Delete(string id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (User.IsInRole("Operator"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != id) return Forbid(); // Operator can only delete self
            }

            await _repo.DeleteAsync(id);
            return NoContent();
        }

        // 🔹 CHANGE ACTIVE STATUS (Backoffice = any, Operator = self only)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Backoffice,Operator")]
        public async Task<ActionResult> ChangeStatus(string id, [FromQuery] bool isActive)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (User.IsInRole("Operator"))
            {
                var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (subject != id) return Forbid(); // Operator can only manage own status
            }

            existing.IsActive = isActive;
            await _repo.UpdateAsync(id, existing);

            return Ok(new { message = $"User {id} active status set to {isActive}" });
        }
    }
}
