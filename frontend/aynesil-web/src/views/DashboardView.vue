<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store'
import { useI18n } from 'vue-i18n'

const auth = useAuthStore()
const { t } = useI18n()
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">{{ t('navigation.dashboard') }}</h1>
      <p class="text-gray-500 mt-1">Hoşgeldiniz, {{ auth.user?.fullName }}</p>
    </div>

    <!-- KPI cards placeholder — populated by business modules -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
      <div v-for="i in 4" :key="i"
        class="bg-white rounded-xl p-5 shadow-sm border border-gray-100 animate-pulse">
        <div class="h-4 bg-gray-200 rounded w-24 mb-3"></div>
        <div class="h-8 bg-gray-200 rounded w-16"></div>
      </div>
    </div>

    <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100 text-center text-gray-400">
      <p class="text-sm">İş modülleri yüklendikçe bu alan içerikle dolacaktır.</p>
    </div>
  </div>
</template>
