<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900">{{ t('navigation.settings') }}</h1>
      <p class="text-gray-500 mt-1">Sistem ve tenant ayarlarını yönetin.</p>
    </div>

    <div class="grid gap-5">
      <!-- Corporation Settings -->
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <h2 class="text-base font-semibold text-gray-800 mb-4">Kurum Ayarları</h2>
        <p class="text-sm text-gray-400">Kurum dil, zaman dilimi ve para birimi ayarları burada yönetilecek.</p>
      </div>

      <!-- Notification Settings -->
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <h2 class="text-base font-semibold text-gray-800 mb-4">Bildirim Ayarları</h2>
        <p class="text-sm text-gray-400">Bildirim kanalları ve tercihleri burada yönetilecek.</p>
      </div>

      <!-- Integration Settings -->
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <h2 class="text-base font-semibold text-gray-800 mb-4">Entegrasyonlar</h2>
        <p class="text-sm text-gray-400">E-posta, SMS ve ödeme sağlayıcı ayarları burada yönetilecek.</p>
      </div>
    </div>
  </div>
</template>
