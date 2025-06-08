namespace DropcoreApi.Core.Shared;

public interface ICrudRepository<TEntity> where TEntity : IEntity
{
    Task<TEntity?> GetByUniqueId(UniqueId id);

    Task Create(TEntity entity);

    async Task<TEntity> CreateAndReturn(TEntity entity)
    {
        await Create(entity);
        return entity;    
    }

    Task Update(TEntity entity);
    Task Delete(UniqueId id);

    Task Delete(TEntity entity) => Delete(entity.UniqueId);
}
