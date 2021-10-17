using Blazored.LocalStorage;
using System.Text;

namespace NotesBin.Core;

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


    private List<SymmetricKey>? symmetricKeys;
    private List<AsymmetricKey>? asymmetricKeys;


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

    public IEnumerable<(Guid id, string name)> GetLoadedCredentials()
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

    public async Task ResetForNewSetup()
    {
        await localStorageService.RemoveItemAsync("NotesBinConfiguration");

        symmetricKeys = new List<SymmetricKey>();
        asymmetricKeys = new List<AsymmetricKey>();
        State = ConfigState.Ready;
    }

    public async Task SaveConfiguration()
    {
        if (symmetricKeys == null || asymmetricKeys == null)
            throw new InvalidOperationException();

        var asymmetricTasks = asymmetricKeys.Select(n => PersistAsymmetricKey(n));
        var asymmetricResults = await Task.WhenAll(asymmetricTasks);

        var symmetricTasks = symmetricKeys.Select(n => PersistSymmetricKey(n));
        var symmetricResults = await Task.WhenAll(symmetricTasks);

        var persisted = new PersistedConfiguration(asymmetricResults, symmetricResults);
        var serializedConfiguration = System.Text.Json.JsonSerializer.Serialize(persisted);

        await localStorageService.SetItemAsStringAsync("NotesBinConfiguration", serializedConfiguration);
    }

    private async Task<PersistedSymmetricKey> PersistSymmetricKey(SymmetricKey symmetricKey)
    {
        var metaData = new SymmetricKeyMetadata
        {
            Name = symmetricKey.Name
        };

        var serialized = System.Text.Json.JsonSerializer.Serialize(metaData);
        var encryptedMetaData = await cryptoProvider.AesEncrypt(symmetricKey.Id.ToByteArray(), symmetricKey.Key, Encoding.UTF8.GetBytes(serialized));

        return new PersistedSymmetricKey(symmetricKey.Id, encryptedMetaData);
    }

    private async Task<PersistedAsymmetricKey> PersistAsymmetricKey(AsymmetricKey asymmetricKey)
    {
        var credTasks = asymmetricKey.Credentials.Select(n => PersistCredential(asymmetricKey, n));
        var credentials = await Task.WhenAll(credTasks);

        var refTasks = asymmetricKey.SymmetricKeyReferences.Select(n => PersistSymmetricKeyReference(asymmetricKey, n));
        var references = await Task.WhenAll(refTasks);

        return new PersistedAsymmetricKey(asymmetricKey.Id, asymmetricKey.PublicKey, credentials, references);
    }

    private async Task<PersistedCredential> PersistCredential(AsymmetricKey asymmetric, Credential credential)
    {
        var encryptedPrivateKey = await cryptoProvider.AesEncrypt(asymmetric.Id.ToByteArray(), credential.AesKey, asymmetric.PrivateKey);
        return new PersistedCredential(credential.Id, credential.Name, encryptedPrivateKey);
    }

    private async Task<PersistedSymmetricKeyReference> PersistSymmetricKeyReference(AsymmetricKey asymmetricKey, SymmetricKeyReference symmetricKeyReference)
    {
        var encryptedSymmetricKey = await cryptoProvider.RsaEncrypt(asymmetricKey.PublicKey, symmetricKeyReference.SymmetricKey.Key);
        return new PersistedSymmetricKeyReference(symmetricKeyReference.SymmetricKey.Id, encryptedSymmetricKey);
    }

    private class SymmetricKeyMetadata
    {
        public string? Name { get; set; }
    }

    public async Task<SymmetricKey> AddSymmetricKey(string name)
    {
        if (symmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        var keyBytes = await cryptoProvider.CreateAesKey(256);

        var symmetricKey = new SymmetricKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = keyBytes
        };

        symmetricKeys.Add(symmetricKey);
        return symmetricKey;
    }

    public async Task<AsymmetricKey> AddAsymmetricKey()
    {
        if (asymmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        var pair = await cryptoProvider.CreateRsaKeyPair(2048);
        var id = Guid.NewGuid();

        var asymmetricKey = new AsymmetricKey
        {
            Id = id,
            PublicKey = pair.publicKey,
            PrivateKey = pair.privateKey
        };

        asymmetricKeys.Add(asymmetricKey);
        return asymmetricKey;
    }

    public async Task<Credential> AddCredential(AsymmetricKey asymmetricKey, string name, string password)
    {
        if (asymmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        if (!asymmetricKeys.Contains(asymmetricKey))
            throw new InvalidOperationException();

        var id = Guid.NewGuid();
        var bytes = await cryptoProvider.DeriveBytes(password, 100000, id.ToByteArray());

        var credential = new Credential
        {
            AesKey = bytes,
            Id = id,
            Name = name
        };

        asymmetricKey.Credentials.Add(credential);
        return credential;
    }

    public Task AddSymmetricKeyReference(SymmetricKey symmetricKey, AsymmetricKey asymmetricKey)
    {
        if (asymmetricKeys == null || symmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        if (!asymmetricKeys.Contains(asymmetricKey))
            throw new InvalidOperationException();

        if (!symmetricKeys.Contains(symmetricKey))
            throw new InvalidOperationException();

        asymmetricKey.SymmetricKeyReferences.Add(new SymmetricKeyReference
        {
            SymmetricKey = symmetricKey
        });

        return Task.CompletedTask;
    }
}
