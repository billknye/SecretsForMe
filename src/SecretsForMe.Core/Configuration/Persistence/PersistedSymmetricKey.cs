namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents a persisted symmetric key.
/// </summary>
/// <param name="Id">The unique id of this key.</param>
/// <param name="EncryptedSymmetricKeyMetadata">Data about this symmetric key, encrypted with itself.</param>
public record PersistedSymmetricKey(
    Guid Id,
    byte[] EncryptedSymmetricKeyMetadata);
