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
    public class FaqsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FaqsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Faq>>> GetAll()
        {
            return Ok(await _context.Faqs.ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] FaqDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var faq = new Faq
            {
                Question = dto.Question,
                Answer = dto.Answer
            };

            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();
            return Ok(faq);
        }
    }
}
