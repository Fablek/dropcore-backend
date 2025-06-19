public class AccountsInMemoryRepository : CrudRepository<Account>, IAccountsRepository
{
    public Task<Account?> GetByUsername(Username username) => Task.FromResult(Entities.FirstOrDefault(e => e.Username == username));
}
