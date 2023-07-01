namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedSymmetricKey(
    Guid Id,
    byte[] EncryptedSymmetricKeyMetadata);
