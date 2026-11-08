<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import LandingPage from './views/LandingPage.vue'
import DesignSystemPage from './views/DesignSystemPage.vue'

// Minimal hash routing: `#/design-system` shows the component showcase, anything else is the landing page.
const hash = ref(window.location.hash)
const onHash = () => {
  const wasDs = hash.value.startsWith('#/design-system')
  hash.value = window.location.hash
  if (wasDs !== hash.value.startsWith('#/design-system')) window.scrollTo({ top: 0 })
}
onMounted(() => window.addEventListener('hashchange', onHash))
onBeforeUnmount(() => window.removeEventListener('hashchange', onHash))

const view = computed(() => (hash.value.startsWith('#/design-system') ? DesignSystemPage : LandingPage))
</script>

<template>
  <component :is="view" />
</template>
