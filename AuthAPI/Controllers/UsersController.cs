using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AuthAPI.Model;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/Users/GetNumericId/{guid}
        [HttpGet("GetNumericId/{guid}")]
        public async Task<IActionResult> GetNumericId(string guid)
        {
            var user = await _userManager.FindByIdAsync(guid);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            // Genera el ID numérico usando el mismo algoritmo que en Angular
            int numericId = GenerateFallbackId(guid);

            return Ok(new { numericId });
        }

        // Algoritmo de hash igual al de Angular
        private int GenerateFallbackId(string guid)
        {
            int hash = 0;
            foreach (var c in guid)
            {
                hash = ((hash << 5) - hash) + c;
                hash = hash & hash; // Convertir a int32
            }
            return Math.Abs(hash) % 1000000; // Limitar a 6 dígitos
        }
    }
}