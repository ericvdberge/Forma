using FluentValidation;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Publishing;
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
        services.AddScoped<ICollectionCoreService, CollectionCoreService>();
        services.AddScoped<ICollectionItemCoreService, CollectionItemCoreService>();
        services.AddScoped<IPageBindingUsageCoreService, PageBindingUsageCoreService>();
        services.AddScoped<IScheduledPublishCoreService, ScheduledPublishCoreService>();
        services.AddScoped<IScheduledPublishEntryCoreService, ScheduledPublishEntryCoreService>();

        // Supporting services: rules and workflows built on the core services.
        services.AddScoped<IPageContentValidationService, PageContentValidationService>();
        services.AddScoped<ICollectionValidationService, CollectionValidationService>();
        services.AddScoped<ICollectionItemValidationService, CollectionItemValidationService>();

        // Drafts and publishing (docs/design/drafts-and-publish.md).
        services.AddScoped<IWebsiteStateLoader, WebsiteStateLoader>();
        services.AddScoped<IPublishCheckService, PublishCheckService>();
        services.AddScoped<IPublishService, PublishService>();
        services.AddScoped<IPageBindingUsageService, PageBindingUsageService>();
        services.AddScoped<IScheduledPublishService, ScheduledPublishService>();

        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton<IValidator<PageDocument>, PageDocumentValidator>();

        return services;
    }
}
