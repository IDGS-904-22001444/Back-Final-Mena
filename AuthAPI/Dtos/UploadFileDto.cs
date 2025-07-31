using Microsoft.AspNetCore.Http;

namespace AuthAPI.Dtos
{
    public class UploadFileDto
    {
        public IFormFile File { get; set; }
    }
}