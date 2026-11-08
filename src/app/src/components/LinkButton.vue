<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'
import { FButton } from '@forma/design-system'

/** FButton that navigates with the router instead of a full page load. */
defineOptions({ inheritAttrs: false })
defineProps<{ to: RouteLocationRaw }>()
</script>

<template>
  <RouterLink v-slot="{ href, navigate }" :to="to" custom>
    <FButton v-bind="$attrs" :href="href" @click="navigate"><slot /></FButton>
  </RouterLink>
</template>
