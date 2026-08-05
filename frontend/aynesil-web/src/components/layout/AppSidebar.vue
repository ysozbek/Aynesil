<script setup lang="ts">
/**
 * AppSidebar — Metronic Demo1 sidebar
 * Metronic CSS class'larını kullanır: kt-sidebar, kt-sidebar-header,
 * kt-sidebar-wrapper, kt-menu, kt-menu-item, kt-menu-link, kt-menu-title
 */
import { nextTick, ref, watch } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import type { MenuItem } from '@/stores/menu.store'

const props = defineProps<{
  items: MenuItem[]
  collapsed: boolean
}>()
defineEmits<{ toggle: [] }>()

const route = useRoute()
const scrollContainer = ref<HTMLElement | null>(null)

function normalizePath(path: string): string {
  return path.length > 1 && path.endsWith('/') ? path.slice(0, -1) : path
}

function isActive(item: MenuItem): boolean {
  if (!item.route) return false
  const menuPath = normalizePath(item.route)
  // Root must be exact — otherwise every path matches startsWith('/').
  if (menuPath === '/') return route.path === '/'
  return route.path === menuPath || route.path.startsWith(`${menuPath}/`)
}

/** Prefer the longest matching route so nested pages highlight the leaf item. */
function isPrimaryActive(item: MenuItem): boolean {
  if (!isActive(item) || !item.route) return false
  const menuPath = normalizePath(item.route)
  if (menuPath === '/') return true

  const allRoutes: string[] = []
  for (const group of props.items) {
    if (group.route) allRoutes.push(normalizePath(group.route))
    for (const child of group.children ?? []) {
      if (child.route) allRoutes.push(normalizePath(child.route))
    }
  }

  const longerMatch = allRoutes.some(
    (r) =>
      r !== menuPath &&
      r.length > menuPath.length &&
      (route.path === r || route.path.startsWith(`${r}/`)),
  )
  return !longerMatch
}

async function scrollActiveIntoView() {
  await nextTick()
  const container = scrollContainer.value
  const active = container?.querySelector<HTMLElement>('[data-menu-active="true"]')
  if (!container || !active) return

  // Scroll only the sidebar panel — avoid shifting the whole page.
  const containerRect = container.getBoundingClientRect()
  const activeRect = active.getBoundingClientRect()
  const delta =
    activeRect.top - containerRect.top
    - (containerRect.height / 2)
    + (activeRect.height / 2)
  container.scrollTo({ top: container.scrollTop + delta, behavior: 'smooth' })
}

watch(
  () => [route.path, props.items] as const,
  () => { void scrollActiveIntoView() },
  { immediate: true, deep: true },
)
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
    <div ref="scrollContainer" class="kt-sidebar-content kt-sidebar-wrapper flex-1 overflow-y-auto py-4">
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
                    :data-menu-active="isPrimaryActive(child) ? 'true' : undefined"
                    :class="isPrimaryActive(child)
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

          <!-- Root leaf (e.g. Ana Sayfa) — spaced so it never looks like the previous group's child -->
          <div v-else-if="item.route" class="kt-menu-item mt-2 mb-1">
            <RouterLink
              :to="item.route"
              class="kt-menu-link flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors"
              :data-menu-active="isPrimaryActive(item) ? 'true' : undefined"
              :class="isPrimaryActive(item)
                ? 'bg-primary text-primary-foreground'
                : 'text-foreground hover:bg-accent'"
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
