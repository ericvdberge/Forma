using System.Text.Json;
using System.Text.Json.Nodes;
using Forma.Application.Documents;
using Forma.Application.Documents.Validation;

namespace Forma.Tests.Documents;

/// <summary>
/// Everything the studio can create (src/api/Contracts/Studio, written by <c>npm run studio:fixtures -w forma-app</c>)
/// must read into the document model and pass level 1 validation. When this fails after a studio change, either the
/// studio produces something the API would reject on save, or the fixtures are stale: regenerate them.
/// </summary>
public class StudioContractTests
{
    private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "Contracts", "Studio");

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(FixturesDirectory, "*.json").Order())
        {
            data.Add(Path.GetFileName(file));
        }

        return data;
    }

    [Fact]
    public void All_fixtures_are_present()
    {
        string[] expected = ["blocks.json", "home.json", "page.json", "saved.json", "templates.json"];
        Assert.Equal(expected, Directory.EnumerateFiles(FixturesDirectory, "*.json").Select(Path.GetFileName).Order());
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Fixture_reads_and_passes_validation(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(FixturesDirectory, fileName));

        var document = JsonSerializer.Deserialize<PageDocument>(json, PageDocumentJson.Options)!;
        var result = new PageDocumentValidator(new RichTextSanitizer()).Validate(document);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Fixture_round_trips_without_loss(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(FixturesDirectory, fileName));

        var document = JsonSerializer.Deserialize<PageDocument>(json, PageDocumentJson.Options)!;
        var (stored, _) = PageDocumentSerializer.Serialize(document);

        Assert.Empty(Differences(JsonNode.Parse(json), JsonNode.Parse(stored), "$"));
    }

    /// <summary>Paths where two JSON values differ (property order doesn't count).</summary>
    private static IEnumerable<string> Differences(JsonNode? a, JsonNode? b, string path)
    {
        switch (a, b)
        {
            case (JsonObject x, JsonObject y):
                foreach (var key in x.Select(p => p.Key).Union(y.Select(p => p.Key)))
                {
                    foreach (var d in Differences(x[key], y[key], $"{path}.{key}"))
                    {
                        yield return d;
                    }
                }
                break;
            case (JsonArray x, JsonArray y) when x.Count == y.Count:
                for (var i = 0; i < x.Count; i++)
                {
                    foreach (var d in Differences(x[i], y[i], $"{path}[{i}]"))
                    {
                        yield return d;
                    }
                }
                break;
            default:
                if (!JsonNode.DeepEquals(a, b))
                {
                    yield return $"{path}: {a?.ToJsonString() ?? "(missing)"} => {b?.ToJsonString() ?? "(missing)"}";
                }
                break;
        }
    }
}
