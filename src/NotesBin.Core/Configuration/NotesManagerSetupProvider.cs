using Blazored.LocalStorage;

namespace NotesBin.Core.Configuration;

public class NotesManagerSetupProvider
{
    private readonly ConfigManager configManager;

    public NotesManagerSetupProvider(ConfigManager configManager)
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

        //var contentProvider = 

        await configManager.SaveConfiguration();
    }
}
