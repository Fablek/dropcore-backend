namespace DropcoreApi.Core.Shared;

public class CrudRepository<TEntity> : ICrudRepository<TEntity> where TEntity : IEntity
{
    protected List<TEntity> Entities { get; } = [];

    public Task<TEntity?> GetByUniqueId(UniqueId id) => Task.FromResult(Entities.FirstOrDefault(e => e.UniqueId == id));

    public Task Create(TEntity entity)
    {
        Entities.Add(entity);
        return Task.CompletedTask;
    }

    public Task Delete(UniqueId id)
    {
        Entities.RemoveAll(e => e.UniqueId == id);
        return Task.CompletedTask;
    }

    public Task Update(TEntity entity)
    {
        Delete(entity.UniqueId);
        Create(entity);

        return Task.CompletedTask;
    }
}