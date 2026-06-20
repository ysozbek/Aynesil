<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Roller ve İzinler</h1>
        <p class="text-gray-500 mt-1">Dinamik roller ve yetki atamalarını yönetin.</p>
      </div>
      <button class="px-4 py-2 bg-[--kt-primary,#4f46e5] text-white rounded-lg text-sm font-medium hover:opacity-90 transition-opacity">
        + Rol Ekle
      </button>
    </div>

    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="p-12 text-center text-gray-400">
        <svg class="w-12 h-12 mx-auto mb-3 text-gray-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        <p class="text-sm">Rol ve yetki yönetimi modülü geliştiriliyor.</p>
        <p class="text-xs text-gray-300 mt-1">RBAC: Roller → İzinler → Kullanıcılar</p>
      </div>
    </div>
  </div>
</template>
