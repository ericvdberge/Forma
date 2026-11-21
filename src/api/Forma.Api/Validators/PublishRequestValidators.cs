using FluentValidation;
using Forma.Api.Dtos;

namespace Forma.Api.Validators;

/// <summary>A publish names a website and at most <see cref="MaxEntries"/> drafts. Whether they are the website's is checked by the endpoint.</summary>
public class PublishRequestValidator : AbstractValidator<PublishRequest>
{
    public const int MaxEntries = 500;

    public PublishRequestValidator()
    {
        RuleFor(r => r.WebsiteId).NotEmpty();

        RuleFor(r => r.Collections.Count + r.Pages.Count + r.Items.Count)
            .LessThanOrEqualTo(MaxEntries).WithMessage($"Publish at most {MaxEntries} changes at once.")
            .OverridePropertyName("entries");
    }
}

/// <summary>A scheduled publish names a website, a time and its drafts. Whether the time is in the future is checked by the endpoint.</summary>
public class SchedulePublishRequestValidator : AbstractValidator<SchedulePublishRequest>
{
    public SchedulePublishRequestValidator()
    {
        RuleFor(r => r.WebsiteId).NotEmpty();

        RuleFor(r => r.Collections.Count + r.Pages.Count + r.Items.Count)
            .LessThanOrEqualTo(PublishRequestValidator.MaxEntries).WithMessage($"Publish at most {PublishRequestValidator.MaxEntries} changes at once.")
            .OverridePropertyName("entries");
    }
}

/// <summary>The three lists of what is in a scheduled publish go together.</summary>
public class UpdateScheduledPublishRequestValidator : AbstractValidator<UpdateScheduledPublishRequest>
{
    public UpdateScheduledPublishRequestValidator()
    {
        RuleFor(r => r)
            .Must(r => (r.Collections is null) == (r.Pages is null) && (r.Pages is null) == (r.Items is null))
            .WithMessage("Send collections, pages and items together, or none of them.")
            .OverridePropertyName("entries");
    }
}
