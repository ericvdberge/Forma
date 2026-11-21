using Forma.Api.Dtos;
using Forma.Application.Entities;
using Forma.Application.Publishing;

namespace Forma.Api.Mappers;

public static class PublishDtoMapper
{
    public static EntityRefDto ToDto(this EntityRef entity) => new(entity.Kind, entity.Id, entity.Name, entity.CollectionId);

    public static DraftDto ToDto(this DraftSummary draft) => new(
        draft.Entity.ToDto(),
        draft.Summary,
        draft.Details,
        draft.IsNew,
        draft.UpdatedAt,
        draft.ScheduledPublish?.Id,
        draft.ScheduledPublish?.PublishAt);

    public static PublishIssueDto ToDto(this PublishIssue issue) => new(
        issue.Severity,
        issue.Code,
        issue.Message,
        issue.Entity?.ToDto(),
        issue.SectionId,
        issue.BlockId,
        issue.Field,
        issue.ItemIndex);

    public static IReadOnlyList<PublishIssueDto> ToDtos(this IEnumerable<PublishIssue> issues) => issues.Select(ToDto).ToList();

    public static PublishCheckDto ToDto(this PublishCheck check) => new(
        check.CanPublish,
        check.Entities.Select(ToDto).ToList(),
        check.Issues.ToDtos(),
        check.Needs.Select(n => new PublishNeedDto(n.Entity.ToDto(), n.NeededBy.Select(ToDto).ToList())).ToList());

    public static ScheduledPublishDto ToDto(this ScheduledPublishDetails details) => new(
        details.Publish.Id,
        details.Publish.WebsiteId,
        details.Publish.PublishAt,
        details.Publish.Status,
        details.Entries.Select(ToDto).ToList(),
        details.Publish.Issues.ToDtos(),
        details.Publish.CreatedAt,
        details.Publish.CompletedAt);

    public static PublishSelection ToSelection(this PublishRequest request) =>
        new(request.WebsiteId, request.Collections.Distinct().ToList(), request.Pages.Distinct().ToList(), request.Items.Distinct().ToList());
}
