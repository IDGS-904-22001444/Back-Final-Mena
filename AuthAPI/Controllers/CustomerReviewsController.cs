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
    public class CustomerReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerReviewsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CustomerReview>>> GetAll()
        {
            return Ok(await _context.CustomerReviews.ToListAsync());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CustomerReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = new CustomerReview
            {
                ClientId = dto.ClientId,
                Comment = dto.Comment,
                Rating = dto.Rating
            };

            _context.CustomerReviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok(review);
        }


        [HttpPut("{id}/reply")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplyToReview(int id, [FromBody] ReplyDto dto)
        {
            var review = await _context.CustomerReviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Reseña no encontrada." });

            review.Reply = dto.Reply;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Respuesta guardada correctamente.", review });
        }
    }
}