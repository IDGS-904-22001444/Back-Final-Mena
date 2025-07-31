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
    public class RawMaterialMovementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RawMaterialMovementsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/RawMaterialMovements
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RawMaterialMovementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Obtener el último movimiento para la materia prima
            var lastMovement = await _context.RawMaterialMovements
                .Where(m => m.RawMaterialId == dto.RawMaterialId)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.MovementId)
                .FirstOrDefaultAsync();

            int prevStock = lastMovement?.CurrentStock ?? 0;
            decimal prevBalance = lastMovement?.Balance ?? 0;
            decimal prevAverage = lastMovement?.Average ?? 0;

            int entrada = dto.EntryQuantity ?? 0;
            int salida = dto.ExitQuantity ?? 0;

            // Calcular existencia, debe, hecho, saldo y promedio
            int newStock = prevStock + entrada - salida;
            decimal debe = entrada * dto.Cost;
            decimal hecho = salida * prevAverage;
            decimal newBalance = prevBalance + debe - hecho;
            decimal newAverage = newStock > 0 ? Math.Round(newBalance / newStock, 2) : 0;

            var movement = new RawMaterialMovement
            {
                RawMaterialId = dto.RawMaterialId,
                Date = dto.Date,
                EntryQuantity = dto.EntryQuantity,
                ExitQuantity = dto.ExitQuantity,
                CurrentStock = newStock,
                Cost = dto.Cost,
                Average = newAverage,
                Debit = debe,
                Credit = hecho,
                Balance = newBalance,
                Status = dto.Status
            };

            _context.RawMaterialMovements.Add(movement);
            await _context.SaveChangesAsync();
            return Ok(movement);
        }

        // GET: api/RawMaterialMovements/kardex/{rawMaterialId}
        [HttpGet("kardex/{rawMaterialId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetKardex(int rawMaterialId)
        {
            var movimientos = await _context.RawMaterialMovements
                .Where(m => m.RawMaterialId == rawMaterialId)
                .OrderBy(m => m.Date)
                .ThenBy(m => m.MovementId)
                .ToListAsync();

            var kardex = new List<object>();
            foreach (var mov in movimientos)
            {
                kardex.Add(new
                {
                    Fecha = mov.Date.ToString("dd 'de' MMMM 'de' yyyy"),
                    Entrada = mov.EntryQuantity,
                    Salida = mov.ExitQuantity,
                    Existencia = mov.CurrentStock,
                    Costo = mov.Cost,
                    Promedio = mov.Average,
                    Debe = mov.Debit,
                    Hecho = mov.Credit,
                    Saldo = mov.Balance
                });
            }

            return Ok(kardex);
        }
    }
}