# Forma — landing page

Vue 3 + Vite + Tailwind CSS v4 marketing site for Forma, built on [`@forma/design-system`](../design-system).

Dependencies are installed from the repo root (npm workspaces):

```bash
npm install                # at the repo root
npm run dev:landing        # http://localhost:5173
npm run build:landing      # type-check + production build
```

- `/` — landing page
- `/#/design-system` — living brand guide & component showcase

## Structure

```
src/
  components/landing/   page sections composed from @forma/design-system
  views/                LandingPage, DesignSystemPage
  config.ts             external links (GitHub)
```
