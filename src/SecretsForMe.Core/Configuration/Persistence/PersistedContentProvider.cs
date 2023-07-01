namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedContentProvider(
    Guid Id,
    Guid SymmetricKeyId,
    string Name,
    byte[] EncryptedProviderData);