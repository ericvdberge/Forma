using System.Net.Http.Json;
using Forma.Api.Dtos;
using Forma.Application.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Forma.Tests.Api;

/// <summary>The API with a fresh SQLite database (sample data seeded), shared by the tests of one class.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Directory.CreateTempSubdirectory("forma-tests-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Forma", $"Data Source={Path.Combine(_directory, "forma.db")}");
        builder.UseSetting("Assets:Path", Path.Combine(_directory, "assets"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { /* best effort */ }
    }
}

/// <summary>Typed calls to the API, read with the API's own JSON rules.</summary>
public static class ApiClientExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(PageDocumentJson.Options))!;

    public static async Task<T> GetAsync<T>(this HttpClient client, string url) =>
        (await client.GetFromJsonAsync<T>(url, PageDocumentJson.Options))!;

    /// <summary>The sample website's page with this slug.</summary>
    public static async Task<PageDto> SamplePageAsync(this HttpClient client, string slug)
    {
        var websites = await client.GetAsync<List<WebsiteDto>>("/websites");
        var pages = await client.GetAsync<List<PageDto>>($"/pages?websiteId={websites[0].Id}");

        return pages.Single(p => p.Slug == slug);
    }

    public static Task<HttpResponseMessage> PublishAsync(this HttpClient client, Guid pageId, PageDocument document) =>
        client.PutAsJsonAsync($"/pages/{pageId}/content", new PublishPageContentRequest(document), PageDocumentJson.Options);
}
