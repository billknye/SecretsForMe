using Microsoft.JSInterop;
using NotesBin.Core;

namespace NotesBin.App;

public class JsCryptoProvider : ICryptoProvider
{
    private readonly ILogger<JsCryptoProvider> logger;
    private readonly IJSRuntime js;
    private IJSObjectReference crypto;

    public JsCryptoProvider(ILogger<JsCryptoProvider> logger, IJSRuntime js)
    {
        this.logger = logger;
        this.js = js;
    }

    public async Task Initialize()
    {
        logger.LogInformation("Initialize");
        crypto = await js.InvokeAsync<IJSObjectReference>("import", "./assets/crypto.js");
    }

    public async Task<byte[]> CreateAesKey(int length)
    {
        logger.LogInformation("CreateAesKey");
        return await crypto.InvokeAsync<byte[]>("getAesKey", length);        
    }

    public async Task<(byte[] publicKey, byte[] privateKey)> CreateRsaKeyPair(int length)
    {
        logger.LogInformation("CreateRsaKeyPair");
        var keyPair = await crypto.InvokeAsync<CryptoKeyPair>("getRsaKeyPair", length);
        return (keyPair.publicKey, keyPair.privateKey);
    }

    public async Task<byte[]> DeriveBytes(string password, int iterations, byte[] salt)
    {
        logger.LogInformation("DeriveBytes");
        return await crypto.InvokeAsync<byte[]>("getDerivedBytes", password, iterations, salt);
    }

    public async Task<byte[]> AesEncrypt(byte[] iv, byte[] aesKey, byte[] rawData)
    {
        logger.LogInformation("AesEncrypt");
        return await crypto.InvokeAsync<byte[]>("aesEncrypt", iv, aesKey, rawData);
    }

    public async Task<byte[]> AesDecrypt(byte[] iv, byte[] aesKey, byte[] encryptedData)
    {
        logger.LogInformation("AesDecrypt");
        return await crypto.InvokeAsync<byte[]>("aesDecrypt", iv, aesKey, encryptedData);
    }

    public async Task<byte[]> RsaEncrypt(byte[] publicKey, byte[] rawData)
    {
        logger.LogInformation("RsaEncrypt");
        return await crypto.InvokeAsync<byte[]>("rsaEncrypt", publicKey, rawData);
    }

    public async Task<byte[]> RsaDecrypt(byte[] privateKey, byte[] encryptedData)
    {
        logger.LogInformation("RsaDecrypt");
        return await crypto.InvokeAsync<byte[]>("rsaDecrypt", privateKey, encryptedData);
    }
}
