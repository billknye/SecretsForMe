using Microsoft.JSInterop;
using NotesBin.Core;

namespace NotesBin.App;

public class IndexedDbFileSystemProvider : IFileSystemProvider
{
    private readonly ILogger<IndexedDbFileSystemProvider> logger;
    private readonly IJSRuntime js;

    private IJSObjectReference? db;

    public IndexedDbFileSystemProvider(ILogger<IndexedDbFileSystemProvider> logger, IJSRuntime js)
    {
        this.logger = logger;
        this.js = js;
    }

    public async Task<byte[]> Get(string key)
    {
        return await db.InvokeAsync<byte[]>("getBlob", key);
    }

    public async Task Initialize()
    {
        if (db != null) return;
        var dbLib = await js.InvokeAsync<IJSObjectReference>("import", "./assets/app.js");
        db = await dbLib.InvokeAsync<IJSObjectReference>("createIndexedDb");
    }

    public async Task<bool> Put(string key, string contentType, string etag, byte[] data, string? expectedEtag = null)
    {
        return await db.InvokeAsync<bool>("storeBlob", key, contentType, etag, data, expectedEtag);
    }

    public Task<bool> Remove(string key, string etag)
    {
        throw new NotImplementedException();
    }
}