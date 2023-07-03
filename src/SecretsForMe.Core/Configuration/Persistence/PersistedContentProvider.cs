namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents a persisted content provider.
/// </summary>
/// <param name="Id">The id of the provider.</param>
/// <param name="SymmetricKeyId">The symmetric key used to encrypt data for this provider.</param>
/// <param name="Name">A friendly, unencrypted name for the provider.</param>
/// <param name="EncryptedProviderData">Provider specific data, encrypted with the symmetric key.</param>
public record PersistedContentProvider(
    Guid Id,
    Guid SymmetricKeyId,
    string Name,
    byte[] EncryptedProviderData);