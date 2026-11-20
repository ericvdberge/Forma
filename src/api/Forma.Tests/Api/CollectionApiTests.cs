using System.Net;
using System.Net.Http.Json;
using Forma.Api.Dtos;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Tests.Builders;
using Microsoft.AspNetCore.Http;

namespace Forma.Tests.Api;

public class CollectionApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Sample_websites_have_a_blog()
    {
        var websiteId = await WebsiteIdAsync();

        var collections = await _client.GetAsync<List<CollectionDto>>($"/collections?websiteId={websiteId}");
        var blog = Assert.Single(collections, c => c.Slug == "blog");
        var items = await _client.GetAsync<List<CollectionItemDto>>($"/collections/{blog.Id}/items");

        Assert.Equal(["Title", "Cover image", "Summary", "Content", "Published on"], blog.Fields.Select(f => f.Label));
        Assert.NotNull(blog.DetailPageId);
        Assert.Equal(3, items.Count(i => i.Status == CollectionItemStatus.Published));
    }

    [Fact]
    public async Task Sample_websites_list_the_blog_on_a_page()
    {
        var blogPage = await _client.SamplePageAsync("blog");

        var content = await _client.GetAsync<PageContentDto>($"/pages/{blogPage.Id}/content");
        var cards = content.Document.Sections.SelectMany(s => s.Blocks).OfType<CollectionCardsBlock>().Single();
        var collection = await _client.GetAsync<CollectionDto>($"/collections/{cards.Props.CollectionId}");

        Assert.Equal(PageContentStatus.Published, content.Status);
        Assert.Equal("blog", collection.Slug);
    }

    [Fact]
    public async Task Creating_a_collection_creates_its_detail_page()
    {
        var websiteId = await WebsiteIdAsync();
        var request = new CollectionRequestBuilder()
            .Named("Team members", "Team member", "team")
            .WithField(null, "Name", CollectionFieldType.Text, required: true)
            .WithField(null, "Photo", CollectionFieldType.Image)
            .BuildCreate(websiteId);

        var response = await _client.PostAsJsonAsync("/collections", request, PageDocumentJson.Options);
        var collection = await response.ReadAsync<CollectionDto>();
        var pages = await _client.GetAsync<List<PageDto>>($"/pages?websiteId={websiteId}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.All(collection.Fields, f => Assert.Matches("^fld_[0-9a-f]{8}$", f.Id));
        var detailPage = Assert.Single(pages, p => p.Id == collection.DetailPageId);
        Assert.Equal(("Team member page", "team/{item}", PageKind.CollectionTemplate, collection.Id), (detailPage.Title, detailPage.Slug, detailPage.Kind, detailPage.CollectionId));
    }

    [Fact]
    public async Task A_slug_is_used_once_per_website()
    {
        var websiteId = await WebsiteIdAsync();
        var request = new CollectionRequestBuilder().Named("More posts", "Post", "blog").BuildCreate(websiteId);

        var response = await _client.PostAsJsonAsync("/collections", request, PageDocumentJson.Options);
        var problem = await response.ReadAsync<HttpValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["The collection ‘Blog posts’ already uses /blog."], problem.Errors["slug"]);
    }

    [Fact]
    public async Task Renaming_the_slug_moves_the_detail_page()
    {
        var websiteId = await WebsiteIdAsync();
        var builder = new CollectionRequestBuilder().Named("Projects", "Project", "projects");
        var created = await (await _client.PostAsJsonAsync("/collections", builder.BuildCreate(websiteId), PageDocumentJson.Options)).ReadAsync<CollectionDto>();

        var response = await _client.PutAsJsonAsync($"/collections/{created.Id}", builder.Named("Work", "Case", "work").BuildUpdate(), PageDocumentJson.Options);
        var pages = await _client.GetAsync<List<PageDto>>($"/pages?websiteId={websiteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("Case page", "work/{item}"), pages.Where(p => p.Id == created.DetailPageId).Select(p => (p.Title, p.Slug)).Single());
    }

    [Fact]
    public async Task Item_values_are_checked_against_the_fields()
    {
        var collection = await CreateRecipesAsync();
        var item = new CollectionItemRequestBuilder("pancakes")
            .With("fld_servings", "four")
            .With("fld_photo", "/media/not-an-image.png")
            .With("fld_unknown", "x")
            .Build();

        var response = await _client.PostAsJsonAsync($"/collections/{collection.Id}/items", item, PageDocumentJson.Options);
        var problem = await response.ReadAsync<HttpValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            new Dictionary<string, string[]>
            {
                ["values.fld_unknown"] = ["Recipes has no field ‘fld_unknown’."],
                ["values.fld_name"] = ["‘Name’ is required."],
                ["values.fld_servings"] = ["Enter a number."],
                ["values.fld_photo"] = ["Images must come from the media library."],
            },
            problem.Errors);
    }

    [Fact]
    public async Task Items_are_created_saved_and_published()
    {
        var collection = await CreateRecipesAsync();
        var draft = new CollectionItemRequestBuilder("pancakes").With("fld_name", "Pancakes").With("fld_servings", 4).Build();
        var published = new CollectionItemRequestBuilder("fluffy-pancakes").With("fld_name", "Fluffy pancakes").With("fld_servings", 4).Published().Build();

        var created = await (await _client.PostAsJsonAsync($"/collections/{collection.Id}/items", draft, PageDocumentJson.Options)).ReadAsync<CollectionItemDto>();
        var response = await _client.PutAsJsonAsync($"/collections/{collection.Id}/items/{created.Id}", published, PageDocumentJson.Options);
        var saved = await _client.GetAsync<CollectionItemDto>($"/collections/{collection.Id}/items/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((CollectionItemStatus.Draft, null), (created.Status, created.PublishedAt));
        Assert.Equal(("fluffy-pancakes", CollectionItemStatus.Published), (saved.Slug, saved.Status));
        Assert.NotNull(saved.PublishedAt);
        JsonAssert.Equal("""{ "fld_name": "Fluffy pancakes", "fld_servings": 4 }""", saved.Values);
    }

    [Fact]
    public async Task An_item_is_only_reached_through_its_own_collection()
    {
        var recipes = await CreateRecipesAsync();
        var other = await CreateRecipesAsync();
        var item = new CollectionItemRequestBuilder("pancakes").With("fld_name", "Pancakes").Build();
        var created = await (await _client.PostAsJsonAsync($"/collections/{recipes.Id}/items", item, PageDocumentJson.Options)).ReadAsync<CollectionItemDto>();

        var get = await _client.GetAsync($"/collections/{other.Id}/items/{created.Id}");
        var delete = await _client.DeleteAsync($"/collections/{other.Id}/items/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_collection_deletes_its_items_and_detail_page()
    {
        var websiteId = await WebsiteIdAsync();
        var collection = await CreateRecipesAsync();

        var response = await _client.DeleteAsync($"/collections/{collection.Id}");
        var pages = await _client.GetAsync<List<PageDto>>($"/pages?websiteId={websiteId}");
        var items = await _client.GetAsync($"/collections/{collection.Id}/items");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(pages, p => p.Id == collection.DetailPageId);
        Assert.Equal(HttpStatusCode.NotFound, items.StatusCode);
    }

    private async Task<Guid> WebsiteIdAsync() => (await _client.GetAsync<List<WebsiteDto>>("/websites"))[0].Id;

    private async Task<CollectionDto> CreateRecipesAsync()
    {
        var request = new CollectionRequestBuilder()
            .WithField("fld_name", "Name", CollectionFieldType.Text, required: true)
            .WithField("fld_servings", "Servings", CollectionFieldType.Number)
            .WithField("fld_photo", "Photo", CollectionFieldType.Image)
            .BuildCreate(await WebsiteIdAsync());

        var response = await _client.PostAsJsonAsync("/collections", request, PageDocumentJson.Options);

        return await response.ReadAsync<CollectionDto>();
    }
}
