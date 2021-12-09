namespace NotesBin.Core.Persistence;

public record PersistedCredential(Guid Id, string Name, byte[] EncryptedAsymmetricKeyPrivateKey);
