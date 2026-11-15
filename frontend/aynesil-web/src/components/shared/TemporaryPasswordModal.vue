<script setup lang="ts">
/**
 * Shows a server-generated temporary password exactly once (new linked account, admin password reset).
 * The password lives only in the parent's memory; the parent clears it on `close`.
 */
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import FormModal from '@/components/shared/FormModal.vue'

const props = defineProps<{
  /** The password; the dialog is open while it is non-empty. */
  password: string
  /** Sentence above the password, e.g. "x hesabı açıldı. Geçici şifre:". */
  intro: string
}>()

const emit = defineEmits<{ close: [] }>()
const { t } = useI18n()
const copied = ref(false)

watch(() => props.password, () => { copied.value = false })

async function copy() {
  try {
    await navigator.clipboard.writeText(props.password)
    copied.value = true
  } catch {
    copied.value = false
  }
}
</script>

<template>
  <FormModal :open="!!password" :title="t('userLink.passwordTitle')" @close="emit('close')" @submit="emit('close')">
    <div class="space-y-3 text-sm">
      <p class="text-foreground">{{ intro }}</p>
      <div class="flex items-center gap-2">
        <code class="flex-1 rounded-lg border border-border bg-accent px-3 py-2 font-mono text-base tracking-wider select-all">{{ password }}</code>
        <button type="button" class="px-3 py-2 text-xs rounded-lg border border-border hover:bg-accent" @click="copy">
          {{ copied ? t('userLink.copied') : t('userLink.copy') }}
        </button>
      </div>
      <p class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-amber-800">{{ t('userLink.passwordWarning') }}</p>
    </div>
  </FormModal>
</template>
