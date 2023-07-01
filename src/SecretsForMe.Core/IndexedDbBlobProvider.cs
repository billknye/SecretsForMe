using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecretsForMe.Core;

namespace SecretsForMe.Core;

/// <summary>
/// Provides low-level blob reading/writing functionality on top of indexed db.
/// </summary>
public class IndexedDbBlobProvider : IBlobProvider
{
    private readonly ILogger<IndexedDbBlobProvider> logger;
    private readonly IJSRuntime js;
    private IJSObjectReference? db;

    public IndexedDbBlobProvider(ILogger<IndexedDbBlobProvider> logger, IJSRuntime js)
    {
        this.logger = logger;
        this.js = js;
    }
    public async Task Initialize()
    {
        if (db != null) return;
        var dbLib = await js.InvokeAsync<IJSObjectReference>("import", "./assets/app.js");
        db = await dbLib.InvokeAsync<IJSObjectReference>("createIndexedDb");
    }

    public async Task<Blob?> Get(Guid key)
    {
        if (db == null) throw new InvalidOperationException();

        logger.LogInformation("Get blob {key}", key);
        var blob = await db.InvokeAsync<Blob?>("getBlob", key.ToString());
        return blob;
    }

    
    public async Task<bool> Put(Guid key, string hash, byte[] data, string? expectedHash = null)
    {
        if (db == null) throw new InvalidOperationException();

        logger.LogInformation("Pub blob {key}", key);
        return await db.InvokeAsync<bool>("storeBlob", key.ToString(), hash, data, expectedHash);
    }

    public async Task<bool> Remove(Guid key, string etag)
    {
        if (db == null) throw new InvalidOperationException();
        return await db.InvokeAsync<bool>("removeBlob", key.ToString(), etag);
    }
}