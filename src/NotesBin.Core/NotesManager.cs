using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesBin.Core;

public class NotesManager
{
    private readonly ICryptoProvider cryptoProvider;
    private readonly ILocalStorageService localStorageService;
    private readonly Credential credential;
    private readonly AsymmetricKey asymmetricKey;
    private readonly SymmetricKey symmetricKey;

    /// <summary>
    /// Creates a new instance of the manager for initial setup
    /// </summary>
    public NotesManager(ICryptoProvider cryptoProvider, ILocalStorageService localStorageService, Credential credential, AsymmetricKey asymmetricKey, SymmetricKey symmetricKey)
    {
        this.cryptoProvider = cryptoProvider;
        this.localStorageService = localStorageService;
        this.credential = credential;
        this.asymmetricKey = asymmetricKey;
        this.symmetricKey = symmetricKey;
    }

    public async Task SaveConfiguration()
    {
        var encryptedAsymmetricKeyPrivateKey = await cryptoProvider.AesEncrypt(asymmetricKey.Id.ToByteArray(), credential.AesKey, asymmetricKey.PrivateKey);

        var config = new PersistedConfiguration(new[]
        {
            new PersistedAsymmetricKey(asymmetricKey.Id, asymmetricKey.PublicKey, new[]
            {
                new PersistedCredential(credential.Id, credential.Name, encryptedAsymmetricKeyPrivateKey)
            }, new[]
            {
                new PersistedSymmetricKeyReference(symmetricKey.Id, await cryptoProvider.RsaEncrypt(asymmetricKey.PublicKey, symmetricKey.Key))
            })
        }, new[]
        {
            new PersistedSymmetricKey(symmetricKey.Id, null)
        });

        var json = System.Text.Json.JsonSerializer.Serialize(config);
        await localStorageService.SetItemAsStringAsync("NotesBinConfiguration", json);
    }
}

public class ConfigManager
{
    private readonly ILocalStorageService localStorageService;
    private readonly ICryptoProvider cryptoProvider;

    public ConfigState State { get; private set; }

    /// <summary>
    /// Provides access to the first exception encountered while loading configuration
    /// </summary>
    public Exception? LoadingException { get; private set; }

    /// <summary>
    /// Holds the persisted configuration object before it is unlocked
    /// </summary>
    private PersistedConfiguration? loadedConfiguration;



    public ConfigManager(ILocalStorageService localStorageService, ICryptoProvider cryptoProvider)
    {
        this.localStorageService = localStorageService;
        this.cryptoProvider = cryptoProvider;
    }

    public async Task Initialize()
    {
        string existingConfig = null;
        try
        {
            await cryptoProvider.Initialize();
            existingConfig = await GetPeristedConfigString();
        }
        catch (Exception ex)
        {
            State = ConfigState.ErrorLoading;
            LoadingException = ex;
        }

        if (existingConfig == null)
        {
            State = ConfigState.Empty;
            return;
        }

        try
        {
            loadedConfiguration = System.Text.Json.JsonSerializer.Deserialize<PersistedConfiguration>(existingConfig);
            State = ConfigState.Loaded;
        }
        catch (Exception ex)
        {
            State = ConfigState.ErrorLoading;
            LoadingException = ex;
        }
    }

    public async Task<string> GetPeristedConfigString()
    {
        var config = await localStorageService.GetItemAsStringAsync("NotesBinConfiguration");
        return config;
    }

    public IEnumerable<(Guid id, string name)> GetPersistedCredentials()
    {
        if (State != ConfigState.Loaded || loadedConfiguration == null)
            throw new InvalidOperationException();

        return loadedConfiguration.AsymmetricKeys.SelectMany(n => n.Credentials).Select(n => (n.Id, n.Name));
    }

    public async Task<bool> TryUnlock(Guid credentialId, string unlockData)
    {
        if (State != ConfigState.Loaded || loadedConfiguration == null)
            throw new InvalidOperationException();

        foreach (var asymmetricKey in loadedConfiguration.AsymmetricKeys)
        {
            var credential = asymmetricKey.Credentials.FirstOrDefault(n => n.Id == credentialId);
            if (credential == null)
                continue;

            try
            {
                var bytes = await cryptoProvider.DeriveBytes(unlockData, 100000, credential.Id.ToByteArray());
                var privateBytes = await cryptoProvider.AesDecrypt(asymmetricKey.Id.ToByteArray(), bytes, credential.EncryptedAsymmetricKeyPrivateKey);
                State = ConfigState.Ready;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        return false;
    }
}

public enum ConfigState
{
    Uknown = 0,
    Loaded = 1,
    ErrorLoading = 2,
    Ready = 3,
    Empty = 4
}

public record PersistedConfiguration(PersistedAsymmetricKey[] AsymmetricKeys, PersistedSymmetricKey[] SymmetricKeys);

public record PersistedAsymmetricKey(Guid Id, byte[] PublicKey, PersistedCredential[] Credentials, PersistedSymmetricKeyReference[] SymmetricKeyReferences);

public record PersistedSymmetricKeyReference(Guid SymmetricKeyId, byte[] EncryptedSymmetricKey);

public record PersistedSymmetricKey(Guid Id, byte[] EncryptedSymmetricKeyMetadata);

public record PersistedCredential(Guid Id, string Name, byte[] EncryptedAsymmetricKeyPrivateKey);

public class SymmetricKey
{
    public Guid Id { get; set; }

    public byte[] Key { get; set; }
}

public class SymmetricProvider
{
    private readonly ICryptoProvider cryptoProvider;

    public SymmetricProvider(ICryptoProvider cryptoProvider)
    {
        this.cryptoProvider = cryptoProvider;
    }

    public async Task<byte[]> Create(int length)
    {
        return await cryptoProvider.CreateAesKey(length);
    }
}

public class AsymmetricProvider
{
    private readonly ICryptoProvider cryptoProvider;

    public AsymmetricProvider(ICryptoProvider cryptoProvider)
    {
        this.cryptoProvider = cryptoProvider;
    }

    public async Task<AsymmetricKey> Create(int length)
    {
        var pair = await cryptoProvider.CreateRsaKeyPair(length);
        var id = Guid.NewGuid();

        return new AsymmetricKey
        {
            Id = id,
            PublicKey = pair.publicKey,
            PrivateKey = pair.privateKey
        };
    }
}

public class AsymmetricKey
{
    public Guid Id { get; set; }

    public byte[] PublicKey { get; set; }

    public byte[] PrivateKey { get; set; }
}

public class CredentialProvider
{
    private readonly ICryptoProvider cryptoProvider;

    public CredentialProvider(ICryptoProvider cryptoProvider)
    {
        this.cryptoProvider = cryptoProvider;
    }
    
    public async Task<Credential> Create(string name, string password)
    {
        var id = Guid.NewGuid();
        var bytes = await cryptoProvider.DeriveBytes(password, 100000, id.ToByteArray());

        var credential = new Credential
        {
            AesKey = bytes,
            Id = id,
            Name = name
        };

        return credential;
    }
}

public class Credential
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public byte[] AesKey { get; set; }
}

public interface ICryptoProvider
{
    Task Initialize();

    Task<byte[]> DeriveBytes(string password, int iterations, byte[] salt);
    Task<(byte[] publicKey, byte[] privateKey)> CreateRsaKeyPair(int length);
    Task<byte[]> CreateAesKey(int length);

    Task<byte[]> AesEncrypt(byte[] iv, byte[] aesKey, byte[] rawData);
    Task<byte[]> AesDecrypt(byte[] iv, byte[] aesKey, byte[] encryptedData);

    Task<byte[]> RsaEncrypt(byte[] publicKey, byte[] rawData);

}