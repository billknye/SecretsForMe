using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NotesBin.Core.Configuration;
using System.Text;

namespace NotesBin.Core.Content;

public class IndexedDbContentProvider : IContentProvider
{
    public static Guid ContentProviderTypeId = Guid.Parse("455EF4D5-D21A-486F-A527-B3160EF0906E");
    private readonly ILogger<IndexedDbContentProvider> logger;
    private readonly Guid contentProviderId;
    private IBlobProvider cryptoBlobProvider;
    private bool initialized;
    private SemaphoreSlim initializeLock;

    private BlobDirectory? root;

    public BlobDirectory Root => root ?? throw new InvalidOperationException();

    public IndexedDbContentProvider(ILoggerFactory loggerFactory, ILogger<IndexedDbContentProvider> logger, Guid contentProviderId, LoadedSymmetricKey loadedSymmetricKey, IJSRuntime js, ICryptoProvider cryptoProvider)
    {
        this.logger = logger;
        this.contentProviderId = contentProviderId;
        initializeLock = new SemaphoreSlim(1);
        var indexedBlobProvider = new IndexedDbBlobProvider(loggerFactory.CreateLogger<IndexedDbBlobProvider>(), js);
        cryptoBlobProvider = new CryptoBlobProvider(indexedBlobProvider, loadedSymmetricKey, cryptoProvider);
    }

    public async Task Initialize()
    {
        if (initialized)
            return;

        await initializeLock.WaitAsync();
        try
        {
            if (initialized)
                return;

            logger.LogInformation("Initializing crypto blob provider...");
            await cryptoBlobProvider.Initialize();

            logger.LogInformation("Getting root index blob...");
            var indexBlob = await cryptoBlobProvider.Get(contentProviderId);
            if (indexBlob == null)
            {
                logger.LogInformation("Initializing new index blob...");
                await InitializeNewIndex();
            }
            else
            {
                logger.LogInformation("Parsing root index blob...");
                await LoadIndex(indexBlob);
            }

            logger.LogInformation("Initialization complete.");
            initialized = true;
        }
        finally
        {
            initializeLock.Release();
        }
    }

    private async Task InitializeNewIndex()
    {
        root = new BlobDirectory(Guid.NewGuid(), "root", Enumerable.Empty<BlobDirectory>(), Enumerable.Empty<BlobContentItem>());

        var serialized = root.GetSerializedBytes();
        await cryptoBlobProvider.Put(contentProviderId, "root", null, serialized);
    }

    private Task LoadIndex(Blob indexBlob)
    {
        root = new BlobDirectory(indexBlob);

        Console.WriteLine($"Loaded blob {System.Text.Encoding.UTF8.GetString(indexBlob.BlobData)}");

        return Task.CompletedTask;
    }

    public IEnumerable<BlobDirectory>? GetDirectories(Guid? parent)
    {
        var searchSet = parent == null ? root : GetDirectory(root.Directories, parent.Value);
        if (searchSet == null)
            return null;

        return searchSet.Directories;
    }

    public IEnumerable<BlobContentItem>? GetContentItems(Guid? parent)
    {
        var searchSet = parent == null ? root : GetDirectory(root.Directories, parent.Value);
        if (searchSet == null)
            return null;

        return searchSet.ContentItems;
    }

    private static BlobDirectory? GetDirectory(IEnumerable<BlobDirectory> containers, Guid id)
    {
        var found = containers.FirstOrDefault(n => n.Id == id);
        if (found != null)
            return found;

        foreach (var dir in containers)
        {
            found = GetDirectory(dir.Directories, id);
            if (found != null)
                return found;
        }

        return null;
    }
}

public class BlobDirectory
{
    public Guid Id { get; }

    public string Name { get; }

    public IEnumerable<BlobDirectory> Directories { get; }

    public IEnumerable<BlobContentItem> ContentItems { get; }

    public BlobDirectory(Blob rootBlob)
    {
        var dir = System.Text.Json.JsonSerializer.Deserialize<BlobDirectory>(Encoding.UTF8.GetString(rootBlob.BlobData))
            ?? throw new InvalidOperationException();
        Id = dir.Id;
        Name = dir.Name;
        Directories = dir.Directories;
        ContentItems = dir.ContentItems;
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public BlobDirectory(Guid id, string name, IEnumerable<BlobDirectory> directories, IEnumerable<BlobContentItem> contentItems)
    {
        Id = id;
        Name = name;
        Directories = directories;
        ContentItems = contentItems;
    }

    public byte[] GetSerializedBytes()
    {
        return Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(this));
    }
}

public class BlobContentItem
{
    public Guid Id { get; }

    public string Name { get; }

    public string ContentType { get; }

    public DateTimeOffset LastUpdated { get; }

    public string ETag { get; }

    public BlobContentItem(Guid id, string name, string contentType, DateTimeOffset lastUpdated, string eTag)
    {
        Id = id;
        Name = name;
        ContentType = contentType;
        LastUpdated = lastUpdated;
        ETag = eTag;
    }
}