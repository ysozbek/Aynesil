<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCorporationStore } from '@/stores/corporation.store'
import { usePermission } from '@/composables/usePermission'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const store = useCorporationStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)

const form = reactive({
  defaultLocale: 'tr',
  defaultCurrency: 'TRY',
  timezone: 'Europe/Istanbul',
  taxOffice: '',
  taxNumber: '',
  settings: '{}',
  rowVersion: 0,
})

const saved = ref(false)
const error = ref('')

onMounted(async () => {
  const settings = await store.fetchSettings(id.value)
  if (settings) {
    Object.assign(form, {
      defaultLocale: settings.defaultLocale,
      defaultCurrency: settings.defaultCurrency,
      timezone: settings.timezone,
      taxOffice: settings.taxOffice ?? '',
      taxNumber: settings.taxNumber ?? '',
      settings: settings.settings ?? '{}',
    })
  }
  if (!store.current) await store.fetchOne(id.value)
  form.rowVersion = (store.current?.rowVersion ?? 0)
})

async function save() {
  error.value = ''
  saved.value = false
  try {
    await store.updateSettings(id.value, {
      defaultLocale: form.defaultLocale,
      defaultCurrency: form.defaultCurrency,
      timezone: form.timezone,
      taxOffice: form.taxOffice || undefined,
      taxNumber: form.taxNumber || undefined,
      settings: form.settings,
      rowVersion: form.rowVersion,
    })
    saved.value = true
    setTimeout(() => (saved.value = false), 3000)
  } catch (e: unknown) {
    error.value = (e as Error).message
  }
}
</script>

<template>
  <div class="max-w-2xl">
    <!-- Header -->
    <div class="mb-6 flex items-center gap-3">
      <button
        @click="router.push({ name: 'corporation-detail', params: { id } })"
        class="flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <div>
        <h1 class="text-xl font-bold text-foreground">{{ t('corporation.settings') }}</h1>
        <p class="text-sm text-muted-foreground">{{ store.current?.displayName }}</p>
      </div>
    </div>

    <!-- Form card -->
    <div class="bg-[--color-card] rounded-xl border border-border shadow-sm p-6 space-y-5">
      <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ error }}</p>
      <div v-if="saved" class="text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">
        {{ t('common.savedSuccess') }}
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.locale') }}</label>
          <select v-model="form.defaultLocale" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="tr">Türkçe (tr)</option>
            <option value="en">English (en)</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.currency') }}</label>
          <select v-model="form.defaultCurrency" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
            <option value="TRY">TRY — Türk Lirası</option>
            <option value="USD">USD — US Dollar</option>
            <option value="EUR">EUR — Euro</option>
          </select>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.timezone') }}</label>
        <select v-model="form.timezone" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
          <option value="Europe/Istanbul">Europe/Istanbul (UTC+3)</option>
          <option value="UTC">UTC</option>
          <option value="Europe/London">Europe/London</option>
          <option value="America/New_York">America/New_York</option>
        </select>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.taxOffice') }}</label>
          <input v-model="form.taxOffice" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.taxNumber') }}</label>
          <input v-model="form.taxNumber" type="text" class="w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary" />
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('corporation.settingsJson') }}</label>
        <textarea
          v-model="form.settings"
          rows="6"
          class="w-full px-3 py-2 text-xs font-mono rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary resize-none"
          placeholder="{}"
        />
        <p class="mt-1 text-xs text-muted-foreground">{{ t('corporation.settingsJsonHint') }}</p>
      </div>

      <div class="flex justify-end">
        <button
          v-if="can('corporation:update')"
          @click="save"
          :disabled="store.saving"
          class="px-5 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {{ store.saving ? t('common.loading') : t('common.save') }}
        </button>
      </div>
    </div>
  </div>
</template>
