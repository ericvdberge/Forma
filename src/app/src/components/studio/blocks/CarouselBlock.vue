<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'
import type { RenderContext } from '@/studio/render'
import type { Block } from '@/studio/types'

const props = defineProps<{ block: Block<'carousel'>; ctx: RenderContext }>()
const index = ref(0)
const count = computed(() => props.block.props.slides.length)
watch(count, (n) => (index.value = Math.min(index.value, Math.max(0, n - 1))))

const go = (dir: number) => (index.value = (index.value + dir + count.value) % count.value)

let timer: ReturnType<typeof setInterval> | undefined
watch(
  () => props.block.props.autoplay && !props.ctx.editing && count.value > 1,
  (on) => {
    clearInterval(timer)
    if (on) timer = setInterval(() => go(1), 4000)
  },
  { immediate: true },
)
onBeforeUnmount(() => clearInterval(timer))
</script>

<template>
  <div class="relative overflow-hidden" :style="{ aspectRatio: block.props.aspect, borderRadius: `${block.props.radius}px` }" aria-roledescription="carousel">
    <div class="flex h-full transition-transform duration-500 ease-out" :style="{ transform: `translateX(-${index * 100}%)` }">
      <figure
        v-for="(s, i) in block.props.slides"
        :key="i"
        class="relative h-full w-full shrink-0"
        :aria-hidden="i !== index"
        aria-roledescription="slide"
      >
        <img :src="s.src" :alt="s.caption" class="size-full object-cover" draggable="false" />
        <figcaption
          v-if="s.caption"
          class="absolute bottom-4 left-4 rounded-full bg-charcoal/70 px-3 py-1.5 text-sm text-warm-white backdrop-blur"
        >
          {{ s.caption }}
        </figcaption>
      </figure>
    </div>
    <template v-if="count > 1">
      <button
        type="button"
        class="absolute left-3 top-1/2 grid size-10 -translate-y-1/2 place-items-center rounded-full bg-white/90 text-charcoal shadow-soft hover:bg-white"
        aria-label="Previous slide"
        @click="go(-1)"
      >
        <ChevronLeft class="size-5" aria-hidden="true" />
      </button>
      <button
        type="button"
        class="absolute right-3 top-1/2 grid size-10 -translate-y-1/2 place-items-center rounded-full bg-white/90 text-charcoal shadow-soft hover:bg-white"
        aria-label="Next slide"
        @click="go(1)"
      >
        <ChevronRight class="size-5" aria-hidden="true" />
      </button>
      <div class="absolute bottom-4 right-4 flex gap-1.5">
        <button
          v-for="(_, i) in block.props.slides"
          :key="i"
          type="button"
          :class="['h-1.5 rounded-full transition-all', i === index ? 'w-5 bg-white' : 'w-1.5 bg-white/60']"
          :aria-label="`Slide ${i + 1}`"
          @click="index = i"
        />
      </div>
    </template>
  </div>
</template>
