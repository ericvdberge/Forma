<script setup lang="ts">
import { useRoute } from 'vue-router'
import { ArrowLeft, ArrowRight } from 'lucide-vue-next'
import { FLogo, cn } from '@forma/design-system'
import { navGroups } from './nav'
import SidebarCollections from './SidebarCollections.vue'

defineProps<{ collapsed: boolean }>()
defineEmits<{ toggle: []; navigate: [] }>()

const route = useRoute()
/** Nested screens (e.g. /pages/:id/edit) keep their section highlighted. */
const isCurrent = (to: string) => (to === '/' ? route.path === '/' : route.path === to || route.path.startsWith(`${to}/`))
</script>

<template>
  <aside
    :class="
      cn(
        'flex h-full flex-col border-r border-charcoal/[0.06] bg-[#f4f4f2] transition-[width] duration-300',
        collapsed ? 'w-[76px]' : 'w-[248px]',
      )
    "
  >
    <RouterLink to="/" :class="cn('flex h-20 shrink-0 items-center', collapsed ? 'justify-center' : 'px-7')" @click="$emit('navigate')">
      <FLogo :variant="collapsed ? 'mark' : 'wordmark'" />
    </RouterLink>

    <nav aria-label="Main" class="flex-1 overflow-y-auto px-3 pb-4">
      <section v-for="(group, g) in navGroups" :key="group.label" :class="g > 0 && (collapsed ? 'mt-3 border-t border-charcoal/[0.06] pt-3' : 'mt-5')">
        <h2 v-if="!collapsed" class="mb-1.5 px-4 text-[0.6875rem] font-medium uppercase tracking-[0.1em] text-subtle">{{ group.label }}</h2>
        <h2 v-else class="sr-only">{{ group.label }}</h2>
        <ul class="flex flex-col gap-1">
          <li v-for="item in group.items" :key="item.to">
            <RouterLink v-slot="{ href, navigate }" :to="item.to" custom>
              <a
                :href="href"
                :aria-current="isCurrent(item.to) ? 'page' : undefined"
                :title="collapsed ? item.label : undefined"
                :class="
                  cn(
                    'flex h-11 items-center gap-3.5 rounded-xl text-[0.9375rem] transition-colors',
                    collapsed ? 'justify-center' : 'px-4',
                    isCurrent(item.to)
                      ? 'bg-charcoal/[0.06] font-medium text-charcoal'
                      : 'text-charcoal/75 hover:bg-charcoal/[0.04] hover:text-charcoal',
                  )
                "
                @click="navigate($event); $emit('navigate')"
              >
                <component :is="item.icon" class="size-5 shrink-0" :stroke-width="1.6" aria-hidden="true" />
                <span v-if="!collapsed">{{ item.label }}</span>
              </a>
            </RouterLink>
            <SidebarCollections v-if="item.collections && !collapsed" @navigate="$emit('navigate')" />
          </li>
        </ul>
      </section>
    </nav>

    <button
      type="button"
      :class="
        cn(
          'flex h-14 shrink-0 items-center gap-3 text-[0.8125rem] text-muted transition-colors hover:text-charcoal',
          collapsed ? 'justify-center' : 'px-7',
        )
      "
      :aria-label="collapsed ? 'Expand sidebar' : 'Collapse sidebar'"
      @click="$emit('toggle')"
    >
      <component :is="collapsed ? ArrowRight : ArrowLeft" class="size-4" aria-hidden="true" />
      <span v-if="!collapsed">Collapse sidebar</span>
    </button>
  </aside>
</template>
