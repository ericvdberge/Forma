using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Entities;
using Forma.Application.Services;
using Forma.Infrastructure.Repositories.CollectionItems;
using Forma.Infrastructure.Repositories.Collections;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Forma.Api.Endpoints;

/// <summary>Endpoints for the items of a collection, e.g. the posts of a blog.</summary>
public class CollectionItemEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/collections/{collectionId:guid}/items").WithTags("Collection items");

        group.MapGet("/", GetItemsAsync).WithName("GetCollectionItems");

        group.MapGet("/{itemId:guid}", GetItemAsync).WithName("GetCollectionItem");

        group.MapPost("/", CreateItemAsync)
            .WithName("CreateCollectionItem")
            .WithValidation<SaveCollectionItemRequest>();

        group.MapPut("/{itemId:guid}", SaveItemAsync)
            .WithName("SaveCollectionItem")
            .WithValidation<SaveCollectionItemRequest>();

        group.MapDelete("/{itemId:guid}", DeleteItemAsync).WithName("DeleteCollectionItem");
    }

    /// <summary>Lists a collection's items, newest first.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="itemCoreService">Service that reads items.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The items, or 404 when the collection does not exist.</returns>
    /// <response code="200">The collection's items, newest first.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Ok<IReadOnlyList<CollectionItemDto>>, NotFound>> GetItemsAsync(
        Guid collectionId,
        ICollectionCoreService collectionCoreService,
        ICollectionItemCoreService itemCoreService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var items = await itemCoreService.GetCollectionItemsAsync(
            new CollectionItemFilter { CollectionId = collection.Id },
            cancellationToken);

        return TypedResults.Ok(items.ToDtos());
    }

    /// <summary>Gets one item of a collection.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <param name="itemCoreService">Service that reads items.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The item, or 404 when the collection has no such item.</returns>
    /// <response code="200">The item.</response>
    /// <response code="404">The collection has no item with this id.</response>
    internal static async Task<Results<Ok<CollectionItemDto>, NotFound>> GetItemAsync(
        Guid collectionId,
        Guid itemId,
        ICollectionItemCoreService itemCoreService,
        CancellationToken cancellationToken)
    {
        var item = await itemCoreService.GetCollectionItemAsync(new CollectionItemFilter { Id = itemId, CollectionId = collectionId }, cancellationToken);
        if (item is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(item.ToDto());
    }

    /// <summary>Adds an item to a collection.</summary>
    /// <remarks>Values are checked against the collection's fields: types, required fields, links, images and rich text.</remarks>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="request">The item's slug, values and status.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="itemCoreService">Service that stores items.</param>
    /// <param name="validationService">Service that checks the values.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The new item, a validation problem, or 404.</returns>
    /// <response code="201">Created; the body is the item.</response>
    /// <response code="400">The request is invalid; errors are keyed <c>slug</c> or <c>values.{fieldId}</c>.</response>
    /// <response code="404">No collection exists with this id.</response>
    internal static async Task<Results<Created<CollectionItemDto>, ValidationProblem, NotFound>> CreateItemAsync(
        Guid collectionId,
        SaveCollectionItemRequest request,
        ICollectionCoreService collectionCoreService,
        ICollectionItemCoreService itemCoreService,
        ICollectionItemValidationService validationService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var errors = await validationService.ValidateAsync(collection, request.Slug, request.Values, null, cancellationToken);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var item = await itemCoreService.CreateAsync(new CollectionItemInsert
        {
            CollectionId = collection.Id,
            Slug = request.Slug,
            Values = request.Values,
            Status = request.Status,
        }, cancellationToken);

        return TypedResults.Created($"/collections/{collection.Id}/items/{item.Id}", item.ToDto());
    }

    /// <summary>Saves an item: its slug, values and status. Publishing is saving with status <c>published</c>.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <param name="request">The item's slug, complete values and status.</param>
    /// <param name="collectionCoreService">Service that reads collections.</param>
    /// <param name="itemCoreService">Service that stores items.</param>
    /// <param name="validationService">Service that checks the values.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The saved item, a validation problem, or 404.</returns>
    /// <response code="200">Saved; the body is the item.</response>
    /// <response code="400">The request is invalid; errors are keyed <c>slug</c> or <c>values.{fieldId}</c>.</response>
    /// <response code="404">The collection has no item with this id.</response>
    internal static async Task<Results<Ok<CollectionItemDto>, ValidationProblem, NotFound>> SaveItemAsync(
        Guid collectionId,
        Guid itemId,
        SaveCollectionItemRequest request,
        ICollectionCoreService collectionCoreService,
        ICollectionItemCoreService itemCoreService,
        ICollectionItemValidationService validationService,
        CancellationToken cancellationToken)
    {
        var collection = await collectionCoreService.GetCollectionAsync(new CollectionFilter { Id = collectionId }, cancellationToken);
        if (collection is null)
            return TypedResults.NotFound();

        var item = await itemCoreService.GetCollectionItemAsync(new CollectionItemFilter { Id = itemId, CollectionId = collection.Id }, cancellationToken);
        if (item is null)
            return TypedResults.NotFound();

        var errors = await validationService.ValidateAsync(collection, request.Slug, request.Values, item.Id, cancellationToken);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var saved = await itemCoreService.UpdateAsync(item.Id, new CollectionItemUpdate
        {
            Slug = request.Slug,
            Values = request.Values,
            Status = request.Status,
        }, cancellationToken);

        return TypedResults.Ok(saved!.ToDto());
    }

    /// <summary>Deletes an item.</summary>
    /// <param name="collectionId">The collection's id.</param>
    /// <param name="itemId">The item's id.</param>
    /// <param name="itemCoreService">Service that deletes items.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204 when deleted, or 404.</returns>
    /// <response code="204">Deleted.</response>
    /// <response code="404">The collection has no item with this id.</response>
    internal static async Task<Results<NoContent, NotFound>> DeleteItemAsync(
        Guid collectionId,
        Guid itemId,
        ICollectionItemCoreService itemCoreService,
        CancellationToken cancellationToken)
    {
        var item = await itemCoreService.GetCollectionItemAsync(new CollectionItemFilter { Id = itemId, CollectionId = collectionId }, cancellationToken);
        if (item is null)
            return TypedResults.NotFound();

        await itemCoreService.DeleteAsync(item.Id, cancellationToken);

        return TypedResults.NoContent();
    }
}
