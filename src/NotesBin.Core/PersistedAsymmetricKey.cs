namespace NotesBin.Core;

public record PersistedAsymmetricKey(Guid Id, byte[] PublicKey, PersistedCredential[] Credentials, PersistedSymmetricKeyReference[] SymmetricKeyReferences);
