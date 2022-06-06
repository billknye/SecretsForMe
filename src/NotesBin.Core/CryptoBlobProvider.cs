using NotesBin.Core.Configuration;

namespace NotesBin.Core;

/// <summary>
/// Wraps a blob provider layering in encrypting pinned to a specific symmetric key
/// </summary>
public class CryptoBlobProvider : IBlobProvider
{
    private readonly IBlobProvider blobProvider;
    private readonly LoadedSymmetricKey symmetricKey;
    private readonly ICryptoProvider cryptoProvider;

    public SymmetricKey SymmetricKey => this.symmetricKey;
    public IBlobProvider BlobProvider => this.blobProvider;

    public CryptoBlobProvider(IBlobProvider blobProvider, LoadedSymmetricKey symmetricKey, ICryptoProvider cryptoProvider)
    {
        this.blobProvider = blobProvider;
        this.symmetricKey = symmetricKey;
        this.cryptoProvider = cryptoProvider;
    }

    public Task Initialize()
    {
        return blobProvider.Initialize();
    }

    public async Task<Blob?> Get(Guid key)
    {
        var encryptedDoc = await blobProvider.Get(key);
        if (encryptedDoc == null)
            return null;

        var ivs = key.ToByteArray();
        var decrypted = await cryptoProvider.AesDecrypt(ivs, symmetricKey.Key, encryptedDoc.BlobData);
        return new Blob(key, encryptedDoc.Etag, encryptedDoc.ContentType, decrypted);
    }

    public async Task<bool> Put(Guid key, string contentType, string etag, byte[] data, string? expectedEtag = null)
    {
        var ivs = key.ToByteArray();
        var encryptedBytes = await cryptoProvider.AesEncrypt(ivs, symmetricKey.Key, data);

        return await blobProvider.Put(key, contentType, etag, encryptedBytes, expectedEtag);
    }

    public Task<bool> Remove(Guid key, string etag)
    {
        return blobProvider.Remove(key, etag);
    }
}

/// <summary>
/// A local file system instance based on a crypto blob provider
/// </summary>
public class LocalFileSystem
{
    private readonly CryptoBlobProvider cryptoBlobProvider;

    public Guid IndexFileKey => cryptoBlobProvider.SymmetricKey.Id;

    public LocalFileSystem(CryptoBlobProvider cryptoBlobProvider)
    {
        this.cryptoBlobProvider = cryptoBlobProvider;
    }

    public async Task Initialize()
    {
        var indexFile = await cryptoBlobProvider.Get(IndexFileKey);
        if (indexFile == null)
        {
            // new
        }
        else
        {

        }
    }

    public async Task<RootFileSystemReference> GetRoot()
    {
        throw new NotImplementedException();
    }

    public async Task<FileFileSystemReference> CreateFile(FileSystemReference parent, string name, string contentType, byte[] content)
    {
        var key = Guid.NewGuid();
        var file = new FileFileSystemReference(key, name, contentType);

        throw new NotImplementedException();
    }

    public async Task<DirectoryFileSystemReference> CreateDirectory(FileSystemReference parent, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> GetFileContent(FileFileSystemReference file)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> SetFileContent(FileFileSystemReference file, byte[] content)
    {
        throw new NotImplementedException();
    }
}

public abstract class FileSystemReference
{
    public Guid Key { get; }

    public string Name { get; }

    public string ContentType { get; }

    public FileSystemReference(Guid key, string name, string contentType)
    {
        Key = key;
        Name = name;
        ContentType = contentType;
    }
}

public abstract class ParentFileSystemReference : FileSystemReference
{
    private readonly List<FileSystemReference> children;

    public IEnumerable<FileSystemReference> Children => Children;

    protected ParentFileSystemReference(Guid key, string name, string contentType, IEnumerable<FileSystemReference> children) : base(key, name, contentType)
    {
        this.children = new List<FileSystemReference>(children);
    }
}

public class DirectoryFileSystemReference : ParentFileSystemReference
{
    public DirectoryFileSystemReference(Guid key, string name, IEnumerable<FileSystemReference> children) : base(key, name, WellKnownContentTypes.Directory, children)
    {
    }
}

public class RootFileSystemReference : ParentFileSystemReference
{
    public RootFileSystemReference(Guid key, string name, IEnumerable<FileSystemReference> children) : base(key, name, WellKnownContentTypes.Root, children)
    {
    }
}

public class FileFileSystemReference : FileSystemReference
{
    public FileFileSystemReference(Guid key, string name, string contentType) : base(key, name, contentType)
    {
    }
}


public static class WellKnownContentTypes
{
    public const string Root = "root";
    public const string Directory = "directory";
}