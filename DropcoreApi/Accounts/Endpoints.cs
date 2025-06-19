using Microsoft.AspNetCore.Mvc;

public static class AccountsEndpoints
{
    public static async Task<IResult> Register(HttpContext http, [FromBody] RegisterRequestDto request, IAuthTokenWriter authTokenWriter, AccountsService accountsService)
    {
        var account = await accountsService.Register(request.Username, request.Password);
        var token = authTokenWriter.GenerateAuthToken(account);

        SetAuthToken(http, token);

        return Results.Ok(new
        {
            AuthToken = token
        });
    }

    public record RegisterRequestDto(
        string Username,
        string Password
    );

    public static async Task<IResult> Login(HttpContext http, [FromBody] LoginRequestDto request, AuthService authService)
    {
        var token = await authService.Authenticate(request.Username, request.Password);
        SetAuthToken(http, token);

        return Results.Ok(new
        {
            AuthToken = token
        });
    }

    public record LoginRequestDto(
        string Username,
        string Password
    );

    static void SetAuthToken(HttpContext http, AuthToken token)
    {
        http.Response.Cookies.Append("auth", token.Token, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });
    }
}