namespace NotesBin.Core.Persistence;

public record PersistedSymmetricKeyReference(Guid SymmetricKeyId, byte[] EncryptedSymmetricKey);
