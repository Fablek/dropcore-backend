using DropcoreApi.Core.Auth;
using DropcoreApi.Core.Types;
using System.Security.Cryptography;
using System.Text;

public class ShaPasswordHasher(ShaPasswordHashConfig config) : IPasswordHasher
{
    public PasswordHash Hash(Password password)
    {
        byte[] data = [.. config.Salt.Bytes, .. Encoding.UTF8.GetBytes(password.Value), .. config.Peper.Bytes];
        var hash = SHA3_512.HashData(data);

        return new PasswordHash(hash);
    }
}
