# @forma/design-system

Forma's brand tokens and Vue 3 components, styled with Tailwind CSS v4. Shared by `src/landing` and (later) `src/app` through npm workspaces.

## Use it in a workspace app

1. Add the workspace to the root `package.json` `workspaces` list, then depend on it:

   ```json
   "dependencies": { "@forma/design-system": "*" }
   ```

2. Use Tailwind v4 through Vite (`@tailwindcss/vite`) and import the styles once, in place of `@import 'tailwindcss'`:

   ```ts
   import '@forma/design-system/styles.css'
   ```

   This stylesheet imports Tailwind, defines the brand `@theme` tokens and `@source`s this package's components, so their classes are generated in the consuming app.

3. Import components:

   ```ts
   import { FButton, FCard, FLogo, palette } from '@forma/design-system'
   ```

## Contents

```
styles/index.css   Tailwind import + @theme tokens (colours, type, radii, shadows, motion)
tokens.ts          palette / typography / brand copy as TS data
components/        F-prefixed Vue components
utils/cn.ts        class-name joiner
index.ts           public API
```

| Token       | Value     | Utility examples               |
| ----------- | --------- | ------------------------------ |
| Charcoal    | `#0B0B0F` | `bg-charcoal`, `text-charcoal` |
| Accent Blue | `#3B82F6` | `bg-accent`, `text-accent-600` |
| Soft Gray   | `#E5E7EB` | `bg-soft-gray`                 |
| Warm White  | `#FAFAF8` | `bg-warm-white`                |
| Canvas / Stone / Sage / Ink | supporting neutrals | `bg-canvas`, `bg-stone` |

Typeface: **Satoshi** (Fontshare). Load it in the app's `index.html`:

```html
<link rel="stylesheet" href="https://api.fontshare.com/v2/css?f[]=satoshi@400,500,700,900&display=swap" />
```

A live showcase of every component is at `/#/design-system` in the landing app.
