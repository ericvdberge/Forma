import type { PageDocument as ApiPageDocument } from '@/api/generated'
import type { PageDocument } from './types'

/*
 * The studio's document types (./types.ts) are hand-written for the editor (generic blocks, literal unions), while the
 * API's are generated from its C# model. This fails `npm run type-check` when the studio could write a document the
 * API's schema doesn't describe, e.g. a new prop or block type that the API doesn't know yet.
 */
type Assert<T extends true> = T
export type StudioDocumentFitsApi = Assert<PageDocument extends ApiPageDocument ? true : false>

/** A studio document as the API client's request type. */
export const toApiDocument = (doc: PageDocument): ApiPageDocument => doc

/** A document the API sent. The server only stores documents that passed its validation, so they fit the studio's types. */
export const fromApiDocument = (doc: ApiPageDocument): PageDocument => doc as unknown as PageDocument
