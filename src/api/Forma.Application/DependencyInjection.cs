using System.Text.Json;
using FluentValidation;
using FluentValidation.Internal;
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

        services.AddScoped<IWebsiteCoreService, WebsiteCoreService>();
        services.AddScoped<IPageCoreService, PageCoreService>();
        services.AddScoped<IMediaCoreService, MediaCoreService>();
        services.AddScoped<IPageContentCoreService, PageContentCoreService>();

        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton<IValidator<PageDocument>, PageDocumentValidator>();

        // Validation errors are keyed by the JSON path the client sent ("sections[0].settings.splitRatio"),
        // not by the C# property names.
        ValidatorOptions.Global.PropertyNameResolver = (_, member, expression) =>
        {
            var name = expression is not null ? PropertyChain.FromExpression(expression).ToString() : member?.Name;
            return name is null ? null : string.Join('.', name.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
        };

        return services;
    }
}
