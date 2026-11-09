namespace Forma.Api.Dtos.Media;

/// <summary>Multipart form for <c>POST /media</c>.</summary>
public record UploadMediaRequest(Guid WebsiteId, IFormFile File);
