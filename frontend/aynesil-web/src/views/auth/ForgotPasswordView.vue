<script setup lang="ts">
/**
 * Şifremi unuttum — e-posta adresiyle sıfırlama isteği (POST /api/auth/request-password-reset).
 * Yanıt hesap var/yok ayrımı yapmaz; her durumda aynı bilgi mesajı gösterilir.
 * NOT: E-posta sağlayıcısı henüz bağlı değil; bağlantı e-postası şimdilik gönderilmez (INotificationService → LoggingNotificationService).
 */
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AuthShell from '@/components/auth/AuthShell.vue'
import { authService } from '@/services/auth.service'

const { t } = useI18n()
const email = ref('')
const loading = ref(false)
const sent = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await authService.requestPasswordReset(email.value.trim())
    sent.value = true
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : t('auth.forgot.error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthShell :title="t('auth.forgot.title')" :description="t('auth.forgot.description')">
    <div class="mb-5 p-3.5 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 text-sm">
      {{ t('auth.forgot.emailNotice') }}
    </div>

    <div v-if="sent" class="mb-5 p-3.5 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 text-sm">
      {{ t('auth.forgot.sent') }}
    </div>
    <div v-if="error" class="mb-5 p-3.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm">
      {{ error }}
    </div>

    <form v-if="!sent" class="space-y-4" @submit.prevent="submit">
      <div class="flex flex-col gap-1.5">
        <label for="email" class="text-sm font-medium text-foreground">{{ t('auth.forgot.email') }}</label>
        <input id="email" v-model="email" type="email" autocomplete="email" required
          class="input flex h-10 w-full rounded-lg border border-input bg-card px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
      </div>
      <button type="submit" :disabled="loading"
        class="btn w-full h-10 rounded-lg bg-primary text-primary-foreground font-medium text-sm hover:bg-primary/90 disabled:opacity-60">
        {{ loading ? t('common.loading') : t('auth.forgot.submit') }}
      </button>
    </form>

    <p class="text-center text-sm mt-5">
      <router-link :to="{ name: 'login' }" class="text-primary hover:underline">{{ t('auth.forgot.backToLogin') }}</router-link>
    </p>
  </AuthShell>
</template>
