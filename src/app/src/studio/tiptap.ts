import { Mark, mergeAttributes } from '@tiptap/core'
import Document from '@tiptap/extension-document'
import StarterKit from '@tiptap/starter-kit'

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    muted: { toggleMuted: () => ReturnType }
  }
}

/** Softer text colour inside a heading or paragraph, e.g. the grey second line of "Build beautiful experiences." */
export const Muted = Mark.create({
  name: 'muted',
  parseHTML: () => [{ tag: 'span[data-muted]' }],
  renderHTML: ({ HTMLAttributes }) => ['span', mergeAttributes(HTMLAttributes, { 'data-muted': '' }), 0],
  addCommands() {
    return { toggleMuted: () => ({ commands }) => commands.toggleMark(this.name) }
  },
})

/** Headings hold one paragraph: Enter does nothing, Shift+Enter breaks the line. */
const SingleParagraph = Document.extend({ content: 'paragraph' })

export function richTextExtensions(variant: 'heading' | 'text') {
  return [
    ...(variant === 'heading' ? [SingleParagraph] : []),
    StarterKit.configure({
      document: variant === 'heading' ? false : undefined,
      heading: false,
      codeBlock: false,
      code: false,
      blockquote: variant === 'heading' ? false : undefined,
      bulletList: variant === 'heading' ? false : undefined,
      orderedList: variant === 'heading' ? false : undefined,
      horizontalRule: false,
      // The studio's own undo history covers text edits too.
      undoRedo: false,
      trailingNode: false,
      link: { openOnClick: false, autolink: true, HTMLAttributes: { rel: 'noopener', target: null } },
    }),
    Muted,
  ]
}
