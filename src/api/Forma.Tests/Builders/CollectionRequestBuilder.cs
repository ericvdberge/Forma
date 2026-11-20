using System.Text.Json;
using Forma.Api.Dtos;
using Forma.Application.Entities;

namespace Forma.Tests.Builders;

/// <summary>Builds collection requests for tests: a <i>Recipes</i> collection with no fields, until a test adds some.</summary>
public class CollectionRequestBuilder
{
    private readonly List<CollectionFieldRequest> _fields = [];
    private string _name = "Recipes";
    private string _singularName = "Recipe";
    private string _slug = $"recipes-{Guid.NewGuid():N}"[..16];

    public CollectionRequestBuilder Named(string name, string singularName, string slug)
    {
        _name = name;
        _singularName = singularName;
        _slug = slug;
        return this;
    }

    public CollectionRequestBuilder WithField(string? id, string label, CollectionFieldType type, bool required = false)
    {
        _fields.Add(new CollectionFieldRequest(id, label, type, required));
        return this;
    }

    public CreateCollectionRequest BuildCreate(Guid websiteId) => new(websiteId, _name, _singularName, _slug, [.. _fields]);

    public UpdateCollectionRequest BuildUpdate() => new(_name, _singularName, _slug, [.. _fields]);
}

/// <summary>Builds item requests for tests: a draft with a slug and no values, until a test adds some.</summary>
public class CollectionItemRequestBuilder(string slug)
{
    private readonly Dictionary<string, JsonElement> _values = [];
    private CollectionItemStatus _status = CollectionItemStatus.Draft;

    public CollectionItemRequestBuilder With(string fieldId, object? value)
    {
        _values[fieldId] = JsonSerializer.SerializeToElement(value);
        return this;
    }

    public CollectionItemRequestBuilder Published()
    {
        _status = CollectionItemStatus.Published;
        return this;
    }

    public SaveCollectionItemRequest Build() => new(slug, _values, _status);
}
