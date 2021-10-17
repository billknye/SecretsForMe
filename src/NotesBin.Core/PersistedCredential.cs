namespace NotesBin.Core;

public record PersistedCredential(Guid Id, string Name, byte[] EncryptedAsymmetricKeyPrivateKey);
