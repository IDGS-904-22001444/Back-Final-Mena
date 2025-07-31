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
    public class PurchasesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchasesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetAll()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Provider)
                .Include(p => p.Admin)
                .ToListAsync();
            return Ok(purchases);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Purchase>> GetById(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Provider)
                .Include(p => p.Admin)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
                return NotFound();

            return Ok(purchase);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var purchase = new Purchase
            {
                ProviderId = dto.ProviderId,
                AdminId = dto.AdminId,
                PurchaseDate = dto.PurchaseDate,
                Total = dto.Total,
                Status = dto.Status
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // Si hay detalles, los insertamos
            if (dto.Details != null && dto.Details.Any())
            {
                foreach (var detailDto in dto.Details)
                {
                    var detail = new PurchaseDetail
                    {
                        PurchaseId = purchase.Id,
                        RawMaterialId = detailDto.RawMaterialId,
                        Quantity = detailDto.Quantity,
                        UnitPrice = detailDto.UnitPrice,
                        Subtotal = detailDto.Subtotal,
                        Status = detailDto.Status
                    };

                    _context.PurchaseDetails.Add(detail);

                    // Actualizar stock de materia prima
                    var rawMaterial = await _context.RawMaterials.FindAsync(detailDto.RawMaterialId);
                    if (rawMaterial == null)
                        return BadRequest($"La materia prima con ID {detailDto.RawMaterialId} no existe.");

                    rawMaterial.Stock += detailDto.Quantity;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(purchase);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PurchaseDto dto)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
                return NotFound();

            purchase.ProviderId = dto.ProviderId;
            purchase.AdminId = dto.AdminId;
            purchase.PurchaseDate = dto.PurchaseDate;
            purchase.Total = dto.Total;
            purchase.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(purchase);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
                return NotFound();

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}