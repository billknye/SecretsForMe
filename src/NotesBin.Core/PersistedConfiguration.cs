namespace NotesBin.Core;

public record PersistedConfiguration(PersistedAsymmetricKey[] AsymmetricKeys, PersistedSymmetricKey[] SymmetricKeys);
