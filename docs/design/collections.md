# Collections: list and detail pages

Status: **first version built** (phase 5 in [page-content.md](page-content.md#8-phases)). [§ 0](#0-what-is-built) says what exists and where it differs from the direction below; the rest of this document is still the direction for what comes next.

## 0. What is built

- **Collections and fields** (`/collections` in the app): name, singular name, slug, an icon (each collection is its own item in the app's sidebar) and an ordered list of fields of type `text`, `richText`, `image`, `url`, `number`, `date` or `boolean`, with presets (Blog posts, Team members, Projects). Field ids (`fld_…`) are given by the API and never change.
- **Items**: a form built from the fields; save as draft or publish. Values are checked against the fields by `CollectionItemValidationService` (types, required, unknown fields, the same text, link, image and rich-text rules as pages, images must be in the website's media library) and come back as a `400` keyed `values.{fieldId}`.
- **Detail page**: creating a collection creates its page (`Kind = CollectionTemplate`, slug `{collection}/{item}`). The studio starts it from a design built from the fields and shows it with a preview item. Blocks connect props to fields through `bindings` (§ 3.1), with `scope: page` only.
- **Collection cards** (`collectionCards` block): a grid of a collection's items with an image, title and text field picked by the person, a "Read more" link to `/{collection}/{item}`, columns, order and limit.
- **Sample data**: development databases get a *Blog posts* collection with three published posts.

Where it differs from the direction below, and why:

| Direction | Built | Why |
|---|---|---|
| `CollectionFields` table, field endpoints (§ 4, § 7) | Fields are a JSON column on `Collections`, saved whole with `PUT /collections/{id}` | The editor edits the list as one form; a collection is read and written whole, like a page document |
| `PublishedValues` beside the draft (§ 4) | One set of values and a `Status` | Enough until there is a public site to render; adding it later doesn't change the API shape |
| `collectionList` with nested `itemBlocks` (§ 3.2) | `collectionCards` with a fixed card design | Covers the common case without nested editing in the studio; `collectionList` can come next |
| A static page's slug may not equal a collection slug (§ 4) | Allowed | `/blog` (a page with the cards) and `/blog/{item}` don't clash: they have a different number of segments |
| `PageBindingUsages`, `409` on breaking a bound field (§ 4, § 5) | Not yet | Publishing reports `binding.field_missing` and `binding.type_mismatch`, so nothing broken goes live. Proposed instead: change sets, [change-sets.md](change-sets.md) |
| `GET /collections/{id}/items?status=…` (§ 7) | No query filters yet | Lists are small; the studio filters published items itself |
| `/collection-items/{id}` (§ 7) | `/collections/{collectionId}/items/{itemId}` | Every item read is narrowed by its collection (IDOR) |

## 1. What it is for

A website owner defines their own **collections**, such as *Blog posts*, *Team members* or *Projects*. Each collection has fields (title, cover image, body, date…) and **items** (the posts themselves). Then they design two kinds of pages once, in the Design Studio:

- **Detail page**: one design used for every item, at `/{collection}/{item}` (e.g. `/blog/opening-our-studio`). Its blocks show the item's fields.
- **List**: a block that repeats a small design for each item of a collection (e.g. "latest 3 posts" on the home page, the full `/blog` index, "other projects" on a project's detail page).

In the content, the item's properties appear as **bindings**: "this heading shows the item's *Title*", "this image shows its *Cover*", "this button links to the item's page".

## 2. Concepts

```
Website
 ├─ Collection        "Blog posts" (slug: blog)
 │   ├─ Field         fld_title   text      required
 │   ├─ Field         fld_cover   image
 │   ├─ Field         fld_body    richText
 │   ├─ Field         fld_date    date
 │   └─ Item          "Opening our studio" (slug: opening-our-studio) → values by field id
 └─ Page
     ├─ kind: static              /about          ← may contain collectionList blocks
     └─ kind: collectionTemplate  /blog/{item}    ← one per collection; has the "page" scope
```

### Field types

| Type | Stored value | Item editor | Can be bound to |
|---|---|---|---|
| `text` | string ≤ 500 | input | `badge.text`, `button.label`, `image.alt`, `image.caption`, any `html` (escaped and wrapped in `<p>`), testimonial/feature item text |
| `richText` | html (same allowlist as the studio) | Tiptap | `heading.html`, `text.html` |
| `image` | `/media/{id}/image` | media picker | `image.src`, `background.image`, gallery/carousel item `src` |
| `images` | `[{ src, alt }]` | media picker (multi) | `gallery.images`, `carousel.slides` |
| `url` | URL (same rules as `href`) | input | `button.href`, `nav` link `href`, `video.url` |
| `number` | number | input | text props, formatted |
| `date` | ISO date | date picker | text props, formatted (`date:long`, `date:short`) |
| `boolean` | bool | toggle | block visibility ("show when set"), later |
| `option` | one of the field's options | select | text props; list filters |
| `reference` | item id in another collection | item picker | nothing directly; enables paths like `author.name` later |

There are also **system fields** on every item: `$slug`, `$url` (the item's detail page), `$createdAt`, `$publishedAt`. `$url` is what makes "Read more" buttons work in a list.

## 3. Content model changes

There are two additive changes to the page document, so no `version` bump is needed.

### 3.1 `bindings` on a block

Bindings sit **beside** `props`, not inside them:

```jsonc
{
  "id": "b_title",
  "type": "heading",
  "props": { "html": "<p>Post title</p>", "level": 1, "size": "xl" },   // literal: editor preview + fallback
  "bindings": {
    "html": { "scope": "page", "field": "fld_title" }
  },
  "style": { … }
}
```

- `props` stays fully typed and valid on its own, so phase 1 validation and the TS types don't change. A bound prop's literal value is the placeholder the studio shows when no preview item is picked, and the fallback when a field is empty.
- A binding key is a prop name (`html`, `src`, `href`). Later it could be a list prop path (`items[].title`) for repeating fields.
- `scope` says where the value comes from:
  - `page`: the item a detail page is showing (only on `collectionTemplate` pages);
  - `item`: the nearest enclosing `collectionList` item;
  - `site`: the website's own settings (name, logo), later.
- `format` (optional): `date:long`, `number:0.00`, `text:uppercase`.

**Inline fields in rich text** (e.g. "Written by *{author}* on *{date}*") use an inline Tiptap node serialised as `<span data-field="page:fld_date">12 March</span>`. That adds `span[data-field]` to the rich-text allowlist, and the inner text is the placeholder.

### 3.2 `collectionList` block

A new block type whose children are the design repeated per item:

```jsonc
{
  "id": "b_posts",
  "type": "collectionList",
  "props": {
    "collectionId": "8f2e…",
    "filter": [ { "field": "fld_category", "op": "equals", "value": "news" } ],
    "sort": [ { "field": "$publishedAt", "direction": "desc" } ],
    "limit": 3,
    "pagination": false,
    "excludeCurrent": true,          // on a detail page: leave out the item being shown
    "columns": 3, "gap": 24,
    "emptyText": "No posts yet."
  },
  "itemBlocks": [
    { "id": "b_cover", "type": "image",   "props": { … }, "bindings": { "src":  { "scope": "item", "field": "fld_cover" } }, "style": { … } },
    { "id": "b_name",  "type": "heading", "props": { … }, "bindings": { "html": { "scope": "item", "field": "fld_title" } }, "style": { … } },
    { "id": "b_more",  "type": "button",  "props": { "label": "Read more", … }, "bindings": { "href": { "scope": "item", "field": "$url" } }, "style": { … } }
  ],
  "style": { … }
}
```

This is the first **nested** block. To get ready for it, phases 1–2 write the document validators as one walk over the tree (a `DocumentWalker` that visits sections and blocks and tracks the current scope), not as code that assumes `sections[].blocks[]` is flat. Id uniqueness covers nested blocks too.

## 4. Database

```
Collections
  Id, WebsiteId FK (cascade), Name TEXT(100), SingularName TEXT(100), Slug TEXT(100),
  CreatedAt, UpdatedAt
  UNIQUE (WebsiteId, Slug)

CollectionFields
  Id TEXT ('fld_…', stable), CollectionId FK (cascade), Key TEXT(60), Label TEXT(100),
  Type TEXT(20), Required BOOL, Position INT, Settings TEXT (JSON: options, min/max, formats)
  UNIQUE (CollectionId, Key)

CollectionItems
  Id, CollectionId FK (cascade), Slug TEXT(200), Status 'Draft'|'Published',
  Values TEXT (JSON, keyed by field id),             -- the item's draft
  PublishedValues TEXT (JSON, nullable),             -- what visitors see
  CreatedAt, UpdatedAt, PublishedAt
  UNIQUE (CollectionId, Slug)

Pages (changed)
  + Kind 'Static' | 'CollectionTemplate'   default 'Static'
  + CollectionId FK nullable (restrict)
  UNIQUE (CollectionId) WHERE Kind = 'CollectionTemplate'   -- one detail page per collection

PageBindingUsages        derived on every draft save / publish, like PageMediaUsages
  PageId, Kind, FieldId     PK all three
```

- **Values are a JSON column keyed by field id**, not an EAV table (`ItemId, FieldId, Value`). An item is read and written whole, just like a page document. Filtering and sorting use SQLite `json_extract`; add generated columns plus indexes if a site ever gets large.
- **Field ids are stable.** Renaming a field's label or key changes nothing in items or bindings.
- **Draft and published values** mirror pages, so editing a live post doesn't change the site until it is published.
- **Slugs.** A static page's slug may not equal a collection slug (checked on both sides), so `/blog/x` routing is never ambiguous.

## 5. Validation

The same two levels as pages ([page-content.md § 6](page-content.md#6-validation)), plus item values.

**Item values** (on every item save, `400`). A validator is built from the collection's field definitions:

- each value matches its field's type;
- `required` fields are set;
- text lengths, `url` and image-source rules, and the rich-text allowlist are the same rule objects the page document uses (so share them as small reusable validators, not inline regexes);
- unknown field ids are rejected.

**Bindings, level 1** (structural, every draft save, no database):

- the binding key is a prop that exists on the block type and is bindable;
- `scope` is known;
- `item` scope only inside `collectionList.itemBlocks`;
- `format` is known.

**Bindings, level 2** (content, publish and `/check`, uses a `CheckContext` that carries the page's collection and a scope stack):

| Code | Severity | Rule |
|---|---|---|
| `binding.field_missing` | error | The field was deleted from the collection |
| `binding.type_mismatch` | error | The field type can't feed this prop (table in §2) |
| `binding.page_scope` | error | `page` scope used on a page that isn't a collection template |
| `list.collection_missing` | error | `collectionList.collectionId` doesn't exist or belongs to another website |
| `list.filter_field` | error | A filter or sort field doesn't exist or doesn't support that operator |
| `binding.optional_field` | warning | Bound to a field that isn't required: the literal fallback will show for some items |
| `video.binding_not_video` | warning | A video's `url` is bound to a link field whose value isn't a YouTube or Vimeo link for some published items |
| `link.page_missing` | warning | Also for `/{collection}/{item}` links whose item doesn't exist or isn't published |

**Changing collections that pages depend on.** Fields change through change sets, a general system for staging changes to several entities and applying them in one transaction. See [change-sets.md](change-sets.md).

## 6. Rendering and the studio

- **Public site:** `/{slug}` → static page; `/{collectionSlug}/{itemSlug}` → the collection's template page, rendered with `page` scope = that **published** item (`404` when unpublished). `collectionList` queries published items. Bound props are resolved first, then the normal block renderer runs, so blocks never know about collections.
- **Studio:**
  - a template page shows a "Previewing: *Opening our studio* ▾" picker in the top bar, which calls `GET /collections/{id}/items?limit=20`;
  - bound props show the preview item's value with a field chip;
  - the settings panel gets a "Connect to field" menu per bindable field, filtered to compatible field types;
  - lists render their `itemBlocks` once per preview item.

## 7. API sketch

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/collections?websiteId=` | Collections of a website (with field definitions) |
| `POST` | `/collections` | Create `{ websiteId, name, singularName, slug }`; also creates its template page |
| `PATCH` / `DELETE` | `/collections/{id}` | Rename, change slug / delete (`409` while pages list it) |
| `POST` | `/collections/{id}/fields` | Add a field |
| `PATCH` / `DELETE` | `/collections/{id}/fields/{fieldId}` | Change / delete a field (`409` when bound and the change breaks it) |
| `PUT` | `/collections/{id}/fields/order` | Reorder fields |
| `GET` | `/collections/{id}/items?status=&search=&sort=&page=` | Items, paged |
| `POST` | `/collections/{id}/items` | Create `{ slug, values }` |
| `GET` / `PUT` / `DELETE` | `/collection-items/{id}` | Read / save values / delete |
| `POST` | `/collection-items/{id}/publish` / `unpublish` | Publish or take offline |

Page content endpoints don't change: a template page's document is loaded and published through `/pages/{id}/content`, like any page.

## 8. What phases 1–4 do now to keep this open

| Decision now | Why it matters later |
|---|---|
| Blocks are typed records; `props` holds literal values only | `bindings` can be added beside them without touching any props type |
| The document model is the API contract (no DTO copy) | `collectionList` and `bindings` are added in one place |
| Level 1 (no DB) / level 2 (with `CheckContext`) split | Binding checks need the collection: they slot into level 2 |
| Validators walk the tree with a scope | Nested `itemBlocks` and `item` scope work without a rewrite |
| Media stored as relative `/media/{id}/image` | Item `image` values use the same format and rule |
| Derived usage tables (`PageMediaUsages`) | `PageBindingUsages` follows the same pattern |
| `PageDocuments` keyed by `PageId` only | A template page is just a page with a `Kind`; storage is unchanged |
