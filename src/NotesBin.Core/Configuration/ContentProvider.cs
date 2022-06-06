using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NotesBin.Core.Configuration.Persistence;
using NotesBin.Core.Content;

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

    public IContentProvider ContentProvider { get; }

    public LoadedContentProvider(ILoggerFactory loggerFactory, Guid id, LoadedSymmetricKey symmetricKey, string name, IJSRuntime js, ICryptoProvider cryptoProvider, Dictionary<string, string> providerOptions)
        : base(id, symmetricKey.Id, name)
    {
        this.providerOptions = providerOptions;

        ContentProvider = ContentProviderFactory.GetContentProvider(loggerFactory, id, symmetricKey, js, cryptoProvider, providerOptions);
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