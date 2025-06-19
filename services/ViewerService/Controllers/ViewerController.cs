using Microsoft.AspNetCore.Mvc;

namespace ViewerService.Controllers;

[ApiController]
[Route("view")]
public class ViewerController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ViewerController(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> Preview(Guid fileId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://file-service:5000/files/{fileId}");

        var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Missing token");

        request.Headers.Add("Authorization", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();

        if (contentType.StartsWith("text/"))
        {
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            return Ok(new { type = "text", content = text });
        }

        if (contentType.StartsWith("image/"))
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            return Ok(new { type = "image", contentType, base64 });
        }

        return Ok(new
        {
            type = "unsupported",
            message = $"File type '{contentType}' not supported by viewer."
        });
    }
}
