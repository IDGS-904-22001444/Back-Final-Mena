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
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerReviews()
        {
            var reviews = await _context.CustomerReviews
                .Include(r => r.Client) // Incluir datos del cliente
                .Select(r => new
                {
                    Id = r.Id,
                    ClientId = r.ClientId,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    Reply = r.Reply,
                    RepliedAt = EF.Property<DateTime?>(r, "RepliedAt"),
                    Client = r.Client == null ? null : new
                    {
                        Id = r.Client.Id,
                        FullName = r.Client.FullName,
                        Email = r.Client.Email,
                        UserName = r.Client.UserName
                    }
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
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
                Rating = dto.Rating,
                CreatedAt = dto.CreatedAt // Se agrega el campo CreatedAt
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
            // Si tienes un campo RepliedAt, puedes agregarlo aquí:
            // review.RepliedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Respuesta guardada correctamente.", review });
        }
    }
}