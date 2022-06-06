namespace NotesBin.Core.Configuration.Persistence;

public record PersistedSymmetricKey(Guid Id, byte[] EncryptedSymmetricKeyMetadata);
