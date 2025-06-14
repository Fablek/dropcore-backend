using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("files")]
public class FilesController : ControllerBase
{
    private readonly string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage");

    public FilesController()
    {
        if (!Directory.Exists(storagePath))
            Directory.CreateDirectory(storagePath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var filePath = Path.Combine(storagePath, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "File uploaded", file = file.FileName });
    }

    [HttpGet("{fileName}")]
    public IActionResult Download(string fileName)
    {
        var filePath = Path.Combine(storagePath, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/octet-stream", fileName);
    }

    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName)
    {
        var filePath = Path.Combine(storagePath, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        System.IO.File.Delete(filePath);
        return Ok(new { message = "File deleted" });
    }
}
