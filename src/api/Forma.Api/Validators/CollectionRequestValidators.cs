using FluentValidation;
using Forma.Api.Dtos;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;

namespace Forma.Api.Validators;

/// <summary>A new collection's names, slug and fields. A slug another collection uses is checked by the endpoint.</summary>
public class CreateCollectionRequestValidator : AbstractValidator<CreateCollectionRequest>
{
    public CreateCollectionRequestValidator()
    {
        RuleFor(r => r.WebsiteId).NotEmpty();

        RuleFor(r => r.Name).NotEmpty().MaximumLength(CollectionLimits.MaxNameLength);

        RuleFor(r => r.SingularName).NotEmpty().MaximumLength(CollectionLimits.MaxNameLength);

        RuleFor(r => r.Slug)
            .MaximumLength(CollectionLimits.MaxSlugLength)
            .Must(DocumentRules.IsSlug).WithMessage("Use lowercase letters, digits and dashes, e.g. blog.");

        RuleFor(r => r.Icon)
            .Must(CollectionIcons.All.Contains).WithMessage("Choose one of the collection icons.");

        RuleFor(r => r.Fields.Count)
            .LessThanOrEqualTo(CollectionLimits.MaxFields).WithMessage($"A collection can have at most {CollectionLimits.MaxFields} fields.")
            .OverridePropertyName(nameof(CreateCollectionRequest.Fields));

        RuleFor(r => r.Fields)
            .Must(CollectionLimits.HaveUniqueIds).WithMessage("Every field needs its own id.");

        RuleForEach(r => r.Fields).SetValidator(new CollectionFieldRequestValidator());
    }
}

/// <summary>A collection's changed names, slug and fields. A slug another collection uses is checked by the endpoint.</summary>
public class UpdateCollectionRequestValidator : AbstractValidator<UpdateCollectionRequest>
{
    public UpdateCollectionRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(CollectionLimits.MaxNameLength);

        RuleFor(r => r.SingularName).NotEmpty().MaximumLength(CollectionLimits.MaxNameLength);

        RuleFor(r => r.Slug)
            .MaximumLength(CollectionLimits.MaxSlugLength)
            .Must(DocumentRules.IsSlug).WithMessage("Use lowercase letters, digits and dashes, e.g. blog.");

        RuleFor(r => r.Icon)
            .Must(CollectionIcons.All.Contains).WithMessage("Choose one of the collection icons.");

        RuleFor(r => r.Fields.Count)
            .LessThanOrEqualTo(CollectionLimits.MaxFields).WithMessage($"A collection can have at most {CollectionLimits.MaxFields} fields.")
            .OverridePropertyName(nameof(UpdateCollectionRequest.Fields));

        RuleFor(r => r.Fields)
            .Must(CollectionLimits.HaveUniqueIds).WithMessage("Every field needs its own id.");

        RuleForEach(r => r.Fields).SetValidator(new CollectionFieldRequestValidator());
    }
}

public class CollectionFieldRequestValidator : AbstractValidator<CollectionFieldRequest>
{
    public CollectionFieldRequestValidator()
    {
        RuleFor(f => f.Id)
            .Must(id => id is null || DocumentRules.IsFieldId(id)).WithMessage("Not a valid field id.");

        RuleFor(f => f.Label).NotEmpty().WithMessage("Give the field a name.").MaximumLength(CollectionLimits.MaxNameLength);

        RuleFor(f => f.Type).IsInEnum();
    }
}

/// <summary>An item's slug. Its values are checked against the collection's fields by the endpoint.</summary>
public class SaveCollectionItemRequestValidator : AbstractValidator<SaveCollectionItemRequest>
{
    public SaveCollectionItemRequestValidator()
    {
        RuleFor(r => r.Slug)
            .MaximumLength(CollectionLimits.MaxItemSlugLength)
            .Must(DocumentRules.IsSlug).WithMessage("Use lowercase letters, digits and dashes, e.g. opening-our-studio.");

        RuleFor(r => r.Values.Count)
            .LessThanOrEqualTo(CollectionLimits.MaxFields).WithMessage($"An item can have at most {CollectionLimits.MaxFields} values.")
            .OverridePropertyName(nameof(SaveCollectionItemRequest.Values));

        RuleFor(r => r.Status).IsInEnum();
    }
}

/// <summary>Limits for collections and their items, and the shared field-list predicate.</summary>
public static class CollectionLimits
{
    public const int MaxNameLength = 100;
    public const int MaxSlugLength = 100;
    public const int MaxItemSlugLength = 200;
    public const int MaxFields = 30;

    /// <summary>Field ids that are set don't repeat; new fields (no id yet) are given unique ones.</summary>
    public static bool HaveUniqueIds(IReadOnlyList<CollectionFieldRequest> fields)
    {
        var ids = fields.Select(f => f.Id).OfType<string>().ToList();

        return ids.Distinct(StringComparer.Ordinal).Count() == ids.Count;
    }
}
