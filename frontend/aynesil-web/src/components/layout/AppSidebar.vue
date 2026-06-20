<script setup lang="ts">
/**
 * AppSidebar — Metronic Demo1 sidebar
 * Metronic CSS class'larını kullanır: kt-sidebar, kt-sidebar-header,
 * kt-sidebar-wrapper, kt-menu, kt-menu-item, kt-menu-link, kt-menu-title
 */
import { useRoute, RouterLink } from 'vue-router'
import type { MenuItem } from '@/stores/menu.store'

defineProps<{
  items: MenuItem[]
  collapsed: boolean
}>()
defineEmits<{ toggle: [] }>()

const route = useRoute()

function isActive(item: MenuItem): boolean {
  return !!item.route && route.path.startsWith(item.route)
}
</script>

<template>
  <!-- kt-sidebar: Metronic sidebar container -->
  <div class="kt-sidebar fixed inset-y-0 start-0 z-20 flex flex-col bg-[--color-card] border-e border-border shadow-sm">

    <!-- Sidebar header (logo alanı) -->
    <div class="kt-sidebar-header flex items-center justify-between px-5 shrink-0 border-b border-border">
      <RouterLink to="/" class="flex items-center gap-2">
        <!-- Full logo -->
        <span class="default-logo font-bold text-xl text-primary">AyNesil</span>
        <!-- Collapsed logo -->
        <span class="small-logo font-bold text-xl text-primary">A</span>
      </RouterLink>

      <!-- Toggle button (desktop) -->
      <button
        @click="$emit('toggle')"
        class="hidden lg:flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
    </div>

    <!-- Sidebar content / navigation -->
    <div class="kt-sidebar-content kt-sidebar-wrapper flex-1 overflow-y-auto py-4">
      <nav class="kt-menu flex flex-col gap-0.5 px-3">

        <template v-for="item in items" :key="item.id">

          <!-- Group heading + children -->
          <template v-if="item.children?.length">
            <div class="kt-menu-item">
              <span class="kt-menu-heading px-2 py-1.5 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                <span class="kt-menu-title">{{ item.label }}</span>
              </span>

              <div class="kt-menu-accordion flex flex-col gap-0.5">
                <div
                  v-for="child in item.children"
                  :key="child.id"
                  class="kt-menu-item"
                >
                  <RouterLink
                    v-if="child.route"
                    :to="child.route"
                    class="kt-menu-link flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors"
                    :class="isActive(child)
                      ? 'bg-primary text-primary-foreground font-medium'
                      : 'text-muted-foreground hover:text-foreground hover:bg-accent'"
                  >
                    <i v-if="child.icon" :class="child.icon" class="ki-outline text-base w-5 text-center shrink-0" />
                    <span class="kt-menu-title">{{ child.label }}</span>
                  </RouterLink>
                </div>
              </div>
            </div>
          </template>

          <!-- Leaf item -->
          <div v-else-if="item.route" class="kt-menu-item">
            <RouterLink
              :to="item.route"
              class="kt-menu-link flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors"
              :class="isActive(item)
                ? 'bg-primary text-primary-foreground font-medium'
                : 'text-muted-foreground hover:text-foreground hover:bg-accent'"
            >
              <i v-if="item.icon" :class="item.icon" class="ki-outline text-base w-5 text-center shrink-0" />
              <span class="kt-menu-title">{{ item.label }}</span>
            </RouterLink>
          </div>

        </template>

      </nav>
    </div>
  </div>
</template>
