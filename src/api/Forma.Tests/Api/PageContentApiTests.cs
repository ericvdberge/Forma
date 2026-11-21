using System.Net;
using System.Text;
using System.Text.Json;
using Forma.Api.Dtos;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;
using Forma.Application.Entities;
using Forma.Tests.Builders;
using Microsoft.AspNetCore.Http;

namespace Forma.Tests.Api;

public class PageContentApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Sample_pages_have_seeded_content()
    {
        var about = await _client.SamplePageAsync("about");
        var contact = await _client.SamplePageAsync("contact");

        var aboutContent = await _client.GetAsync<PageContentDto>($"/pages/{about.Id}/content");
        var contactContent = await _client.GetAsync<PageContentDto>($"/pages/{contact.Id}/content");

        Assert.Equal((ContentVersion.Live, false), (aboutContent.Version, aboutContent.HasDraft));
        Assert.NotNull(contactContent.PublishedAt);
        Assert.NotEmpty(contactContent.Document.Sections);
        Assert.DoesNotContain("{{title}}", PageDocumentSerializer.Serialize(contactContent.Document));
    }

    [Fact]
    public async Task Unknown_page_is_404()
    {
        var document = new PageDocumentBuilder().WithSection("s_a").Build();

        var get = await _client.GetAsync($"/pages/{Guid.NewGuid()}/content");
        var put = await _client.SaveDraftAsync(Guid.NewGuid(), document);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task Publishing_stores_the_document()
    {
        var page = await _client.SamplePageAsync("home");
        var document = new PageDocumentBuilder()
            .WithSection("s_hero", section => section.WithHeading("b_title", "<p>Welcome</p>", level: 1))
            .Build();

        var response = await _client.PublishPageAsync(page, document);
        var content = await _client.GetAsync<PageContentDto>($"/pages/{page.Id}/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((ContentVersion.Live, false), (content.Version, content.HasDraft));
        JsonAssert.Equal("""
            {
              "version": 1,
              "sections": [
                {
                  "id": "s_hero",
                  "settings": {
                    "type": "content",
                    "name": "",
                    "layout": "stack",
                    "splitRatio": 50,
                    "splitAt": 1,
                    "verticalAlign": "center",
                    "padding": { "desktop": { "top": 64, "bottom": 64, "left": 32, "right": 32 }, "tablet": null, "mobile": null },
                    "background": {
                      "kind": "none",
                      "color": "#ffffff",
                      "image": "",
                      "position": "center",
                      "gradientFrom": "#f5f4f0",
                      "gradientTo": "#ffffff",
                      "gradientAngle": 180,
                      "overlayColor": "#000000",
                      "overlayOpacity": 0
                    },
                    "contentWidth": "normal",
                    "align": "left",
                    "theme": "light",
                    "minHeight": 0,
                    "gap": 20,
                    "radius": 0,
                    "animation": { "entrance": "none", "delay": 0 },
                    "anchor": "",
                    "className": "",
                    "visibility": { "desktop": true, "tablet": true, "mobile": true }
                  },
                  "blocks": [
                    {
                      "type": "heading",
                      "id": "b_title",
                      "style": {
                        "align": "inherit",
                        "marginTop": 0,
                        "marginBottom": 0,
                        "maxWidth": "none",
                        "anchor": "",
                        "className": "",
                        "visibility": { "desktop": true, "tablet": true, "mobile": true }
                      },
                      "props": { "html": "<p>Welcome</p>", "level": 1, "size": "lg" }
                    }
                  ]
                }
              ]
            }
            """, content.Document);
    }

    [Fact]
    public async Task Warnings_do_not_block_publishing()
    {
        var page = await _client.SamplePageAsync("about");
        var document = new PageDocumentBuilder()
            .WithSection("s_cta", section => section.WithButton("b_cta", "Contact us", href: "#"))
            .Build();

        var response = await _client.PublishPageAsync(page, document);
        var result = await response.ReadAsync<PublishResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonAssert.Equal($$"""
            [
              {
                "entity": { "kind": "page", "id": "{{page.Id}}", "name": "About", "collectionId": null },
                "severity": "warning",
                "code": "link.placeholder",
                "message": "The button ‘Contact us’ still links to # (nowhere).",
                "sectionId": "s_cta",
                "blockId": "b_cta",
                "field": "href",
                "itemIndex": null
              }
            ]
            """, result.Warnings);
    }

    [Fact]
    public async Task Content_errors_are_a_400_problem_with_the_issues()
    {
        var page = await _client.SamplePageAsync("contact");
        var before = await _client.GetAsync<PageContentDto>($"/pages/{page.Id}/content");
        var document = new PageDocumentBuilder()
            .WithSection("s_photo", section => section.WithImage("b_photo", src: $"/media/{Guid.NewGuid()}/image"))
            .Build();

        var response = await _client.PublishPageAsync(page, document);
        var problem = await response.ReadAsync<HttpValidationProblemDetails>();
        var issues = ((JsonElement)problem.Extensions["issues"]!).Deserialize<List<PublishIssueDto>>(PageDocumentJson.Options);
        var after = await _client.GetAsync<PageContentDto>($"/pages/{page.Id}/content");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["This image was deleted from the media library."], problem.Errors["Contact"]);
        Assert.Equal(
            [new PublishIssueDto(IssueSeverity.Error, "media.not_found", "This image was deleted from the media library.", new EntityRefDto(DraftKind.Page, page.Id, "Contact", null), "s_photo", "b_photo", "src", null)],
            issues);
        Assert.Equal((before.PublishedAt, ContentVersion.Live), (after.PublishedAt, after.Version));
        JsonAssert.Equal(PageDocumentSerializer.Serialize(before.Document), after.Document);
    }

    [Fact]
    public async Task Rule_violations_are_a_400_problem()
    {
        var page = await _client.SamplePageAsync("contact");
        var document = new PageDocumentBuilder()
            .WithSection("s_split", section => section.WithSettings(s => s with { SplitRatio = 99 }))
            .Build();

        var response = await _client.SaveDraftAsync(page.Id, document);
        var problem = await response.ReadAsync<HttpValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Document.Sections[0].Settings.SplitRatio", problem.Errors.Keys);
    }

    [Fact]
    public async Task Malformed_json_is_a_400_problem()
    {
        var page = await _client.SamplePageAsync("contact");
        var document = new PageDocumentBuilder()
            .WithSection("s_a", section => section.WithHeading("b_a"))
            .Build();
        var json = JsonSerializer.Serialize(new SavePageDraftRequest(document), PageDocumentJson.Options)
            .Replace("\"type\":\"heading\"", "\"type\":\"marquee\"");

        var response = await _client.PutAsync($"/pages/{page.Id}/content/draft", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}

/// <summary>On its own database: the tests above change the sample pages.</summary>
public class SamplePagesApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("home")]
    [InlineData("about")]
    [InlineData("contact")]
    public async Task Sample_pages_can_be_published(string slug)
    {
        var page = await _client.SamplePageAsync(slug);
        var content = await _client.GetAsync<PageContentDto>($"/pages/{page.Id}/content");

        var response = await _client.PublishPageAsync(page, content.Document);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
