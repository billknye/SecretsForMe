using Blazored.LocalStorage;

namespace NotesBin.Core;

public class NotesManagerSetupProvider
{
    private readonly ICryptoProvider cryptoProvider;
    private readonly ILocalStorageService localStorageService;
    private readonly CredentialProvider credentialProvider;
    private readonly AsymmetricProvider asymmetricProvider;
    private readonly SymmetricProvider symmetricProvider;

    public NotesManagerSetupProvider(ICryptoProvider cryptoProvider, ILocalStorageService localStorageService,
        CredentialProvider credentialProvider, AsymmetricProvider asymmetricProvider, SymmetricProvider symmetricProvider)
    {
        this.cryptoProvider = cryptoProvider;
        this.localStorageService = localStorageService;
        this.credentialProvider = credentialProvider;
        this.asymmetricProvider = asymmetricProvider;
        this.symmetricProvider = symmetricProvider;
    }

    public async Task<NotesManager> CompleteSetup(string name, string password)
    {
        // Create asymmetric key
        var asymmetric = await asymmetricProvider.Create(2048);

        // Create credential
        var cred = await credentialProvider.Create(name, password);        

        // Create symmetric key
        var symmetric = await symmetricProvider.Create(256);
        var symmetricKey = new SymmetricKey
        {
            Id = Guid.NewGuid(),
            Key = symmetric
        };

        var manager = new NotesManager(cryptoProvider, localStorageService, cred, asymmetric, symmetricKey);
        await manager.SaveConfiguration();
        return manager;
    }
}
