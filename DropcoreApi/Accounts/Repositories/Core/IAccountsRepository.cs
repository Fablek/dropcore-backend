public interface IAccountsRepository : ICrudRepository<Account>
{
    Task<Account?> GetByUsername(Username username);
}
