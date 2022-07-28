namespace SecretsForMe.Core.Configuration.Persistence;

public record PersistedConfiguration(PersistedAsymmetricKey[] AsymmetricKeys, PersistedSymmetricKey[] SymmetricKeys, PersistedContentProvider[] ContentProviders);
