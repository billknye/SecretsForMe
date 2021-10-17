namespace NotesBin.Core.Cryptography.Symmetric;

public abstract class SymmetricCryptoKey
{
    public Guid SymmetricCryptoKeyId { get; }

    public string Name { get; set; }

    public DateTimeOffset Created { get; set; }

    public SymmetricCryptoKey(Guid symmetricCryptoKeyId, string name)
    {
        this.SymmetricCryptoKeyId = symmetricCryptoKeyId;
        this.Name = name;
    }
}
