namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents a persisted asymmetric key.
/// </summary>
/// <remarks>
/// Allows decryption of symmetric keys.
/// </remarks>
/// <param name="Id">The unique id of this key.</param>
/// <param name="PublicKey">The public bytes of this key.</param>
/// <param name="Credentials">A set of credentials that can be used to obtain the private key.</param>
/// <param name="SymmetricKeyReferences">A set of symmetric key references that can be decrypted with this asymmetric key.</param>
public record PersistedAsymmetricKey(
    Guid Id,
    byte[] PublicKey,
    PersistedCredential[] Credentials,
    PersistedSymmetricKeyReference[] SymmetricKeyReferences);
