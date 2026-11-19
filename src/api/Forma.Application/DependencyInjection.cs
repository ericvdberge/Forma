using System.Text.Json;
using FluentValidation;
using FluentValidation.Internal;
using Forma.Application.Documents;
using Forma.Application.Documents.Checks;
using Forma.Application.Documents.Validation;
using Forma.Application.Plugins;
using Forma.Application.Services;
using Forma.Infrastructure.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        // The built-in publish rules, plus whatever plugins subscribed to the page.check hook add.
        services.AddScoped<PageDocumentChecker>();
        services.AddScoped<IPageDocumentChecker, PluginContentChecker>();
        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton<IValidator<PageDocument>, PageDocumentValidator>();

        // Plugins (docs/design/plugins.md): the sandbox comes from AddInfrastructure.
        services.AddSingleton<IPluginCatalog>(sp => new PluginCatalog(
            sp.GetRequiredService<IWasmSandbox>(),
            sp.GetRequiredService<IOptions<PluginOptions>>(),
            PluginEvents.All,
            sp.GetRequiredService<ILogger<PluginCatalog>>()));
        services.AddSingleton<IPluginRunLog, PluginRunLog>();
        services.AddSingleton<IPluginRunner, PluginRunner>();
        services.AddSingleton<PluginEventQueue>();
        services.AddSingleton<IPluginEventPublisher>(sp => sp.GetRequiredService<PluginEventQueue>());
        services.AddHostedService<PluginEventDispatcher>();
        services.AddScoped<IPluginCoreService, PluginCoreService>();

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
