using FluentValidation;
using Forma.Api.Dtos;

namespace Forma.Api.Validators;

/// <summary>
/// Cheap checks on the upload before it reaches the core service. Whether the bytes really are
/// a supported image is decided there, when the image is decoded.
/// </summary>
public class UploadMediaRequestValidator : AbstractValidator<UploadMediaRequest>
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;
    private const int MaxFileNameLength = 255;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public UploadMediaRequestValidator()
    {
        RuleFor(r => r.WebsiteId).NotEmpty();

        RuleFor(r => r.File).NotNull().DependentRules(() =>
        {
            RuleFor(r => r.File.Length)
                .GreaterThan(0).WithMessage("The file is empty.")
                .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage($"The file is larger than {MaxFileSizeInBytes / (1024 * 1024)} MB.")
                .OverridePropertyName(nameof(UploadMediaRequest.File));

            RuleFor(r => r.File.FileName)
                .NotEmpty()
                .MaximumLength(MaxFileNameLength)
                .Must(name => AllowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Only JPEG, PNG, GIF and WebP images are allowed.")
                .OverridePropertyName(nameof(UploadMediaRequest.File));
        });
    }
}
