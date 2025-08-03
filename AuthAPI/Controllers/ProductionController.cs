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
    public class ProductionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductionController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Production/start
        [HttpPost("start")]
        public async Task<IActionResult> StartProduction([FromBody] ProductionOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return BadRequest("El producto no existe.");

            // Obtener la receta (BOM) solo materiales activos
            var materiales = await _context.ProductMaterials
                .Where(pm => pm.ProductId == dto.ProductId && pm.Status == 1)
                .Include(pm => pm.RawMaterial)
                .ToListAsync();

            // Verificar stock suficiente
            foreach (var mat in materiales)
            {
                var requerido = mat.RequiredQuantity * dto.QuantityToProduce;
                if (mat.RawMaterial == null || mat.RawMaterial.Stock < requerido)
                {
                    return BadRequest($"Stock insuficiente de {mat.RawMaterial?.Name ?? "materia prima"}.");
                }
            }

            // Descontar materias primas y registrar movimientos
            foreach (var mat in materiales)
            {
                var cantidad = (int)(mat.RequiredQuantity * dto.QuantityToProduce);
                mat.RawMaterial!.Stock -= cantidad;

                // Registrar movimiento de salida en RawMaterialMovements
                var lastMovement = await _context.RawMaterialMovements
                    .Where(m => m.RawMaterialId == mat.RawMaterialId)
                    .OrderByDescending(m => m.Date)
                    .ThenByDescending(m => m.MovementId)
                    .FirstOrDefaultAsync();

                int prevStock = lastMovement?.CurrentStock ?? 0;
                decimal prevBalance = lastMovement?.Balance ?? 0;
                decimal prevAverage = lastMovement?.Average ?? 0;

                int entrada = 0;
                int salida = cantidad;

                int newStock = prevStock + entrada - salida;
                decimal debe = entrada * mat.RawMaterial.UnitCost;
                decimal hecho = salida * prevAverage;
                decimal newBalance = prevBalance + debe - hecho;
                decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

                var movement = new RawMaterialMovement
                {
                    RawMaterialId = mat.RawMaterialId,
                    Date = DateTime.UtcNow,
                    EntryQuantity = entrada,
                    ExitQuantity = salida,
                    CurrentStock = newStock,
                    Cost = mat.RawMaterial.UnitCost,
                    Average = newAverage,
                    Debit = debe,
                    Credit = hecho,
                    Balance = newBalance,
                    Status = 1
                };

                _context.RawMaterialMovements.Add(movement);
            }

            // Aumentar stock del producto terminado
            product.Stock += dto.QuantityToProduce;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Producción realizada y stock actualizado." });
        }
    }
}