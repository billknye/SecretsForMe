namespace SecretsForMe.Core.Configuration.Persistence;

/// <summary>
/// Represents the persisted configuration data.
/// </summary>
/// <param name="SchemaVersion">The schema version of this configuration.</param>
/// <param name="AsymmetricKeys">A set of asymmetric keys.</param>
/// <param name="SymmetricKeys">A set of symmetric keys.</param>
/// <param name="ContentProviders">A set of content providers.</param>
public record PersistedConfiguration(
    Version SchemaVersion, 
    PersistedAsymmetricKey[] AsymmetricKeys, 
    PersistedSymmetricKey[] SymmetricKeys, 
    PersistedContentProvider[] ContentProviders);
