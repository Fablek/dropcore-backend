using Microsoft.AspNetCore.Mvc;

namespace FileService.DTOs;

public class FileUploadDto
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;
}
