using FluentValidation;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Core services: create, read, update and delete, one per entity.
        services.AddScoped<IWebsiteCoreService, WebsiteCoreService>();
        services.AddScoped<IPageCoreService, PageCoreService>();
        services.AddScoped<IMediaCoreService, MediaCoreService>();
        services.AddScoped<IPageContentCoreService, PageContentCoreService>();

        // Supporting services: rules and workflows built on the core services.
        services.AddScoped<IPageContentValidationService, PageContentValidationService>();

        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton<IValidator<PageDocument>, PageDocumentValidator>();

        return services;
    }
}
