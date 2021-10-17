namespace NotesBin.Core;

public record PersistedSymmetricKey(Guid Id, byte[] EncryptedSymmetricKeyMetadata);
