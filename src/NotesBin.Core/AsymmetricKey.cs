namespace NotesBin.Core;

public class AsymmetricKey
{
    public Guid Id { get; set; }

    public byte[] PublicKey { get; set; }

    public byte[] PrivateKey { get; set; }

    public List<Credential> Credentials { get; set; }

    public List<SymmetricKeyReference> SymmetricKeyReferences { get; set; }

    public AsymmetricKey()
    {
        Credentials = new List<Credential>();
        SymmetricKeyReferences = new List<SymmetricKeyReference>();
    }
}
