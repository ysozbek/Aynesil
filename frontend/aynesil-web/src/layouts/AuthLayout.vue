<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <!-- Metronic auth page scaffold: centered card on gradient background -->
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-[--kt-primary-light] to-[--kt-body-bg]">
    <div class="w-full max-w-md">
      <!-- Brand -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-[--kt-primary]">AyNesil</h1>
        <p class="text-sm text-gray-500 mt-1">Özel Eğitim & Terapi Yönetim Platformu</p>
      </div>

      <!-- Auth card -->
      <div class="bg-white rounded-2xl shadow-xl p-8">
        <RouterView />
      </div>
    </div>
  </div>
</template>
