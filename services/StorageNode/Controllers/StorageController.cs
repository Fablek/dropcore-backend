using Microsoft.AspNetCore.Mvc;

namespace StorageNode.Controllers;

[ApiController]
[Route("store")]
public class StorageController : ControllerBase
{
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage");

    public StorageController()
    {
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Invalid file.");

        var filePath = Path.Combine(_storagePath, file.FileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Ok(new { message = $"Saved {file.FileName}" });
    }

    [HttpGet("{fileName}")]
    public IActionResult Get(string fileName)
    {
        var path = Path.Combine(_storagePath, fileName);
        if (!System.IO.File.Exists(path))
            return NotFound("File not found.");

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        return File(fileStream, "application/octet-stream", fileName);
    }

    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName)
    {
        var path = Path.Combine(_storagePath, fileName);
        if (!System.IO.File.Exists(path))
            return NotFound("File not found.");

        System.IO.File.Delete(path);
        return Ok(new { message = $"Deleted {fileName}" });
    }
}
