using AuthAPI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentacionController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public DocumentacionController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // POST: api/documentacion/upload
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] UploadFileDto dto)
        {
            var file = dto.File;
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Solo se permiten archivos PDF.");

            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
                throw new InvalidOperationException("No se ha configurado el WebRootPath. Crea la carpeta wwwroot en la raíz del proyecto.");

            var docsPath = Path.Combine(webRootPath, "docs");
            if (!Directory.Exists(docsPath))
                Directory.CreateDirectory(docsPath);

            var filePath = Path.Combine(docsPath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { message = "Archivo subido correctamente.", fileName = file.FileName });
        }

        // GET: api/documentacion/download/{fileName}
        [HttpGet("download/{fileName}")]
        [AllowAnonymous]
        public IActionResult Download(string fileName)
        {
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
                return NotFound("No se ha configurado el WebRootPath. Crea la carpeta wwwroot en la raíz del proyecto.");

            var docsPath = Path.Combine(webRootPath, "docs");
            var filePath = Path.Combine(docsPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Archivo no encontrado.");

            var mimeType = "application/pdf";
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, mimeType, fileName);
        }

        // GET: api/documentacion/list
        [HttpGet("list")]
        [AllowAnonymous]
        public IActionResult List()
        {
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
                return NotFound("No se ha configurado el WebRootPath. Crea la carpeta wwwroot en la raíz del proyecto.");

            var docsPath = Path.Combine(webRootPath, "docs");
            if (!Directory.Exists(docsPath))
                return Ok(new string[0]);

            var files = Directory.GetFiles(docsPath, "*.pdf")
                                 .Select(Path.GetFileName)
                                 .ToArray();

            return Ok(files);
        }

        // DELETE: api/documentacion/delete/{fileName}
        [HttpDelete("delete/{fileName}")]
        [Authorize] // O [AllowAnonymous] si quieres que sea público
        public IActionResult Delete(string fileName)
        {
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
                return NotFound("No se ha configurado el WebRootPath. Crea la carpeta wwwroot en la raíz del proyecto.");

            var docsPath = Path.Combine(webRootPath, "docs");
            var filePath = Path.Combine(docsPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Archivo no encontrado.");

            System.IO.File.Delete(filePath);

            return Ok(new { message = "Archivo eliminado correctamente.", fileName });
        }
    }
}