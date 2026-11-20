# Plugins: event-driven, sandboxed in WebAssembly

Status: **phase 1 implemented**, phases 2–4 proposed · Scope: letting third-party code react to and extend what happens to pages, without trusting it.

Related:

- [page-content.md](page-content.md): the page documents and the check/publish flow that plugins hook into.
- [`examples/plugins/secure-links`](../../examples/plugins/secure-links): a complete plugin in Rust.

## 1. Goals

1. People can extend Forma (extra publish rules, search indexing, notifications, exports) without forking it.
2. A plugin is **untrusted code**. A buggy or malicious one can't read files, reach the network, see other websites' data, crash the API, or hang a request.
3. Plugins react to **events** the API already has (a draft saved, a page published), and can extend one decision point: the **publish check**.
4. A plugin is one portable file that works in any deployment (dev, Docker, later a hosted Forma), whatever language it was written in.

Out of scope for phase 1: installing plugins from the app, per-website settings, plugins that call out to the network, and UI extensions in the studio.

## 2. The main decision: WebAssembly modules run in Wasmtime

| | WebAssembly, Wasmtime (chosen) | .NET assemblies (`AssemblyLoadContext`) | Out-of-process (webhooks, containers) |
|---|---|---|---|
| Isolation | Memory-safe sandbox; only imports the host gives | None: full access to the process | Strong, but needs infrastructure |
| CPU and memory limits | Fuel, epoch deadline, memory limit per call | Not enforceable in-process | Per container |
| Languages | Rust, Go (TinyGo), C/C++, Zig, AssemblyScript… | .NET only | Any |
| Cost per call | ~0.5 ms (fresh instance, see § 7) | ~0 | A network round trip |
| Can be a sync hook in a request | Yes | Yes | Only with timeouts and retries |
| Deployment | A file in a folder | A file in a folder | A service per plugin |

[Wasmtime](https://wasmtime.dev) (the `Wasmtime` NuGet package, Bytecode Alliance) is the most widely used standalone runtime, ships native builds for linux/macOS/windows x64 and arm64, and supports the limits we need: fuel metering, epoch interruption and memory limits.

**Why not WASI.** WASI would give plugins a standard libc-like interface, but every WASI capability (files, clocks, random, environment) is something to deny. Phase 1 gives plugins **no WASI at all**: their only import is `forma.log`. Rust's `wasm32-unknown-unknown` target and AssemblyScript build such modules out of the box; capabilities are added later as narrow, explicit host functions (§ 8).

**Why not the Component Model / WIT yet.** It would give typed interfaces instead of JSON, but toolchain support outside Rust is still uneven. The JSON ABI (§ 5) is a few lines in any language, and events are JSON in the API anyway. Revisit once WIT tooling is common.

## 3. Events

A plugin lists the events it handles in its manifest. There are two kinds:

| Event | Kind | When | Data | Output |
|---|---|---|---|---|
| `page.draftSaved` | Notification | After a draft save that changed something (not for no-op retries) | `page`, `revision`, `document` | Ignored |
| `page.published` | Notification | After a draft was published | `page`, `revision`, `document` (what is now live) | Ignored |
| `page.check` | Hook | `POST /pages/{id}/check` and every publish | `page`, `document` (the draft) | `{ "issues": [...] }` |

`page` is `{ id, websiteId, title, slug }`. `document` is the page document exactly as the API stores and returns it.

**Notifications** are queued after the change is committed and run on a background service (`PluginEventDispatcher`), so plugins never slow a save down and can't undo it. The queue is in memory (capacity 1000; if it is full, new events are dropped and logged). Delivery is at most once, which is fine for side effects that only inform (§ 8 has the outbox for when it isn't).

**Hooks** run during the request, after the built-in checks, one plugin after another. `PluginContentChecker` decorates the built-in `PageDocumentChecker`, so check and publish see exactly the same issues.

Every plugin gets the same envelope:

```json
{
  "event": "page.published",
  "id": "8d1e…",                 // unique per event; the same for every plugin that receives it
  "occurredAt": "2026-11-19T20:22:04Z",
  "data": { "page": { "id": "…", "websiteId": "…", "title": "About", "slug": "about" }, "revision": 2, "document": { … } }
}
```

### `page.check` output

```json
{
  "issues": [
    { "severity": "warning", "code": "link.insecure", "message": "This link uses http://…", "blockId": "b_nav", "field": "links", "itemIndex": 1 }
  ]
}
```

The fields are the built-in [content issue](page-content.md)'s. What a plugin returns is checked before anyone sees it:

- `severity` is `error` (blocks publishing) or `warning`; anything else drops the issue.
- `code` must match `^[a-z][a-zA-Z0-9._-]{0,59}$` and is stored as `{pluginId}:{code}` (e.g. `secure-links:link.insecure`), so plugins can't pose as built-in rules or each other.
- `message` is trimmed and cut at 300 characters.
- `blockId` and `sectionId` must exist in the page; otherwise they are dropped, with `field` and `itemIndex`. A `blockId` alone is enough: the section is filled in, so the studio can select it.
- At most 50 issues per plugin.

A plugin that fails (trap, limit, invalid JSON) adds one **warning**, `plugin.failed`, instead of blocking: a broken plugin must not stop people from publishing. A plugin that wants to block publishing must run and return an `error`.

## 4. The sandbox

`WasmSandbox` (Forma.Infrastructure) compiles each module once at startup and runs every invocation in a **fresh store and instance**, which are thrown away afterwards: nothing carries over between calls, events or plugins.

| Limit | Default (`Plugins:` config) | What happens |
|---|---|---|
| Imports | only `forma.log` | Anything else (WASI included) fails at load, with the import named |
| Fuel | `FuelPerInvocation` 200 000 000 | Deterministic instruction budget; trap "used up its instruction budget" |
| Wall clock | `Timeout` 2 s | Epoch interruption (10 ms ticks); trap "took longer than…" |
| Memory | `MaxMemoryBytes` 32 MB | `memory.grow` fails; a module whose initial memory is larger doesn't instantiate |
| Instances, tables, memories | 1 each, 10 000 table elements | Instantiation fails |
| Stack | 512 KB | Trap "overflowed its stack" |
| Output | `MaxOutputBytes` 256 KB | Run fails |
| Logs | `MaxLogLines` 50 per run, 1000 characters per line | Extra lines are counted, not kept |
| Module file | 10 MB | Load error |

Pointers and lengths the plugin returns are bounds-checked against its memory before anything is read. The sandbox **never throws for anything the plugin does**: every outcome is a `PluginInvocation` (succeeded, output, error, logs, fuel, duration).

What the sandbox doesn't protect against, and why that's acceptable for now:

- **Wasmtime bugs.** The runtime is the trust boundary. It is heavily fuzzed and has a [security process](https://docs.wasmtime.dev/security.html); keep the package up to date.
- **Malicious output within the rules** (e.g. a misleading warning). Codes are namespaced and plugins are installed by whoever runs the server, who decides what to trust.
- **Side channels** (timing). No plugin sees another website's data except through events it was given anyway; per-website plugins (§ 8) will scope this further.

## 5. The ABI

A plugin is a core WebAssembly module that:

| Exports | Signature | Purpose |
|---|---|---|
| `memory` | memory | Where input and output live |
| `forma_alloc` | `(len: i32) -> i32` | Returns a pointer to `len` writable bytes for the input |
| `forma_handle` | `(ptr: i32, len: i32) -> i64` | Handles the event (UTF-8 JSON at `ptr`); returns `(outPtr << 32) \| outLen`, or `0` for no output |

| Imports | Signature | Purpose |
|---|---|---|
| `forma.log` | `(level: i32, ptr: i32, len: i32)` | Logs a UTF-8 line: 0 debug, 1 info, 2 warning, 3 error. Shows up in the plugin's run history and the API log |

The host calls `forma_alloc`, copies the envelope in, calls `forma_handle`, and reads the output. Because the instance is discarded after one call, plugins don't need to free anything. The Rust version of this ABI is [`src/forma.rs`](../../examples/plugins/secure-links/src/forma.rs) in the example (40 lines).

## 6. Installing a plugin

```
{Plugins:Path}/              the repository's plugins/ in development and docker compose, /app/plugins in the image
  secure-links/
    plugin.json
    plugin.wasm
```

```json
{
  "id": "secure-links",                 // ^[a-z][a-z0-9-]{0,39}$, must equal the folder name
  "name": "Secure links",
  "version": "1.0.0",
  "description": "Warns about links that use plain http:// before a page is published.",
  "events": ["page.check", "page.published"],   // may be empty when the plugin only adds blocks
  "module": "plugin.wasm",              // optional; a file inside the folder
  "blocks": []                          // optional; see "Blocks" below
}
```

Plugins are read at startup (restart the API to add or update one). A folder that can't be loaded (unreadable manifest, unknown property or event, id/folder mismatch, missing or invalid module, a forbidden import) is **listed with its error and skipped**; it never stops the API.

### API

| Endpoint | Returns |
|---|---|
| `GET /plugins` | Every plugin folder: id, name, version, events, `loaded`, `enabled`, `error`, the module's SHA-256, its `blocks` |
| `GET /plugins/{id}/runs` | The plugin's last 25 runs, newest first: event, success, error, duration, fuel, logs. `404` for an unknown id |
| `PUT /plugins/{id}/enabled` | Body `{ "enabled": false }`: turns the plugin off (or on) at once. Returns the plugin |
| `DELETE /plugins/{id}` | Deletes the plugin's folder; it stops running at once. `204` |

The run history is in memory and resets when the API restarts; it is there to answer "did my plugin run, and why did it fail?".

### Turning plugins off and deleting them

A plugin that is **off** stays installed and loaded, but isn't run for any event and its blocks aren't offered in the studio. Which plugins are off is kept in `{Plugins:Path}/plugins.state.json` (`{ "disabled": ["secure-links"] }`), next to the plugins so it moves with them; a damaged file turns every plugin on. **Deleting** removes the plugin's folder. Both take effect immediately: the catalog swaps its list as a whole, so a run already in progress finishes with the plugin it started with (its module is freed by the garbage collector, not disposed under it).

Both need the API to write to the plugins folder; `docker compose` mounts it read-write. When it can't (e.g. a read-only mount), the endpoints answer `409` and nothing changes. Pages that use blocks of a plugin that is off or deleted keep them; the publish check warns with `plugin.block_missing`.

Anyone who can use the app can do this until there are users and roles (see Roadmap).

### Blocks

A plugin can add blocks to the studio's library, declared in its manifest:

```json
"blocks": [{
  "id": "product-card",                 // same rules as a plugin id, unique within the plugin
  "label": "Product card",
  "description": "A product with photo, price and a buy button",
  "icon": "shopping-bag",               // a Lucide icon name; unknown names get a puzzle piece
  "fields": [                           // the inspector, in order; at most 20
    { "key": "name", "label": "Product name", "kind": "text", "default": "Lounge chair" },
    { "key": "image", "label": "Photo", "kind": "image" },
    { "key": "accent", "label": "Button colour", "kind": "color", "default": "#0b0b0f", "advanced": true }
  ],
  "template": "<div style=\"...\"><img data-if=\"image\" src=\"{{image}}\" alt=\"{{name}}\"><h3>{{name}}</h3>…</div>"
}]
```

Field kinds are the built-in inspector controls: `text`, `textarea`, `url`, `image`, `color`, `number` and `slider` (with `min`/`max`), `toggle`, `select` and `segmented` (with `options`). A page stores the block as `{ "type": "plugin", "props": { "plugin": "product-card", "block": "product-card", "values": { … } } }`; values are strings (≤ 2,000 characters), numbers or booleans, and aren't checked against the plugin's fields, so pages stay saveable when the plugin changes or is removed.

**Why a declarative template, not a render hook.** Drawing on the canvas happens on every keystroke; a round trip to the API (and a sandbox run) per edit would be slow, and a template can be checked once, at load. A plugin that needs logic about its block uses the `page.check` hook, which receives the block like any other.

**What a template may contain**, checked when the plugin loads (a template that breaks a rule fails the plugin with the reason) and again in the studio when the block is drawn:

- tags: `div`, `span`, `p`, `h2`–`h4`, `strong`, `em`, `small`, `s`, `br`, `hr`, `ul`, `ol`, `li`, `figure`, `figcaption`, `a`, `img`;
- attributes: `style`, `href` (on `a`), `src` (on `img`), `alt`, `title`, and `data-if="key"`, which drops the element when that value is empty or off. No scripts, event handlers or classes;
- `href` and `src` are a single `{{key}}` slot or a fixed `https://`, relative or `#` address. In the studio a slot only takes `http(s):`, `mailto:`, `tel:`, `#` or relative links, and `https:` or relative images;
- `style` can't load anything (`url()`, `@import`, `expression()`, escapes). A slot in a `style` only takes a colour (`#rrggbb`) or a number;
- `{{key}}` in text becomes plain text, never markup; every slot must be one of the block's fields.

The studio is the only renderer today; a public renderer must apply the same rules.

## 7. Layers and performance

| Layer | Plugin code | Knows about |
|---|---|---|
| `Forma.Api` | `PluginEndpoints`, `PluginDto`, `PluginRunDto` | HTTP |
| `Forma.Application` | `PluginEvents` (names and payloads), `PluginRunner`, `PluginEventQueue` + `PluginEventDispatcher`, `PluginContentChecker`, `PluginRunLog`, `PluginCoreService`; `PageContentCoreService` publishes the events | Events and their meaning |
| `Forma.Infrastructure` | `WasmSandbox`, `PluginCatalog` (incl. on/off and delete), `PluginManifest`, `PluginBlockDefinition` + `PluginBlockTemplate` (block rules), `PluginOptions` | Bytes in, bytes out; nothing about pages |

Measured with the example plugin on the About page (≈ 10 KB document): **0.5–0.7 ms** per invocation once warm, ≈ 30 ms for the very first one, ≈ 0.6–0.7 M fuel. Compilation happens once, at startup. If many hook plugins make checks slow, the next step is Wasmtime's pooling allocator and instance pre-instantiation (`InstancePre`), not keeping instances alive (which would break isolation).

## 8. Later phases

**Phase 2: managing plugins from the app.** Turning plugins off and deleting them is done (see above). Still to do: upload a plugin (`POST /plugins`, multipart manifest + module, validated exactly as at startup, hot-loaded), enable it per website (`WebsitePlugins` table: `WebsiteId`, `PluginId`, `Enabled`, `Settings` JSON), and pass the website's settings in the envelope (`config`). A Plugins page in the app lists plugins, their status and run history. Only administrators may upload (needs users, see Roadmap).

**Phase 3: capabilities.** Narrow host functions, each declared in the manifest (`"capabilities": ["http:api.example.com", "kv"]`) and shown to whoever installs the plugin:

- `forma.http_request`, only to the hosts listed, with its own timeout and response size limit, counted against the plugin's wall-clock budget. Turns notifications into webhooks, search indexing, cache purges.
- `forma.kv_get` / `forma.kv_set`: a small per-plugin, per-website key-value store.
- `forma.now`: the current time, if a plugin needs it beyond `occurredAt`.

**Phase 4: reliable delivery and more events.** An outbox table written in the same transaction as the change, so notifications survive restarts and can be retried with backoff (at-least-once; plugins dedupe on the event `id`). New events as features land: `media.uploaded`, `media.deleted`, `page.created`, `page.deleted`, and hooks that transform content (e.g. `page.rendering` once there is a public renderer: the output would go through the same document validation as a save).

## 9. Testing

`Forma.Tests/Plugins` builds test plugins from WebAssembly text (`TestPlugins`):

- **Sandbox**: input/output and logs round-trip; an endless loop runs out of fuel; the timeout stops a plugin with fuel left; memory can't grow past the limit; out-of-bounds and oversized output is refused; log lines are capped; nothing carries over between invocations; WASI imports, missing exports and non-wasm bytes are refused at load.
- **Catalog**: valid plugins load; id/folder mismatch, unknown events, unknown manifest properties, missing modules and modules outside the folder are listed with their error.
- **API**: `GET /plugins` lists load errors; `page.check` issues are prefixed, pointed at the right section, and unknown pointers, severities and codes are dropped; a crashing hook adds `plugin.failed`; a plugin error blocks publishing (`422`); saving and publishing reach a notification plugin in the background with the right envelope.
