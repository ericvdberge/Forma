# Forma — API

The Forma backend: .NET 10 minimal APIs, EF Core and SQLite.

```bash
dotnet run --project src/api/Forma.Api --launch-profile http   # http://localhost:5134, docs at /scalar
dotnet test src/api/Forma.Api.slnx
```

In development the API applies migrations and seeds sample data on startup into `forma.db` (git-ignored). Each `dotnet build` writes the OpenAPI document to `Forma.Api/OpenApi/forma-api.json`; the app's client is generated from it (`npm run generate:api -w forma-app`).

## Projects

| Project                | Responsibility                                                       |
| ---------------------- | -------------------------------------------------------------------- |
| `Forma.Api`            | Minimal API endpoints, DTOs and Mapperly mappers, OpenAPI + Scalar   |
| `Forma.Application`    | Domain entities, core and supporting services, the page document model and its validation |
| `Forma.Infrastructure` | EF Core `DbContext`, SQLite, migrations, seeding, repositories, media file storage and image processing |
| `Forma.Analyzers`      | Build rules for the repositories (FORMA001–003)                       |
| `Forma.Tests`          | xUnit: studio contract tests, validation and API tests               |

A request goes endpoint → (supporting service) → core service → repository → DAO. How each layer is written is in [`CLAUDE.md`](../../CLAUDE.md).

## Data model

The SQLite tables, from the EF Core model snapshot. Every table also has `CreatedAt` and `UpdatedAt`, and every foreign key cascades on delete.

```mermaid
erDiagram
    Websites ||--o{ Pages : "has"
    Websites ||--o{ Media : "has"
    Websites ||--o{ Collections : "has"
    Pages ||--|| PageContents : "has"
    Collections ||--o{ CollectionItems : "has"
    Collections |o--o| Pages : "detail page"

    Websites {
        Guid Id PK
        string Name
        string Domain UK
        string Description
        string Status
    }
    Pages {
        Guid Id PK
        Guid WebsiteId FK
        string Title
        string Slug "unique per website"
        string Status
        string Kind "Static or CollectionTemplate"
        Guid CollectionId FK "nullable, unique"
    }
    PageContents {
        Guid PageId PK
        string Content "JSON page document"
        int SchemaVersion
        string Status
    }
    Media {
        Guid Id PK
        Guid WebsiteId FK
        string FileName
        string ContentType
        string ImagePath
        string ThumbnailPath
        int Width
        int Height
        long SizeInBytes
    }
    Collections {
        Guid Id PK
        Guid WebsiteId FK
        string Name "e.g. Blog posts"
        string SingularName "e.g. Blog post"
        string Slug "unique per website"
        string Icon
        string Fields "JSON list of fields"
    }
    CollectionItems {
        Guid Id PK
        Guid CollectionId FK
        string Slug "unique per collection"
        string Status "Draft or Published"
        string Values "JSON, keyed by field id"
        DateTime PublishedAt "nullable"
    }
```

A collection's detail page is a `Pages` row with `Kind = CollectionTemplate` and slug `{collection}/{item}`. Some references live inside JSON columns, so the API checks them and the database doesn't:

```mermaid
flowchart LR
    F["Collections.Fields<br/>id, label, type, required"]
    V["CollectionItems.Values<br/>{ fieldId: value }"]
    B["PageContents.Content<br/>block bindings: { prop: { scope, field } }"]
    C["collectionCards block<br/>collectionId, imageField, titleField, textField"]
    Col[("Collections.Id")]
    V -- "keys are field ids" --> F
    B -- "field is a field id" --> F
    C -- "*Field are field ids" --> F
    C -- "collectionId" --> Col
```
