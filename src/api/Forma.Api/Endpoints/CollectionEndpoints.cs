using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Entities;
using Forma.Application.Publishing;
using Forma.Application.Services;
using Forma.Infrastructure.Daos;
using Forma.Infrastructure.Repositories.Collections;
using Forma.Infrastructure.Repositories.PageBindingUsages;
using Forma.Infrastructure.Repositories.Pages;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>
/// Endpoints for a website's collections (<i>Blog posts</i>, <i>Team members</i>…) and their fields. A collection's slug
/// and fields are a draft until they are published; its names and icon are saved directly.
/// Specification: <c>docs/design/collections.md</c> and <c>docs/design/drafts-and-publish.md</c>.
/// </summary>
public class CollectionEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/collections").WithTags("Collections");

        group.MapGet("/", GetCollectionsAsync).WithName("GetCollections");

        group.MapGet("/{collectionId:guid}", GetCollectionAsync).WithName("GetCollection");

        group.MapGet("/{collectionId:guid}/usages", GetUsagesAsync).WithName("GetCollectionFieldUsages");

        group.MapPost("/", CreateCollectionAsync)
            .WithName("CreateCollection")
            .WithValidation<CreateCollectionRequest>();

        group.MapPut("/{collectionId:guid}", UpdateCollectionAsync)
            .WithName("UpdateCollection")
            .WithValidation<UpdateCollectionRequest>();

        group.MapPut("/{collectionId:guid}/draft", SaveDraftAsync)
            .WithName("SaveCollectionDraft")
            .WithValidation<SaveCollectionDraftRequest>();

        group.MapDelete("/{collectionId:guid}/draft", DiscardDraftAsync).WithName("DiscardCollectionDraft");

        group.MapDelete("/{collectionId:guid}", DeleteCollectionAsync).WithName("DeleteCollection");
    }

    /// <summary>Lists the collections of a website, by name.</summary>
    /// <param name="websiteId">The website whose collections are listed.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="pageCoreService">Service that reads the collections' detail pages.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The website's collections, or an empty list when it has none.</returns>
    /// <response code="200">The website's collections with their fields and drafts.</response>
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

    /// <summary>Gets a collection with its fields and draft.</summary>
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

    /// <summary>Lists which live pages show which of the collection's fields: "Used by…" in the field editor.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="usageCoreService">Service that reads which fields pages show.</param>
    /// <param name="pageCoreService">Service that reads the pages' titles.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The usages, or 404 when the collection does not exist.</returns>
    /// <response code="200">One entry per page and field it shows.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<IReadOnlyList<FieldUsageDto>>, NotFound>> GetUsagesAsync(
        Guid collectionId,
        ICollectionCoreService collectionCoreService,
        IPageBindingUsageCoreService usageCoreService,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var usages = await usageCoreService.GetPageBindingUsagesAsync(
            new PageBindingUsageFilter { CollectionId = collection.Id, WebsiteId = collection.WebsiteId },
            cancellationToken);

        var pages = await pageCoreService.GetPagesAsync(new PageFilter { WebsiteId = collection.WebsiteId }, cancellationToken);
        var titles = pages.ToDictionary(p => p.Id, p => p.Title);

        var dtos = usages
            .Where(u => titles.ContainsKey(u.PageId))
            .Select(u => new FieldUsageDto(u.FieldId, u.PageId, titles[u.PageId], u.Kind))
            .OrderBy(u => u.PageTitle)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<FieldUsageDto>>(dtos);
    }

    /// <summary>Creates a collection, as a draft, and the detail page its items are shown with.</summary>
    /// <remarks>Visitors don't see the collection until it is published. The detail page starts empty; design it in the studio like any page.</remarks>
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

        var fields = request.Fields.Select(f => f.ToField()).ToList();

        var collection = await collectionCoreService.CreateAsync(new CollectionInsert
        {
            WebsiteId = request.WebsiteId,
            Name = request.Name.Trim(),
            SingularName = request.SingularName.Trim(),
            Slug = request.Slug,
            Icon = request.Icon,
            Fields = fields,
            DraftSlug = request.Slug,
            DraftFields = fields,
        }, cancellationToken);

        var detailPage = await pageCoreService.CreateAsync(new PageInsert
        {
            WebsiteId = collection.WebsiteId,
            Title = DetailPageTitle(collection),
            Slug = DetailPageSlug(collection.Slug),
            Kind = PageKind.CollectionTemplate,
            CollectionId = collection.Id,
        }, cancellationToken);

        return TypedResults.Created($"/collections/{collection.Id}", collection.ToDto(detailPage.Id));
    }

    /// <summary>Changes a collection's names and icon. These can't break anything, so they are saved directly.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="request">The collection's names and icon.</param>
    /// <param name="collectionCoreService">Service that stores collections.</param>
    /// <param name="pageCoreService">Service that keeps the detail page's title in step.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The changed collection, a validation problem, or 404.</returns>
    /// <response code="200">Saved; the body is the collection.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<CollectionDto>, ValidationProblem, NotFound>> UpdateCollectionAsync(
        Guid collectionId,
        UpdateCollectionRequest request,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var updated = await collectionCoreService.UpdateAsync(collection.Id, new CollectionUpdate
        {
            Name = request.Name.Trim(),
            SingularName = request.SingularName.Trim(),
            Icon = request.Icon,
        }, cancellationToken);

        var detailPage = await GetDetailPageAsync(updated!, pageCoreService, cancellationToken);

        if (detailPage is not null)
            await pageCoreService.UpdateAsync(detailPage.Id, new PageUpdate { Title = DetailPageTitle(updated!) }, cancellationToken);

        return TypedResults.Ok(updated!.ToDto(detailPage?.Id));
    }

    /// <summary>Saves a collection's draft: its slug and fields, and where the values of removed fields go.</summary>
    /// <remarks>
    /// Nothing changes on the site until the draft is published. Then the values of each removed field move to the field
    /// <c>valueMoves</c> names, or are removed; pages that show a removed field must be published with it.
    /// </remarks>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="request">The slug, the complete list of fields and the value moves.</param>
    /// <param name="collectionCoreService">Service that stores collections.</param>
    /// <param name="pageCoreService">Service that reads the detail page.</param>
    /// <param name="validationService">Service that checks the slug is free.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The collection with its draft, a validation problem, or 404.</returns>
    /// <response code="200">Saved; the body is the collection.</response>
    /// <response code="400">The request is invalid, another collection uses the slug, or a move doesn't start at a removed field.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<CollectionDto>, ValidationProblem, NotFound>> SaveDraftAsync(
        Guid collectionId,
        SaveCollectionDraftRequest request,
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

        var fields = request.Fields.Select(f => f.ToField()).ToList();

        if (MoveProblems(collection, fields, request.ValueMoves) is { Count: > 0 } moveErrors)
            return TypedResults.ValidationProblem(moveErrors);

        var updated = await collectionCoreService.UpdateAsync(collection.Id, new CollectionUpdate
        {
            DraftSlug = request.Slug,
            DraftFields = fields,
            DraftValueMoves = request.ValueMoves,
        }, cancellationToken);

        var detailPage = await GetDetailPageAsync(updated!, pageCoreService, cancellationToken);

        return TypedResults.Ok(updated!.ToDto(detailPage?.Id));
    }

    /// <summary>Discards a collection's draft: its slug and fields go back to the live version, and it leaves any scheduled publish.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="collectionCoreService">Service that stores collections.</param>
    /// <param name="pageCoreService">Service that reads the detail page.</param>
    /// <param name="scheduledPublishService">Service that takes the collection out of its scheduled publish.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The collection, a validation problem, or 404.</returns>
    /// <response code="200">The draft is gone; the body is the collection.</response>
    /// <response code="400">The collection was never published, so there is no live version to go back to.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<CollectionDto>, ValidationProblem, NotFound>> DiscardDraftAsync(
        Guid collectionId,
        ICollectionCoreService collectionCoreService,
        IPageCoreService pageCoreService,
        IScheduledPublishService scheduledPublishService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        if (!collection.IsLive)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["draft"] = [$"{collection.Name} was never published, so there is nothing to go back to. Delete it instead."] });

        var updated = await collectionCoreService.UpdateAsync(collection.Id, new CollectionUpdate { ClearDraft = true }, cancellationToken);

        await scheduledPublishService.RemoveAsync(collection.WebsiteId, DraftKind.Collection, collection.Id, cancellationToken);

        var detailPage = await GetDetailPageAsync(updated!, pageCoreService, cancellationToken);

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

    /// <summary>A value can only move away from a live field the draft removes, to a field the draft keeps.</summary>
    private static Dictionary<string, string[]> MoveProblems(Collection collection, IReadOnlyList<CollectionField> fields, IReadOnlyDictionary<string, string?> moves)
    {
        var errors = new Dictionary<string, string[]>();
        var removed = collection.Fields.Where(f => fields.All(d => d.Id != f.Id)).Select(f => f.Id).ToHashSet();

        foreach (var from in moves.Keys.Where(from => !removed.Contains(from)))
            errors[$"valueMoves.{from}"] = ["Values can only move away from a field you remove."];

        return errors;
    }

    private static Task<Page?> GetDetailPageAsync(Collection collection, IPageCoreService pageCoreService, CancellationToken cancellationToken) =>
        pageCoreService.GetPageAsync(
            new PageFilter { WebsiteId = collection.WebsiteId, CollectionId = collection.Id, Kind = PageDaoKind.CollectionTemplate },
            cancellationToken);

    /// <summary>E.g. <c>Blog post page</c>.</summary>
    private static string DetailPageTitle(Collection collection) => $"{collection.SingularName} page";

    /// <summary>E.g. <c>blog/{item}</c>: the address every item is shown at.</summary>
    private static string DetailPageSlug(string slug) => $"{slug}/{{item}}";
}
