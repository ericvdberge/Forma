<script setup lang="ts">
import { computed, useId } from 'vue'

/**
 * Abstract architecture illustration used in product mockups and thumbnails.
 * `dark`: cantilevered villa at dusk. `light`: sculpted white facade on a pale sky.
 */
const props = withDefaults(defineProps<{ id?: string; tone?: 'dark' | 'light' }>(), { tone: 'dark' })
const autoId = useId()
const uid = computed(() => props.id ?? `arch-${autoId}`)
</script>

<template>
  <svg
    v-if="tone === 'dark'"
    viewBox="0 0 480 320"
    preserveAspectRatio="xMidYMid slice"
    class="size-full"
    aria-hidden="true"
  >
    <defs>
      <linearGradient :id="`${uid}-sky`" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#4a4a52" />
        <stop offset="0.55" stop-color="#1d1d22" />
        <stop offset="1" stop-color="#0b0b0f" />
      </linearGradient>
      <linearGradient :id="`${uid}-slab`" x1="0" y1="0" x2="1" y2="0">
        <stop offset="0" stop-color="#e9e5de" />
        <stop offset="1" stop-color="#fbfaf7" />
      </linearGradient>
      <linearGradient :id="`${uid}-glass`" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#2a2c33" />
        <stop offset="1" stop-color="#121318" />
      </linearGradient>
      <radialGradient :id="`${uid}-glow`" cx="0.25" cy="0.15" r="0.6">
        <stop offset="0" stop-color="#ffffff" stop-opacity="0.18" />
        <stop offset="1" stop-color="#ffffff" stop-opacity="0" />
      </radialGradient>
    </defs>
    <rect width="480" height="320" :fill="`url(#${uid}-sky)`" />
    <rect width="480" height="320" :fill="`url(#${uid}-glow)`" />
    <!-- roof slab -->
    <polygon points="170,128 480,62 480,92 186,152" :fill="`url(#${uid}-slab)`" />
    <polygon points="186,152 480,92 480,104 194,166" fill="#b9b3a9" />
    <!-- upper glazing -->
    <polygon points="194,166 480,104 480,176 204,226" :fill="`url(#${uid}-glass)`" />
    <g stroke="#3d4049" stroke-width="2">
      <line x1="240" y1="156" x2="246" y2="219" />
      <line x1="290" y1="145" x2="294" y2="210" />
      <line x1="340" y1="134" x2="343" y2="201" />
      <line x1="390" y1="123" x2="392" y2="192" />
      <line x1="440" y1="112" x2="441" y2="183" />
    </g>
    <polygon points="250,170 285,163 288,205 252,212" fill="#6e7480" opacity="0.25" />
    <!-- mid slab -->
    <polygon points="204,226 480,176 480,196 210,248" :fill="`url(#${uid}-slab)`" />
    <!-- lower volume -->
    <polygon points="250,248 480,206 480,320 256,320" fill="#d9d4cb" />
    <polygon points="290,262 450,233 450,320 294,320" fill="#141519" />
    <g stroke="#2f323a" stroke-width="2">
      <line x1="340" y1="253" x2="342" y2="320" />
      <line x1="395" y1="243" x2="396" y2="320" />
    </g>
    <!-- ground line -->
    <polygon points="0,300 480,284 480,320 0,320" fill="#0b0b0f" opacity="0.65" />
  </svg>

  <svg v-else viewBox="0 0 480 320" preserveAspectRatio="xMidYMid slice" class="size-full" aria-hidden="true">
    <defs>
      <linearGradient :id="`${uid}-sky`" x1="0" y1="0" x2="0.4" y2="1">
        <stop offset="0" stop-color="#f3f2ef" />
        <stop offset="1" stop-color="#dcdad5" />
      </linearGradient>
      <linearGradient :id="`${uid}-curve`" x1="0" y1="0" x2="1" y2="0.3">
        <stop offset="0" stop-color="#d6d3cd" />
        <stop offset="0.45" stop-color="#fbfaf8" />
        <stop offset="1" stop-color="#e4e1db" />
      </linearGradient>
      <linearGradient :id="`${uid}-face`" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#f7f6f3" />
        <stop offset="1" stop-color="#cfccc5" />
      </linearGradient>
      <linearGradient :id="`${uid}-glass`" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#3a3c42" />
        <stop offset="1" stop-color="#16171b" />
      </linearGradient>
    </defs>
    <rect width="480" height="320" :fill="`url(#${uid}-sky)`" />
    <!-- tall sculpted arc -->
    <path d="M300,-10 C350,40 380,120 372,320 L480,320 L480,-10 Z" :fill="`url(#${uid}-face)`" />
    <path d="M300,-10 C350,40 380,120 372,320" fill="none" stroke="#ffffff" stroke-width="3" opacity="0.8" />
    <!-- sweeping balcony band -->
    <path
      d="M120,170 C180,120 300,100 480,110 L480,150 C320,142 214,160 150,206 Z"
      :fill="`url(#${uid}-curve)`"
    />
    <path d="M150,206 C214,160 320,142 480,150 L480,160 C322,154 220,170 156,214 Z" fill="#bdb9b1" />
    <!-- glazing under the band -->
    <path d="M156,214 C220,170 322,154 480,160 L480,236 C330,232 236,246 172,292 Z" :fill="`url(#${uid}-glass)`" />
    <g stroke="#55585f" stroke-width="2" opacity="0.8">
      <line x1="214" y1="187" x2="224" y2="262" />
      <line x1="270" y1="171" x2="276" y2="248" />
      <line x1="330" y1="162" x2="333" y2="240" />
      <line x1="390" y1="158" x2="392" y2="236" />
      <line x1="446" y1="159" x2="447" y2="236" />
    </g>
    <path d="M232,190 C250,182 264,178 280,176 L282,214 C266,216 252,220 236,228 Z" fill="#9aa0aa" opacity="0.18" />
    <!-- lower slab -->
    <path d="M172,292 C236,246 330,232 480,236 L480,262 C340,258 250,272 196,320 L168,320 Z" fill="#f4f3f0" />
    <rect x="0" y="312" width="480" height="8" fill="#c9c6bf" opacity="0.6" />
  </svg>
</template>
