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

        [HttpGet("by-purchase/{purchaseId}")]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetByPurchaseId(int purchaseId)
        {
            var details = await _context.PurchaseDetails
                .Include(d => d.RawMaterial)
                .Where(d => d.PurchaseId == purchaseId)
                .ToListAsync();

            if (details == null || !details.Any())
                return NotFound();

            return Ok(details);
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

            // Actualizar stock al crear y registrar movimiento
            var rawMaterial = await _context.RawMaterials.FindAsync(dto.RawMaterialId);
            if (rawMaterial == null)
                return BadRequest("La materia prima especificada no existe.");

            var lastMovement = await _context.RawMaterialMovements
                .Where(m => m.RawMaterialId == dto.RawMaterialId)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.MovementId)
                .FirstOrDefaultAsync();

            int prevStock = lastMovement?.CurrentStock ?? 0;
            decimal prevBalance = lastMovement?.Balance ?? 0;
            decimal prevAverage = lastMovement?.Average ?? 0;

            int entrada = dto.Quantity;
            int salida = 0;

            int newStock = prevStock + entrada - salida;
            decimal debe = entrada * dto.UnitPrice;
            decimal hecho = salida * prevAverage;
            decimal newBalance = prevBalance + debe - hecho;
            decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

            var movement = new RawMaterialMovement
            {
                RawMaterialId = dto.RawMaterialId,
                Date = DateTime.UtcNow,
                EntryQuantity = entrada,
                ExitQuantity = salida,
                CurrentStock = newStock,
                Cost = dto.UnitPrice,
                Average = newAverage,
                Debit = debe,
                Credit = hecho,
                Balance = newBalance,
                Status = 1
            };

            _context.RawMaterialMovements.Add(movement);

            rawMaterial.Stock = newStock;
            rawMaterial.UnitCost = newAverage;

            await _context.SaveChangesAsync();

            // Actualizar el precio de los productos que usan esta materia prima
            await UpdateProductPricesByRawMaterial(dto.RawMaterialId);

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
                var oldRawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
                if (oldRawMaterial == null)
                    return BadRequest("La materia prima anterior no existe.");

                oldRawMaterial.Stock -= detail.Quantity;

                var newRawMaterial = await _context.RawMaterials.FindAsync(dto.RawMaterialId);
                if (newRawMaterial == null)
                    return BadRequest("La nueva materia prima especificada no existe.");

                newRawMaterial.Stock += dto.Quantity;

                // Registrar movimiento en la nueva materia prima
                var lastMovement = await _context.RawMaterialMovements
                    .Where(m => m.RawMaterialId == dto.RawMaterialId)
                    .OrderByDescending(m => m.Date)
                    .ThenByDescending(m => m.MovementId)
                    .FirstOrDefaultAsync();

                int prevStock = lastMovement?.CurrentStock ?? 0;
                decimal prevBalance = lastMovement?.Balance ?? 0;
                decimal prevAverage = lastMovement?.Average ?? 0;

                int entrada = dto.Quantity;
                int salida = 0;

                int newStock = prevStock + entrada - salida;
                decimal debe = entrada * dto.UnitPrice;
                decimal hecho = salida * prevAverage;
                decimal newBalance = prevBalance + debe - hecho;
                decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

                var movement = new RawMaterialMovement
                {
                    RawMaterialId = dto.RawMaterialId,
                    Date = DateTime.UtcNow,
                    EntryQuantity = entrada,
                    ExitQuantity = salida,
                    CurrentStock = newStock,
                    Cost = dto.UnitPrice,
                    Average = newAverage,
                    Debit = debe,
                    Credit = hecho,
                    Balance = newBalance,
                    Status = 1
                };

                _context.RawMaterialMovements.Add(movement);

                newRawMaterial.Stock = newStock;
                newRawMaterial.UnitCost = newAverage;

                await _context.SaveChangesAsync();

                await UpdateProductPricesByRawMaterial(dto.RawMaterialId);
            }
            else
            {
                // Si no cambió, solo ajustar la diferencia de cantidad y registrar movimiento
                var rawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
                if (rawMaterial == null)
                    return BadRequest("La materia prima especificada no existe.");

                int cantidadDiferencia = dto.Quantity - detail.Quantity;
                rawMaterial.Stock += cantidadDiferencia;

                var lastMovement = await _context.RawMaterialMovements
                    .Where(m => m.RawMaterialId == detail.RawMaterialId)
                    .OrderByDescending(m => m.Date)
                    .ThenByDescending(m => m.MovementId)
                    .FirstOrDefaultAsync();

                int prevStock = lastMovement?.CurrentStock ?? 0;
                decimal prevBalance = lastMovement?.Balance ?? 0;
                decimal prevAverage = lastMovement?.Average ?? 0;

                int entrada = cantidadDiferencia > 0 ? cantidadDiferencia : 0;
                int salida = cantidadDiferencia < 0 ? -cantidadDiferencia : 0;

                int newStock = prevStock + entrada - salida;
                decimal debe = entrada * dto.UnitPrice;
                decimal hecho = salida * prevAverage;
                decimal newBalance = prevBalance + debe - hecho;
                decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

                var movement = new RawMaterialMovement
                {
                    RawMaterialId = detail.RawMaterialId,
                    Date = DateTime.UtcNow,
                    EntryQuantity = entrada,
                    ExitQuantity = salida,
                    CurrentStock = newStock,
                    Cost = dto.UnitPrice,
                    Average = newAverage,
                    Debit = debe,
                    Credit = hecho,
                    Balance = newBalance,
                    Status = 1
                };

                _context.RawMaterialMovements.Add(movement);

                rawMaterial.Stock = newStock;
                rawMaterial.UnitCost = newAverage;

                await _context.SaveChangesAsync();

                await UpdateProductPricesByRawMaterial(detail.RawMaterialId);
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

            // Actualizar stock al eliminar y registrar movimiento de salida
            var rawMaterial = await _context.RawMaterials.FindAsync(detail.RawMaterialId);
            if (rawMaterial != null)
            {
                var lastMovement = await _context.RawMaterialMovements
                    .Where(m => m.RawMaterialId == detail.RawMaterialId)
                    .OrderByDescending(m => m.Date)
                    .ThenByDescending(m => m.MovementId)
                    .FirstOrDefaultAsync();

                int prevStock = lastMovement?.CurrentStock ?? 0;
                decimal prevBalance = lastMovement?.Balance ?? 0;
                decimal prevAverage = lastMovement?.Average ?? 0;

                int entrada = 0;
                int salida = detail.Quantity;

                int newStock = prevStock + entrada - salida;
                decimal debe = entrada * detail.UnitPrice;
                decimal hecho = salida * prevAverage;
                decimal newBalance = prevBalance + debe - hecho;
                decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

                var movement = new RawMaterialMovement
                {
                    RawMaterialId = detail.RawMaterialId,
                    Date = DateTime.UtcNow,
                    EntryQuantity = entrada,
                    ExitQuantity = salida,
                    CurrentStock = newStock,
                    Cost = detail.UnitPrice,
                    Average = newAverage,
                    Debit = debe,
                    Credit = hecho,
                    Balance = newBalance,
                    Status = 1
                };

                _context.RawMaterialMovements.Add(movement);

                rawMaterial.Stock = newStock;
                rawMaterial.UnitCost = newAverage;

                await _context.SaveChangesAsync();

                await UpdateProductPricesByRawMaterial(detail.RawMaterialId);
            }

            _context.PurchaseDetails.Remove(detail);
            await _context.SaveChangesAsync();
            return NoContent();
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