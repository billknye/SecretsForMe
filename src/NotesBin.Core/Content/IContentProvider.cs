namespace NotesBin.Core.Content;

public interface IContentProvider
{
    Task Initialize();

    IEnumerable<BlobDirectory>? GetDirectories(Guid? parent);

    IEnumerable<BlobContentItem>? GetContentItems(Guid? parent);
}
