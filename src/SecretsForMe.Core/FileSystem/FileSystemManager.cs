
namespace SecretsForMe.Core.FileSystem;

public class FileSystemManager
{
    public FileSystemManager()
    {

    }

    public async Task InitializeFileSystem(LocalFileSystem localFileSystem)
    {
        await localFileSystem.Initialize();
        //await localFileSystem.GetIndex();
    }

    public async Task<IEnumerable<IndexEntry>> GetRootEntries()
    {
        return Enumerable.Empty<IndexEntry>();
    }
}
