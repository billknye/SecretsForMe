
namespace SecretsForMe.Core.FileSystem;

public class Index
{
    public Guid SymmetricKeyId { get; set; }

    public IndexEntry Root { get; set; }
}

public class IndexEntry
{
    public Guid IndexEntryId { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    public string Name { get; set; }

    public string ContentType { get; set; }

    public Guid? BlobId { get; set; }

    public List<IndexEntry>? Children { get; set; }
}

public class FileSystemReference
{
    public string Path { get; set; }

    Dictionary<Guid, IndexEntry> entries;
}

public class FileSystemIndexEntryReference
{
    public IndexEntry IndexEntry { get; set; }
}