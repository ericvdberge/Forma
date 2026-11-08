<script setup lang="ts">
import { cn } from '../utils/cn'

/** Publication state: a coloured dot with a label. */
withDefaults(defineProps<{ status: 'live' | 'draft' | 'scheduled' | 'archived'; label?: string }>(), {})

const dots = { live: 'bg-positive', draft: 'bg-charcoal/25', scheduled: 'bg-accent', archived: 'bg-stone' }
const texts = { live: 'text-positive', draft: 'text-muted', scheduled: 'text-accent-600', archived: 'text-subtle' }
const labels = { live: 'Live', draft: 'Draft', scheduled: 'Scheduled', archived: 'Archived' }
</script>

<template>
  <span :class="cn('inline-flex items-center gap-1.5 text-[0.8125rem] font-medium', texts[status])">
    <span :class="cn('size-2 rounded-full', dots[status])" aria-hidden="true" />
    {{ label ?? labels[status] }}
  </span>
</template>
