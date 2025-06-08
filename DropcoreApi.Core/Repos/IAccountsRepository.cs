using DropcoreApi.Core.Models;
using DropcoreApi.Core.Shared;
using DropcoreApi.Core.Types;

public interface IAccountsRepository : ICrudRepository<Account>
{
    Task<Account?> GetByUsername(Username username);
}
