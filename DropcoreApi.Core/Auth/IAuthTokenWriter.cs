namespace DropcoreApi.Core.Auth;

public interface IAuthTokenWriter
{
    AuthToken GenerateAuthToken(Account account);
}