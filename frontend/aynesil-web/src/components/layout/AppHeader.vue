<script setup lang="ts">
import type { AuthUser } from '@/types/auth.types'

defineProps<{
  user?: AuthUser | null
}>()

defineEmits<{
  'toggle-sidebar': []
  'logout': []
}>()
</script>

<template>
  <header class="h-[70px] flex items-center justify-between px-6 bg-white border-b border-[--kt-border-color,#e5e7eb] shadow-sm">
    <!-- Left: Toggle + Breadcrumb -->
    <div class="flex items-center gap-4">
      <button
        @click="$emit('toggle-sidebar')"
        class="p-2 rounded-lg hover:bg-gray-100 transition-colors text-gray-500"
        title="Menüyü aç/kapat"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
      <slot name="breadcrumb" />
    </div>

    <!-- Right: Notifications + User menu -->
    <div class="flex items-center gap-3">
      <!-- Notification bell -->
      <button class="relative p-2 rounded-lg hover:bg-gray-100 transition-colors text-gray-500">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
      </button>

      <!-- User menu -->
      <div v-if="user" class="flex items-center gap-3 pl-3 border-l border-gray-200">
        <div class="text-right hidden sm:block">
          <p class="text-sm font-medium text-gray-700">{{ user.fullName }}</p>
          <p class="text-xs text-gray-400">{{ user.email }}</p>
        </div>
        <div class="w-9 h-9 rounded-full bg-[--kt-primary,#4f46e5] flex items-center justify-center text-white font-semibold text-sm">
          {{ user.fullName.charAt(0).toUpperCase() }}
        </div>
        <button
          @click="$emit('logout')"
          class="p-2 rounded-lg hover:bg-gray-100 transition-colors text-gray-400 hover:text-gray-600"
          title="Çıkış Yap"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
          </svg>
        </button>
      </div>
    </div>
  </header>
</template>
