using AuthAPI.Data;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class QuotationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuotationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quotations>>> GetAll()
        {
            var quotations = await _context.Quotations.ToListAsync();
            return Ok(quotations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Quotations>> GetById(int id)
        {
            var quotation = await _context.Quotations.FindAsync(id);
            if (quotation == null)
                return NotFound();
            return Ok(quotation);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Quotations quotation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Quotations.Add(quotation);
            await _context.SaveChangesAsync();
            return Ok(quotation);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Quotations quotation)
        {
            if (id != quotation.Id)
                return BadRequest();

            var existing = await _context.Quotations.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Actualiza las propiedades
            existing.ProductId = quotation.ProductId;
            existing.Quantity = quotation.Quantity;
            existing.QuotationStatusId = quotation.QuotationStatusId;
            existing.RequestDate = quotation.RequestDate;
            existing.Total = quotation.Total;
            existing.Requirements = quotation.Requirements;
            existing.UserId = quotation.UserId;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var quotation = await _context.Quotations.FindAsync(id);
            if (quotation == null)
                return NotFound();

            _context.Quotations.Remove(quotation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}