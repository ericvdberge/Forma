using Forma.Application.Entities;

namespace Forma.Application.Documents;

/// <summary>
/// Which block props can show a collection field, and which field types can feed them
/// (<c>docs/design/collections.md</c> § 2). Mirrors <c>bindableProps</c> in <c>src/app/src/studio/bindings.ts</c>.
/// </summary>
public static class BindableProps
{
    private static readonly CollectionFieldType[] Words = [CollectionFieldType.Text, CollectionFieldType.Number, CollectionFieldType.Date];
    private static readonly CollectionFieldType[] Html = [CollectionFieldType.Text, CollectionFieldType.RichText, CollectionFieldType.Number, CollectionFieldType.Date];
    private static readonly CollectionFieldType[] Image = [CollectionFieldType.Image];
    private static readonly CollectionFieldType[] Link = [CollectionFieldType.Url];

    private static readonly Dictionary<Type, Dictionary<string, CollectionFieldType[]>> Table = new()
    {
        [typeof(HeadingBlock)] = new() { ["html"] = Html },
        [typeof(TextBlock)] = new() { ["html"] = Html },
        [typeof(ButtonBlock)] = new() { ["label"] = Words, ["href"] = Link },
        [typeof(BadgeBlock)] = new() { ["text"] = Words },
        [typeof(ImageBlock)] = new() { ["src"] = Image, ["alt"] = Words, ["caption"] = Words },
        [typeof(VideoBlock)] = new() { ["url"] = Link },
    };

    /// <summary>Whether <paramref name="prop"/> of <paramref name="block"/> can be bound to a field at all.</summary>
    public static bool IsBindable(Block block, string prop) =>
        Table.TryGetValue(block.GetType(), out var props) && props.ContainsKey(prop);

    /// <summary>Whether a field of type <paramref name="type"/> can feed <paramref name="prop"/> of <paramref name="block"/>.</summary>
    public static bool Accepts(Block block, string prop, CollectionFieldType type) =>
        Table.TryGetValue(block.GetType(), out var props) && props.TryGetValue(prop, out var types) && types.Contains(type);
}
