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
    public class GeneralStatusesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GeneralStatusesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GeneralStatus>>> GetAll()
        {
            var statuses = await _context.GeneralStatuses.ToListAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GeneralStatus>> GetById(int id)
        {
            var status = await _context.GeneralStatuses.FindAsync(id);
            if (status == null)
                return NotFound();
            return Ok(status);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GeneralStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = new GeneralStatus
            {
                StatusName = dto.StatusName
            };

            _context.GeneralStatuses.Add(status);
            await _context.SaveChangesAsync();
            return Ok(status);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GeneralStatusDto dto)
        {
            var status = await _context.GeneralStatuses.FindAsync(id);
            if (status == null)
                return NotFound();

            status.StatusName = dto.StatusName;
            await _context.SaveChangesAsync();
            return Ok(status);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _context.GeneralStatuses.FindAsync(id);
            if (status == null)
                return NotFound();

            _context.GeneralStatuses.Remove(status);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}