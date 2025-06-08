using DropcoreApi.Core.Auth;
using DropcoreApi.Core.Exceptions;
using DropcoreApi.Core.Models;
using DropcoreApi.Core.Types;

public class AccountsService(IAccountsRepository accountsRepository, IPasswordHasher passwordHasher)
{
    public async Task<Account> Register(Username username, Password password)
    {
        if (await accountsRepository.GetByUsername(username) is not null)
            throw new AppException("Can not register account - account with this same username already exist");

        return await accountsRepository.CreateAndReturn(new Account(
            UniqueId: DropcoreApi.Core.Types.UniqueId.CreateNew(),
            Username: username,
            PasswordHash: passwordHasher.Hash(password)
        ));
    }
}
