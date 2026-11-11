<script setup lang="ts">
/** A frosted strip of `FStatTile`s, split by hairlines; wraps to a grid on small screens. */
</script>

<template>
  <div
    class="grid grid-cols-2 gap-x-6 gap-y-5 rounded-panel bg-white/80 p-5 ring-1 ring-charcoal/[0.06] shadow-soft backdrop-blur lg:grid-cols-4 lg:gap-0 lg:px-2 [&>*]:lg:border-l [&>*]:lg:border-charcoal/[0.08] [&>*]:lg:px-6 [&>*:first-child]:lg:border-l-0"
  >
    <slot />
  </div>
</template>
