<script setup lang="ts">
/**
 * DefaultLayout — Metronic Demo1 Shell
 * Metronic'in orijinal Demo1 layout class'larını kullanır:
 *   demo1  kt-header-fixed  kt-sidebar-fixed
 *   kt-header  kt-sidebar  kt-wrapper  kt-content
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
const sidebarCollapsed = ref(false)

onMounted(async () => {
  await Promise.all([menu.load(), settings.load()])
})
</script>

<template>
  <!--
    Metronic Demo1 body classes:
    - demo1           : Demo1 teması etkinleştir
    - kt-header-fixed : Header sabit kalır
    - kt-sidebar-fixed: Sidebar sabit kalır
  -->
  <div
    class="demo1 kt-header-fixed kt-sidebar-fixed"
    :class="{ 'kt-sidebar-collapse': sidebarCollapsed }"
  >
    <!-- ── Sidebar ──────────────────────────────────────────────────────── -->
    <AppSidebar
      :items="menu.tree"
      :collapsed="sidebarCollapsed"
      @toggle="sidebarCollapsed = !sidebarCollapsed"
    />

    <!-- ── Main wrapper ─────────────────────────────────────────────────── -->
    <div class="kt-wrapper flex flex-col min-h-screen">

      <!-- Header -->
      <AppHeader
        :user="auth.user"
        @toggle-sidebar="sidebarCollapsed = !sidebarCollapsed"
        @logout="auth.logout()"
      />

      <!-- Page content -->
      <main class="kt-content grow p-6">
        <RouterView />
      </main>

      <!-- Footer -->
      <footer class="px-6 py-3 text-xs text-muted-foreground border-t border-border">
        © {{ new Date().getFullYear() }} AyNesil — Tüm hakları saklıdır.
      </footer>

    </div>
  </div>
</template>
