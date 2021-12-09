namespace NotesBin.Core.Persistence;

public record PersistedIndexEntry(Guid Key, string Name, DateTimeOffset LastUpdated, string ContentType, IEnumerable<PersistedIndexEntry> Children);