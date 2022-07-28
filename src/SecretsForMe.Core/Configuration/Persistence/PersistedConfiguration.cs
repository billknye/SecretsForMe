namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedConfiguration(Version SchemaVersion, PersistedAsymmetricKey[] AsymmetricKeys, PersistedSymmetricKey[] SymmetricKeys, PersistedContentProvider[] ContentProviders);
