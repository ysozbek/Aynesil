<script setup lang="ts">
/**
 * Parola sıfırlama — e-postadaki bağlantıdaki token (?token=…) ile yeni parola (POST /api/auth/confirm-password-reset).
 * Token geçersiz/süresi dolmuşsa backend 422 `Auth.InvalidResetToken` döner; mesaj ekranda gösterilir.
 * Token bağlantıda yoksa elle girilebilir.
 */
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AuthShell from '@/components/auth/AuthShell.vue'
import { authService } from '@/services/auth.service'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const form = reactive({
  token: typeof route.query.token === 'string' ? route.query.token : '',
  newPassword: '',
  confirmPassword: '',
})
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  if (!form.token) { error.value = t('auth.reset.tokenRequired'); return }
  if (form.newPassword.length < 8) { error.value = t('account.password.tooShort'); return }
  if (form.newPassword !== form.confirmPassword) { error.value = t('account.password.mismatch'); return }
  loading.value = true
  try {
    await authService.confirmPasswordReset(form.token, form.newPassword)
    router.push({ name: 'login', query: { passwordChanged: '1' } })
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : t('auth.reset.error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthShell :title="t('auth.reset.title')" :description="t('auth.reset.description')">
    <div v-if="error" class="mb-5 p-3.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm">
      {{ error }}
    </div>

    <form class="space-y-4" @submit.prevent="submit">
      <div v-if="!route.query.token" class="flex flex-col gap-1.5">
        <label for="token" class="text-sm font-medium text-foreground">{{ t('auth.reset.token') }}</label>
        <input id="token" v-model="form.token" type="text" autocomplete="off"
          class="input flex h-10 w-full rounded-lg border border-input bg-card px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
      </div>
      <div class="flex flex-col gap-1.5">
        <label for="newPassword" class="text-sm font-medium text-foreground">{{ t('account.password.new') }}</label>
        <input id="newPassword" v-model="form.newPassword" type="password" autocomplete="new-password" required
          class="input flex h-10 w-full rounded-lg border border-input bg-card px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
      </div>
      <div class="flex flex-col gap-1.5">
        <label for="confirmPassword" class="text-sm font-medium text-foreground">{{ t('account.password.confirm') }}</label>
        <input id="confirmPassword" v-model="form.confirmPassword" type="password" autocomplete="new-password" required
          class="input flex h-10 w-full rounded-lg border border-input bg-card px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
      </div>
      <button type="submit" :disabled="loading"
        class="btn w-full h-10 rounded-lg bg-primary text-primary-foreground font-medium text-sm hover:bg-primary/90 disabled:opacity-60">
        {{ loading ? t('common.loading') : t('auth.reset.submit') }}
      </button>
    </form>

    <p class="text-center text-sm mt-5">
      <router-link :to="{ name: 'login' }" class="text-primary hover:underline">{{ t('auth.forgot.backToLogin') }}</router-link>
    </p>
  </AuthShell>
</template>
