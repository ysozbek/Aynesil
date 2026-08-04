<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{
  open: boolean
  title?: string
  message?: string
  confirmLabel?: string
  confirmClass?: string
  loading?: boolean
}>(), {
  title: 'Onayla',
  message: 'Bu işlemi gerçekleştirmek istediğinizden emin misiniz?',
  confirmLabel: 'Onayla',
  confirmClass: 'bg-red-600 hover:bg-red-700 text-white',
  loading: false,
})

const emit = defineEmits<{
  'confirm': []
  'cancel': []
}>()

const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-[9999] flex items-center justify-center"
    >
      <!-- Backdrop -->
      <div class="absolute inset-0 bg-black/50 backdrop-blur-[2px]" @click="emit('cancel')" />

      <!-- Modal -->
      <div class="relative z-10 w-full max-w-md rounded-2xl bg-white dark:bg-[--color-card] p-6 shadow-2xl border border-gray-100">
        <h2 class="text-base font-semibold text-foreground mb-2">{{ title }}</h2>
        <p class="text-sm text-muted-foreground mb-6">{{ message }}</p>

        <div class="flex justify-end gap-3">
          <button
            @click="emit('cancel')"
            :disabled="loading"
            class="px-4 py-2 rounded-lg text-sm font-medium border border-border hover:bg-accent transition-colors disabled:opacity-50"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            @click="emit('confirm')"
            :disabled="loading"
            :class="['px-4 py-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-50', confirmClass]"
          >
            <span v-if="loading" class="flex items-center gap-2">
              <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              {{ t('common.loading') }}
            </span>
            <span v-else>{{ confirmLabel }}</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
