using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll() =>
            await _repo.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(string id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return user;
        }

        [HttpPost]
        public async Task<ActionResult> Create(User user)
        {
            // enforce role
            if (user.Role != "Backoffice" && user.Role != "Operator")
                return BadRequest("Role must be either 'Backoffice' or 'Operator'");

            // hash password
            user.PasswordHash = HashPassword(user.PasswordHash);

            await _repo.CreateAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, User updated)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            updated.Id = id;

            // If password provided, re-hash
            if (!string.IsNullOrWhiteSpace(updated.PasswordHash))
                updated.PasswordHash = HashPassword(updated.PasswordHash);
            else
                updated.PasswordHash = existing.PasswordHash; // keep old password

            await _repo.UpdateAsync(id, updated);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
