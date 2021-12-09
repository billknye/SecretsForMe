namespace NotesBin.Core.Persistence;

public record PersistedConfiguration(PersistedAsymmetricKey[] AsymmetricKeys, PersistedSymmetricKey[] SymmetricKeys);
