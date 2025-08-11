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

            // Verificar stock suficiente y recolectar faltantes
            var faltantes = new List<string>();
            foreach (var mat in materiales)
            {
                var requerido = mat.RequiredQuantity * dto.QuantityToProduce;
                if (mat.RawMaterial == null || mat.RawMaterial.Stock < requerido)
                {
                    faltantes.Add(mat.RawMaterial?.Name ?? $"ID:{mat.RawMaterialId}");
                }
            }

            if (faltantes.Any())
            {
                return BadRequest(new
                {
                    message = "No hay suficiente stock para producir el producto.",
                    faltantes = faltantes
                });
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

                // Actualizar el UnitCost y Stock de la materia prima
                mat.RawMaterial.UnitCost = newAverage;
                mat.RawMaterial.Stock = newStock;

                // Actualizar el precio de los productos que usan esta materia prima
                await UpdateProductPricesByRawMaterial(mat.RawMaterialId);
            }

            // Aumentar stock del producto terminado
            product.Stock += dto.QuantityToProduce;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Producción realizada y stock actualizado." });
        }

        /// <summary>
        /// Actualiza el precio de venta de todos los productos que usan la materia prima modificada.
        /// </summary>
        private async Task UpdateProductPricesByRawMaterial(int rawMaterialId)
        {
            var productMaterials = await _context.ProductMaterials
                .Where(pm => pm.RawMaterialId == rawMaterialId && pm.Status == 1)
                .ToListAsync();

            var productIds = productMaterials.Select(pm => pm.ProductId).Distinct();

            foreach (var productId in productIds)
            {
                var materials = await _context.ProductMaterials
                    .Where(pm => pm.ProductId == productId && pm.Status == 1)
                    .Include(pm => pm.RawMaterial)
                    .ToListAsync();

                decimal totalCost = materials.Sum(pm => pm.RequiredQuantity * (pm.RawMaterial?.UnitCost ?? 0));
                decimal finalPrice = Math.Round(totalCost * 1.25m, 2);

                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.SalePrice = finalPrice;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}