using Microsoft.JSInterop;

namespace NotesBin.App;

public class FileUtilities
{
    private readonly IJSRuntime js;

    public FileUtilities(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task DownloadFile(string fileName, string contentType, byte[] contents)
    {
        var fileUtilities = await js.InvokeAsync<IJSObjectReference>("import", "./assets/fileUtilities.js");
        await fileUtilities.InvokeVoidAsync("downloadFileBytes", fileName, contentType, contents);
    }
}