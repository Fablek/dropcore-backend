using Microsoft.AspNetCore.Mvc;

namespace FileService.DTOs;

public class FileUploadDto
{
    public IFormFile File { get; set; } = null!;
}
