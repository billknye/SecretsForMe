namespace NotesBin.Core;

public interface IFileSystemProvider
{
    Task Initialize();
    Task<byte[]> Get(string key);
    Task<bool> Put(string key, string contentType, string etag, byte[] data, string? expectedEtag = null);
    Task<bool> Remove(string key, string etag);
}