using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

public class AccountsMongoRepository(IMongoDatabase db) : IAccountsRepository
{
    readonly IMongoCollection<AccountMongoEntity> _usersCollection = db.GetCollection<AccountMongoEntity>("users");

    public async Task Create(Account entity)
    {
        if (await GetByUniqueId(entity.UniqueId) is not null || await GetByUsername(entity.Username) is not null)
            throw new Exception($"Can not create user with username '{entity.Username.Value}' and unique id {entity.UniqueId.Guid}");

        await _usersCollection.InsertOneAsync(AccountMongoEntity.FromModel(entity));
    }

    public async Task Delete(DropcoreApi.Core.Types.UniqueId id)
    {
        await _usersCollection.DeleteOneAsync(a => a.UniqueId == id.Guid);
    }

    public async Task<Account?> GetByUniqueId(DropcoreApi.Core.Types.UniqueId id)
    {
        var account = await _usersCollection.Find(a => a.UniqueId == id.Guid).SingleOrDefaultAsync();

        return account?.ToModel();
    }

    public async Task<Account?> GetByUsername(Username username)
    {
        var account = await _usersCollection.Find(a => a.Username == username.Value).SingleOrDefaultAsync();

        return account?.ToModel();
    }

    public async Task Update(Account entity)
    {
        await _usersCollection.FindOneAndUpdateAsync(a => a.UniqueId == entity.UniqueId.Guid, Builders<AccountMongoEntity>.Update
            .Set(a => a.Username, entity.Username)
            .Set(a => a.PasswordHashBase64, entity.PasswordHash.Base64)
        );
    }

    class AccountMongoEntity {
        public ObjectId Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UniqueId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHashBase64 { get; set; } = string.Empty;

        public static AccountMongoEntity FromModel(Account account) => new() { Id = ObjectId.Empty, UniqueId = account.UniqueId.Guid, Username = account.Username.Value, PasswordHashBase64 = account.PasswordHash.Base64 };
        public Account ToModel() => new(UniqueId, Username, Secret.FromBase64String(PasswordHashBase64));
    }
}