using System.Security.Claims;

public static class LifeCheckersEndpoints
{
    public static IResult Hello() => Results.Text($"Hello {DateTime.Now}");
    public static IResult HelloWithAuth(ClaimsPrincipal claims) => Results.Text($"Hello {claims.Claims.Single(c => c.Type == "user-id").Value} {DateTime.Now}");
}