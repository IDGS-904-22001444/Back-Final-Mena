using AuthAPI.Data;
using AuthAPI.Dtos;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotationRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuotationRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/QuotationRequests/products
        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetAllProducts()
        {
            // Devuelve todos los productos, aunque no tengan stock
            var products = await _context.Products
                .Select(p => new
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    SalePrice = p.SalePrice,
                    Stock = p.Stock,
                    Status = p.Status
                })
                .ToListAsync();
            return Ok(products);
        }

        // POST: api/QuotationRequests
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] QuotationRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = new QuotationRequest
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email,
                Country = dto.Country,
                Region = dto.Region,
                Company = dto.Company,
                AnimalType = dto.AnimalType,
                NeedHabitatSystem = dto.NeedHabitatSystem,
                NeedBiologyResearch = dto.NeedBiologyResearch,
                NeedZoosAquariums = dto.NeedZoosAquariums,
                NeedNaturalReserves = dto.NeedNaturalReserves,
                NeedOther = dto.NeedOther,
                Comments = dto.Comments,
                AcceptsInfo = dto.AcceptsInfo,
                ProductId = dto.ProductId // Nuevo campo para el producto seleccionado
            };

            _context.QuotationRequests.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // GET: api/QuotationRequests
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<QuotationRequest>>> GetAll()
        {
            var list = await _context.QuotationRequests.ToListAsync();
            return Ok(list);
        }

        // GET: api/QuotationRequests/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var entity = await _context.QuotationRequests.FindAsync(id);
            if (entity == null)
                return NotFound();

            // Incluye el producto base cotizado en la respuesta
            var product = entity.ProductId.HasValue
                ? await _context.Products.FindAsync(entity.ProductId.Value)
                : null;

            return Ok(new
            {
                Quotation = entity,
                Product = product == null ? null : new
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    SalePrice = product.SalePrice
                }
            });
        }

        // PUT: api/QuotationRequests/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] QuotationRequestDto dto)
        {
            var entity = await _context.QuotationRequests.FindAsync(id);
            if (entity == null)
                return NotFound();

            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.Phone = dto.Phone;
            entity.Email = dto.Email;
            entity.Country = dto.Country;
            entity.Region = dto.Region;
            entity.Company = dto.Company;
            entity.AnimalType = dto.AnimalType;
            entity.NeedHabitatSystem = dto.NeedHabitatSystem;
            entity.NeedBiologyResearch = dto.NeedBiologyResearch;
            entity.NeedZoosAquariums = dto.NeedZoosAquariums;
            entity.NeedNaturalReserves = dto.NeedNaturalReserves;
            entity.NeedOther = dto.NeedOther;
            entity.Comments = dto.Comments;
            entity.AcceptsInfo = dto.AcceptsInfo;
            entity.ProductId = dto.ProductId; // Permite actualizar el producto cotizado

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // DELETE: api/QuotationRequests/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.QuotationRequests.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.QuotationRequests.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}