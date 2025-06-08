namespace DropcoreApi.Core.Types;

public record PasswordHash(byte[] Bytes)
{
    public string Base64 => Convert.ToBase64String(Bytes);

    public static implicit operator PasswordHash(Secret secret) => new(secret.Bytes);
}