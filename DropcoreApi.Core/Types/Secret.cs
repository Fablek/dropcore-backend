using System.Text;

namespace DropcoreApi.Core.Types;

public record Secret(byte[] Bytes)
{
    public string Base64 => Convert.ToBase64String(Bytes);

    public static Secret FromBase64String(string base64) => new(Convert.FromBase64String(base64));
    public static Secret FromUtf8String(string utf8) => new(Encoding.UTF8.GetBytes(utf8));
}