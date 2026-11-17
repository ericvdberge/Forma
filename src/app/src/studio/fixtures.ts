/**
 * Normalised studio content written to src/api/Contracts/Studio by `npm run studio:fixtures`.
 * The API seeds sample pages from home.json and page.json, and its contract tests check that every
 * fixture passes the server's validation, so a change here that the API wouldn't accept fails CI.
 */
import homeJson from '@/data/studio/home.json'
import pageJson from '@/data/studio/page.json'
import savedJson from '@/data/studio/saved.json'
import { createBlock, libraryBlocks } from './blocks'
import { createSection, globalElements, normalizeDocument, sectionTemplates, type PageDocumentJson } from './sections'
import type { Block, BlockType, PageDocument, Section } from './types'

/** Replaces random ids (`s_ab12cd34`) with stable ones, so regenerating doesn't change the files. */
function stableIds(doc: PageDocument, prefix: string): PageDocument {
  let n = 0
  const random = /_[0-9a-f]{8}$/
  for (const section of doc.sections) {
    if (random.test(section.id)) section.id = `s_${prefix}${++n}`
    for (const block of section.blocks) if (random.test(block.id)) block.id = `b_${prefix}${++n}`
  }
  return doc
}

const page = (sections: Section[]): PageDocument => ({ version: 1, sections })

export function buildFixtures(): Record<string, PageDocument> {
  // Every block type: the library's plus the ones only templates add.
  const types: BlockType[] = [...new Set<BlockType>([...libraryBlocks, 'nav'])]
  const blocks: Block[] = types.map((type) => createBlock(type))
  const saved = savedJson.map((item) =>
    item.kind === 'section'
      ? normalizeDocument({ sections: [item.section] } as PageDocumentJson).sections[0]!
      : normalizeDocument({ sections: [{ blocks: [item.block] }] } as PageDocumentJson).sections[0]!,
  )

  return {
    // Sample content the API seeds into new databases ({{title}} and {{website}} are filled in per page).
    'home.json': stableIds(normalizeDocument(homeJson as PageDocumentJson), 'home'),
    'page.json': stableIds(normalizeDocument(pageJson as PageDocumentJson), 'page'),
    // Everything the studio can create, for the contract tests.
    'blocks.json': stableIds(page([createSection({}, blocks)]), 'block'),
    'templates.json': stableIds(page([...sectionTemplates, ...globalElements].map((t) => t.create())), 'template'),
    'saved.json': stableIds(page(saved), 'saved'),
  }
}
