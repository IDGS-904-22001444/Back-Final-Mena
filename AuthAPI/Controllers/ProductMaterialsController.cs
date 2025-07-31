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
    public class ProductMaterialsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductMaterialsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductMaterial>>> GetAll()
        {
            var list = await _context.ProductMaterials
                .Include(pm => pm.Product)
                .Include(pm => pm.RawMaterial)
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductMaterial>> GetById(int id)
        {
            var item = await _context.ProductMaterials
                .Include(pm => pm.Product)
                .Include(pm => pm.RawMaterial)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductMaterialDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = new ProductMaterial
            {
                ProductId = dto.ProductId,
                RawMaterialId = dto.RawMaterialId,
                RequiredQuantity = dto.RequiredQuantity,
                Status = dto.Status
            };

            _context.ProductMaterials.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductMaterialDto dto)
        {
            var entity = await _context.ProductMaterials.FindAsync(id);
            if (entity == null)
                return NotFound();

            entity.ProductId = dto.ProductId;
            entity.RawMaterialId = dto.RawMaterialId;
            entity.RequiredQuantity = dto.RequiredQuantity;
            entity.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.ProductMaterials.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.ProductMaterials.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}