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
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetAll()
        {
            var sales = await _context.Sales.ToListAsync();
            return Ok(sales);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetById(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return NotFound();
            return Ok(sale);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return BadRequest("El producto especificado no existe.");

            if (product.Stock < dto.Quantity)
                return BadRequest("Stock insuficiente para realizar la venta.");

            var sale = new Sale
            {
                ClientId = dto.ClientId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                SaleDate = dto.SaleDate,
                Total = dto.Total,
                Status = dto.Status
            };

            _context.Sales.Add(sale);

            // Descontar stock
            product.Stock -= dto.Quantity;

            await _context.SaveChangesAsync();
            return Ok(sale);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaleDto dto)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return NotFound();

            // Si cambia el producto, revertir stock en el anterior y descontar en el nuevo
            if (sale.ProductId != dto.ProductId)
            {
                var oldProduct = await _context.Products.FindAsync(sale.ProductId);
                if (oldProduct != null)
                    oldProduct.Stock += sale.Quantity;

                var newProduct = await _context.Products.FindAsync(dto.ProductId);
                if (newProduct == null)
                    return BadRequest("El nuevo producto especificado no existe.");

                if (newProduct.Stock < dto.Quantity)
                    return BadRequest("Stock insuficiente para realizar la venta.");

                newProduct.Stock -= dto.Quantity;

                sale.ProductId = dto.ProductId;
                sale.Quantity = dto.Quantity;
            }
            else
            {
                // Si no cambia el producto, ajustar la diferencia de cantidad
                var product = await _context.Products.FindAsync(sale.ProductId);
                if (product == null)
                    return BadRequest("El producto especificado no existe.");

                int diferencia = dto.Quantity - sale.Quantity;
                if (diferencia > 0 && product.Stock < diferencia)
                    return BadRequest("Stock insuficiente para aumentar la cantidad de venta.");

                product.Stock -= diferencia;
                sale.Quantity = dto.Quantity;
            }

            sale.ClientId = dto.ClientId;
            sale.SaleDate = dto.SaleDate;
            sale.Total = dto.Total;
            sale.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(sale);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return NotFound();

            // Devolver stock al producto
            var product = await _context.Products.FindAsync(sale.ProductId);
            if (product != null)
            {
                product.Stock += sale.Quantity;
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}