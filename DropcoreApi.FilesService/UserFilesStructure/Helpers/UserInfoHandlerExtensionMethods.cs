using DropcoreApi.Core.Types;
using System.Security.Claims;

public static class UserInfoHandlerExtensionMethods
{
    public static UniqueId GetUserUniqueId(this ClaimsPrincipal principal)
    {
        var value = principal.Claims.Single(c => c.Type == "user-id").Value;
        return Guid.Parse(value);
    }
}
