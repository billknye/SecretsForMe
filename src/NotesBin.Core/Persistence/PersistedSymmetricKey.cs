namespace NotesBin.Core.Persistence;

public record PersistedSymmetricKey(Guid Id, byte[] EncryptedSymmetricKeyMetadata);
