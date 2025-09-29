using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EvCharge.Api.Services;
using EvCharge.Api.DTOs;
using EvCharge.Api.Repositories;
using EvCharge.Api.Domain;
using MongoDB.Driver;

namespace EvCharge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SecurityService _security;
        private readonly UserRepository _userRepo;
        private readonly EvOwnerRepository _ownerRepo;

        public AuthController(SecurityService security, IConfiguration config)
        {
            _security = security;
            _userRepo = new UserRepository(config);
            _ownerRepo = new EvOwnerRepository(config);
        }

        // 🔹 Login for Backoffice/Operator
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] SystemLoginRequest request)
        {
            var user = await _userRepo.GetByUsernameAsync(request.Username);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "Invalid credentials or inactive account." });

            var hashed = _security.HashPassword(request.Password);
            if (hashed != user.PasswordHash)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _security.CreateJwtToken(user.Id, user.Role, out var exp);
            return Ok(new AuthResponse { AccessToken = token, Role = user.Role, ExpiresAtUtc = exp });
        }

        // 🔹 EV Owner register
        [HttpPost("owner/register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> OwnerRegister([FromBody] OwnerRegisterRequest req)
        {
            // Check for duplicate NIC
            var existingNic = await _ownerRepo.GetByNicAsync(req.NIC);
            if (existingNic != null) return Conflict(new { message = "NIC already registered." });

            // Check for duplicate email
            var existingEmail = (await _ownerRepo.GetAllAsync())
                                .FirstOrDefault(o => o.Email.ToLower() == req.Email.ToLower());
            if (existingEmail != null) return Conflict(new { message = "Email already registered." });

            var owner = new EvOwner
            {
                NIC = req.NIC,
                FirstName = req.FirstName,
                LastName = req.LastName,
                Email = req.Email,
                Phone = req.Phone,
                IsActive = true,
                PasswordHash = _security.HashPassword(req.Password)
            };

            await _ownerRepo.CreateAsync(owner);

            var token = _security.CreateJwtToken(owner.NIC, "Owner", out var exp);
            return Created("", new AuthResponse { AccessToken = token, Role = "Owner", ExpiresAtUtc = exp });
        }

        // 🔹 EV Owner login (by email)
        [HttpPost("owner/login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> OwnerLogin([FromBody] OwnerLoginRequest req)
        {
            var owner = (await _ownerRepo.GetAllAsync())
                        .FirstOrDefault(o => o.Email.ToLower() == req.Email.ToLower());

            if (owner == null || !owner.IsActive)
                return Unauthorized(new { message = "Invalid credentials or inactive account." });

            var hash = _security.HashPassword(req.Password);
            if (hash != owner.PasswordHash)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _security.CreateJwtToken(owner.NIC, "Owner", out var exp);
            return Ok(new AuthResponse { AccessToken = token, Role = "Owner", ExpiresAtUtc = exp });
        }
    }
}
