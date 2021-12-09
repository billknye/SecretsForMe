namespace NotesBin.Core;

public interface IBlobProvider
{
    Task Initialize();
    Task<byte[]> Get(Guid key);
    Task<bool> Put(Guid key, string contentType, string etag, byte[] data, string? expectedEtag = null);
    Task<bool> Remove(Guid key, string etag);
}

