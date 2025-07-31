using AuthAPI.Data;
using AuthAPI.Dtos;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    //api/account
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        // api/account/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new AppUser
            {
                Email = registerDto.EmailAddress,
                FullName = registerDto.Fullname,
                UserName = registerDto.EmailAddress
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            if (registerDto.Roles is null)
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                foreach (var role in registerDto.Roles)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            // Enviar correo al administrador con mejor diseño
            var adminSettings = _configuration.GetSection("AdminSettings");
            var adminEmail = adminSettings["AdminEmail"];
            var adminName = adminSettings["AdminName"];

            var mailSettings = _configuration.GetSection("MailSettings");
            var email = mailSettings["SenderEmail"];
            var displayName = mailSettings["SenderName"];
            var smtpServer = mailSettings["Server"];
            var smtpPort = int.Parse(mailSettings["Port"]);
            var smtpUser = mailSettings["UserName"];
            var smtpPass = mailSettings["Password"];

            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(displayName, email));
            message.To.Add(new MimeKit.MailboxAddress(adminName, adminEmail));
            message.Subject = "Nuevo registro de usuario";

            var htmlBody = $@"
    <div style='max-width:600px;margin:30px auto;padding:30px 40px;background:#f9f9f9;border-radius:12px;box-shadow:0 2px 8px rgba(0,0,0,0.07);font-family:Segoe UI,Arial,sans-serif;color:#222;'>
        <div style='text-align:center;margin-bottom:24px;'>
            <img src='https://i.imgur.com/8Km9tLL.png' alt='ReptiTrack' style='width:60px;height:60px;border-radius:50%;background:#4CAF50;margin-bottom:10px;'/>
            <h2 style='color:#4CAF50;margin:0;'>Nuevo usuario registrado</h2>
        </div>
        <table style='width:100%;margin-bottom:24px;'>
            <tr>
                <td style='font-weight:bold;padding:8px 0;width:120px;'>Nombre:</td>
                <td style='padding:8px 0;'>{user.FullName}</td>
            </tr>
            <tr>
                <td style='font-weight:bold;padding:8px 0;'>Email:</td>
                <td style='padding:8px 0;'><a href='mailto:{user.Email}' style='color:#1976d2;text-decoration:none;'>{user.Email}</a></td>
            </tr>
        </table>
        <div style='margin-bottom:24px;'>
            <span style='display:inline-block;padding:10px 18px;background:#e3f2fd;color:#1976d2;border-radius:6px;font-size:15px;'>
                Revisa el panel de administración para asignar credenciales o activar la cuenta.
            </span>
        </div>
        <div style='text-align:center;color:#888;font-size:13px;margin-top:30px;'>
            ReptiTrack &copy; {DateTime.Now.Year}
        </div>
    </div>
    ";

            message.Body = new MimeKit.TextPart("html") { Text = htmlBody };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Account Created Sucessfully!!!"
            });
        }

        //api/account/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found with this email"
                });
            }

            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!result)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Password"
                });
            }

            var token = await GenerateToken(user);
            var refreshToken = GenerateRefreshToken();
            _ = int.TryParse(_configuration.GetSection("JWTSettings").GetSection("RefreshTokenValidityIn").Value!, out int RefreshTokenValidityIn);
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(RefreshTokenValidityIn);
            await _userManager.UpdateAsync(user);


            return Ok(new AuthResponseDto
            {
                Token = token,
                IsSuccess = true,
                Message = "Login Success",
                RefreshToken = refreshToken
            });
        }


        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // PUT: api/account/{id} (Solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserById(string id, [FromBody] UserDetailDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "User not found" });

            user.FullName = updateDto.FullName ?? user.FullName;
            user.Email = updateDto.Email ?? user.Email;
            user.UserName = updateDto.Email ?? user.UserName;
            user.PhoneNumber = updateDto.PhoneNumber ?? user.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario actualizado correctamente por el administrador" });
        }

        // DELETE: api/account/{id} (Solo Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "User not found" });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Usuario eliminado correctamente por el administrador" });
        }

        // api/account/forgot-password
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<AuthResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            if (user is null)
            {
                return Ok(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User does not exist with this email"
                });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"http://localhost:4200/reset-password?email={user.Email}&token={WebUtility.UrlEncode(token)}";

            // Leer configuración de correo
            var mailSettings = _configuration.GetSection("MailSettings");
            var email = mailSettings["SenderEmail"];
            var displayName = mailSettings["SenderName"];
            var smtpServer = mailSettings["Server"];
            var smtpPort = int.Parse(mailSettings["Port"]);
            var smtpUser = mailSettings["UserName"];
            var smtpPass = mailSettings["Password"];

            // Enviar correo con HTML
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(displayName, email));
            message.To.Add(new MimeKit.MailboxAddress(user.FullName ?? user.Email, user.Email));
            message.Subject = "Recuperación de contraseña";

            var htmlBody = $@"
        <div style='font-family: Arial, sans-serif; color: #333;'>
            <h2>Hola {user.FullName ?? user.Email},</h2>
            <p>Recibimos una solicitud para restablecer tu contraseña.</p>
            <p>
                <a href='{resetLink}' style='
                    display: inline-block;
                    padding: 10px 20px;
                    background-color: #4CAF50;
                    color: white;
                    text-decoration: none;
                    border-radius: 5px;
                    font-weight: bold;
                '>Restablecer contraseña</a>
            </p>
            <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
            <br>
            <p style='font-size:12px;color:#888;'>ReptiTrack &copy; {DateTime.Now.Year}</p>
        </div>";

            message.Body = new MimeKit.TextPart("html") { Text = htmlBody };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Se ha enviado un correo con las instrucciones para restablecer la contraseña."
            });
        }

       




        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto change)
        {
            var user = await _userManager.FindByEmailAsync(change.Email);
            if (user is null)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User does not exist with this email"
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, change.CurrentPassword, change.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Password changed successfully"
                });
            }

            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = result.Errors.FirstOrDefault()?.Description ?? "Error changing password"
            });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            //resetPasswordDto.Token = WebUtility.UrlDecode(resetPasswordDto.Token);

            if(user is null)
            {
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User does not exist with this email"
                });
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new AuthResponseDto
                {
                    IsSuccess = true,
                    Message = "Password reset Successfully"
                });
            }

            return BadRequest(new AuthResponseDto
            {
                IsSuccess = false,
                Message = result.Errors.FirstOrDefault()!.Description


            });
        }

        private async Task<string> GenerateToken(AppUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtSection = _configuration.GetSection("JWTSettings");
            var key = Encoding.ASCII.GetBytes(jwtSection["securityKey"]!);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new (JwtRegisteredClaimNames.Name, user.FullName ?? ""),
                new (JwtRegisteredClaimNames.NameId, user.Id ?? ""),
                new (JwtRegisteredClaimNames.Aud, jwtSection["ValidAudience"]!),
                new (JwtRegisteredClaimNames.Iss, jwtSection["ValidIssuer"]!)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        //api/account/detail
        [Authorize] // Sirve para proteger el servicio
        [HttpGet("detail")]
        public async Task<ActionResult<UserDetailDto>> GetUserDetail()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(currentUserId!);

            if (user == null)
            {
                return NotFound(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found"
                });
            }

            return Ok(new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = [.. await _userManager.GetRolesAsync(user)],
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                AccessFailedCount = user.AccessFailedCount
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDetailDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDetailDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles.ToArray(),
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    AccessFailedCount = user.AccessFailedCount
                });
            }

            return Ok(userDtos);
        }
    }
}
