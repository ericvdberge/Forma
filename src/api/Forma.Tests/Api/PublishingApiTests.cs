using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forma.Api.Dtos;
using Forma.Api.Workers;
using Forma.Application.Documents;
using Forma.Application.Entities;
using Forma.Application.Publishing;
using Forma.Tests.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Tests.Api;

/// <summary>
/// Drafts and publishing (<c>docs/design/drafts-and-publish.md</c>). Each test makes its own <i>Talks</i> collection:
/// fields Title and Speaker, three live items, and a live detail page whose heading shows Speaker.
/// </summary>
public class PublishingApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly TestClock Clock = new(DateTimeOffset.UtcNow);

    private readonly WebApplicationFactoryWithClock _app = new(factory, Clock);

    private HttpClient Client => _app.Client;

    [Fact]
    public async Task Saving_a_draft_leaves_the_live_page_as_it_is()
    {
        var talks = await CreateTalksAsync();
        var draft = DetailPage("fld_speaker", heading: "Edited");

        var save = await Client.SaveDraftAsync(talks.DetailPage.Id, draft);
        var live = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content");
        var edited = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content?version=draft");
        var drafts = await Client.GetAsync<List<DraftDto>>($"/drafts?websiteId={talks.WebsiteId}");

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        Assert.Equal((ContentVersion.Live, true), (live.Version, live.HasDraft));
        Assert.Equal(ContentVersion.Draft, edited.Version);
        Assert.Contains(drafts, d => d.Entity.Id == talks.DetailPage.Id && d.Summary == "Edited");
    }

    [Fact]
    public async Task Removing_a_field_blocks_until_the_pages_that_show_it_are_in_the_publish()
    {
        var talks = await CreateTalksAsync();
        await SaveFieldSwapDraftAsync(talks);
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_host"));

        var alone = await Client.CheckAsync(new PublishRequest(talks.WebsiteId, [talks.Collection.Id], [], []));
        var together = await Client.CheckAsync(new PublishRequest(talks.WebsiteId, [talks.Collection.Id], [talks.DetailPage.Id], []));
        var publishAlone = await Client.PublishAsync(new PublishRequest(talks.WebsiteId, [talks.Collection.Id], [], []));

        Assert.False(alone.CanPublish);
        var issue = Assert.Single(alone.Issues, i => i.Severity == Forma.Application.Documents.Validation.IssueSeverity.Error);
        Assert.Equal(("binding.field_missing", talks.DetailPage.Id), (issue.Code, issue.Entity!.Id));
        var need = Assert.Single(alone.Needs);
        Assert.Equal((DraftKind.Page, talks.DetailPage.Id), (need.Entity.Kind, need.Entity.Id));
        Assert.Equal([talks.Collection.Id], need.NeededBy.Select(n => n.Id));
        Assert.Equal(HttpStatusCode.BadRequest, publishAlone.StatusCode);
        Assert.True(together.CanPublish);
        Assert.Empty(together.Needs);
    }

    [Fact]
    public async Task Moved_values_end_up_in_the_new_field_of_every_item()
    {
        var talks = await CreateTalksAsync();
        await SaveFieldSwapDraftAsync(talks);
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_host"));

        var response = await Client.PublishAsync(new PublishRequest(talks.WebsiteId, [talks.Collection.Id], [talks.DetailPage.Id], []));
        var collection = await Client.GetAsync<CollectionDto>($"/collections/{talks.Collection.Id}");
        var items = await Client.GetAsync<List<CollectionItemDto>>($"/collections/{talks.Collection.Id}/items");
        var usages = await Client.GetAsync<List<FieldUsageDto>>($"/collections/{talks.Collection.Id}/usages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(collection.Draft);
        Assert.Equal(["fld_title", "fld_host"], collection.Fields.Select(f => f.Id));
        Assert.All(items, i => Assert.Equal($"Speaker of {i.Slug}", i.Values["fld_host"].GetString()));
        Assert.All(items, i => Assert.False(i.Values.ContainsKey("fld_speaker")));
        Assert.Equal([("fld_host", talks.DetailPage.Id)], usages.Select(u => (u.FieldId, u.PageId)));
    }

    [Fact]
    public async Task A_publish_rolls_back_completely_when_one_step_fails()
    {
        var talks = await CreateTalksAsync();
        await SaveFieldSwapDraftAsync(talks);
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_host"));
        var failing = _app.WithServices(s => s.AddScoped<IPageBindingUsageService, FailingUsageService>());

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            var response = await failing.PublishAsync(new PublishRequest(talks.WebsiteId, [talks.Collection.Id], [talks.DetailPage.Id], []));
            response.EnsureSuccessStatusCode();
        });

        var collection = await Client.GetAsync<CollectionDto>($"/collections/{talks.Collection.Id}");
        var items = await Client.GetAsync<List<CollectionItemDto>>($"/collections/{talks.Collection.Id}/items");
        var page = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content");

        Assert.Equal(["fld_title", "fld_speaker"], collection.Fields.Select(f => f.Id));
        Assert.NotNull(collection.Draft);
        Assert.All(items, i => Assert.True(i.Values.ContainsKey("fld_speaker")));
        Assert.True(page.HasDraft);
    }

    [Fact]
    public async Task An_id_from_another_website_is_not_found()
    {
        var talks = await CreateTalksAsync();
        var otherWebsite = (await Client.GetAsync<List<WebsiteDto>>("/websites")).First(w => w.Id != talks.WebsiteId);
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_speaker", heading: "Edited"));

        var check = await Client.PostAsJsonAsync("/publish/check", new PublishRequest(otherWebsite.Id, [], [talks.DetailPage.Id], []), PageDocumentJson.Options);
        var publish = await Client.PublishAsync(new PublishRequest(otherWebsite.Id, [talks.Collection.Id], [], []));
        var schedule = await ScheduleAsync(new SchedulePublishRequest(otherWebsite.Id, Clock.GetUtcNow().AddHours(1), [], [talks.DetailPage.Id], []));
        var drafts = await Client.GetAsync<List<DraftDto>>($"/drafts?websiteId={otherWebsite.Id}");

        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, publish.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, schedule.StatusCode);
        Assert.DoesNotContain(drafts, d => d.Entity.Id == talks.DetailPage.Id);
    }

    [Fact]
    public async Task A_scheduled_publish_runs_when_it_is_due()
    {
        var talks = await CreateTalksAsync();
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_speaker", heading: "Scheduled"));
        var scheduled = await (await ScheduleAsync(new SchedulePublishRequest(talks.WebsiteId, Clock.GetUtcNow().AddHours(1), [], [talks.DetailPage.Id], []))).ReadAsync<ScheduledPublishDto>();

        var early = await _app.Worker.RunDueAsync();
        var stillDraft = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content");
        Clock.Advance(TimeSpan.FromHours(2));
        await _app.Worker.RunDueAsync();
        var live = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content");
        var upcoming = await Client.GetAsync<List<ScheduledPublishDto>>($"/scheduled-publishes?websiteId={talks.WebsiteId}");

        Assert.Equal(ScheduledPublishStatus.Pending, scheduled.Status);
        Assert.Equal(0, early);
        Assert.True(stillDraft.HasDraft);
        Assert.False(live.HasDraft);
        Assert.Contains("Scheduled", PageDocumentSerializer.Serialize(live.Document));
        Assert.DoesNotContain(upcoming, p => p.Id == scheduled.Id);
    }

    [Fact]
    public async Task A_scheduled_publish_that_is_blocked_when_it_runs_publishes_nothing_and_fails()
    {
        var talks = await CreateTalksAsync();
        await SaveFieldSwapDraftAsync(talks);
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_host"));
        var scheduled = await (await ScheduleAsync(new SchedulePublishRequest(talks.WebsiteId, Clock.GetUtcNow().AddHours(1), [talks.Collection.Id], [talks.DetailPage.Id], []))).ReadAsync<ScheduledPublishDto>();

        // Break it: the page goes back to showing Speaker, which the collection's draft removes.
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_speaker", heading: "Broken"));
        Clock.Advance(TimeSpan.FromHours(2));
        await _app.Worker.RunDueAsync();

        var failed = Assert.Single(await Client.GetAsync<List<ScheduledPublishDto>>($"/scheduled-publishes?websiteId={talks.WebsiteId}"), p => p.Id == scheduled.Id);
        var collection = await Client.GetAsync<CollectionDto>($"/collections/{talks.Collection.Id}");
        var page = await Client.GetAsync<PageContentDto>($"/pages/{talks.DetailPage.Id}/content");

        Assert.Equal(ScheduledPublishStatus.Failed, failed.Status);
        Assert.Contains(failed.Issues, i => i.Code == "binding.field_missing" && i.Entity!.Id == talks.DetailPage.Id);
        Assert.Equal(["fld_title", "fld_speaker"], collection.Fields.Select(f => f.Id));
        Assert.True(page.HasDraft);
        Assert.DoesNotContain("Broken", PageDocumentSerializer.Serialize(page.Document));
    }

    [Fact]
    public async Task Discarding_a_scheduled_draft_takes_it_out_of_the_scheduled_publish()
    {
        var talks = await CreateTalksAsync();
        var item = talks.Items[0];
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_speaker", heading: "Monday"));
        await Client.PutAsJsonAsync($"/collections/{talks.Collection.Id}/items/{item.Id}/draft", Talk(item.Slug, "Renamed"), PageDocumentJson.Options);
        var scheduled = await (await ScheduleAsync(new SchedulePublishRequest(talks.WebsiteId, Clock.GetUtcNow().AddDays(30), [], [talks.DetailPage.Id], [item.Id]))).ReadAsync<ScheduledPublishDto>();

        var discard = await Client.DeleteAsync($"/pages/{talks.DetailPage.Id}/content/draft");
        var after = Assert.Single(await Client.GetAsync<List<ScheduledPublishDto>>($"/scheduled-publishes?websiteId={talks.WebsiteId}"), p => p.Id == scheduled.Id);

        Assert.Equal(HttpStatusCode.NoContent, discard.StatusCode);
        Assert.Equal(2, scheduled.Entries.Count);
        Assert.Equal([item.Id], after.Entries.Select(e => e.Id));
    }

    [Fact]
    public async Task A_draft_can_be_in_one_scheduled_publish_at_a_time()
    {
        var talks = await CreateTalksAsync();
        await Client.SaveDraftAsync(talks.DetailPage.Id, DetailPage("fld_speaker", heading: "Once"));
        var request = new SchedulePublishRequest(talks.WebsiteId, Clock.GetUtcNow().AddDays(30), [], [talks.DetailPage.Id], []);

        var first = await ScheduleAsync(request);
        var second = await ScheduleAsync(request);
        var problem = await second.ReadAsync<HttpValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(["‘Talk page’ is already scheduled for another publish."], problem.Errors["entries"]);
    }

    /* ---------- Test data ---------- */

    private sealed record Talks(Guid WebsiteId, CollectionDto Collection, PageDto DetailPage, IReadOnlyList<CollectionItemDto> Items);

    private Task<HttpResponseMessage> ScheduleAsync(SchedulePublishRequest request) =>
        Client.PostAsJsonAsync("/scheduled-publishes", request, PageDocumentJson.Options);

    /// <summary>A live collection with three live items and a live detail page that shows Speaker.</summary>
    private async Task<Talks> CreateTalksAsync()
    {
        var websiteId = (await Client.GetAsync<List<WebsiteDto>>("/websites"))[0].Id;
        var slug = $"talks-{Guid.NewGuid():N}"[..14];
        var builder = TalksFields(slug);

        var collection = await (await Client.PostAsJsonAsync("/collections", builder.BuildCreate(websiteId), PageDocumentJson.Options)).ReadAsync<CollectionDto>();

        var items = new List<CollectionItemDto>();
        foreach (var talk in new[] { "intro", "deep-dive", "wrap-up" })
            items.Add(await (await Client.PostAsJsonAsync($"/collections/{collection.Id}/items", Talk(talk, $"Talk {talk}"), PageDocumentJson.Options)).ReadAsync<CollectionItemDto>());

        var pages = await Client.GetAsync<List<PageDto>>($"/pages?websiteId={websiteId}");
        var detailPage = pages.Single(p => p.Id == collection.DetailPageId);
        await Client.SaveDraftAsync(detailPage.Id, DetailPage("fld_speaker"));

        var publish = await Client.PublishAsync(new PublishRequest(websiteId, [collection.Id], [detailPage.Id], items.Select(i => i.Id).ToList()));
        Assert.True(publish.IsSuccessStatusCode, await publish.Content.ReadAsStringAsync());

        var live = await Client.GetAsync<CollectionDto>($"/collections/{collection.Id}");
        var liveItems = await Client.GetAsync<List<CollectionItemDto>>($"/collections/{collection.Id}/items");

        return new Talks(websiteId, live, detailPage, liveItems);
    }

    private static CollectionRequestBuilder TalksFields(string slug) => new CollectionRequestBuilder()
        .Named("Talks", "Talk", slug)
        .WithField("fld_title", "Title", CollectionFieldType.Text, required: true)
        .WithField("fld_speaker", "Speaker", CollectionFieldType.Text, required: true);

    /// <summary>Removes Speaker, adds Host, and moves the values of Speaker to Host.</summary>
    private async Task SaveFieldSwapDraftAsync(Talks talks)
    {
        var draft = TalksFields(talks.Collection.Slug)
            .WithoutField("fld_speaker")
            .WithField("fld_host", "Host", CollectionFieldType.Text, required: true)
            .BuildDraft(new Dictionary<string, string?> { ["fld_speaker"] = "fld_host" });

        var response = await Client.PutAsJsonAsync($"/collections/{talks.Collection.Id}/draft", draft, PageDocumentJson.Options);
        response.EnsureSuccessStatusCode();
    }

    private static SaveCollectionItemRequest Talk(string slug, string title) =>
        new CollectionItemRequestBuilder(slug).With("fld_title", title).With("fld_speaker", $"Speaker of {slug}").Build();

    /// <summary>A detail page whose heading shows <paramref name="fieldId"/>.</summary>
    private static PageDocument DetailPage(string fieldId, string heading = "Talk") => new PageDocumentBuilder()
        .WithSection("s_talk", section => section
            .WithHeading("b_title", $"<p>{heading}</p>", level: 1)
            .WithText("b_by", bindings: new Dictionary<string, Binding> { ["html"] = new(BindingScope.Page, fieldId) }))
        .Build();

    /* ---------- Hosting ---------- */

    /// <summary>A rebuild of usages that always fails: the last step of a publish.</summary>
    private sealed class FailingUsageService : IPageBindingUsageService
    {
        public Task RebuildAsync(Page page, PageDocument document, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The usages could not be stored.");

        public Task RebuildAllIfEmptyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>The API with a clock the tests move forward.</summary>
    private sealed class WebApplicationFactoryWithClock(ApiFactory factory, TestClock clock)
    {
        private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _app =
            factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(clock)));

        private HttpClient? _client;

        public HttpClient Client => _client ??= _app.CreateClient();

        public ScheduledPublishWorker Worker => _app.Services.GetRequiredService<ScheduledPublishWorker>();

        /// <summary>A client of the API with some services replaced.</summary>
        public HttpClient WithServices(Action<IServiceCollection> configure) =>
            _app.WithWebHostBuilder(b => b.ConfigureTestServices(configure)).CreateClient();
    }
}

/// <summary>A clock tests set and move forward.</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
