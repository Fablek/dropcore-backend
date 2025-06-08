namespace DropcoreApi.Core.Types;

public record AuthToken(string Token)
{
    public static implicit operator AuthToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new AppException("Auth token can not be empty");

        return new(token);
    }
}