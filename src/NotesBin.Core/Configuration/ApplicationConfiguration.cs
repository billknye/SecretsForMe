using NotesBin.Core.Cryptography.Symmetric;

namespace NotesBin.Core.Configuration;

public class ApplicationConfiguration
{
    private readonly List<SymmetricCryptoey> symmetricCryptoKeys;
    private readonly List<AsymmetricCryptoKey> asymmetricCryptoKey;

    public IReadOnlyCollection<SymmetricCryptoey> SymmetricCryptoKeys => symmetricCryptoKeys;
    public IReadOnlyCollection<AsymmetricCryptoKey> AsymmetricCryptoKeys => asymmetricCryptoKey;

    public ApplicationConfiguration()
    {
        symmetricCryptoKeys = new List<SymmetricCryptoey>();
        asymmetricCryptoKey = new List<AsymmetricCryptoKey>();
    }
}

public class SymmetricCryptoey
{
    public Guid Id { get; set; }
}

public class AsymmetricCryptoKey
{
    public Guid Id { get; set; }


}