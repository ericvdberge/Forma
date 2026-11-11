using Forma.Api.Abstractions;
using Forma.Api.Dtos;
using Forma.Api.Mappers;
using Forma.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Forma.Api.Endpoints;

/// <summary>Endpoints for a website's media library: listing, uploading, serving and deleting images.</summary>
public class MediaEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/media").WithTags("Media");

        group.MapGet("/", GetMediaItemsAsync).WithName("GetMediaItems");
        group.MapGet("/{id:guid}", GetMediaItemAsync).WithName("GetMediaItem");
        group.MapGet("/{id:guid}/image", GetImageAsync)
            .WithName("GetMediaImage")
            .Produces<Stream>(StatusCodes.Status200OK, "image/jpeg", "image/png", "image/gif", "image/webp");
        group.MapGet("/{id:guid}/thumbnail", GetThumbnailAsync)
            .WithName("GetMediaThumbnail")
            .Produces<Stream>(StatusCodes.Status200OK, "image/webp");
        group.MapPost("/", UploadMediaAsync)
            .WithName("UploadMedia")
            .WithValidation<UploadMediaRequest>()
            .DisableAntiforgery();
        group.MapDelete("/{id:guid}", DeleteMediaAsync).WithName("DeleteMedia");
    }

    /// <summary>Lists the media items of a website, newest first.</summary>
    /// <param name="websiteId">The website whose media library is listed.</param>
    /// <param name="mediaCoreService">Service that reads the media library.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The website's media items, or an empty list when it has none.</returns>
    /// <response code="200">The website's media items, newest first.</response>
    internal static async Task<Ok<IReadOnlyList<MediaItemDto>>> GetMediaItemsAsync(
        Guid websiteId,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var media = await mediaCoreService.GetMediaItemsAsync(websiteId, cancellationToken);
        return TypedResults.Ok(media.ToDtos());
    }

    /// <summary>Gets a single media item's details.</summary>
    /// <param name="id">The media item's id.</param>
    /// <param name="mediaCoreService">Service that reads the media library.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The media item, or 404 when it does not exist.</returns>
    /// <response code="200">The media item.</response>
    /// <response code="404">No media item exists with this id.</response>
    internal static async Task<Results<Ok<MediaItemDto>, NotFound>> GetMediaItemAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var media = await mediaCoreService.GetMediaItemAsync(id, cancellationToken);
        return media is null ? TypedResults.NotFound() : TypedResults.Ok(media.ToDto());
    }

    /// <summary>Streams the original image as it was uploaded.</summary>
    /// <remarks>The response may be cached indefinitely, because a media item's files never change.</remarks>
    /// <param name="id">The media item's id.</param>
    /// <param name="mediaCoreService">Service that opens the stored image.</param>
    /// <param name="response">The HTTP response, used to set caching headers.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The image in its original format, or 404 when it does not exist.</returns>
    /// <response code="200">The original image (JPEG, PNG, GIF or WebP).</response>
    /// <response code="404">No media item or stored image exists with this id.</response>
    internal static async Task<Results<FileStreamHttpResult, NotFound>> GetImageAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        ToFileResult(await mediaCoreService.GetImageAsync(id, cancellationToken), response);

    /// <summary>Streams the image's WebP thumbnail.</summary>
    /// <remarks>The response may be cached indefinitely, because a media item's files never change.</remarks>
    /// <param name="id">The media item's id.</param>
    /// <param name="mediaCoreService">Service that opens the stored thumbnail.</param>
    /// <param name="response">The HTTP response, used to set caching headers.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The WebP thumbnail, or 404 when it does not exist.</returns>
    /// <response code="200">The WebP thumbnail.</response>
    /// <response code="404">No media item or stored thumbnail exists with this id.</response>
    internal static async Task<Results<FileStreamHttpResult, NotFound>> GetThumbnailAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        ToFileResult(await mediaCoreService.GetThumbnailAsync(id, cancellationToken), response);

    /// <summary>Uploads an image to a website's media library and creates its thumbnail.</summary>
    /// <remarks>
    /// Accepts JPEG, PNG, GIF and WebP files up to 10 MB. The file's contents are checked as well as
    /// its extension, so a file that is not really a supported image is rejected.
    /// </remarks>
    /// <param name="request">The multipart form with the website id and the image file.</param>
    /// <param name="mediaCoreService">Service that checks, stores and records the image.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created media item, a validation problem, or 404 when the website does not exist.</returns>
    /// <response code="201">The image was stored; the body is the new media item.</response>
    /// <response code="400">The file is missing, empty, too large, or not a supported image.</response>
    /// <response code="404">No website exists with the given id.</response>
    /// <exception cref="InvalidOperationException">The core service returned an upload result this endpoint does not handle.</exception>
    internal static async Task<Results<Created<MediaItemDto>, ValidationProblem, NotFound>> UploadMediaAsync(
        [FromForm] UploadMediaRequest request,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();
        var result = await mediaCoreService.UploadAsync(request.WebsiteId, request.File.FileName, content, cancellationToken);

        return result switch
        {
            MediaUploadResult.Uploaded uploaded =>
                TypedResults.Created($"/media/{uploaded.Media.Id}", uploaded.Media.ToDto()),
            MediaUploadResult.Rejected rejected =>
                TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(UploadMediaRequest.File)] = [rejected.Error] }),
            MediaUploadResult.WebsiteNotFound => TypedResults.NotFound(),
            _ => throw new InvalidOperationException($"Unhandled upload result {result.GetType().Name}."),
        };
    }

    /// <summary>Deletes a media item together with its original image and thumbnail.</summary>
    /// <param name="id">The media item's id.</param>
    /// <param name="mediaCoreService">Service that removes the media item and its files.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204 when the media item was deleted, or 404 when it does not exist.</returns>
    /// <response code="204">The media item and its files were deleted.</response>
    /// <response code="404">No media item exists with this id.</response>
    internal static async Task<Results<NoContent, NotFound>> DeleteMediaAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var deleted = await mediaCoreService.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static Results<FileStreamHttpResult, NotFound> ToFileResult((Stream Content, string ContentType)? file, HttpResponse response)
    {
        if (file is null)
        {
            return TypedResults.NotFound();
        }

        // A media item's files never change, so clients may cache them for good.
        response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        // The stream result disposes the stream once it has been sent.
        return TypedResults.Stream(file.Value.Content, file.Value.ContentType);
    }
}
