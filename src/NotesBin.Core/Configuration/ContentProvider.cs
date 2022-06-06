using NotesBin.Core.Configuration.Persistence;

namespace NotesBin.Core.Configuration;

public abstract class ContentProvider
{
    public Guid Id { get; }

    public Guid SymmetricKeyId { get; }

    public string Name { get; }


    public ContentProvider(Guid id, Guid symmetricKeyId, string name)
    {
        Id = id;
        SymmetricKeyId = symmetricKeyId;
        Name = name;
    }

}

public class LoadedContentProvider : ContentProvider
{
    private readonly Dictionary<string, string> providerOptions;

    public LoadedContentProvider(Guid id, Guid symmetricKeyId, string name, Dictionary<string, string> providerOptions)
        : base(id, symmetricKeyId, name)
    {
        this.providerOptions = providerOptions;
    }

    public Dictionary<string, string> GetProviderOptions()
    {
        return providerOptions;
    }
}

public class PassThroughContentProvider : ContentProvider
{
    public PersistedContentProvider ContentProvider { get; }

    public PassThroughContentProvider(Guid id, Guid symmetricKeyId, string name, Persistence.PersistedContentProvider contentProvider)
        : base(id, symmetricKeyId, name)
    {
        ContentProvider = contentProvider;
    }
}