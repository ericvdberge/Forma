# Plugins

Put installed plugins here, one folder per plugin (`plugin.json` + `plugin.wasm`). `docker compose` mounts this folder at `/app/plugins`. The folders themselves are git-ignored.

How plugins work: [`docs/design/plugins.md`](../docs/design/plugins.md). An example to build and copy here: [`examples/plugins/secure-links`](../examples/plugins/secure-links).
