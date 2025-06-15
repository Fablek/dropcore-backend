using FileService.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace FileService.Controllers;

[ApiController]
[Route("files")]
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
    public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
    {
        var file = dto.File;

        if (file == null || file.Length == 0)
            return BadRequest("Invalid file.");

        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", file.FileName);

        var response = await _httpClient.PostAsync("http://storage-node:5000/store", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        return Ok(await response.Content.ReadAsStringAsync());
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> Download(string fileName)
    {
        var response = await _httpClient.GetAsync($"http://storage-node:5000/store/{fileName}");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var stream = await response.Content.ReadAsStreamAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return File(stream, contentType, fileName);
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        var response = await _httpClient.DeleteAsync($"http://storage-node:5000/store/{fileName}");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        return Ok(await response.Content.ReadAsStringAsync());
    }
}
