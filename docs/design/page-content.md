# Page content: backend design

Status: **phase 1 implemented**, phases 2–5 proposed · Scope: storing, validating and publishing what the Design Studio edits.

Related:

- [page-content-api.md](page-content-api.md): the endpoint specification.
- [collections.md](collections.md): future collection pages (list and detail pages filled from an entity's fields).

## 1. Goals

1. The Design Studio (`/pages/:id/edit`) saves its work to the API instead of the Vue Query cache.
2. A page has a **draft**, which autosaves while you edit, and a **published** version, which visitors see.
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

The studio clones it whole for undo, publishes by copying it, and never needs one block on its own. So it is stored **as one JSON value**, not spread over `Sections` and `Blocks` tables.

| | One JSON document (chosen) | Section and block tables |
|---|---|---|
| Save | One row write | Diff and upsert up to hundreds of rows |
| Order | Array order | Position columns to keep in step |
| Publish / discard | Copy one value | Copy a tree of rows |
| Block-specific props | Typed in C#, stored as JSON | A JSON column anyway, or one table per type |
| Query "pages that use X" | Derived usage tables (below) | Joins |

Anything that needs relational integrity is pulled **out** of the document into small derived tables, rebuilt on every save (`PageMediaUsages` now, binding usages later).

## 3. Layers

The existing flow stays the same: Endpoint → CoreService → Repository → Dao.

| Layer | New in phase 1 | Notes |
|---|---|---|
| `Forma.Api` | `PageContentEndpoints`, `PageContentDto`, `SaveDraftRequest`, `SavedDraftDto`, `SaveDraftRequestValidator` | Cheap request checks, mapping, HTTP results |
| `Forma.Application` | `Documents/*` (the document model), `PageContent` entity, `IPageContentCoreService`, `IRichTextSanitizer` | Owns the document model and its rules |
| `Forma.Infrastructure` | `PageDocumentDao`, `IPageDocumentRepository`, `IPageRepository.GetPageAsync` | Stores the document as a JSON string; knows nothing about blocks |

**An exception to the DTO rule.** Elsewhere, Api DTOs are separate records mapped from Application entities. The document model (`Forma.Application.Documents`) is used **as is** in the DTOs. Mirroring 16 block types and their props as DTOs, plus two mappers, would only add drift. The document *is* the contract.

**One source of truth for the shape.** The C# document model drives the OpenAPI document (`Forma.Api/OpenApi/forma-api.json`), and the app's generated client (`openapi-ts`) creates TS types from it. The studio keeps its own hand-written types (`src/app/src/studio/types.ts`: generic blocks and literal unions the editor relies on), and `src/app/src/studio/contract.ts` asserts at compile time that a studio document is assignable to the generated `PageDocument`. TypeScript's structural typing lets extra properties through, so the contract test below covers those. UI-only metadata (labels, icons, `advanced`) stays in `blocks.ts`.

## 4. Database

### Phase 1

```
PageDocuments
  PageId         GUID     PK, FK → Pages.Id (ON DELETE CASCADE)
  Kind           TEXT(20) PK, 'Draft' | 'Published'
  SchemaVersion  INT      the document's `version`
  Content        TEXT     the document as JSON (canonical serialisation)
  ContentHash    TEXT(64) SHA-256 of Content, hex
  Revision       INT      Draft: incremented on every save (optimistic concurrency). Published: the draft revision it was copied from.
  UpdatedAt      DATETIME UTC
```

- **Why a separate table and not columns on `Pages`?** `GET /pages` keeps reading small rows, and the two versions share one shape.
- **No row yet** means an empty draft at revision 0, and "never published".
- **`hasUnpublishedChanges`** = no published row, or `Draft.ContentHash != Published.ContentHash`. Nothing is diffed at runtime.
- **`publishedAt`** = `Published.UpdatedAt`. `Pages.Status` stays what the Pages list shows (`Published` once there is a published row).
- **Seeding.** `PageContentSeeder` (Forma.Application, run with the sample data) gives the sample pages content from `src/api/Contracts/Studio`: `home.json` for `home`, the starter `page.json` for `about` and `contact`, plus a published copy for published pages. It reads each one into the model and writes it back, so stored JSON and hashes are canonical. Pages that already have content are left alone.

### Later phases

```
PageMediaUsages        (phase 3)
  PageId, Kind, MediaItemId     PK all three; FK → Pages (cascade), FK → Media (restrict)
  -- rebuilt from the document on every draft save and publish

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
| `""` | No image yet (allowed in a draft, an error when publishing) |
| `/media/{guid}/image` | A media item; must belong to the page's website |
| `/media/{name}.svg` | A built-in demo image shipped with the app (allowlist) |

Phase 1 implementation: change `useMedia.ts` so `src` keeps the relative URL, and resolve it in the blocks that render images.

## 6. Validation

There are two levels, with different audiences and status codes.

### Level 1: structural (every draft save → `400`)

If this fails, the **frontend** has a bug, so it is rejected and logged. A user can't cause it from the studio.

1. **Deserialisation** (System.Text.Json):
   - `type` picks the block record (`[JsonPolymorphic]`); an unknown type fails;
   - enums are strict strings;
   - `UnmappedMemberHandling.Disallow` rejects unknown and misspelled properties;
   - `AllowOutOfOrderMetadataProperties` because the studio writes `id` before `type`.
2. **`PageDocumentValidator`** (FluentValidation, in `Forma.Application`), using the inspector's ranges:

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

**Error format.** A `ValidationProblem` (`400`), keyed by JSON path:
`"document.sections[2].blocks[0].props.radius": ["Must be between 0 and 40."]`.
Malformed JSON (unknown type, bad enum, unknown or missing property) gets the same `400` shape: `JsonBodyExceptionHandler` turns the serializer's error into an `errors` entry keyed by the JSON path, e.g. `"document.sections[0].blocks[2]": ["Read unrecognized type discriminator id 'marquee'."]`.

Numbers are read strictly (`"12"` is not a number), and an OpenAPI schema transformer describes them as plain `integer`/`number`, so the generated TS types aren't `number | string`.

### Level 2: content (publish → `422`, also `POST /pages/{id}/check`), phase 2

These are problems a person can fix. They are returned as a list of issues, not as a ValidationProblem, so the studio can point at the block:

```json
{
  "issues": [
    { "severity": "error", "code": "image.missing", "sectionId": "s_hero", "blockId": "b_img", "field": "src",
      "message": "This image has no picture yet." }
  ]
}
```

| Code | Severity | Rule |
|---|---|---|
| `image.missing` | error | image, gallery or carousel item without `src` |
| `media.not_found` | error | `/media/{id}` doesn't exist or belongs to another website |
| `link.empty` | error | button, nav link or nav CTA without `href` |
| `link.page_missing` | error | `/slug` that is not a page of this website |
| `form.no_fields` | error | form without fields |
| `image.alt_missing` | warning | image without `alt` |
| `heading.multiple_h1` / `heading.skipped_level` | warning | heading order |
| `section.empty` / `page.empty` | warning | nothing to show |

Errors block publishing; warnings are shown but don't. These checks need lookups (media, page slugs, later collection fields), so they live in the core service as an `IPageDocumentChecker` with a `CheckContext` (website, page, later the collection scope; see [collections.md](collections.md#5-validation)).

### Contract test (keeps C# and TS in step)

- `npm run studio:fixtures -w forma-app` writes `src/api/Contracts/Studio/*.json` (`src/app/src/studio/fixtures.ts`): every block type with its defaults, every section template, the saved items, and the sample pages `home.json` / `page.json`.
- `Forma.Tests` (`StudioContractTests`) deserialises each fixture, validates it with level 1 and checks it round-trips through storage without losing anything.
- CI runs both, so a slider whose max goes past the server rule fails the build.

## 7. Saving, concurrency and schema versions

- **Autosave.** The studio PUTs the whole draft about 1.5 s after the last change (flushed when leaving the editor), one request at a time, and shows "Saving… / Saved / Couldn't save". Undo history stays client-side. Discard is an ordinary undoable change that autosaves the published document, so it needs no endpoint.
- **Concurrency.** Each save sends `baseRevision`. If it no longer matches, the API returns `409` with the current content, and the studio offers "Load the latest version" or "Keep mine" (the latter re-saves with the new revision).
- **Schema versions.** Adding a field with a default needs no new version: C# model defaults and `normalizeDocument` fill it in. Renaming or removing a field adds an `IDocumentUpgrader` (`v1 → v2`) that runs on **read**; the next save writes the latest version.

## 8. Phases

| Phase | Backend | Frontend |
|---|---|---|
| **1** ✅ | Document model, `PageDocuments`, `GET /pages/{id}/content`, `PUT /pages/{id}/draft`, level 1 validation, sanitiser, seeding, contract test | Load from the API, autosave, conflict dialog, relative media URLs, types from the generated client |
| 2 | `POST /pages/{id}/publish`, `POST /pages/{id}/check`, level 2 issues | Publish through the API, issues panel that selects the block |
| 3 | `SavedItems` endpoints, `PageMediaUsages`, `409` on deleting used media | Saved tab through the API, "used on N pages" on the Media page |
| 4 | `PageRevisions`, shared header/footer, public renderer | History panel |
| 5 | Collections (see [collections.md](collections.md)) | Collection editor, bindings in the studio |

## 9. Open questions

1. Should a 0-section draft of a seeded page show the starter template (frontend) or should the server create one on page creation? *Proposal: the server, when `POST /pages` exists; the seeder covers it until then.*
2. Should the level 2 warnings also run on draft save, so the studio can show them live? *Proposal: yes, later, through `/check` called after an autosave; not in the save response.*
