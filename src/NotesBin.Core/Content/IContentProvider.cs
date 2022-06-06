namespace NotesBin.Core.Content;

public interface IContentProvider
{
    Task Initialize();

    IEnumerable<BlobDirectory>? GetDirectories(Guid? parent);

    IEnumerable<BlobContentItem>? GetContentItems(Guid? parent);

    Task<(BlobContentItem Item, byte[] Data)?> GetContentItem(Guid itemId);

    Task CreateContentItem();

    Task UpdateContentItem(Guid contentItemId, string name, string contentType, byte[] content);
}
