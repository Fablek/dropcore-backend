using DropcoreApi.Core.Types;

public record ShaPasswordHashConfig(Secret Salt, Secret Peper);
