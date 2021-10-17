namespace NotesBin.Core;

public record PersistedSymmetricKeyReference(Guid SymmetricKeyId, byte[] EncryptedSymmetricKey);
