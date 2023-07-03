﻿using SecretsForMe.Core.Configuration;

namespace SecretsForMe.Core.Blobs;

/// <summary>
/// Wraps a blob provider layering in encrypting pinned to a specific symmetric key
/// </summary>
public class CryptoBlobProvider : IBlobProvider
{
    private readonly IBlobProvider blobProvider;
    private readonly LoadedSymmetricKey symmetricKey;
    private readonly ICryptoProvider cryptoProvider;

    public SymmetricKey SymmetricKey => symmetricKey;
    public IBlobProvider BlobProvider => blobProvider;

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
        return new Blob(encryptedDoc.Hash, decrypted);
    }

    public async Task<bool> Put(Guid key, string hash, byte[] data, string? expectedHash = null)
    {
        var ivs = key.ToByteArray();
        var encryptedBytes = await cryptoProvider.AesEncrypt(ivs, symmetricKey.Key, data);

        return await blobProvider.Put(key, hash, encryptedBytes, expectedHash);
    }

    public Task<bool> Remove(Guid key, string etag)
    {
        return blobProvider.Remove(key, etag);
    }
}