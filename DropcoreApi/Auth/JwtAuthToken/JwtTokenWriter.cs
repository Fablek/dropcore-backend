using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class JwtTokenWriter : IAuthTokenWriter
{
    readonly JwtSecurityTokenHandler _handler = new();

    public AuthToken GenerateAuthToken(Account account)
    {
        var token = new JwtSecurityToken(
            notBefore: DateTime.UtcNow, 
            expires: DateTime.UtcNow.AddDays(1), 
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(SettingsHelper.GetAuthTokenKey().Bytes),
                SecurityAlgorithms.HmacSha256
            ),
            claims: [
                new Claim("user-id", account.UniqueId.Guid.ToString())    
            ]
        );

        return new AuthToken(_handler.WriteToken(token));
    }
}
