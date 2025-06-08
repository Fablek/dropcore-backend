namespace DropcoreApi.Core.Models;

public record Account(UniqueId UniqueId, Username Username, PasswordHash PasswordHash) : IEntity;