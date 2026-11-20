# Page content API

Status: **implemented** (content and publishing), saved items proposed. Design and rationale: [page-content.md](page-content.md).

Conventions (the same as the rest of the API):

- JSON with camelCase properties and camelCase string enums; dates are UTC ISO 8601.
- Validation errors are `400` `application/problem+json` (`ValidationProblem`).
- Ids are GUIDs, except section and block ids inside a document, which the studio creates (`s_ab12cd34`, `b_ab12cd34`).
- The OpenAPI document (`src/api/Forma.Api/OpenApi/forma-api.json`) is generated on build and is the source of the app's typed client. This page explains intent; the OpenAPI document is the exact schema.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/pages/{pageId}/content` | A page's content |
| `PUT` | `/pages/{pageId}/content` | Publish a page (the studio's **Publish** button) |
| `GET` | `/saved-items?websiteId=` | The Saved tab (proposed) |
| `POST` | `/saved-items` | Save a section or block (proposed) |
| `PATCH` | `/saved-items/{id}` | Rename (proposed) |
| `DELETE` | `/saved-items/{id}` | Delete (proposed) |
| `DELETE` | `/media/{id}` (changed) | `409` when the image is used on a page (proposed) |

The studio keeps edits in the browser and only sends a page when it is published: there are no drafts, autosave or revisions on the server.

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
  "status": "published",
  "createdAt": "2026-11-15T08:30:00Z",
  "updatedAt": "2026-11-17T19:02:11Z"
}
```

| Property | Notes |
|---|---|
| `status` | `published` once the page was published; `draft` only for seeded sample content that never was. |
| `updatedAt` | When the content was last published (or seeded). |

### `ContentIssue`

```json
{ "severity": "warning", "code": "link.page_missing", "message": "The button ‘Pricing’ links to /pricing, which isn’t a page of this website.",
  "sectionId": "s_cta", "blockId": "b_btn", "field": "href", "itemIndex": null }
```

`itemIndex` points into a list prop (for example the third FAQ item). Codes are listed in [page-content.md § 6](page-content.md#6-validation).

---

## Endpoints

### `GET /pages/{pageId}/content`

Loads a page's content. The studio calls it once when it opens a page.

| Status | Body | When |
|---|---|---|
| `200` | `PageContent` | |
| `204` | | The page has no content yet; the studio starts from its starter page |
| `404` | | No page with this id |

### `PUT /pages/{pageId}/content`

Publishes a page: stores the document and sets its status to `published`. The studio calls it when **Publish** is chosen.

Request (max 1 MB):

```json
{ "document": { "version": 1, "sections": [ … ] } }
```

| Status | Body | When |
|---|---|---|
| `200` | `{ "content": PageContent, "warnings": [ ContentIssue ] }` | Published. Warnings are worth a look but didn't block. |
| `400` | `ValidationProblem` | The JSON doesn't match the schema, the document breaks a structural rule, or it has content errors |
| `404` | | No page with this id |
| `413` | | Body larger than 1 MB |

Structural errors are keyed by the property path (`Document.Sections[0].Settings.SplitRatio`). Content errors are keyed by where they are (`sectionId.blockId.field`), and the problem's `issues` extension lists them in full so the studio can select the block:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The page has errors to fix before it can be published.",
  "status": 400,
  "errors": { "s_hero.b_img.src": ["This image has no picture yet."] },
  "issues": [
    { "severity": "error", "code": "image.missing", "message": "This image has no picture yet.",
      "sectionId": "s_hero", "blockId": "b_img", "field": "src", "itemIndex": null }
  ]
}
```

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
