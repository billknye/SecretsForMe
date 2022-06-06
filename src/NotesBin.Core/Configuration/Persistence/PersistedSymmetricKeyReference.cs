namespace NotesBin.Core.Configuration.Persistence;

public record PersistedSymmetricKeyReference(Guid SymmetricKeyId, byte[] EncryptedSymmetricKey);
