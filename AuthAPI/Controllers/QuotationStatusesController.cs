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
    public class QuotationStatusesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuotationStatusesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuotationStatus>>> GetAll()
        {
            var statuses = await _context.QuotationStatuses.ToListAsync();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QuotationStatus>> GetById(int id)
        {
            var status = await _context.QuotationStatuses.FindAsync(id);
            if (status == null)
                return NotFound();
            return Ok(status);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuotationStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = new QuotationStatus
            {
                StatusName = dto.StatusName
            };

            _context.QuotationStatuses.Add(status);
            await _context.SaveChangesAsync();
            return Ok(status);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] QuotationStatusDto dto)
        {
            var status = await _context.QuotationStatuses.FindAsync(id);
            if (status == null)
                return NotFound();

            status.StatusName = dto.StatusName;
            await _context.SaveChangesAsync();
            return Ok(status);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _context.QuotationStatuses.FindAsync(id);
            if (status == null)
                return NotFound();

            _context.QuotationStatuses.Remove(status);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}