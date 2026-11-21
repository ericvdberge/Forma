using System.Text.Json;
using Forma.Application.Entities;

namespace Forma.Application.Publishing;

/// <summary>
/// How an item's values change when its collection's draft is published: values of removed fields move to the field
/// <see cref="Collection.DraftValueMoves"/> names (when that one is still empty) or are dropped.
/// </summary>
public static class ValueMoves
{
    /// <summary>The values as they are after publishing <paramref name="collection"/>'s draft.</summary>
    public static IReadOnlyDictionary<string, JsonElement> Apply(Collection collection, IReadOnlyDictionary<string, JsonElement> values)
    {
        if (collection.DraftFields is not { } draft)
            return values;

        var kept = draft.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        var moves = collection.DraftValueMoves ?? new Dictionary<string, string?>();

        var result = values
            .Where(v => kept.Contains(v.Key))
            .ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);

        foreach (var (from, to) in moves)
        {
            if (to is null || !kept.Contains(to) || kept.Contains(from))
                continue;

            if (values.TryGetValue(from, out var value) && DraftDescriber.HasValue(values, from) && !DraftDescriber.HasValue(result, to))
                result[to] = value;
        }

        return result;
    }

    /// <summary>Whether <see cref="Apply"/> changes anything.</summary>
    public static bool Changes(IReadOnlyDictionary<string, JsonElement> before, IReadOnlyDictionary<string, JsonElement> after) =>
        before.Count != after.Count
        || before.Any(kv => !after.TryGetValue(kv.Key, out var value) || value.GetRawText() != kv.Value.GetRawText());
}
