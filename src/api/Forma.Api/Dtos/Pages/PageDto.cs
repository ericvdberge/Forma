using Forma.Application.Entities.Pages;

namespace Forma.Api.Dtos.Pages;

public record PageDto(
    Guid Id,
    Guid WebsiteId,
    string Title,
    string Slug,
    PageStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
