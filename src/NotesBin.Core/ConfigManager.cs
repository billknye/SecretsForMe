using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NotesBin.Core;

public class ConfigManager
{
    private readonly ILogger<ConfigManager> logger;
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

    public IEnumerable<AsymmetricKey> AsymmetricKeys => asymmetricKeys ?? Enumerable.Empty<AsymmetricKey>();
    public IEnumerable<SymmetricKey> SymmetricKeys => symmetricKeys ?? Enumerable.Empty<SymmetricKey>();


    public ConfigManager(ILogger<ConfigManager> logger, ILocalStorageService localStorageService, ICryptoProvider cryptoProvider)
    {
        this.logger = logger;
        this.localStorageService = localStorageService;
        this.cryptoProvider = cryptoProvider;
    }

    public async Task Initialize()
    {
        using var _ = logger.BeginScope("Initialize");
        string existingConfig = null;

        try
        {
            await cryptoProvider.Initialize();
            existingConfig = await GetPeristedConfigString();
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Error initializing: {ex}");
            State = ConfigState.ErrorLoading;
            LoadingException = ex;
        }

        if (existingConfig == null)
        {
            logger.LogInformation("No existing config, new setup");
            State = ConfigState.Empty;
            return;
        }

        try
        {
            logger.LogInformation("Existing config, loading...");
            loadedConfiguration = System.Text.Json.JsonSerializer.Deserialize<PersistedConfiguration>(existingConfig);
            State = ConfigState.Loaded;
            logger.LogInformation("Existing config loaded.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Error loading existing config: {ex}");
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
        logger.LogInformation("Attempting Unlock via credential {credentialId}", credentialId);

        if (State != ConfigState.Loaded || loadedConfiguration == null)
            throw new InvalidOperationException();

        var asymmetricKeys = new List<AsymmetricKey>();
        var symmetricKeys = new List<SymmetricKey>();

        foreach (var asymmetricKey in loadedConfiguration.AsymmetricKeys)
        {
            var credential = asymmetricKey.Credentials.FirstOrDefault(n => n.Id == credentialId);
            if (credential == null)
                continue;

            try
            {
                var bytes = await cryptoProvider.DeriveBytes(unlockData, 100000, credential.Id.ToByteArray());
                var privateBytes = await cryptoProvider.AesDecrypt(asymmetricKey.Id.ToByteArray(), bytes, credential.EncryptedAsymmetricKeyPrivateKey);

                var loadedAsymmetricKey = new AsymmetricKey
                {
                    Id = asymmetricKey.Id,
                    PublicKey = asymmetricKey.PublicKey,
                    PrivateKey = privateBytes
                };

                asymmetricKeys.Add(loadedAsymmetricKey);

                loadedAsymmetricKey.Credentials.Add(new Credential
                {
                    Id = credential.Id,
                    Name = credential.Name,
                    AesKey = bytes
                });

                foreach (var symmetricKey in loadedConfiguration.SymmetricKeys)
                {
                    var reference = asymmetricKey.SymmetricKeyReferences.FirstOrDefault(n => n.SymmetricKeyId == symmetricKey.Id);
                    if (reference == null)
                    {
                        logger.LogWarning("Could not find reference for symmetric key: {id}", symmetricKey.Id);
                        continue;
                    }

                    var symmetricKeyBytes = await cryptoProvider.RsaDecrypt(privateBytes, reference.EncryptedSymmetricKey);

                    var decryptedMetaData = await cryptoProvider.AesDecrypt(symmetricKey.Id.ToByteArray(), symmetricKeyBytes, symmetricKey.EncryptedSymmetricKeyMetadata);
                    var deserialiedMetaData = System.Text.Json.JsonSerializer.Deserialize<SymmetricKeyMetadata>(Encoding.UTF8.GetString(decryptedMetaData));

                    var sym = new SymmetricKey
                    {
                        Id = symmetricKey.Id,
                        Key = symmetricKeyBytes,
                        Name = deserialiedMetaData?.Name ?? "Unnamed Key"
                    };

                    symmetricKeys.Add(sym);
                }

                this.asymmetricKeys = asymmetricKeys;
                this.symmetricKeys = symmetricKeys;

                State = ConfigState.Ready;

                logger.LogInformation("Unlock succeeded");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unlock failed: {ex}");
                return false;
            }
        }

        return false;
    }

    public async Task ResetForNewSetup()
    {
        logger.LogInformation("Resetting state for new setup");

        await localStorageService.RemoveItemAsync("NotesBinConfiguration");

        symmetricKeys = new List<SymmetricKey>();
        asymmetricKeys = new List<AsymmetricKey>();
        State = ConfigState.Ready;
    }

    public async Task SaveConfiguration()
    {
        logger.LogInformation("Begin Save Configuration");

        if (symmetricKeys == null || asymmetricKeys == null)
            throw new InvalidOperationException();

        var asymmetricTasks = asymmetricKeys.Select(n => PersistAsymmetricKey(n));
        var asymmetricResults = await Task.WhenAll(asymmetricTasks);

        var symmetricTasks = symmetricKeys.Select(n => PersistSymmetricKey(n));
        var symmetricResults = await Task.WhenAll(symmetricTasks);

        var persisted = new PersistedConfiguration(asymmetricResults, symmetricResults);
        var serializedConfiguration = System.Text.Json.JsonSerializer.Serialize(persisted);

        await localStorageService.SetItemAsStringAsync("NotesBinConfiguration", serializedConfiguration);

        logger.LogInformation($"Save complete, {serializedConfiguration.Length} characters.");
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
