# Page content API

Status: **implemented** (content, drafts and publishing), saved items proposed. Design and rationale: [page-content.md](page-content.md); drafts and publishing: [drafts-and-publish.md](drafts-and-publish.md).

Conventions (the same as the rest of the API):

- JSON with camelCase properties and camelCase string enums; dates are UTC ISO 8601.
- Validation errors are `400` `application/problem+json` (`ValidationProblem`).
- Ids are GUIDs, except section and block ids inside a document, which the studio creates (`s_ab12cd34`, `b_ab12cd34`).
- The OpenAPI document (`src/api/Forma.Api/OpenApi/forma-api.json`) is generated on build and is the source of the app's typed client. This page explains intent; the OpenAPI document is the exact schema.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/pages/{pageId}/content?version=` | A page's live content, or its draft (`version=draft`) |
| `PUT` | `/pages/{pageId}/content/draft` | Save the page's draft (the studio's **Save**) |
| `DELETE` | `/pages/{pageId}/content/draft` | Discard the draft |
| `POST` | `/publish` | Put drafts live, a page among them (the studio's **Publish**); see [drafts-and-publish.md § 7](drafts-and-publish.md#7-api) |
| `GET` | `/saved-items?websiteId=` | The Saved tab (proposed) |
| `POST` | `/saved-items` | Save a section or block (proposed) |
| `PATCH` | `/saved-items/{id}` | Rename (proposed) |
| `DELETE` | `/saved-items/{id}` | Delete (proposed) |
| `DELETE` | `/media/{id}` (changed) | `409` when the image is used on a page (proposed) |

The studio keeps edits in the browser until **Save**, which stores them as the page's draft on the server. Visitors only ever see the live version; **Publish** saves the draft and puts it live with `POST /publish`, alone or together with other drafts. There is no autosave or revision history yet.

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
  "document": { "version": 1, "sections": [] },
  "version": "draft",
  "hasDraft": true,
  "draftUpdatedAt": "2026-11-21T09:12:40Z",
  "publishedAt": "2026-11-17T19:02:11Z",
  "createdAt": "2026-11-15T08:30:00Z",
  "updatedAt": "2026-11-21T09:12:40Z"
}
```

| Property | Notes |
|---|---|
| `version` | Which version `document` is: `live` or `draft`. |
| `hasDraft` | Whether there are saved changes that aren't live. |
| `draftUpdatedAt` | When the draft was last saved; `null` without one. |
| `publishedAt` | When the page was last published; `null` while it never was (then there is no live version). |

### `ContentIssue`

```json
{ "severity": "warning", "code": "link.page_missing", "message": "The button ‘Pricing’ links to /pricing, which isn’t a page of this website.",
  "sectionId": "s_cta", "blockId": "b_btn", "field": "href", "itemIndex": null }
```

`itemIndex` points into a list prop (for example the third FAQ item). Codes are listed in [page-content.md § 6](page-content.md#6-validation).

---

## Endpoints

### `GET /pages/{pageId}/content?version=live|draft`

Loads a page's content. `live` (the default) is what visitors see; the website preview reads it. `draft` is the draft, or the live version when there is none; the studio reads it (and the live one) when it opens a page.

| Status | Body | When |
|---|---|---|
| `200` | `PageContent` | |
| `204` | | The page has no content in that version yet: never published (`live`), or nothing at all; the studio starts from its starter page |
| `404` | | No page with this id |

### `PUT /pages/{pageId}/content/draft`

Saves the page's draft. Nothing changes on the site. The studio calls it on **Save** (and Ctrl+S), and before **Publish**.

Request (max 1 MB):

```json
{ "document": { "version": 1, "sections": [ … ] } }
```

| Status | Body | When |
|---|---|---|
| `200` | `PageContent` (`version: draft`) | Saved |
| `400` | `ValidationProblem` | The JSON doesn't match the schema, or the document breaks a structural rule (keyed by property path, e.g. `Document.Sections[0].Settings.SplitRatio`) |
| `404` | | No page with this id |
| `413` | | Body larger than 1 MB |

Content rules (missing images, links that go nowhere, fields that don't exist) are not checked here: a draft may be unfinished. They are checked when it is published.

### `DELETE /pages/{pageId}/content/draft`

Discards the draft: the page goes back to its live version, and it is taken out of any pending scheduled publish. `204`, or `404` when there is no such page.

### Publishing a page

`POST /publish` with `{ websiteId, collections: [], pages: [pageId], items: [] }` (see [drafts-and-publish.md § 6–7](drafts-and-publish.md#6-publishing)). The content rules run on the site as it would be. Errors are a `400` problem whose `issues` extension lists them, each with the `entity` it is on and, for pages, the section and block, so the studio can select it; `needs` lists drafts that would fix them:

```json
{
  "title": "Some changes need attention before they can go live.",
  "status": 400,
  "errors": { "About": ["This image has no picture yet."] },
  "issues": [
    { "severity": "error", "code": "image.missing", "message": "This image has no picture yet.",
      "entity": { "kind": "page", "id": "0b6c…", "name": "About", "collectionId": null },
      "sectionId": "s_hero", "blockId": "b_img", "field": "src", "itemIndex": null }
  ],
  "needs": []
}
```

`PUT /pages/{pageId}/content` (publish one page with its document) is gone: the studio saves the draft and publishes it.

---

## Saved items (sketch)

`SavedItem`: `{ id, websiteId, name, kind: "section" | "block", section?: Section, block?: Block, createdAt, updatedAt }`. The content is validated with the same structural rules as a page.

- `GET /saved-items?websiteId=` → `200 SavedItem[]`, newest first.
- `POST /saved-items` with `{ websiteId, name, kind, section | block }` → `201`, `400`, `404` (website).
- `PATCH /saved-items/{id}` with `{ name }` → `200`, `400`, `404`.
- `DELETE /saved-items/{id}` → `204`, `404`.

### `DELETE /media/{id}` (changed)

When the image is used by a page, the result is `409` with `{ "usages": [ { "pageId", "pageTitle" } ] }`. `?force=true` deletes it anyway. The pages then fail `media.not_found` on their next publish.

---

## Future: collections

Collection list and detail pages reuse these endpoints unchanged: a detail template is a page with `kind = collectionTemplate`, and bindings are an extra optional `bindings` property on blocks. Their own endpoints (`/collections`, `/collections/{id}/fields`, `/collections/{id}/items`) are specified in [collections.md](collections.md#7-api-sketch).
