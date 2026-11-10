import type { InjectionKey, ShallowRef } from 'vue'
import type { Editor } from '@tiptap/vue-3'

/** The rich-text editor currently being typed in, shared with the studio's formatting toolbar. */
export const textEditorKey: InjectionKey<ShallowRef<Editor | null>> = Symbol('studio-text-editor')
