namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents a persisted symmetric key reference.
/// </summary>
/// <remarks>
/// Used by aymmetric keys to decrypt the symmetric keys.
/// </remarks>
/// <param name="SymmetricKeyId">The id of the symmetric key being referenced.</param>
/// <param name="EncryptedSymmetricKey">The encrypted symmetric key data.</param>
public record PersistedSymmetricKeyReference(
    Guid SymmetricKeyId,
    byte[] EncryptedSymmetricKey);
