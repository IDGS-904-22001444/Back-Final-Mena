using AuthAPI.Dtos;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            if (string.IsNullOrEmpty(createRoleDto.RoleName))
            {
                return BadRequest("El nombre del Rol es requerido");
            }

            var roleExist = await _roleManager.RoleExistsAsync(createRoleDto.RoleName);
            if (roleExist)
            {
                return BadRequest("El Rol ya existe");
            }

            var roleResult = await _roleManager.CreateAsync(new IdentityRole(createRoleDto.RoleName));
            if (roleResult.Succeeded)
            {
                return Ok(new { message = "Rol creado con exito" });
            }

            return BadRequest("La creacion del Rol fallo");
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleDtos = new List<RoleResponseDto>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                roleDtos.Add(new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    TotalUsers = usersInRole.Count
                });
            }

            return Ok(roleDtos);
        }

        [HttpDelete("{id}")]

        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFound("El Rol no fue encontrado ");
            }
            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                return Ok(new { message = "Rol elimimnado correctamente" });
            }
            return BadRequest("El Rol no se pudo eliminar correctamente");
        }

        [HttpPost("assing")]
        public async Task<IActionResult> AssignRole([FromBody] RolesAssingDto rolesAssingDto)
        {
            // Buscar usuario por Id (el email está en UserName)
            var user = await _userManager.FindByIdAsync(rolesAssingDto.UserId);
            if (user is null)
            {
                return NotFound("El usuario no fue encontrado");
            }

            // Buscar rol por nombre (roleId es el nombre del rol)
            var role = await _roleManager.FindByNameAsync(rolesAssingDto.RoleId);
            if (role is null)
            {
                return NotFound("El Rol no fue encontrado");
            }

            var result = await _userManager.AddToRoleAsync(user, role.Name!);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault();
                return BadRequest(error?.Description ?? "No se pudo asignar el rol.");
            }

            // Configuración de correo
            try
            {
                var mailSettings = HttpContext.RequestServices.GetService<IConfiguration>()!.GetSection("MailSettings");
                var senderEmail = mailSettings["SenderEmail"];
                var senderName = mailSettings["SenderName"];
                var smtpServer = mailSettings["Server"];
                var smtpPort = int.Parse(mailSettings["Port"]);
                var smtpUser = mailSettings["UserName"];
                var smtpPass = mailSettings["Password"];

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
                // Usar UserName como email de destino (es el email en tu base)
                message.To.Add(new MimeKit.MailboxAddress(user.FullName ?? user.UserName, user.UserName));
                message.Subject = "¡Nuevo rol asignado en ReptiTrack!";

                var htmlBody = $@"
<div style='max-width:600px;margin:30px auto;padding:0;background:#f9f9f9;border-radius:16px;box-shadow:0 2px 8px rgba(0,0,0,0.07);font-family:Segoe UI,Arial,sans-serif;color:#222;'>
    <div style='background:#fafafa;border-radius:16px;padding:40px 40px 32px 40px;text-align:center;'>
        <div style='margin-bottom:18px;'>
            <div style='width:70px;height:70px;border-radius:50%;background:#1976d2;display:inline-block;line-height:70px;font-size:32px;color:#fff;font-weight:bold;'>
                {(string.IsNullOrWhiteSpace(user.FullName) ? "U" : user.FullName.Substring(0, 1).ToUpper())}
            </div>
        </div>
        <h2 style='color:#43a047;font-size:1.7em;margin-bottom:24px;margin-top:0;'>¡Nuevo rol asignado!</h2>
        <p style='font-size:1.1em;'>Hola <b>{user.FullName ?? user.UserName}</b>,</p>
        <p style='margin:18px 0 24px 0;'>Te informamos que se te ha asignado el siguiente rol en la plataforma:</p>
        <div style='display:inline-block;padding:14px 32px;background:#e3f2fd;color:#1976d2;border-radius:8px;font-size:18px;font-weight:bold;margin-bottom:24px;'>
            {role.Name}
        </div>
        <p style='margin:24px 0 0 0;'>Accede a tu cuenta para realizar tus compras, gracias por confiar en ReptiTrack.</p>
        <div style='text-align:center;color:#888;font-size:14px;margin-top:30px;'>
            ReptiTrack &copy; {DateTime.Now.Year}
        </div>
    </div>
</div>
";

                message.Body = new MimeKit.TextPart("html") { Text = htmlBody };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Si falla el envío de correo, el rol ya fue asignado, pero se notifica el error de correo
                return Ok(new
                {
                    message = "Rol asignado correctamente, pero hubo un error al enviar la notificación por correo.",
                    error = ex.Message
                });
            }

            return Ok(new { message = "Rol asignado correctamente y notificación enviada" });
        }
    }
}
