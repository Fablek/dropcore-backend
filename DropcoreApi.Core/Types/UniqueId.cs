namespace DropcoreApi.Core.Types;

public record UniqueId(Guid Guid)
{
    public static implicit operator UniqueId(Guid id) => new(id);

    public static UniqueId CreateNew() => new(Guid.NewGuid());
}
