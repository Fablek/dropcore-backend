using DropcoreApi.Core.Models;
using DropcoreApi.Core.Shared;
using DropcoreApi.Core.Types;

public class AccountsCrudRepository : CrudRepository<Account>, IAccountsRepository
{
    public Task<Account?> GetByUsername(Username username) => Task.FromResult(Entities.FirstOrDefault(e => e.Username == username));
}
