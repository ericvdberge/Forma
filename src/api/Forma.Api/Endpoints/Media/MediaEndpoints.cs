using Forma.Api.Abstractions;
using Forma.Api.Dtos.Media;
using Forma.Api.Mappers.Media;
using Forma.Application.Services.Media;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Forma.Api.Endpoints.Media;

public class MediaEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/media").WithTags("Media");

        group.MapGet("/", GetMediaAsync).WithName("GetMedia");
        group.MapGet("/{id:guid}", GetMediaItemAsync).WithName("GetMediaItem");
        group.MapPost("/", UploadMediaAsync)
            .WithName("UploadMedia")
            .DisableAntiforgery();
        group.MapDelete("/{id:guid}", DeleteMediaAsync).WithName("DeleteMedia");
    }

    private static async Task<Ok<IReadOnlyList<MediaItemDto>>> GetMediaAsync(
        Guid websiteId,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        var media = await mediaCoreService.GetMediaAsync(websiteId, cancellationToken);
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

    /// <summary>Uploads an image (JPEG, PNG, GIF or WebP, at most 10 MB) and generates its thumbnail.</summary>
    private static async Task<Results<Created<MediaItemDto>, ValidationProblem, NotFound>> UploadMediaAsync(
        [FromForm] Guid websiteId,
        IFormFile file,
        IMediaCoreService mediaCoreService,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await mediaCoreService.UploadAsync(websiteId, file.FileName, file.Length, content, cancellationToken);

        return result switch
        {
            MediaUploadResult.Uploaded uploaded =>
                TypedResults.Created($"/media/{uploaded.Media.Id}", uploaded.Media.ToDto()),
            MediaUploadResult.Rejected rejected =>
                TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [rejected.Error] }),
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
}
