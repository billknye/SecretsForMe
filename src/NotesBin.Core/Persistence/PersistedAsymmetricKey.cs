namespace NotesBin.Core.Persistence;

public record PersistedAsymmetricKey(Guid Id, byte[] PublicKey, PersistedCredential[] Credentials, PersistedSymmetricKeyReference[] SymmetricKeyReferences);
