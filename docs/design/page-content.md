# Page content: backend design

Status: **phases 1–2 implemented** (publish-only since the PR 16 review), phases 3–5 proposed · Scope: storing, validating and publishing what the Design Studio edits.

Related:

- [page-content-api.md](page-content-api.md): the endpoint specification.
- [collections.md](collections.md): future collection pages (list and detail pages filled from an entity's fields).

## 1. Goals

1. The Design Studio (`/pages/:id/edit`) stores its work through the API.
2. Edits stay in the browser until **Publish**, which sends the whole page with one `PUT`. There are no server-side drafts, autosave or revisions. *Proposed to change: drafts on the server, published alone or together with other drafts, see [drafts-and-publish.md](drafts-and-publish.md).*
3. Anything stored has passed validation: a broken or malicious document never reaches the database, and a page with fixable problems (an image without a file, a link to a deleted page) can't be published.
4. The model can grow into collection pages (see [collections.md](collections.md)) without a rewrite.

Out of scope for now: the public site renderer, shared header and footer, publish history, and multiple users.

## 2. The main decision: a page is one JSON document

The studio already works with one plain-JSON `PageDocument` per page (`src/app/src/studio/types.ts`):

```
PageDocument { version, sections[] }
  Section    { id, settings, blocks[] }
    Block    { id, type, props, style }   // props depend on type (16 block types)
```

The studio clones it whole for undo, publishes it whole, and never needs one block on its own. So it is stored **as one JSON value**, not spread over `Sections` and `Blocks` tables.

| | One JSON document (chosen) | Section and block tables |
|---|---|---|
| Save | One row write | Diff and upsert up to hundreds of rows |
| Order | Array order | Position columns to keep in step |
| Publish | Write one value | Write a tree of rows |
| Block-specific props | Typed in C#, stored as JSON | A JSON column anyway, or one table per type |
| Query "pages that use X" | Derived usage tables (below) | Joins |

Anything that needs relational integrity is pulled **out** of the document into small derived tables, rebuilt on every publish (`PageMediaUsages` now, binding usages later).

## 3. Layers

The existing flow stays the same: Endpoint → CoreService → Repository → Dao. Core services and repositories only create, read, update and delete (see `CLAUDE.md`); rules and workflows live in supporting services and endpoints.

| Layer | Page content | Notes |
|---|---|---|
| `Forma.Api` | `PageContentEndpoints`, `PageContentDto`, `PublishPageContentRequest` (+ validator), `PublishedPageContentDto`, `ContentIssueDto` | Request validation, the publish workflow, HTTP results |
| `Forma.Application` | `Documents/*` (the document model and its validators), `PageContent` / `PageContentInsert` / `PageContentUpdate`, `IPageContentCoreService` (CRUD), `IPageContentValidationService` (content rules), `IRichTextSanitizer` | Owns the document model and its rules |
| `Forma.Infrastructure` | `PageContentDao`, `IPageContentRepository` (CRUD), `PageContentFilter` + `ApplyFilter` | Stores the document as a JSON string; knows nothing about blocks |

**Publishing** is done by the endpoint with core services only: find the page (`PageFilter { Id }`), run the content rules, then create the content or update it with `Status = Published`. Filters on every read (`PageContentFilter { PageId = page.Id }`) are how access will be scoped per website later (IDOR).

**An exception to the DTO rule.** Elsewhere, Api DTOs are separate records mapped from Application entities. The document model (`Forma.Application.Documents`) is used **as is** in the DTOs. Mirroring 16 block types and their props as DTOs, plus two mappers, would only add drift. The document *is* the contract.

**One source of truth for the shape.** The C# document model drives the OpenAPI document (`Forma.Api/OpenApi/forma-api.json`), and the app's generated client (`openapi-ts`) creates TS types from it. The studio keeps its own hand-written types (`src/app/src/studio/types.ts`: generic blocks and literal unions the editor relies on), and `src/app/src/studio/contract.ts` asserts at compile time that a studio document is assignable to the generated `PageDocument`. UI-only metadata (labels, icons, `advanced`) stays in `blocks.ts`.

## 4. Database

### Phase 1

```
PageContents
  PageId         GUID     PK, FK → Pages.Id (ON DELETE CASCADE)
  Status         TEXT(20) 'Draft' | 'Published'
  SchemaVersion  INT      the document's `version`
  Content        TEXT     the document as JSON
  CreatedAt      DATETIME UTC
  UpdatedAt      DATETIME UTC
```

- **Why a separate table and not columns on `Pages`?** `GET /pages` keeps reading small rows.
- **No row yet** means the page has no content: `GET` answers `204` and the studio starts from its starter page.
- **Publishing** creates the row, or updates `Content` and sets `Status = Published`. `Pages.Status` is not changed by it yet (the app updates its cached Pages list); that needs a page update endpoint.
- **Seeding.** `PageContentSeeder` (Forma.Application, run with the sample data) gives the sample pages content from `Forma.Application/Seeding`: `home.json` for `home`, the starter `page.json` for `about` and `contact`. Content of published sample pages is stored as published. Pages that already have content are left alone.

### Later phases

```
PageMediaUsages        (phase 3)
  PageId, MediaItemId           PK both; FK → Pages (cascade), FK → Media (restrict)
  -- rebuilt from the document on every publish

SavedItems             (phase 3)
  Id, WebsiteId FK (cascade), Name TEXT(100), Kind 'Section'|'Block',
  SchemaVersion, Content TEXT (JSON), CreatedAt, UpdatedAt

PageRevisions          (phase 4, publish history and rollback)
  Id, PageId FK (cascade), Revision, Content, PublishedAt
```

## 5. Media references

Blocks currently store `apiUrl(dto.imageUrl)`, which is an **absolute URL including the API host** (`useMedia.ts`). Pages would break when the host changes (`:5134` in dev, `:8080` in Docker, a real domain later).

The rule: a document stores **`/media/{id}/image`** (relative, the media item's `imageUrl` exactly as the API returns it). The studio and the renderer prefix the base URL when displaying it. Allowed image sources:

| Pattern | Meaning |
|---|---|
| `""` | No image yet (an error when publishing) |
| `/media/{guid}/image` | A media item; must belong to the page's website |
| `/media/{name}.svg` | A built-in demo image shipped with the app (allowlist) |

Phase 1 implementation: change `useMedia.ts` so `src` keeps the relative URL, and resolve it in the blocks that render images.

## 6. Validation

There are two levels, both run by `PUT /pages/{id}/content` and both answered with a `400` problem.

### Level 1: structural (`PublishPageContentRequestValidator` → `400`)

If this fails, the **frontend** has a bug, so it is rejected and logged. A user can't cause it from the studio.

1. **Deserialisation** (System.Text.Json):
   - `type` picks the block record (`[JsonPolymorphic]`); an unknown type fails;
   - enums are strict strings;
   - `UnmappedMemberHandling.Disallow` rejects unknown and misspelled properties;
   - `AllowOutOfOrderMetadataProperties` because the studio writes `id` before `type`.
2. **`PageDocumentValidator`** (FluentValidation, in `Forma.Application/Documents/Validation`, one small validator per part with its messages next to its rules), using the inspector's ranges:

| Where | Rule |
|---|---|
| Document | `version == 1`; at most 100 sections; serialised size ≤ 512 KB (the request limit is 1 MB) |
| Ids | Section and block ids match `^[a-z]+_[A-Za-z0-9_-]{1,40}$` and are unique within the document |
| Section | At most 60 blocks; `splitRatio` 20–80; `splitAt` 0–60 (past the last block means everything is in the first column, e.g. a new empty section); `gap` 0–64; `minHeight` 0–900; `radius` 0–40; `animation.delay` 0–2 |
| Padding | Every side 0–400, desktop and the tablet/mobile overrides |
| Background | Colours match `^#([0-9a-fA-F]{3,4}\|[0-9a-fA-F]{6}\|[0-9a-fA-F]{8})$`; `image` is a valid image source (§5); `position` matches `^(left\|center\|right\|top\|bottom)( (left\|center\|right\|top\|bottom))?$`; `gradientAngle` 0–360; `overlayOpacity` 0–100 |
| Block style | `marginTop`/`marginBottom` 0–160 |
| `anchor`, `className` | `anchor` matches `^[a-z0-9-]{0,60}$`; `className` is space-separated tokens matching `^[A-Za-z0-9_-]+$`, ≤ 200 chars |
| Lists | ≤ 50 items (gallery, carousel, faq, testimonials, features, nav links, form fields); `columns` 1–4 |
| Strings | Plain text ≤ 500 chars; `html` ≤ 20 000 chars; URLs ≤ 2 000 chars |
| URLs (`href`, `ctaHref`) | Empty, `#anchor`, `/relative`, `http(s)://`, `mailto:` or `tel:`; never `javascript:`, `data:` or `vbscript:` |
| `video.url` | Empty or YouTube / Vimeo |
| Per type | heading `level` 1–4; divider `thickness` 1–6; spacer `height` 8–240; map `zoom` 3–19 and `height` 160–640; `radius`/`gap` 0–40; feature `icon` from the icon list |
| Rich text | `IRichTextSanitizer.IsClean(html)`: sanitising with the allowlist would change nothing |

**Rich text allowlist** (what the studio's Tiptap setup produces): `p, br, strong, em, u, s, a[href, rel], ul, ol[start], li, blockquote, span[data-muted]`. `href` allows `http(s)`, `mailto`, `tel` and relative links. The server **rejects** anything outside it: `IRichTextSanitizer.IsClean` parses the html and compares it with the sanitised version, so differences in how the same markup is written (entities, quoting) don't count. What is stored is exactly what was sent, so the client never has to reconcile a changed document. The renderer sanitises again before output (defence in depth). Implementation: Ganss.Xss `HtmlSanitizer` (`RichTextSanitizer`).

**Error format.** A `ValidationProblem` (`400`), keyed by the property path:
`"Document.Sections[2].Blocks[0].Props.Radius": ["'Radius' must be between 0 and 40."]`.
Malformed JSON (unknown type, bad enum, unknown or missing property) is a `400` `application/problem+json` from ASP.NET Core itself.

Numbers are read strictly (`"12"` is not a number), and an OpenAPI schema transformer describes them as plain `integer`/`number`, so the generated TS types aren't `number | string`.

### Level 2: content (`IPageContentValidationService` → `400` with `issues`)

These are problems a person can fix. Errors are returned in the `400` problem, keyed by `sectionId.blockId.field`, with the full list under `issues` so the studio can point at the block:

```json
{
  "status": 400,
  "errors": { "s_hero.b_img.src": ["This image has no picture yet."] },
  "issues": [
    { "severity": "error", "code": "image.missing", "sectionId": "s_hero", "blockId": "b_img", "field": "src",
      "message": "This image has no picture yet." }
  ]
}
```

| Code | Severity | Rule |
|---|---|---|
| `image.missing` | error | image, gallery image or carousel slide without `src` |
| `media.not_found` | error | `/media/{id}` doesn't exist or belongs to another website (also section backgrounds) |
| `gallery.empty` / `carousel.empty` | error | no images / slides |
| `video.missing` | error | video without a link |
| `link.empty` | error | button, nav link or nav CTA (when it has a label) without `href` |
| `form.no_fields` | error | form without fields |
| `link.placeholder` | warning | link is still `#` |
| `link.page_missing` | warning | `/slug` that is not a page of this website (`/` is the home page) |
| `image.alt_missing` | warning | image without `alt` |
| `background.image_missing` | warning | background set to an image with none chosen |
| `heading.multiple_h1` / `heading.skipped_level` | warning | heading order |
| `section.empty` / `page.empty` | warning | nothing to show |

Errors block publishing; warnings are shown but don't. **Calibrated on the sample content:** the studio's defaults and sample pages use `#` as a placeholder everywhere, and the sample home page links to `/signup` and `/templates`, which aren't pages. Both are therefore warnings, not errors. Otherwise no sample page could be published, and pages can't be created through the API yet. Once they can, `link.page_missing` can become an error.

Issues come errors first, then in page order. The studio's **Publish** button sends the page; when the answer is a `400` with issues, it lists them and choosing one selects its block or section. Warnings come back with the `200` and are shown after publishing. These checks need lookups (media, page slugs, later collection fields), so they live in a supporting service, `IPageContentValidationService`, built on the core services, and the endpoint calls it before storing anything.

### Tests

`Forma.Tests` builds documents with `PageDocumentBuilder` (studio defaults; a test only sets what it is about) and compares API results with the expected JSON. `SamplePagesApiTests` publishes every seeded sample page, so the seed content always passes validation.

## 7. Saving and schema versions

- **Publish only.** The studio keeps edits (and undo history) in the browser and sends the whole page on Publish. Closing the tab with unpublished changes asks first. Discard is an ordinary undoable change back to the published document.
- **Concurrency.** None for now: the last publish wins. Revisions (`If-Match`) can come back with multiple users or publish history (phase 4).
- **Schema versions.** Adding a field with a default needs no new version: C# model defaults and `normalizeDocument` fill it in. Renaming or removing a field adds an upgrader (`v1 → v2`) in `PageDocumentSerializer.Deserialize` that runs on **read**; the next publish writes the latest version.

## 8. Phases

| Phase | Backend | Frontend |
|---|---|---|
| **1** ✅ | Document model, `PageContents`, `GET /pages/{id}/content`, level 1 validation, sanitiser, seeding | Load from the API, relative media URLs, types from the generated client |
| 2 ✅ | `PUT /pages/{id}/content` (publish), level 2 issues | Publish through the API, issues dialog that selects the block |
| 3 | `SavedItems` endpoints, `PageMediaUsages`, `409` on deleting used media | Saved tab through the API, "used on N pages" on the Media page |
| 4 | `PageRevisions`, shared header/footer, public renderer | History panel |
| 5 | Collections (see [collections.md](collections.md)) | Collection editor, bindings in the studio |

## 9. Open questions

1. Should a 0-section draft of a seeded page show the starter template (frontend) or should the server create one on page creation? *Proposal: the server, when `POST /pages` exists; the seeder covers it until then.*
2. Should the level 2 warnings run before publishing, so the studio can show them live? *Proposal: later, from the editor's own copy of the rules or a dry-run flag on the `PUT`.*
