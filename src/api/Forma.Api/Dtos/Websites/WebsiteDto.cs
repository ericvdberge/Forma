using Forma.Application.Entities.Websites;

namespace Forma.Api.Dtos.Websites;

public record WebsiteDto(
    Guid Id,
    string Name,
    string Domain,
    string Description,
    WebsiteStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
