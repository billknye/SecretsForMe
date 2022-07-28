using Blazored.LocalStorage;
using SecretsForMe.Core.Content;

namespace SecretsForMe.Core.Configuration;

public class SecretsManagerSetupProvider
{
    private readonly ConfigManager configManager;

    public SecretsManagerSetupProvider(ConfigManager configManager)
    {
        this.configManager = configManager;
    }

    public async Task CompleteSetup(string name, string password)
    {
        await configManager.ResetForNewSetup();
        var symmetricKey = await configManager.AddSymmetricKey("Default Key");
        var asymmetricKey = await configManager.AddAsymmetricKey();
        var credential = await configManager.AddCredential(asymmetricKey, name, password);
        await configManager.AddSymmetricKeyReference(symmetricKey, asymmetricKey);

        await configManager.AddContentProvider(symmetricKey, "Default IndexedDb", new Dictionary<string, string>
        {
            { nameof(IndexedDbContentProvider.ContentProviderTypeId), IndexedDbContentProvider.ContentProviderTypeId.ToString() }
        });

        await configManager.SaveConfiguration();
    }
}
