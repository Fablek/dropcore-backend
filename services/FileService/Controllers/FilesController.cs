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

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized("Missing user email.");

        // --- Get user data from UserService
        var userResponse = await _httpClient.GetAsync($"http://user-service:5000/users/{email}");
        if (!userResponse.IsSuccessStatusCode)
            return StatusCode((int)userResponse.StatusCode, "UserService validation failed");

        var user = await userResponse.Content.ReadFromJsonAsync<UserDto>();
        if (user == null)
            return BadRequest("Could not read user data.");

        if (user.UsedSpace + file.Length > user.SpaceLimit)
            return BadRequest("Not enough available space.");

        // --- Upload file to storage node
        var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", uniqueName);

        var response = await _httpClient.PostAsync("http://storage-node:5000/store", content);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        // --- Save metadata
        var metadata = new FileMetadata
        {
            FileName = file.FileName,
            StorageFileName = uniqueName,
            ContentType = file.ContentType ?? "application/octet-stream",
            OwnerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
            FileSize = file.Length
        };

        db.Files.Add(metadata);
        await db.SaveChangesAsync();

        // --- Update used space
        await _httpClient.PostAsJsonAsync("http://user-service:5000/users/increase", new UsageUpdateDto
        {
            Email = email,
            Delta = file.Length
        });

        return Ok(metadata);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, [FromServices] FileDbContext db)
    {
        var file = await db.Files.FindAsync(id);
        if (file == null)
            return NotFound();

        var response = await _httpClient.GetAsync($"http://storage-node:5000/store/{file.StorageFileName}");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var stream = await response.Content.ReadAsStreamAsync();
        return File(stream, file.ContentType, file.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromServices] FileDbContext db)
    {
        var file = await db.Files.FindAsync(id);
        if (file == null)
            return NotFound();

        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (file.OwnerId != ownerId)
            return Forbid();

        var response = await _httpClient.DeleteAsync($"http://storage-node:5000/store/{file.StorageFileName}");
        if (response.StatusCode != System.Net.HttpStatusCode.NoContent &&
            response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            return StatusCode((int)response.StatusCode);
        }

        db.Files.Remove(file);
        await db.SaveChangesAsync();

        await _httpClient.PostAsJsonAsync("http://user-service:5000/users/decrease", new UsageUpdateDto
        {
            Email = email!,
            Delta = file.FileSize 
        });

        return Ok("Deleted");
    }

    [HttpGet]
    public async Task<IActionResult> List([FromServices] FileDbContext db)
    {
        var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var files = await db.Files.Where(f => f.OwnerId == ownerId).ToListAsync();
        return Ok(files);
    }
}
