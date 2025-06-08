namespace DropcoreApi.Core.Types;

public record Password(string Value)
{
    public static implicit operator Password(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new AppException("Password can not be empty");

        return new(password);
    }
}