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
                var rawMaterialIdsToUpdate = new HashSet<int>();

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

                    // Registrar movimiento de entrada en RawMaterialMovements
                    var lastMovement = await _context.RawMaterialMovements
                        .Where(m => m.RawMaterialId == detailDto.RawMaterialId)
                        .OrderByDescending(m => m.Date)
                        .ThenByDescending(m => m.MovementId)
                        .FirstOrDefaultAsync();

                    int prevStock = lastMovement?.CurrentStock ?? 0;
                    decimal prevBalance = lastMovement?.Balance ?? 0;
                    decimal prevAverage = lastMovement?.Average ?? 0;

                    int entrada = detailDto.Quantity;
                    int salida = 0;

                    int newStock = prevStock + entrada - salida;
                    decimal debe = entrada * detailDto.UnitPrice;
                    decimal hecho = salida * prevAverage;
                    decimal newBalance = prevBalance + debe - hecho;
                    decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

                    var movement = new RawMaterialMovement
                    {
                        RawMaterialId = detailDto.RawMaterialId,
                        Date = DateTime.UtcNow,
                        EntryQuantity = entrada,
                        ExitQuantity = salida,
                        CurrentStock = newStock,
                        Cost = detailDto.UnitPrice,
                        Average = newAverage,
                        Debit = debe,
                        Credit = hecho,
                        Balance = newBalance,
                        Status = 1
                    };

                    _context.RawMaterialMovements.Add(movement);

                    // Actualizar el UnitCost y Stock de la materia prima
                    rawMaterial.UnitCost = newAverage;
                    rawMaterial.Stock = newStock;

                    rawMaterialIdsToUpdate.Add(detailDto.RawMaterialId);
                }

                await _context.SaveChangesAsync();

                // Actualizar el precio de los productos que usan las materias primas afectadas
                foreach (var rawMaterialId in rawMaterialIdsToUpdate)
                {
                    await UpdateProductPricesByRawMaterial(rawMaterialId);
                }
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