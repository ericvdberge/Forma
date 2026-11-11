using Forma.Application.Entities;

namespace Forma.Api.Dtos;

public record PageDto(
    Guid Id,
    Guid WebsiteId,
    string Title,
    string Slug,
    PageStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
