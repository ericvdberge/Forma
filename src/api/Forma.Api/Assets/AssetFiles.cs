using Forma.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Forma.Api.Assets;

/// <summary>Serves the assets folder (uploaded media and thumbnails) as static files.</summary>
public static class AssetFiles
{
    public const string RequestPath = "/assets";

    public static WebApplication UseAssetFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AssetStorageOptions>();
        Directory.CreateDirectory(options.RootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(options.RootPath),
            RequestPath = RequestPath,
            OnPrepareResponse = context =>
            {
                var headers = context.Context.Response.Headers;
                // Every upload gets a new file name, so a stored file never changes.
                headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
                headers[HeaderNames.XContentTypeOptions] = "nosniff";
            },
        });

        return app;
    }
}
