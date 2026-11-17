using Forma.Application.Entities;

namespace Forma.Api.Dtos;

public record WebsiteDto(
    Guid Id,
    string Name,
    string Domain,
    string Description,
    WebsiteStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
