using Forma.Application.Entities;

namespace Forma.Application.Services;

/// <summary>
/// A website as visitors see it, or would see it after a publish: what the content rules look things up in.
/// <see cref="Collections"/> and <see cref="Items"/> hold the live slug, fields and values (or the draft's, for what is
/// being published), and only what is, or will be, live.
/// </summary>
/// <param name="Media">Ids of the website's media items.</param>
/// <param name="Pages">The website's pages.</param>
/// <param name="Collections">The live collections, with the slug and fields visitors would see.</param>
/// <param name="Items">The live items of those collections, with the slug and values visitors would see.</param>
public sealed record SiteSnapshot(
    IReadOnlySet<Guid> Media,
    IReadOnlyList<Page> Pages,
    IReadOnlyList<Collection> Collections,
    IReadOnlyList<CollectionItem> Items);
