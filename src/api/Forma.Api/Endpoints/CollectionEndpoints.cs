using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.Pages;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for a website's collections (<i>Blog posts</i>, <i>Team members</i>…) and their fields.
/// Specification: <c>docs/design/collections.md</c>.
/// </summary>
public class CollectionEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/collections").WithTags("Collections");

        group.MapGet("/", GetCollectionsAsync).WithName("GetCollections");

        group.MapGet("/{collectionId:guid}", GetCollectionAsync).WithName("GetCollection");

        group.MapPost("/", CreateCollectionAsync)
            .WithName("CreateCollection")
            .WithValidation<CreateCollectionRequest>();

        group.MapPut("/{collectionId:guid}", UpdateCollectionAsync)
            .WithName("UpdateCollection")
            .WithValidation<UpdateCollectionRequest>();

        group.MapDelete("/{collectionId:guid}", DeleteCollectionAsync).WithName("DeleteCollection");
    }

    /// <summary>Lists the collections of a website, by name.</summary>
    /// <param name="websiteId">The website whose collections are listed.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="pageCoreService">Service that reads the collections' detail pages.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The website's collections, or an empty list when it has none.</returns>
    /// <response code="200">The website's collections with their fields.</response>
    internal static async Task<Ok<IReadOnlyList<CollectionDto>>> GetCollectionsAsync(
        Guid websiteId,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var collections = await collectionCoreService.GetCollectionsAsync(new CollectionFilter { WebsiteId = websiteId }, cancellationToken);
        var detailPages = await pageCoreService.GetPagesAsync(
            new PageFilter { WebsiteId = websiteId, Kind = PageDaoKind.CollectionTemplate },
            cancellationToken);

        var detailPageIds = detailPages.ToDictionary(p => p.CollectionId!.Value, p => p.Id);
        var dtos = collections.Select(c => c.ToDto(detailPageIds.GetValueOrDefault(c.Id))).ToList();

        return TypedResults.Ok<IReadOnlyList<CollectionDto>>(dtos);
    }

    /// <summary>Gets a collection with its fields.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="pageCoreService">Service that reads the collection's detail page.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The collection, or 404 when it does not exist.</returns>
    /// <response code="200">The collection.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<CollectionDto>, NotFound>> GetCollectionAsync(
        Guid collectionId,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var detailPage = await GetDetailPageAsync(collection, pageCoreService, cancellationToken);

        return TypedResults.Ok(collection.ToDto(detailPage?.Id));
    }

    /// <summary>Creates a collection and the detail page its items are shown with.</summary>
    /// <remarks>The detail page starts empty; design it in the studio like any page.</remarks>
    /// <param name="request">The collection's names, slug and fields.</param>
    /// <param name="collectionCoreService">Service that stores collections.</param>
    /// <param name="pageCoreService">Service that creates the detail page.</param>
    /// <param name="websiteCoreService">Service that reads websites.</param>
    /// <param name="validationService">Service that checks the slug is free.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The new collection, a validation problem, or 404 when the website does not exist.</returns>
    /// <response code="201">Created; the body is the collection.</response>
    /// <response code="400">The request is invalid, or another collection uses the slug.</response>
    /// <response code="404">No website exists with this id.</response>
    internal static async Task<Results<Created<CollectionDto>, ValidationProblem, NotFound>> CreateCollectionAsync(
        CreateCollectionRequest request,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        IWebsiteCoreService websiteCoreService,
        ICollectionValidationService validationService,
        CancellationToken cancellationToken)
    {
        var websites = await websiteCoreService.GetWebsitesAsync(cancellationToken);
        if (websites.All(w => w.Id != request.WebsiteId))
            return TypedResults.NotFound();

        var errors = await validationService.ValidateAsync(request.WebsiteId, request.Slug, null, cancellationToken);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var collection = await collectionCoreService.CreateAsync(new CollectionInsert
        {
            WebsiteId = request.WebsiteId,
            Name = request.Name.Trim(),
            SingularName = request.SingularName.Trim(),
            Slug = request.Slug,
            Fields = request.Fields.Select(f => f.ToField()).ToList(),
        }, cancellationToken);

        var detailPage = await pageCoreService.CreateAsync(new PageInsert
        {
            WebsiteId = collection.WebsiteId,
            Title = DetailPageTitle(collection),
            Slug = DetailPageSlug(collection),
            Kind = PageKind.CollectionTemplate,
            CollectionId = collection.Id,
        }, cancellationToken);

        return TypedResults.Created($"/collections/{collection.Id}", collection.ToDto(detailPage.Id));
    }

    /// <summary>Changes a collection's names, slug and fields.</summary>
    /// <remarks>
    /// Items keep their values for fields that stay. Values of removed fields are dropped the next time an item is saved.
    /// Pages that show a removed field report it when they are next published.
    /// </remarks>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="request">The collection's names, slug and complete list of fields.</param>
    /// <param name="collectionCoreService">Service that stores collections.</param>
    /// <param name="pageCoreService">Service that keeps the detail page's address in step.</param>
    /// <param name="validationService">Service that checks the slug is free.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The changed collection, a validation problem, or 404.</returns>
    /// <response code="200">Saved; the body is the collection.</response>
    /// <response code="400">The request is invalid, or another collection uses the slug.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<CollectionDto>, ValidationProblem, NotFound>> UpdateCollectionAsync(
        Guid collectionId,
        UpdateCollectionRequest request,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        ICollectionValidationService validationService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var errors = await validationService.ValidateAsync(collection.WebsiteId, request.Slug, collection.Id, cancellationToken);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var updated = await collectionCoreService.UpdateAsync(collection.Id, new CollectionUpdate
        {
            Name = request.Name.Trim(),
            SingularName = request.SingularName.Trim(),
            Slug = request.Slug,
            Fields = request.Fields.Select(f => f.ToField()).ToList(),
        }, cancellationToken);

        var detailPage = await GetDetailPageAsync(updated!, pageCoreService, cancellationToken);

        if (detailPage is not null)
            await pageCoreService.UpdateAsync(
                detailPage.Id,
                new PageUpdate { Title = DetailPageTitle(updated!), Slug = DetailPageSlug(updated!) },
                cancellationToken);

        return TypedResults.Ok(updated!.ToDto(detailPage?.Id));
    }

    /// <summary>Deletes a collection, its items and its detail page.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="collectionCoreService">Service that deletes collections.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204 when deleted, or 404.</returns>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<NoContent, NotFound>> DeleteCollectionAsync(
        Guid collectionId,
        ICollectionCoreService collectionCoreService,
        CancellationToken cancellationToken)
    {
        var deleted = await collectionCoreService.DeleteAsync(collectionId, cancellationToken);

        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static Task<Page?> GetDetailPageAsync(Collection collection, IPageCoreService pageCoreService, CancellationToken cancellationToken) =>
        pageCoreService.GetPageAsync(
            new PageFilter { WebsiteId = collection.WebsiteId, CollectionId = collection.Id, Kind = PageDaoKind.CollectionTemplate },
            cancellationToken);

    /// <summary>E.g. <c>Blog post page</c>.</summary>
    private static string DetailPageTitle(Collection collection) => $"{collection.SingularName} page";

    /// <summary>E.g. <c>blog/{item}</c>: the address every item is shown at.</summary>
    private static string DetailPageSlug(Collection collection) => $"{collection.Slug}/{{item}}";
}
