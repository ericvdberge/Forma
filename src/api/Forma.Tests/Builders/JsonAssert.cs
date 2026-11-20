using System.Text.Json;
using System.Text.Json.Nodes;
using Forma.Application.Documents;

namespace Forma.Tests.Builders;

public static class JsonAssert
{
    /// <summary>Asserts that <paramref name="actual"/> serialises to <paramref name="expectedJson"/> (formatting and property order don't count).</summary>
    public static void Equal(string expectedJson, object actual)
    {
        var expected = JsonNode.Parse(expectedJson);
        var actualNode = JsonSerializer.SerializeToNode(actual, actual.GetType(), PageDocumentJson.Options);

        Assert.True(
            JsonNode.DeepEquals(expected, actualNode),
            $"Expected:{Environment.NewLine}{expected?.ToJsonString(Indented)}{Environment.NewLine}Actual:{Environment.NewLine}{actualNode?.ToJsonString(Indented)}");
    }

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
}
