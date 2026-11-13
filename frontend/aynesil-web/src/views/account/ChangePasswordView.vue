<script setup lang="ts">
/**
 * Signed-in user changes their own password (POST /api/auth/change-password).
 * Needed after an admin opens an account with a temporary password (educator / guardian records).
 * The backend revokes every session on success, so the user is sent back to the login page.
 */
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiService } from '@/services/api.service'
import { useAuthStore } from '@/stores/auth.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()

const form = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const error = ref('')
const saving = ref(false)

async function submit() {
  error.value = ''
  if (!form.currentPassword || !form.newPassword) { error.value = t('account.password.required'); return }
  if (form.newPassword.length < 8) { error.value = t('account.password.tooShort'); return }
  if (form.newPassword !== form.confirmPassword) { error.value = t('account.password.mismatch'); return }
  saving.value = true
  try {
    await apiService.post('/auth/change-password', {
      currentPassword: form.currentPassword,
      newPassword: form.newPassword,
    })
    await auth.logout()
    router.push({ name: 'login', query: { passwordChanged: '1' } })
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-md">
    <h1 class="text-2xl font-bold text-foreground mb-1">{{ t('account.password.title') }}</h1>
    <p class="text-sm text-muted-foreground mb-6">{{ t('account.password.description') }}</p>

    <form class="rounded-xl border border-border bg-card p-5 space-y-4 shadow-sm" @submit.prevent="submit">
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('account.password.current') }}</label>
        <input v-model="form.currentPassword" type="password" autocomplete="current-password"
          class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
      </div>
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('account.password.new') }}</label>
        <input v-model="form.newPassword" type="password" autocomplete="new-password"
          class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
      </div>
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('account.password.confirm') }}</label>
        <input v-model="form.confirmPassword" type="password" autocomplete="new-password"
          class="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm" />
      </div>
      <p v-if="error" class="text-sm text-red-600">{{ error }}</p>
      <div class="flex justify-end">
        <button type="submit" :disabled="saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-50">
          {{ t('account.password.submit') }}
        </button>
      </div>
    </form>
  </div>
</template>
