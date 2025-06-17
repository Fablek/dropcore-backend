using FileService.Data;
using FileService.DTOs;
using FileService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Security.Claims;

namespace FileService.Controllers;

[ApiController]
[Route("files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage");

    public FilesController([FromServices] IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();

        if (!Directory.Exists(storagePath))
            Directory.CreateDirectory(storagePath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] FileUploadDto dto, [FromServices] FileDbContext db)
    {
        var file = dto.File;

        if (file == null || file.Length == 0)
            return BadRequest("Invalid file.");

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", uniqueFileName);

        var response = await _httpClient.PostAsync("http://storage-node:5000/store", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var metadata = new FileMetadata
        {
            FileName = file.FileName,
            StorageFileName = uniqueFileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            OwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown"
        };

        db.Files.Add(metadata);
        await db.SaveChangesAsync();

        return Ok(metadata);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, [FromServices] FileDbContext db)
    {
        var file = await db.Files.FindAsync(id);
        if (file == null)
            return NotFound("File not found.");

        var response = await _httpClient.GetAsync($"http://storage-node:5000/store/{file.StorageFileName}");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var stream = await response.Content.ReadAsStreamAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return File(stream, contentType, file.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] FileDbContext db)
    {
        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var entity = await db.Files.FindAsync(id);

        if (entity == null)
            return NotFound("File not found in database.");

        if (entity.OwnerId != ownerId)
            return Forbid("You do not have permission to delete this file.");

        var response = await _httpClient.DeleteAsync($"http://storage-node:5000/store/{entity.StorageFileName}");

        if (response.StatusCode != System.Net.HttpStatusCode.NoContent &&
            response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        db.Files.Remove(entity);
        await db.SaveChangesAsync();

        return Ok("File metadata deleted.");
    }

    [HttpGet]
    public async Task<IActionResult> List([FromServices] FileDbContext db)
    {
        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var files = await db.Files
            .Where(f => f.OwnerId == ownerId)
            .ToListAsync();

        return Ok(files);
    }
}
