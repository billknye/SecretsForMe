namespace NotesBin.Core.Configuration;

public abstract class SymmetricKey
{
    public Guid Id { get; set; }

    public SymmetricKey(Guid id)
    {
        Id = id;
    }
}

public class LoadedSymmetricKey : SymmetricKey
{
    public string Name { get; }

    public byte[] Key { get; }

    public LoadedSymmetricKey(Guid id, string name, byte[] key)
        : base(id)
    {
        Name = name;
        Key = key;
    }
}

public class PassThroughSymmetricKey : SymmetricKey
{
    public Persistence.PersistedSymmetricKey EncryptedKey { get; }

    public PassThroughSymmetricKey(Guid id, Persistence.PersistedSymmetricKey persistedSymmetricKey)
        : base(id)
    {
        this.EncryptedKey = persistedSymmetricKey;
    }
}
