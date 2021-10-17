
namespace NotesBin.Core.Cryptography.Asymmetric;

public abstract class AsymmetricCryptoKey
{
    public Guid AsymmetricCryptoKeyId { get; }

    public string Name { get; set; }

    public AsymmetricCryptoKey(Guid asymmetricCryptoKeyId, string name)
    {
        AsymmetricCryptoKeyId = asymmetricCryptoKeyId;
        Name = name;
    }
}
