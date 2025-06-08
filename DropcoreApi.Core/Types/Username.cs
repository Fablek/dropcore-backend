namespace DropcoreApi.Core.Types;

public record Username(string Value)
{
    public static implicit operator Username(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new AppException("Username can not be empty");

        return new(username);
    }
}