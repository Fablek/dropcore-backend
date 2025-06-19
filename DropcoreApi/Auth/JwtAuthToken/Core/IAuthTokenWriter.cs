public interface IAuthTokenWriter
{
    AuthToken GenerateAuthToken(Account account);
}