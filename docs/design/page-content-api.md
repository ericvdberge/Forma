# Page content API

Status: **phases 1–2 implemented**, phase 3 proposed. Design and rationale: [page-content.md](page-content.md).

Conventions (the same as the rest of the API):

- JSON with camelCase properties and camelCase string enums; dates are UTC ISO 8601.
- Validation errors are `400` `application/problem+json` (`ValidationProblem`), keyed by JSON path.
- Ids are GUIDs, except section and block ids inside a document, which the studio creates (`s_ab12cd34`, `b_ab12cd34`).
- The OpenAPI document (`src/api/Forma.Api/OpenApi/forma-api.json`) is generated on build and is the source of the app's typed client. This page explains intent; the OpenAPI document is the exact schema.

| Phase | Method | Route | Purpose |
|---|---|---|---|
| 1 | `GET` | `/pages/{pageId}/content` | Draft and published document of a page |
| 1 | `PUT` | `/pages/{pageId}/draft` | Save the draft (autosave) |
| 2 | `POST` | `/pages/{pageId}/check` | Content issues of the draft, without publishing |
| 2 | `POST` | `/pages/{pageId}/publish` | Publish the draft |
| 3 | `GET` | `/saved-items?websiteId=` | The Saved tab |
| 3 | `POST` | `/saved-items` | Save a section or block |
| 3 | `PATCH` | `/saved-items/{id}` | Rename |
| 3 | `DELETE` | `/saved-items/{id}` | Delete |
| 3 | `DELETE` | `/media/{id}` (changed) | `409` when the image is used on a page |

---

## Schemas

### `PageDocument`

```jsonc
{
  "version": 1,
  "sections": [
    {
      "id": "s_hero",
      "settings": {
        "type": "hero",                // hero | content | features | split | cta | header | footer | sidebar
        "name": "",
        "layout": "stack",             // stack | split
        "splitRatio": 45,              // 20–80, % of the first column
        "splitAt": 1,                  // index of the first block in the second column
        "verticalAlign": "center",     // start | center | end
        "padding": {
          "desktop": { "top": 80, "bottom": 80, "left": 48, "right": 48 },
          "tablet":  { "top": 64 },     // optional, each side optional: inherits from desktop
          "mobile":  { "top": 48, "left": 20, "right": 20 }
        },
        "background": {
          "kind": "image",             // none | color | image | gradient
          "color": "#ffffff",
          "image": "/media/7d1c…/image",
          "position": "left center",
          "gradientFrom": "#f5f4f0", "gradientTo": "#ffffff", "gradientAngle": 180,
          "overlayColor": "#000000", "overlayOpacity": 0
        },
        "contentWidth": "wide",        // narrow | normal | wide | full
        "align": "left",               // left | center
        "theme": "light",              // light | dark
        "minHeight": 0, "gap": 20, "radius": 0,
        "animation": { "entrance": "fade-up", "delay": 0 },   // none | fade | fade-up | zoom | slide-left
        "anchor": "", "className": "",
        "visibility": { "desktop": true, "tablet": true, "mobile": true }
      },
      "blocks": [
        {
          "id": "b_title",
          "type": "heading",
          "props": { "html": "<p>Build beautiful experiences.</p>", "level": 1, "size": "xl" },
          "style": {
            "align": "inherit",        // inherit | left | center | right
            "marginTop": 0, "marginBottom": 0,
            "maxWidth": "md",          // none | sm | md | lg
            "anchor": "", "className": "",
            "visibility": { "desktop": true, "tablet": true, "mobile": true }
          }
        }
      ]
    }
  ]
}
```

`props` per block `type` (same as `BlockPropsMap` in the studio):

| `type` | `props` |
|---|---|
| `heading` | `html`, `level` 1–4, `size` xl·lg·md·sm |
| `text` | `html`, `size` lg·md·sm, `tone` default·muted |
| `button` | `label`, `href`, `variant` primary·secondary·accent, `arrow`, `size` sm·md·lg |
| `badge` | `text` |
| `image` | `src`, `alt`, `aspect` auto·16/9·4/3·1/1·3/4, `radius`, `caption` |
| `gallery` | `images[] { src, alt }`, `columns`, `gap`, `radius` |
| `video` | `url`, `aspect` 16/9·4/3·1/1, `radius` |
| `divider` | `style` solid·dashed, `thickness` |
| `spacer` | `height` |
| `form` | `fields[] { label, type text·email·textarea, required }`, `submitLabel`, `successMessage` |
| `map` | `address`, `zoom`, `height`, `radius` |
| `carousel` | `slides[] { src, caption }`, `autoplay`, `aspect` 16/9·4/3·21/9, `radius` |
| `faq` | `items[] { question, answer }` |
| `testimonials` | `items[] { quote, name, role }`, `columns` |
| `features` | `items[] { icon, title, text }`, `columns`, `card` |
| `nav` | `logo`, `links[] { label, href, menu? }`, `ctaLabel`, `ctaHref`, `orientation` horizontal·vertical, `card` |

Documents are always sent **complete** (normalised by the studio). A missing required property is a `400`, not a default.

### `PageContent`

```json
{
  "pageId": "0b6c…",
  "draft": { "version": 1, "sections": [] },
  "draftRevision": 12,
  "draftUpdatedAt": "2026-11-17T19:02:11Z",
  "published": { "version": 1, "sections": [] },
  "publishedAt": "2026-11-15T08:30:00Z",
  "hasUnpublishedChanges": true
}
```

| Property | Notes |
|---|---|
| `draft` | Never null: a page without content gets `{ "version": 1, "sections": [] }`. |
| `draftRevision` | `0` when the draft has never been saved. Send it back as `baseRevision`. |
| `draftUpdatedAt` | `null` when the draft has never been saved. |
| `published`, `publishedAt` | `null` when the page has never been published. |
| `hasUnpublishedChanges` | `true` when there is no published version or it differs from the draft. |

---

## Phase 1

### `GET /pages/{pageId}/content`

Loads a page's draft and published document. The studio calls it once when it opens a page.

| Status | Body | When |
|---|---|---|
| `200` | `PageContent` | |
| `404` | | No page with this id |

### `PUT /pages/{pageId}/draft`

Replaces the page's draft. The studio's autosave calls it.

Request (max 1 MB):

```json
{ "document": { "version": 1, "sections": [ … ] }, "baseRevision": 12 }
```

| Status | Body | When |
|---|---|---|
| `200` | `{ "revision": 13, "updatedAt": "…", "hasUnpublishedChanges": true }` | Saved |
| `400` | `ValidationProblem` | The document fails level 1 validation, or the JSON doesn't match the schema |
| `404` | | No page with this id |
| `409` | `PageContent` (the current state) | `baseRevision` isn't the current draft revision: it was saved elsewhere |
| `413` | | Body larger than 1 MB |

Rules:

- Malformed JSON gets the same `400` shape, keyed by the JSON path of the offending value.

- `baseRevision` must be ≥ 0. `0` means "there is no saved draft yet"; if one exists, the result is `409`.
- Saving a document identical to the current draft still returns `200` with the **same** revision (no new revision, no `UpdatedAt` change). Autosave retries are therefore harmless.
- The page's `UpdatedAt` is touched so the Pages list sorts recently edited pages first.

Example `400`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "document.sections[0].blocks[3].props.href": ["Links must start with https://, http://, mailto:, tel:, / or #."],
    "document.sections[1].settings.splitRatio": ["Must be between 20 and 80."]
  }
}
```

---

## Phase 2

### `POST /pages/{pageId}/check`

Runs the publish rules on the current draft without publishing.

`200` → `{ "draftRevision": 13, "issues": [ ContentIssue ] }`, `404`.

`ContentIssue`:

```json
{ "severity": "warning", "code": "link.page_missing", "message": "The button ‘Pricing’ links to /pricing, which isn’t a page of this website.",
  "sectionId": "s_cta", "blockId": "b_btn", "field": "href", "itemIndex": null }
```

`itemIndex` points into a list prop (for example the third FAQ item). Codes are listed in [page-content.md § 6](page-content.md#6-validation).

### `POST /pages/{pageId}/publish`

Request `{ "revision": 13 }`: the draft revision the user saw when they pressed Publish.

| Status | Body | When |
|---|---|---|
| `200` | `PageContent` | Published: the draft was copied, `Pages.Status = Published` |
| `404` | | No page |
| `409` | `PageContent` | `revision` isn't the current draft revision |
| `422` | `{ "issues": [ ContentIssue ] }` | At least one `error`-level issue. Warnings never block. |

---

## Phase 3 (sketch)

### Saved items

`SavedItem`: `{ id, websiteId, name, kind: "section" | "block", section?: Section, block?: Block, createdAt, updatedAt }`. The content is validated with the same level 1 rules as a page.

- `GET /saved-items?websiteId=` → `200 SavedItem[]`, newest first.
- `POST /saved-items` with `{ websiteId, name, kind, section | block }` → `201`, `400`, `404` (website).
- `PATCH /saved-items/{id}` with `{ name }` → `200`, `400`, `404`.
- `DELETE /saved-items/{id}` → `204`, `404`.

### `DELETE /media/{id}` (changed)

When the image is used by a page's draft or published version, the result is `409` with `{ "usages": [ { "pageId", "pageTitle", "kind": "draft" | "published" } ] }`. `?force=true` deletes it anyway. The pages then fail `media.not_found` on their next publish.

---

## Future: collections

Collection list and detail pages reuse these endpoints unchanged: a detail template is a page with `kind = collectionTemplate`, and bindings are an extra optional `bindings` property on blocks. Their own endpoints (`/collections`, `/collections/{id}/fields`, `/collections/{id}/items`) are specified in [collections.md](collections.md#7-api-sketch).
