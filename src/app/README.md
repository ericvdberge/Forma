# Forma — app

The Forma CMS: dashboard, website management and pages. Vue 3 + Vite + Vue Router + Tailwind CSS v4, built on [`@forma/design-system`](../design-system).

```bash
npm install          # at the repo root
npm run dev:app      # http://localhost:5173 (or the next free port)
npm run build:app    # type-check + production build
```

## Routes

| Path             | View                                                        |
| ---------------- | ----------------------------------------------------------- |
| `/`              | Dashboard                                                   |
| `/pages`         | Pages of the active website: filter, create, publish, archive |
| `/websites/:id?` | Switch the active website and edit its details              |
| everything else  | Designed sections that aren't built yet show "coming soon"  |

## Structure

```
src/
  components/shell/      sidebar, top bar, global search (Ctrl/⌘ K)
  components/dashboard/  dashboard sections
  data/                  types + seed content
  stores/cms.ts          in-memory store; swap its actions for API calls once src/api exists
  views/                 routed pages
```

Data lives in memory and resets on reload. The active website and sidebar state are remembered per browser.
