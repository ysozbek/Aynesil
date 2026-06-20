<script setup lang="ts">
/**
 * DefaultLayout — Metronic Demo1 shell
 * Implements the Demo1 layout pattern from Metronic 9 Tailwind:
 *  - Sidebar navigation (collapsible, desktop + mobile)
 *  - Top header bar (breadcrumb, user menu, notifications)
 *  - Main content area (router-view)
 *  - Footer
 *
 * Layout uses Metronic's CSS classes from @keenthemes/ktui.
 * The sidebar menu is driven by the menuStore (dynamic, permission-filtered).
 */
import { onMounted, ref } from 'vue'
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useMenuStore } from '@/stores/menu.store'
import { useSettingsStore } from '@/stores/settings.store'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppHeader from '@/components/layout/AppHeader.vue'

const auth = useAuthStore()
const menu = useMenuStore()
const settings = useSettingsStore()
const sidebarOpen = ref(true)

onMounted(async () => {
  await Promise.all([menu.load(), settings.load()])
})
</script>

<template>
  <div class="flex h-screen overflow-hidden bg-[--kt-body-bg]" :class="{ 'sidebar-collapse': !sidebarOpen }">
    <!-- Sidebar -->
    <AppSidebar :items="menu.tree" :open="sidebarOpen" @toggle="sidebarOpen = !sidebarOpen" />

    <!-- Main column -->
    <div class="flex flex-col flex-1 min-w-0 overflow-hidden">
      <!-- Header -->
      <AppHeader
        :user="auth.user"
        @toggle-sidebar="sidebarOpen = !sidebarOpen"
        @logout="auth.logout()"
      />

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto p-6">
        <RouterView />
      </main>

      <!-- Footer -->
      <footer class="px-6 py-3 text-xs text-gray-400 border-t border-[--kt-border-color]">
        © {{ new Date().getFullYear() }} AyNesil — Tüm hakları saklıdır.
      </footer>
    </div>
  </div>
</template>
