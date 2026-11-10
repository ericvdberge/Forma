<script setup lang="ts">
import { computed, ref } from 'vue'
import { ArrowRight, ChevronDown, Menu, X } from 'lucide-vue-next'
import { cn } from '@forma/design-system'
import { tone, type RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'nav'>; ctx: RenderContext }>()
const open = ref(false)
const vertical = computed(() => props.block.props.orientation === 'vertical')
/** Horizontal menus collapse behind a menu button on phones. */
const collapsed = computed(() => !vertical.value && props.ctx.bp === 'mobile')
const t = computed(() => tone(props.ctx.theme))
const go = (e: MouseEvent) => props.ctx.editing && e.preventDefault()
</script>

<template>
  <nav
    :class="
      cn(
        'relative text-left',
        vertical ? 'flex flex-col gap-1' : 'flex items-center gap-6',
        block.props.card && 'rounded-2xl px-5 py-3 shadow-soft backdrop-blur',
      )
    "
    :style="block.props.card ? { background: ctx.theme === 'dark' ? 'rgb(255 255 255 / 0.08)' : 'rgb(255 255 255 / 0.82)' } : undefined"
  >
    <span v-if="block.props.logo" :class="cn('font-display text-[1.375rem] font-bold tracking-[-0.04em]', vertical && 'mb-3')">
      {{ block.props.logo }}
    </span>

    <template v-if="!collapsed">
      <ul :class="cn(vertical ? 'flex flex-col' : 'mx-auto flex items-center gap-7')">
        <li v-for="(l, i) in block.props.links" :key="i">
          <a
            :href="l.href"
            :class="cn('inline-flex items-center gap-1 text-sm hover:opacity-100', vertical ? 'rounded-lg py-2 opacity-80' : 'opacity-85')"
            @click="go"
          >
            {{ l.label }}
            <ChevronDown v-if="l.menu" class="size-3.5" aria-hidden="true" />
          </a>
        </li>
      </ul>
      <a
        v-if="block.props.ctaLabel"
        :href="block.props.ctaHref"
        :class="
          cn(
            'inline-flex h-10 items-center gap-2 rounded-full px-4 text-sm font-medium',
            vertical && 'mt-3 self-start',
            ctx.theme === 'dark' ? 'bg-warm-white text-charcoal' : 'bg-charcoal text-warm-white',
          )
        "
        @click="go"
      >
        {{ block.props.ctaLabel }} <ArrowRight class="size-4" aria-hidden="true" />
      </a>
    </template>
    <template v-else>
      <button type="button" class="ml-auto grid size-10 place-items-center rounded-full" :aria-expanded="open" aria-label="Menu" @click="open = !open">
        <component :is="open ? X : Menu" class="size-5" aria-hidden="true" />
      </button>
      <ul
        v-if="open"
        class="absolute inset-x-0 top-full z-10 mt-2 flex flex-col rounded-2xl p-2 shadow-float"
        :style="{ background: ctx.theme === 'dark' ? '#171717' : '#ffffff', color: t.text }"
      >
        <li v-for="(l, i) in block.props.links" :key="i">
          <a :href="l.href" class="block rounded-xl px-3 py-2.5 text-sm hover:bg-charcoal/5" @click="go">{{ l.label }}</a>
        </li>
        <li v-if="block.props.ctaLabel">
          <a :href="block.props.ctaHref" class="block rounded-xl px-3 py-2.5 text-sm font-medium" @click="go">{{ block.props.ctaLabel }}</a>
        </li>
      </ul>
    </template>
  </nav>
</template>
