<script setup lang="ts">
import { FButton, FModal } from '@forma/design-system'
import { DOCS_URL, EXAMPLE_URL } from './links'

/**
 * How to install a plugin. The API loads plugins from its plugins folder at startup; installing from the app
 * (upload, enable per website) is phase 2 of docs/design/plugins.md.
 */
const open = defineModel<boolean>('open', { required: true })
const steps = [
  { title: 'Build the module', body: 'Compile your plugin to WebAssembly for wasm32-unknown-unknown (no WASI).' },
  {
    title: 'Add it to the plugins folder',
    body: 'Put plugin.json and plugin.wasm in plugins/{id}/ — the folder name must match the id in plugin.json.',
  },
  { title: 'Restart the API', body: 'Plugins load at startup. With Docker: docker compose up -d --force-recreate.' },
]
</script>

<template>
  <FModal v-model:open="open" size="md" labelledby="install-plugin-title">
    <div class="flex flex-col gap-5 p-6 sm:p-7">
      <div>
        <h2 id="install-plugin-title" class="font-display text-xl font-bold tracking-[-0.02em]">Install a plugin</h2>
        <p class="mt-2 text-sm text-muted">
          Plugins are WebAssembly modules that run in a sandbox when pages are saved, checked or published. Uploading
          from the app is coming next; for now, add them to the server’s plugins folder.
        </p>
      </div>
      <ol class="flex flex-col gap-3">
        <li v-for="(s, i) in steps" :key="s.title" class="flex gap-3">
          <span class="grid size-7 shrink-0 place-items-center rounded-full bg-charcoal text-xs font-semibold text-warm-white">
            {{ i + 1 }}
          </span>
          <span>
            <span class="block text-sm font-medium">{{ s.title }}</span>
            <span class="block text-sm text-muted">{{ s.body }}</span>
          </span>
        </li>
      </ol>
      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <FButton variant="secondary" size="sm" :href="EXAMPLE_URL" target="_blank">Example plugin</FButton>
        <FButton size="sm" :href="DOCS_URL" target="_blank" data-autofocus>Read the docs</FButton>
      </div>
    </div>
  </FModal>
</template>
