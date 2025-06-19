public class AccountsService(IAccountsRepository accountsRepository, IPasswordHasher passwordHasher)
{
    public async Task<Account> Register(Username username, Password password)
    {
        if (await accountsRepository.GetByUsername(username) is not null)
            throw new AppException("Can not register account - account with this same username already exist");

        return await accountsRepository.CreateAndReturn(new Account(
            UniqueId: UniqueId.CreateNew(),
            Username: username,
            PasswordHash: passwordHasher.Hash(password)
        ));
    }
}
