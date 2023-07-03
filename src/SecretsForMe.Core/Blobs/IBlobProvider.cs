namespace SecretsForMe.Core.Blobs;

public interface IBlobProvider
{
    Task Initialize();
    Task<Blob?> Get(Guid key);
    Task<bool> Put(Guid key, string hash, byte[] data, string? expectedHash = null);
    Task<bool> Remove(Guid key, string hash);
}

public record Blob(string Hash, byte[] BlobData);