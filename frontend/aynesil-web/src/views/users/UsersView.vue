<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Kullanıcılar</h1>
        <p class="text-gray-500 mt-1">Sistem kullanıcılarını yönetin.</p>
      </div>
      <button class="px-4 py-2 bg-[--kt-primary,#4f46e5] text-white rounded-lg text-sm font-medium hover:opacity-90 transition-opacity">
        + Kullanıcı Ekle
      </button>
    </div>

    <!-- Placeholder table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="px-6 py-4 border-b border-gray-100">
        <input
          type="search"
          placeholder="Kullanıcı ara..."
          class="w-full max-w-sm px-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-[--kt-primary,#4f46e5]"
        />
      </div>
      <div class="p-12 text-center text-gray-400">
        <svg class="w-12 h-12 mx-auto mb-3 text-gray-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        <p class="text-sm">Kullanıcı yönetimi modülü geliştiriliyor.</p>
      </div>
    </div>
  </div>
</template>
