<script setup lang="ts">
/** Oturum açmamış kullanıcı ekranları (şifremi unuttum / sıfırlama) için ortak çerçeve: marka + kart + alt bilgi. */
defineProps<{ title: string; description?: string }>()
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-accent/30 px-4">
    <div class="w-full max-w-[420px]">
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary text-primary-foreground text-2xl font-bold mb-4 shadow-lg">
          A
        </div>
        <h1 class="text-2xl font-bold text-foreground">AyNesil</h1>
        <p class="text-sm text-muted-foreground mt-1">Özel Eğitim & Terapi Yönetim Platformu</p>
      </div>

      <div class="card bg-card rounded-2xl shadow-lg border border-border p-8">
        <div class="mb-6">
          <h2 class="text-xl font-semibold text-foreground">{{ title }}</h2>
          <p v-if="description" class="text-sm text-muted-foreground mt-1">{{ description }}</p>
        </div>
        <slot />
      </div>

      <p class="text-center text-xs text-muted-foreground mt-6">
        © {{ new Date().getFullYear() }} AyNesil. Tüm hakları saklıdır.
      </p>
    </div>
  </div>
</template>
