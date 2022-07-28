using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecretsForMe.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretsForMe.Core.Content;

public static class ContentProviderFactory
{
    // TODO refactor, yuck
    public static IContentProvider GetContentProvider(ILoggerFactory loggerFactory, Guid contentProviderId, LoadedSymmetricKey loadedSymmetricKey, IJSRuntime js, ICryptoProvider cryptoProvider, Dictionary<string, string> providerOptions)
    {
        if (!providerOptions.TryGetValue("ContentProviderTypeId", out var typeId))
            throw new InvalidOperationException();

        if (!Guid.TryParse(typeId, out var typeGuid))
            throw new InvalidOperationException();

        if (typeGuid == IndexedDbContentProvider.ContentProviderTypeId)
        {
            var indexedDb = new IndexedDbContentProvider(loggerFactory, loggerFactory.CreateLogger<IndexedDbContentProvider>(), contentProviderId, loadedSymmetricKey, js, cryptoProvider);
            return indexedDb;
        }

        throw new InvalidOperationException();
    }
}
