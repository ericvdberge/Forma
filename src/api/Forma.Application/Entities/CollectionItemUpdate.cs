using System.Text.Json;

namespace Forma.Application.Entities;

/// <summary>
/// Changes to a collection item. Every property is optional: a property that is <c>null</c> is left as it is.
/// Publishing is an update that sets <see cref="Status"/> to <see cref="CollectionItemStatus.Published"/>.
/// </summary>
public class CollectionItemUpdate
{
    public string? Slug { get; init; }
    public IReadOnlyDictionary<string, JsonElement>? Values { get; init; }
    public CollectionItemStatus? Status { get; init; }
}
