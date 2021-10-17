using Microsoft.JSInterop;
using NotesBin.Core;

namespace NotesBin.App;

public class JsCryptoProvider : ICryptoProvider
{
    private readonly IJSRuntime js;
    private IJSObjectReference crypto;

    public JsCryptoProvider(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task Initialize()
    {
        crypto = await js.InvokeAsync<IJSObjectReference>("import", "./assets/crypto.js");
    }

    public async Task<byte[]> CreateAesKey(int length)
    {
        return await crypto.InvokeAsync<byte[]>("getAesKey", length);        
    }

    public async Task<(byte[] publicKey, byte[] privateKey)> CreateRsaKeyPair(int length)
    {
        var keyPair = await crypto.InvokeAsync<CryptoKeyPair>("getRsaKeyPair", length);
        return (keyPair.publicKey, keyPair.privateKey);
    }

    public async Task<byte[]> DeriveBytes(string password, int iterations, byte[] salt)
    {
        return await crypto.InvokeAsync<byte[]>("getDerivedBytes", password, iterations, salt);
    }

    public async Task<byte[]> AesEncrypt(byte[] iv, byte[] aesKey, byte[] rawData)
    {
        return await crypto.InvokeAsync<byte[]>("aesEncrypt", iv, aesKey, rawData);
    }

    public async Task<byte[]> AesDecrypt(byte[] iv, byte[] aesKey, byte[] encryptedData)
    {
        return await crypto.InvokeAsync<byte[]>("aesDecrypt", iv, aesKey, encryptedData);
    }

    public async Task<byte[]> RsaEncrypt(byte[] publicKey, byte[] rawData)
    {
        return await crypto.InvokeAsync<byte[]>("rsaEncrypt", publicKey, rawData);
    }
}
