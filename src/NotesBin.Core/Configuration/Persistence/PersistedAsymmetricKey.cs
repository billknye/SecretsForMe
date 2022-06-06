namespace NotesBin.Core.Configuration.Persistence;

public record PersistedAsymmetricKey(Guid Id, byte[] PublicKey, PersistedCredential[] Credentials, PersistedSymmetricKeyReference[] SymmetricKeyReferences);
