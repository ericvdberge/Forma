# Secure links: an example Forma plugin

A plugin written in Rust that:

- **`page.check`** (hook): warns about links that use plain `http://` in buttons, menus and rich text, pointing at the block, prop and menu item, like the built-in checks;
- **`page.published`** (notification): logs which page went live.

How plugins work: [`docs/design/plugins.md`](../../../docs/design/plugins.md).

## Build

```bash
rustup target add wasm32-unknown-unknown
cargo build --release --target wasm32-unknown-unknown
```

The module is `target/wasm32-unknown-unknown/release/secure_links.wasm` (about 70 KB). Build for `wasm32-unknown-unknown`, not `wasm32-wasip1`: plugins get no WASI, and a WASI import is refused when the plugin loads.

## Install

Copy `plugin.json` and the module into a folder named after the plugin's id under the API's plugins folder (`Plugins:Path`: the repository's [`plugins/`](../../../plugins) in development and with `docker compose`, `/app/plugins` in the image), then restart the API:

```bash
mkdir -p ../../../plugins/secure-links
cp plugin.json ../../../plugins/secure-links/
cp target/wasm32-unknown-unknown/release/secure_links.wasm ../../../plugins/secure-links/plugin.wasm
```

`GET /plugins` should list it as `loaded`. Add an `http://` link to a page, choose **Publish** in the studio (or `POST /pages/{id}/check`), and the warning appears with the code `secure-links:link.insecure`. `GET /plugins/secure-links/runs` shows each run with its logs.

## Files

- [`src/forma.rs`](src/forma.rs): the plugin ABI (`forma_alloc`, `forma_handle`, `forma.log`). Copy it to start a new plugin.
- [`src/lib.rs`](src/lib.rs): the plugin itself.
