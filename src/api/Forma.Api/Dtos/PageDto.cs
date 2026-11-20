using Forma.Application.Entities;

namespace Forma.Api.Dtos;

/// <summary>A page of a website.</summary>
/// <param name="Id">The page's id.</param>
/// <param name="WebsiteId">The website the page belongs to.</param>
/// <param name="Title">The page's title.</param>
/// <param name="Slug">The page's address without the leading slash; <c>{collection}/{item}</c> for a detail page.</param>
/// <param name="Status">Whether the page is published.</param>
/// <param name="Kind"><c>static</c> for a page of its own; <c>collectionTemplate</c> for a collection's detail page.</param>
/// <param name="CollectionId">The collection a detail page shows the items of; <c>null</c> for other pages.</param>
/// <param name="CreatedAt">When the page was created (UTC).</param>
/// <param name="UpdatedAt">When the page last changed (UTC).</param>
public record PageDto(
    Guid Id,
    Guid WebsiteId,
    string Title,
    string Slug,
    PageStatus Status,
    PageKind Kind,
    Guid? CollectionId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
