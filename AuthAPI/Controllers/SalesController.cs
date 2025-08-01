using AuthAPI.Data;
using AuthAPI.Dtos;
using AuthAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;

        public SalesController(AppDbContext context, UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: api/Sales (Solo Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var sales = await _context.Sales
                .Include(s => s.Client)
                .Include(s => s.Product)
                .Select(s => new
                {
                    Id = s.Id,
                    ClientId = s.ClientId,
                    ProductId = s.ProductId,
                    Quantity = s.Quantity,
                    SaleDate = s.SaleDate,
                    Total = s.Total,
                    Status = s.Status,
                    Client = s.Client == null ? null : new
                    {
                        Id = s.Client.Id,
                        UserName = s.Client.UserName,
                        Email = s.Client.Email,
                        FullName = s.Client.FullName
                    },
                    Product = s.Product == null ? null : new
                    {
                        ProductId = s.Product.ProductId,
                        Name = s.Product.Name,
                        Description = s.Product.Description,
                        SalePrice = s.Product.SalePrice,
                        Stock = s.Product.Stock,
                        Status = s.Product.Status
                    }
                })
                .ToListAsync();

            return Ok(sales);
        }

        // GET: api/Sales/{id} (Solo Admin)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Client)
                .Include(s => s.Product)
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    Id = s.Id,
                    ClientId = s.ClientId,
                    ProductId = s.ProductId,
                    Quantity = s.Quantity,
                    SaleDate = s.SaleDate,
                    Total = s.Total,
                    Status = s.Status,
                    Client = s.Client == null ? null : new
                    {
                        Id = s.Client.Id,
                        UserName = s.Client.UserName,
                        Email = s.Client.Email,
                        FullName = s.Client.FullName
                    },
                    Product = s.Product == null ? null : new
                    {
                        ProductId = s.Product.ProductId,
                        Name = s.Product.Name,
                        Description = s.Product.Description,
                        SalePrice = s.Product.SalePrice,
                        Stock = s.Product.Stock,
                        Status = s.Product.Status
                    }
                })
                .FirstOrDefaultAsync();

            if (sale == null)
                return NotFound();

            return Ok(sale);
        }

        // POST: api/Sales (Público o autenticado)
        [HttpPost]
        [AllowAnonymous] // Cambia a [Authorize] si quieres restringir
        public async Task<IActionResult> Create([FromBody] SaleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(dto.ClientId);
            if (user == null)
                return BadRequest("El usuario especificado no existe.");

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
            product.Stock -= dto.Quantity;
            await _context.SaveChangesAsync();

            // --- Notificación al admin ---
            var adminSettings = _configuration.GetSection("AdminSettings");
            var adminEmail = adminSettings["AdminEmail"];
            var adminName = adminSettings["AdminName"];

            var mailSettings = _configuration.GetSection("MailSettings");
            var senderEmail = mailSettings["SenderEmail"];
            var senderName = mailSettings["SenderName"];
            var smtpServer = mailSettings["Server"];
            var smtpPort = int.Parse(mailSettings["Port"]);
            var smtpUser = mailSettings["UserName"];
            var smtpPass = mailSettings["Password"];

            var adminMessage = new MimeMessage();
            adminMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            adminMessage.To.Add(new MailboxAddress(adminName, adminEmail));
            adminMessage.Subject = "Nueva compra realizada";

            adminMessage.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>¡Nueva compra realizada!</h2>
                    <p>Cliente: {user.FullName} ({user.Email})</p>
                    <p>Producto: {product.Name}</p>
                    <p>Cantidad: {dto.Quantity}</p>
                    <p>Total: ${dto.Total}</p>
                    <p>Fecha: {dto.SaleDate:dd/MM/yyyy HH:mm}</p>
                "
            };

            // --- Ticket al cliente ---
            var clientMessage = new MimeMessage();
            clientMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            clientMessage.To.Add(new MailboxAddress(user.FullName ?? user.Email, user.Email));
            clientMessage.Subject = "¡Gracias por tu compra! - Ticket de compra";

            clientMessage.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family:Arial,sans-serif;'>
                        <h2>Ticket de compra</h2>
                        <p>Gracias por tu compra, {user.FullName}.</p>
                        <table style='border-collapse:collapse;'>
                            <tr><td><b>Producto:</b></td><td>{product.Name}</td></tr>
                            <tr><td><b>Cantidad:</b></td><td>{dto.Quantity}</td></tr>
                            <tr><td><b>Total:</b></td><td>${dto.Total}</td></tr>
                            <tr><td><b>Fecha:</b></td><td>{dto.SaleDate:dd/MM/yyyy HH:mm}</td></tr>
                        </table>
                        <p>ReptiTrack &copy; {DateTime.Now.Year}</p>
                    </div>
                "
            };

            // Enviar correos
            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(adminMessage);
                await smtp.SendAsync(clientMessage);
                await smtp.DisconnectAsync(true);
            }

            return Ok(sale);
        }

        // PUT: api/Sales/{id} (Solo Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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

        // GET: api/Sales/my (Cliente/Admin)
        [HttpGet("my")]
        [Authorize(Roles = "Cliente,Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetMySales()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var sales = await _context.Sales
                .Include(s => s.Product)
                .Where(s => s.ClientId == userId)
                .Select(s => new
                {
                    Id = s.Id,
                    ClientId = s.ClientId,
                    ProductId = s.ProductId,
                    Quantity = s.Quantity,
                    SaleDate = s.SaleDate,
                    Total = s.Total,
                    Status = s.Status,
                    Product = s.Product == null ? null : new
                    {
                        ProductId = s.Product.ProductId,
                        Name = s.Product.Name,
                        Description = s.Product.Description,
                        SalePrice = s.Product.SalePrice,
                        Stock = s.Product.Stock,
                        Status = s.Product.Status
                    }
                })
                .ToListAsync();

            return Ok(sales);
        }

        // DELETE: api/Sales/{id} (Solo Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return NotFound();

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