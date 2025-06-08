using DropcoreApi.Core.Auth;
using DropcoreApi.Core.Models;
using DropcoreApi.Core.Types;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenWriter : IAuthTokenWriter
{
    readonly JwtSecurityTokenHandler _handler = new();

    public AuthToken GenerateAuthToken(Account account)
    {
        var token = new JwtSecurityToken(
            issuer: "localhost", 
            audience: "localhost", 
            notBefore: DateTime.UtcNow, 
            expires: DateTime.UtcNow.AddDays(1), 
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret for jwt token 123456 long long long long long")),
                SecurityAlgorithms.HmacSha256
            ),
            claims: [
                new Claim("user-id", account.UniqueId.Guid.ToString())    
            ]
        );

        return new AuthToken(_handler.WriteToken(token));
    }
}
