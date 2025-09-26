using EvCharge.Api.Domain;
using EvCharge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<ActionResult<List<EvOwner>>> GetAll() =>
            await _repo.GetAllAsync();

        [HttpGet("{nic}")]
        public async Task<ActionResult<EvOwner>> GetByNic(string nic)
        {
            var owner = await _repo.GetByNicAsync(nic);
            if (owner == null) return NotFound();
            return owner;
        }

        [HttpPost]
        public async Task<ActionResult> Create(EvOwner owner)
        {
            await _repo.CreateAsync(owner);
            return CreatedAtAction(nameof(GetByNic), new { nic = owner.NIC }, owner);
        }

        [HttpPut("{nic}")]
        public async Task<ActionResult> Update(string nic, EvOwner updated)
        {
            var existing = await _repo.GetByNicAsync(nic);
            if (existing == null) return NotFound();

            updated.NIC = nic; // keep NIC as PK
            await _repo.UpdateAsync(nic, updated);
            return NoContent();
        }

        [HttpDelete("{nic}")]
        public async Task<ActionResult> Delete(string nic)
        {
            var existing = await _repo.GetByNicAsync(nic);
            if (existing == null) return NotFound();

            await _repo.DeleteAsync(nic);
            return NoContent();
        }
    }
}
