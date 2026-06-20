<script setup lang="ts">
import { useRouter } from 'vue-router'
const router = useRouter()
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-center">
      <p class="text-6xl font-bold text-gray-300">404</p>
      <h1 class="text-2xl font-semibold text-gray-800 mt-4">Sayfa Bulunamadı</h1>
      <p class="text-gray-500 mt-2">Aradığınız sayfa mevcut değil.</p>
      <button @click="router.push('/')" class="mt-6 px-5 py-2 rounded-lg bg-[--kt-primary,#4f46e5] text-white hover:opacity-90 transition-opacity">
        Ana Sayfa
      </button>
    </div>
  </div>
</template>
