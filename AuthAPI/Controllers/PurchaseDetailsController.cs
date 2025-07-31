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
    public class PurchaseDetailsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseDetailsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetAll()
        {
            var details = await _context.PurchaseDetails
                .Include(d => d.RawMaterial)
                .Include(d => d.Purchase)
                .ToListAsync();
            return Ok(details);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseDetail>> GetById(int id)
        {
            var detail = await _context.PurchaseDetails
                .Include(d => d.RawMaterial)
                .Include(d => d.Purchase)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detail == null)
                return NotFound();

            return Ok(detail);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var detail = new PurchaseDetail
            {
                PurchaseId = dto.PurchaseId,
                RawMaterialId = dto.RawMaterialId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                Subtotal = dto.Subtotal,
                Status = dto.Status
            };

            _context.PurchaseDetails.Add(detail);

            // Actualizar stock al crear
            var rawMaterial = await _context.RawMaterials.FindAsync(dto.RawMaterialId);
            if (rawMaterial == null)
                return BadRequest("La materia prima especificada no existe.");

            rawMaterial.Stock += dto.Quantity;

            await _context.SaveChangesAsync();
            return Ok(detail);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PurchaseDetailDto dto)
        {
            var detail = await _context.PurchaseDetails.FindAsync(id);
            if (detail == null)
                return NotFound();

            // Si la materia prima cambió, revertir stock en la anterior y sumar en la nueva
            if (detail.RawMaterialId != dto.RawMaterialId)
            {
                // Revertir stock en la materia prima anterior
                var oldRawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
                if (oldRawMaterial == null)
                    return BadRequest("La materia prima anterior no existe.");

                oldRawMaterial.Stock -= detail.Quantity;

                // Sumar stock en la nueva materia prima
                var newRawMaterial = await _context.RawMaterials.FindAsync(dto.RawMaterialId);
                if (newRawMaterial == null)
                    return BadRequest("La nueva materia prima especificada no existe.");

                newRawMaterial.Stock += dto.Quantity;
            }
            else
            {
                // Si no cambió, solo ajustar la diferencia de cantidad
                var rawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
                if (rawMaterial == null)
                    return BadRequest("La materia prima especificada no existe.");

                rawMaterial.Stock += (dto.Quantity - detail.Quantity);
            }

            // Actualizar los datos del detalle
            detail.PurchaseId = dto.PurchaseId;
            detail.RawMaterialId = dto.RawMaterialId;
            detail.Quantity = dto.Quantity;
            detail.UnitPrice = dto.UnitPrice;
            detail.Subtotal = dto.Subtotal;
            detail.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(detail);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detail = await _context.PurchaseDetails.FindAsync(id);
            if (detail == null)
                return NotFound();

            // Actualizar stock al eliminar
            var rawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
            if (rawMaterial != null)
            {
                rawMaterial.Stock -= detail.Quantity;
            }

            _context.PurchaseDetails.Remove(detail);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}