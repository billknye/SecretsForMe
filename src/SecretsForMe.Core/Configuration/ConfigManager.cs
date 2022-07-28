using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecretsForMe.Core.Configuration.Persistence;
using System.Text;

namespace SecretsForMe.Core.Configuration;

public class ConfigManager
{
    private const string LocalStorageKeyName = "SecretsForMeConfig";
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<ConfigManager> logger;
    private readonly ILocalStorageService localStorageService;
    private readonly ICryptoProvider cryptoProvider;
    private readonly IJSRuntime js;

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
    private List<ContentProvider>? contentProviders;

    public IEnumerable<AsymmetricKey> AsymmetricKeys => asymmetricKeys ?? Enumerable.Empty<AsymmetricKey>();
    public IEnumerable<SymmetricKey> SymmetricKeys => symmetricKeys ?? Enumerable.Empty<SymmetricKey>();
    public IEnumerable<ContentProvider> ContentProviders => contentProviders ?? Enumerable.Empty<ContentProvider>();

    public ConfigManager(ILoggerFactory loggerFactory, ILogger<ConfigManager> logger, ILocalStorageService localStorageService, ICryptoProvider cryptoProvider, IJSRuntime js)
    {
        this.loggerFactory = loggerFactory;
        this.logger = logger;
        this.localStorageService = localStorageService;
        this.cryptoProvider = cryptoProvider;
        this.js = js;
    }

    public async Task Initialize()
    {
        using var _ = logger.BeginScope("Initialize");
        string? existingConfig = null;

        try
        {
            await cryptoProvider.Initialize();
            existingConfig = await GetPeristedConfigString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error initializing");
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
            logger.LogWarning(ex, "Error loading existing config");
            State = ConfigState.ErrorLoading;
            LoadingException = ex;
        }
    }

    public async Task<string> GetPeristedConfigString()
    {
        var config = await localStorageService.GetItemAsStringAsync(LocalStorageKeyName);
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
            {
                asymmetricKeys.Add(new PassThroughAsymmetricKey(asymmetricKey));
                logger.LogDebug("Loaded PasThrough Asymmetric Key: {AsymmetricKey}", asymmetricKey.Id);
                continue;
            }

            try
            {
                var bytes = await cryptoProvider.DeriveBytes(unlockData, 100000, credential.Id.ToByteArray());
                var privateBytes = await cryptoProvider.AesDecrypt(asymmetricKey.Id.ToByteArray(), bytes, credential.EncryptedAsymmetricKeyPrivateKey);

                var loadedAsymmetricKey = new LoadedAsymmetricKey(asymmetricKey.Id, asymmetricKey.PublicKey, privateBytes);
                asymmetricKeys.Add(loadedAsymmetricKey);

                loadedAsymmetricKey.Credentials.Add(new Credential
                {
                    Id = credential.Id,
                    Name = credential.Name,
                    AesKey = bytes
                });

                logger.LogDebug("Loaded Credential: {CredentialId}", credential.Id);

                foreach (var symmetricKey in loadedConfiguration.SymmetricKeys)
                {
                    var reference = asymmetricKey.SymmetricKeyReferences.FirstOrDefault(n => n.SymmetricKeyId == symmetricKey.Id);
                    if (reference == null)
                    {
                        // Leave non-deryptable keys alone
                        var passThroughSymmetricKey = new PassThroughSymmetricKey(symmetricKey.Id, symmetricKey);
                        symmetricKeys.Add(passThroughSymmetricKey);

                        logger.LogInformation("Could not find reference for symmetric key: {id}", symmetricKey.Id);
                        continue;
                    }

                    var symmetricKeyBytes = await cryptoProvider.RsaDecrypt(privateBytes, reference.EncryptedSymmetricKey);

                    var decryptedMetaData = await cryptoProvider.AesDecrypt(symmetricKey.Id.ToByteArray(), symmetricKeyBytes, symmetricKey.EncryptedSymmetricKeyMetadata);
                    var deserialiedMetaData = System.Text.Json.JsonSerializer.Deserialize<SymmetricKeyMetadata>(Encoding.UTF8.GetString(decryptedMetaData));

                    var sym = new LoadedSymmetricKey(symmetricKey.Id, deserialiedMetaData?.Name ?? "Unnamed Key", symmetricKeyBytes);
                    symmetricKeys.Add(sym);

                    logger.LogDebug("Loaded Symmetric Key: {SymmetricKey}", symmetricKey.Id);
                }

                foreach (var reference in asymmetricKey.SymmetricKeyReferences)
                {
                    var symmetricKey = symmetricKeys.OfType<LoadedSymmetricKey>().First(n => n.Id == reference.SymmetricKeyId);

                    loadedAsymmetricKey.SymmetricKeyReferences.Add(new SymmetricKeyReference
                    {
                        SymmetricKey = symmetricKey
                    });
                }

                var contentProviders = new List<ContentProvider>();

                foreach (var contentProvider in loadedConfiguration.ContentProviders)
                {
                    var symmetricKey = symmetricKeys.First(n => n.Id == contentProvider.SymmetricKeyId);

                    if (symmetricKey is LoadedSymmetricKey loadedSymmetricKey)
                    {
                        var decryptedProviderOptions = await cryptoProvider.AesDecrypt(contentProvider.Id.ToByteArray(), loadedSymmetricKey.Key, contentProvider.EncryptedProviderData);
                        var providerOptions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(decryptedProviderOptions));
                        var loadedContentProvider = new LoadedContentProvider(loggerFactory, contentProvider.Id, loadedSymmetricKey, contentProvider.Name, js, cryptoProvider, providerOptions);
                        await loadedContentProvider.ContentProvider.Initialize();
                        contentProviders.Add(loadedContentProvider);
                    }
                    else if (symmetricKey is PassThroughSymmetricKey passThroughSymmetricKey)
                    {
                        contentProviders.Add(new PassThroughContentProvider(contentProvider.Id, contentProvider.SymmetricKeyId, contentProvider.Name, contentProvider));
                    }
                }

                this.asymmetricKeys = asymmetricKeys;
                this.symmetricKeys = symmetricKeys;
                this.contentProviders = contentProviders;

                State = ConfigState.Ready;

                logger.LogInformation("Unlock succeeded");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Unlock failed");
                return false;
            }
        }

        return false;
    }

    public async Task ResetForNewSetup()
    {
        logger.LogInformation("Resetting state for new setup");

        await localStorageService.RemoveItemAsync(LocalStorageKeyName);

        symmetricKeys = new List<SymmetricKey>();
        asymmetricKeys = new List<AsymmetricKey>();
        contentProviders = new List<ContentProvider>();
        State = ConfigState.Ready;
    }

    public async Task SaveConfiguration()
    {
        logger.LogInformation("Begin Save Configuration");

        if (symmetricKeys == null || asymmetricKeys == null || contentProviders == null)
            throw new InvalidOperationException();

        var asymmetricTasks = asymmetricKeys.Select(n => PersistAsymmetricKey(n));
        var asymmetricResults = await Task.WhenAll(asymmetricTasks);

        var symmetricTasks = symmetricKeys.Select(n => PersistSymmetricKey(n));
        var symmetricResults = await Task.WhenAll(symmetricTasks);

        var contentProviderTasks = contentProviders.Select(n => PersistContentProvider(n));
        var contentProviderResults = await Task.WhenAll(contentProviderTasks);

        var version = new Version(1, 0);

        var persisted = new PersistedConfiguration(version, asymmetricResults, symmetricResults, contentProviderResults);
        var serializedConfiguration = System.Text.Json.JsonSerializer.Serialize(persisted);

        await localStorageService.SetItemAsStringAsync(LocalStorageKeyName, serializedConfiguration);

        logger.LogInformation($"Save complete, {serializedConfiguration.Length} characters.");
    }

    private async Task<PersistedSymmetricKey> PersistSymmetricKey(SymmetricKey symmetricKey)
    {
        if (symmetricKey is PassThroughSymmetricKey passThroughSymmetricKey)
        {
            return passThroughSymmetricKey.EncryptedKey;
        }
        else if (symmetricKey is LoadedSymmetricKey loadedSymmetricKey)
        {
            var metaData = new SymmetricKeyMetadata
            {
                Name = loadedSymmetricKey.Name
            };

            var serialized = System.Text.Json.JsonSerializer.Serialize(metaData);
            var encryptedMetaData = await cryptoProvider.AesEncrypt(symmetricKey.Id.ToByteArray(), loadedSymmetricKey.Key, Encoding.UTF8.GetBytes(serialized));

            return new PersistedSymmetricKey(symmetricKey.Id, encryptedMetaData);
        }

        throw new InvalidOperationException();
    }

    private async Task<PersistedAsymmetricKey> PersistAsymmetricKey(AsymmetricKey asymmetricKey)
    {
        if (asymmetricKey is LoadedAsymmetricKey loaded)
        {
            var credTasks = loaded.Credentials.Select(n => PersistCredential(loaded, n));
            var credentials = await Task.WhenAll(credTasks);

            var refTasks = loaded.SymmetricKeyReferences.Select(n => PersistSymmetricKeyReference(asymmetricKey, n));
            var references = await Task.WhenAll(refTasks);

            return new PersistedAsymmetricKey(asymmetricKey.Id, asymmetricKey.PublicKey, credentials, references);
        }
        else if (asymmetricKey is PassThroughAsymmetricKey passThrough)
        {
            return passThrough.PersistedAsymmetricKey;
        }

        throw new InvalidOperationException();
    }

    private async Task<PersistedCredential> PersistCredential(LoadedAsymmetricKey asymmetric, Credential credential)
    {
        var encryptedPrivateKey = await cryptoProvider.AesEncrypt(asymmetric.Id.ToByteArray(), credential.AesKey, asymmetric.PrivateKey);
        return new PersistedCredential(credential.Id, credential.Name, encryptedPrivateKey);
    }

    private async Task<PersistedSymmetricKeyReference> PersistSymmetricKeyReference(AsymmetricKey asymmetricKey, SymmetricKeyReference symmetricKeyReference)
    {
        var encryptedSymmetricKey = await cryptoProvider.RsaEncrypt(asymmetricKey.PublicKey, symmetricKeyReference.SymmetricKey.Key);
        return new PersistedSymmetricKeyReference(symmetricKeyReference.SymmetricKey.Id, encryptedSymmetricKey);
    }

    private async Task<PersistedContentProvider> PersistContentProvider(ContentProvider contentProvider)
    {
        if (contentProvider is PassThroughContentProvider passThroughContentProvider)
        {
            return passThroughContentProvider.ContentProvider;
        }
        else if (contentProvider is LoadedContentProvider loaded)
        {
            var symmetricKey = symmetricKeys.FirstOrDefault(n => n.Id == loaded.SymmetricKeyId);
            if (symmetricKey is LoadedSymmetricKey loadedSymmetricKey)
            {
                var serialized = System.Text.Json.JsonSerializer.Serialize(loaded.GetProviderOptions());
                var encryptedContentProviderData = await cryptoProvider.AesEncrypt(loaded.Id.ToByteArray(), loadedSymmetricKey.Key, Encoding.UTF8.GetBytes(serialized));
                var persistedContentProvider = new PersistedContentProvider(loaded.Id, symmetricKey.Id, contentProvider.Name, encryptedContentProviderData);
                return persistedContentProvider;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        throw new InvalidOperationException();
    }

    private class SymmetricKeyMetadata
    {
        public string? Name { get; set; }
    }

    public async Task<LoadedSymmetricKey> AddSymmetricKey(string name)
    {
        if (symmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        var keyBytes = await cryptoProvider.CreateAesKey(256);

        var symmetricKey = new LoadedSymmetricKey(Guid.NewGuid(), name, keyBytes);

        symmetricKeys.Add(symmetricKey);
        return symmetricKey;
    }

    public async Task<LoadedAsymmetricKey> AddAsymmetricKey()
    {
        if (asymmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        var pair = await cryptoProvider.CreateRsaKeyPair(2048);
        var id = Guid.NewGuid();
        var asymmetricKey = new LoadedAsymmetricKey(id, pair.publicKey, pair.privateKey);
        asymmetricKeys.Add(asymmetricKey);
        return asymmetricKey;
    }

    public async Task<Credential> AddCredential(LoadedAsymmetricKey asymmetricKey, string name, string password)
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

    public Task AddSymmetricKeyReference(LoadedSymmetricKey symmetricKey, LoadedAsymmetricKey asymmetricKey)
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

    public Task AddContentProvider(LoadedSymmetricKey symmetricKey, string name, Dictionary<string, string> providerOptions)
    {
        if (symmetricKeys == null || State != ConfigState.Ready)
            throw new InvalidOperationException();

        if (contentProviders == null)
            throw new InvalidOperationException();

        ArgumentNullException.ThrowIfNull(symmetricKey);

        var id = Guid.NewGuid();
        var provider = new LoadedContentProvider(loggerFactory, id, symmetricKey, name, js, cryptoProvider, providerOptions);
        provider.ContentProvider.Initialize();
        contentProviders.Add(provider);

        return Task.CompletedTask;
    }
}
