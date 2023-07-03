using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SecretsForMe.Core.Blobs;

/// <summary>
/// Provides low-level blob reading/writing functionality on top of indexed db.
/// </summary>
public class IndexedDbBlobProvider : IBlobProvider
{
    private readonly ILogger<IndexedDbBlobProvider> logger;
    private readonly IJSRuntime js;
    private IJSObjectReference? db;

    /// <summary>
    /// Creates a new instance of the provider.
    /// </summary>
    /// <param name="logger">A logger instance.</param>
    /// <param name="js">A reference to the JavaScript runtime.</param>
    public IndexedDbBlobProvider(ILogger<IndexedDbBlobProvider> logger, IJSRuntime js)
    {
        this.logger = logger;
        this.js = js;
    }

    /// <summary>
    /// Initializes the provider instance.
    /// </summary>
    /// <returns>A task that completes when initialization is complete.</returns>
    public async Task Initialize()
    {
        if (db != null) return;
        var dbLib = await js.InvokeAsync<IJSObjectReference>("import", "./assets/app.js");
        db = await dbLib.InvokeAsync<IJSObjectReference>("createIndexedDb");
    }

    /// <summary>
    /// Tries to retrieve the blob with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the blob to retrieve.</param>
    /// <returns>The <see cref="Blob"/> object or <see langword="null"/> when not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider has not already been initialized.</exception>
    public async Task<Blob?> Get(Guid key)
    {
        if (db == null) throw new InvalidOperationException();

        logger.LogInformation("Get blob {key}", key);
        var blob = await db.InvokeAsync<Blob?>("getBlob", key.ToString());
        return blob;
    }

    /// <summary>
    /// Attempts to set the blob with the specified <paramref name="key"/> with the specified <paramref name="data"/>,
    /// optionally only doing so when the existing object's hash matches the specified <paramref name="expectedHash"/>.
    /// </summary>
    /// <param name="key">The key of the blob to set.</param>
    /// <param name="hash">The hash of the data being set.</param>
    /// <param name="data">The data of the blob to store.</param>
    /// <param name="expectedHash">The hash of the existing blob stored with the provided key.</param>
    /// <returns><see langword="true"/> on success, <see langword="false"/> on failure.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider has not already been initialized.</exception>
    public async Task<bool> Put(Guid key, string hash, byte[] data, string? expectedHash = null)
    {
        if (db == null) throw new InvalidOperationException();

        logger.LogInformation("Pub blob {key}", key);
        return await db.InvokeAsync<bool>("storeBlob", key.ToString(), hash, data, expectedHash);
    }

    /// <summary>
    /// Tries to remove the blob with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the blob to remove.</param>
    /// <param name="hash">The expected hash of the blob.</param>
    /// <returns><see langword="true"/> on success, <see langword="false"/> on failure.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider has not already been initialized.</exception>
    public async Task<bool> Remove(Guid key, string hash)
    {
        if (db == null) throw new InvalidOperationException();
        return await db.InvokeAsync<bool>("removeBlob", key.ToString(), hash);
    }
}