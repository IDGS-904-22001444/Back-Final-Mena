using AuthAPI.Data;
using AuthAPI.Dtos;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProvidersController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/Providers
        [HttpPost]
        public async Task<IActionResult> AddProvider([FromBody] ProviderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var provider = new Provider
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                ContactPerson = dto.ContactPerson,
                Status = dto.Status
            };
            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();
            return Ok(provider);
        }

        // GET: /api/Providers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Provider>>> GetAllProviders()
        {
            var providers = await _context.Providers.ToListAsync();
            return Ok(providers);
        }

        // PUT: /api/Providers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] ProviderDto dto)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "Proveedor no encontrado" });

            provider.Name = dto.Name;
            provider.Phone = dto.Phone;
            provider.Email = dto.Email;
            provider.Address = dto.Address;
            provider.ContactPerson = dto.ContactPerson;
            provider.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(provider);
        }

        // DELETE: /api/Providers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "Proveedor no encontrado" });

            _context.Providers.Remove(provider);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // GET: /api/Providers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Provider>> GetProviderById(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null)
                return NotFound(new { message = "Proveedor no encontrado" });

            return Ok(provider);
        }
    }
}