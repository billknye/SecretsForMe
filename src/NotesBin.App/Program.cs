using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using NotesBin.App;
using NotesBin.Core;
using NotesBin.Core.Configuration;
using NotesBin.Core.FileSystem;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<ConfigManager>();
builder.Services.AddScoped<ICryptoProvider, JsCryptoProvider>();
builder.Services.AddScoped<IBlobProvider, IndexedDbBlobProvider>();
builder.Services.AddScoped<NotesManagerSetupProvider>();
builder.Services.AddScoped<FileUtilities>();
builder.Services.AddScoped<FileSystemManager>();

var host = builder.Build();

var config = host.Services.GetRequiredService<ConfigManager>();
await config.Initialize();

var nav = host.Services.GetRequiredService<NavigationManager>();
if (config.State == ConfigState.Loaded)
{
    nav.NavigateTo("/config/unlock");
}
else
{
    nav.NavigateTo("/config/setup");
}

await host.RunAsync();
