<script setup lang="ts">
/**
 * AppSidebar — Metronic Demo1 sidebar
 * Renders the permission-filtered dynamic menu tree.
 * Supports collapsible groups (parent items with children).
 * Active state synced with Vue Router current route.
 */
import { useRoute, RouterLink } from 'vue-router'
import type { MenuItem } from '@/stores/menu.store'

defineProps<{
  items: MenuItem[]
  open: boolean
}>()
defineEmits<{ toggle: [] }>()

const route = useRoute()

function isActive(item: MenuItem): boolean {
  return !!item.route && route.path.startsWith(item.route)
}
</script>

<template>
  <!-- Metronic sidebar classes: kt-sidebar, kt-sidebar--dark -->
  <aside
    class="kt-sidebar flex flex-col bg-[--kt-sidebar-bg,#1e1e2d] text-white transition-all duration-300"
    :class="open ? 'w-[260px]' : 'w-[70px]'"
  >
    <!-- Logo -->
    <div class="flex items-center h-[70px] px-5 border-b border-white/10">
      <span class="text-xl font-bold text-white" v-if="open">AyNesil</span>
      <span class="text-xl font-bold text-white" v-else>A</span>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 overflow-y-auto py-4">
      <template v-for="item in items" :key="item.id">
        <!-- Group (has children) -->
        <template v-if="item.children?.length">
          <div class="px-4 mb-1">
            <p v-if="open" class="text-xs uppercase text-white/40 font-semibold tracking-wider px-2 mb-2">
              {{ item.label }}
            </p>
            <template v-for="child in item.children" :key="child.id">
              <RouterLink
                v-if="child.route"
                :to="child.route"
                class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-white/70 hover:text-white hover:bg-white/10 transition-colors"
                :class="{ 'bg-white/15 text-white': isActive(child) }"
              >
                <i v-if="child.icon" :class="child.icon" class="text-base w-5 text-center" />
                <span v-if="open">{{ child.label }}</span>
              </RouterLink>
            </template>
          </div>
        </template>

        <!-- Leaf item -->
        <template v-else-if="item.route">
          <div class="px-4 mb-1">
            <RouterLink
              :to="item.route"
              class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-white/70 hover:text-white hover:bg-white/10 transition-colors"
              :class="{ 'bg-white/15 text-white': isActive(item) }"
            >
              <i v-if="item.icon" :class="item.icon" class="text-base w-5 text-center" />
              <span v-if="open">{{ item.label }}</span>
            </RouterLink>
          </div>
        </template>
      </template>
    </nav>
  </aside>
</template>
