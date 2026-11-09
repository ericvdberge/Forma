using Forma.Api.Abstractions;
using Forma.Api.Dtos.Media;
using Forma.Api.Mappers.Media;
using Forma.Application.Services.Media;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Forma.Api.Endpoints.Media;

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

    private static async Task<Ok<IReadOnlyList<MediaItemDto>>> GetMediaItemsAsync(
        Guid websiteId,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var media = await mediaCoreService.GetMediaItemsAsync(websiteId, cancellationToken);
        return TypedResults.Ok(media.ToDtos());
    }

    private static async Task<Results<Ok<MediaItemDto>, NotFound>> GetMediaItemAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var media = await mediaCoreService.GetMediaItemAsync(id, cancellationToken);
        return media is null ? TypedResults.NotFound() : TypedResults.Ok(media.ToDto());
    }

    /// <summary>Sends the original image as it was uploaded.</summary>
    private static async Task<Results<FileStreamHttpResult, NotFound>> GetImageAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        ToFileResult(await mediaCoreService.GetImageAsync(id, cancellationToken), response);

    /// <summary>Sends the WebP thumbnail.</summary>
    private static async Task<Results<FileStreamHttpResult, NotFound>> GetThumbnailAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        HttpResponse response,
        CancellationToken cancellationToken) =>
        ToFileResult(await mediaCoreService.GetThumbnailAsync(id, cancellationToken), response);

    /// <summary>Uploads an image (JPEG, PNG, GIF or WebP, at most 10 MB) and generates its thumbnail.</summary>
    private static async Task<Results<Created<MediaItemDto>, ValidationProblem, NotFound>> UploadMediaAsync(
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

    private static async Task<Results<NoContent, NotFound>> DeleteMediaAsync(
        Guid id,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var deleted = await mediaCoreService.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static Results<FileStreamHttpResult, NotFound> ToFileResult(MediaFile? file, HttpResponse response)
    {
        if (file is null)
        {
            return TypedResults.NotFound();
        }

        // A media item's files never change, so clients may cache them for good.
        response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        // The stream result disposes the stream once it has been sent.
        return TypedResults.Stream(file.Content, file.ContentType);
    }
}
