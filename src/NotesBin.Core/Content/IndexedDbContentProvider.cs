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
        await SaveIndex();
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

    private static (BlobDirectory Directory, BlobContentItem ContentItem)? GetContentItem(BlobDirectory container, Guid id)
    {
        var found = container.ContentItems.FirstOrDefault(n => n.Id == id);
        if (found != null)
            return (container, found);

        foreach (var dir in container.Directories)
        {
            var found2 = GetContentItem(dir, id);
            if (found2 != null)
                return found2;
        }

        return null;
    }

    public async Task CreateContentItem()
    {
        var item = new BlobContentItem(Guid.NewGuid(), "New Item", "text/plain", DateTimeOffset.UtcNow, null);
        root.AddContentItem(item);
        await SaveIndex();

        await cryptoBlobProvider.Put(item.Id, "text/plain", null, Array.Empty<byte>());
    }

    private async Task SaveIndex()
    {
        var serialized = root.GetSerializedBytes();
        await cryptoBlobProvider.Put(contentProviderId, "root", null, serialized);
    }

    public async Task<(BlobContentItem Item, byte[] Data)?> GetContentItem(Guid itemId)
    {
        var item = GetContentItem(root, itemId);

        if (item == null)
            return null;

        var blob = await cryptoBlobProvider.Get(item.Value.ContentItem.Id);

        return (item.Value.ContentItem, blob.BlobData);
    }

    public async Task UpdateContentItem(Guid contentItemId, string name, string contentType, byte[] content)
    {
        var existing = GetContentItem(root, contentItemId);
        if (existing == null)
            throw new InvalidOperationException();

        var contentItem = existing.Value.ContentItem;

        contentItem.Name = name;
        contentItem.LastUpdated = DateTimeOffset.UtcNow;
        contentItem.ContentType = contentType;

        await cryptoBlobProvider.Put(contentItemId, contentType, null, content);

        await SaveIndex();
    }
}

public class BlobDirectory
{
    private readonly List<BlobDirectory> directories;
    private readonly List<BlobContentItem> contentItems;

    public Guid Id { get; }

    public string Name { get; }

    public IEnumerable<BlobDirectory> Directories => directories;

    public IEnumerable<BlobContentItem> ContentItems => contentItems;

    public BlobDirectory(Blob rootBlob)
    {
        var dir = System.Text.Json.JsonSerializer.Deserialize<BlobDirectory>(Encoding.UTF8.GetString(rootBlob.BlobData))
            ?? throw new InvalidOperationException();
        Id = dir.Id;
        Name = dir.Name;
        directories = dir.Directories.ToList();
        contentItems = dir.ContentItems.ToList();
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public BlobDirectory(Guid id, string name, IEnumerable<BlobDirectory> directories, IEnumerable<BlobContentItem> contentItems)
    {
        Id = id;
        Name = name;
        this.directories = directories.ToList();
        this.contentItems = contentItems.ToList();
    }

    public void AddContentItem(BlobContentItem item)
    {
        this.contentItems.Add(item);
    }

    public byte[] GetSerializedBytes()
    {
        return Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(this));
    }
}

public class BlobContentItem
{
    public Guid Id { get; }

    public string Name { get; set; }

    public string ContentType { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    public string ETag { get; set; }

    public BlobContentItem(Guid id, string name, string contentType, DateTimeOffset lastUpdated, string eTag)
    {
        Id = id;
        Name = name;
        ContentType = contentType;
        LastUpdated = lastUpdated;
        ETag = eTag;
    }
}