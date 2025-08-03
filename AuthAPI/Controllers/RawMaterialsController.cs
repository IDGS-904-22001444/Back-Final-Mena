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
    public class RawMaterialsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RawMaterialsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RawMaterial>>> GetAll()
        {
            var materials = await _context.RawMaterials.ToListAsync();
            return Ok(materials);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RawMaterial>> GetById(int id)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
                return NotFound();
            return Ok(material);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RawMaterialDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var material = new RawMaterial
            {
                Name = dto.Name,
                Description = dto.Description,
                UnitOfMeasure = dto.UnitOfMeasure,
                UnitCost = dto.UnitCost,
                Stock = dto.Stock,
                Status = dto.Status
            };

            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            // Registrar movimiento inicial si el stock es mayor a 0
            if (material.Stock > 0)
            {
                int entrada = material.Stock;
                int salida = 0;
                int newStock = entrada;
                decimal debe = entrada * material.UnitCost;
                decimal hecho = 0;
                decimal newBalance = debe;
                decimal newAverage = entrada > 0 ? Math.Round(newBalance / entrada, 2) : 0;

                var movement = new RawMaterialMovement
                {
                    RawMaterialId = material.Id,
                    Date = DateTime.UtcNow,
                    EntryQuantity = entrada,
                    ExitQuantity = salida,
                    CurrentStock = newStock,
                    Cost = material.UnitCost,
                    Average = newAverage,
                    Debit = debe,
                    Credit = hecho,
                    Balance = newBalance,
                    Status = 1
                };

                _context.RawMaterialMovements.Add(movement);
                await _context.SaveChangesAsync();
            }

            return Ok(material);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RawMaterialDto dto)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
                return NotFound();

            material.Name = dto.Name;
            material.Description = dto.Description;
            material.UnitOfMeasure = dto.UnitOfMeasure;
            material.UnitCost = dto.UnitCost;
            material.Stock = dto.Stock;
            material.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(material);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var material = await _context.RawMaterials.FindAsync(id);
            if (material == null)
                return NotFound();

            _context.RawMaterials.Remove(material);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}