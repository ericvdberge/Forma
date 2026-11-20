// Writes the studio's normalised content (src/studio/fixtures.ts) to src/api/Contracts/Studio.
// Run after changing block defaults, templates or the sample pages: `npm run studio:fixtures -w forma-app`.
import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { createServer } from 'vite'

const appRoot = fileURLToPath(new URL('..', import.meta.url))
const outDir = fileURLToPath(new URL('../../api/Contracts/Studio/', import.meta.url))

const server = await createServer({ root: appRoot, logLevel: 'error', server: { middlewareMode: true }, appType: 'custom' })
try {
  const { buildFixtures } = await server.ssrLoadModule('/src/studio/fixtures.ts')
  await mkdir(outDir, { recursive: true })
  for (const [name, doc] of Object.entries(buildFixtures())) {
    await writeFile(outDir + name, JSON.stringify(doc, null, 2) + '\n')
    console.log(`wrote ${name}`)
  }
} finally {
  await server.close()
}
