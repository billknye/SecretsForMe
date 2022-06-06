namespace NotesBin.Core.Configuration;

public class AsymmetricKey
{
    public Guid Id { get; set; }

    public byte[] PublicKey { get; set; }

    protected AsymmetricKey(Guid id, byte[] publicKey)
    {
        Id = id;
        PublicKey = publicKey;
    }
}

public class LoadedAsymmetricKey : AsymmetricKey
{
    public byte[] PrivateKey { get; }

    public List<Credential> Credentials { get; set; }

    public List<SymmetricKeyReference> SymmetricKeyReferences { get; set; }

    public LoadedAsymmetricKey(Guid id, byte[] publicKey, byte[] privateKey)
        : base(id, publicKey)
    {
        PrivateKey = privateKey;
        Credentials = new List<Credential>();
        SymmetricKeyReferences = new List<SymmetricKeyReference>();
    }

}

public class PassThroughAsymmetricKey : AsymmetricKey
{
    public Persistence.PersistedAsymmetricKey PersistedAsymmetricKey { get; }

    public PassThroughAsymmetricKey(Persistence.PersistedAsymmetricKey persistedAsymmetricKey)
        : base(persistedAsymmetricKey.Id, persistedAsymmetricKey.PublicKey)
    {
        PersistedAsymmetricKey = persistedAsymmetricKey;
    }
}