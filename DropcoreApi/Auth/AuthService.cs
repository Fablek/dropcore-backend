public class AuthService(IAccountsRepository accountsRepository, IPasswordHasher passwordHasher, IAuthTokenWriter authTokenWriter)
{
    public async Task<AuthToken> Authenticate(Username username, Password password)
    {
        if (await accountsRepository.GetByUsername(username) is Account account && account.PasswordHash.Base64 == passwordHasher.Hash(password).Base64)
            return authTokenWriter.GenerateAuthToken(account);

        throw new AppException("Username or password are incorrect");
    }
}
