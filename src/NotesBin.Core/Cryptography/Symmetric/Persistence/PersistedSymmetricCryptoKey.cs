namespace NotesBin.Core.Cryptography.Symmetric.Persistence;

public record PersistedSymmetricCryptoKey(Guid symmetricCryptoKeyId, byte[] EncryptedData);