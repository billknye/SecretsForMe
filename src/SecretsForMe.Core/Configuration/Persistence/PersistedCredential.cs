namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents a persisted credential.
/// </summary>
/// <remarks>
/// Allows decryption of asymmetric private keys.
/// </remarks>
/// <param name="Id">The unique id of the credential.</param>
/// <param name="Name">A friendly, unencrypted name for this credential.</param>
/// <param name="EncryptedAsymmetricKeyPrivateKey">The encrypted private key this credential protects.</param>
public record PersistedCredential(
    Guid Id,
    string Name,
    byte[] EncryptedAsymmetricKeyPrivateKey);
