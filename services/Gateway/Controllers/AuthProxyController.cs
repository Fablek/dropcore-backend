using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[Route("auth")]
public class AuthProxyController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public AuthProxyController(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] object body)
    {
        var url = $"{_config["Services:AuthService"]}/auth/login";
        var response = await _http.PostAsJsonAsync(url, body);

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json", System.Text.Encoding.UTF8);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] object body)
    {
        var url = $"{_config["Services:AuthService"]}/auth/register";
        var response = await _http.PostAsJsonAsync(url, body);

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json", System.Text.Encoding.UTF8);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var url = $"{_config["Services:AuthService"]}/auth/me";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (Request.Headers.TryGetValue("Authorization", out var token))
        {
            request.Headers.Add("Authorization", token.ToString());
        }

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json", System.Text.Encoding.UTF8);
    }
}
