namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedSymmetricKeyReference(Guid SymmetricKeyId, byte[] EncryptedSymmetricKey);
