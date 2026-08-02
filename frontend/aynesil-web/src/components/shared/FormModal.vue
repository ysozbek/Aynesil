<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{
  open: boolean
  title: string
  subtitle?: string
  saving?: boolean
  wide?: boolean
}>(), {
  saving: false,
  wide: false,
})

const emit = defineEmits<{
  'submit': []
  'close': []
}>()

const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <!-- Backdrop -->
      <div class="absolute inset-0 bg-black/40" @click="emit('close')" />

      <!-- Modal panel -->
      <div
        :class="[
          'relative z-10 w-full rounded-xl bg-[--color-card] shadow-xl flex flex-col max-h-[90vh]',
          wide ? 'max-w-3xl' : 'max-w-lg',
        ]"
      >
        <!-- Header -->
        <div class="flex items-start justify-between px-6 py-4 border-b border-border shrink-0">
          <div>
            <h2 class="text-base font-semibold text-foreground">{{ title }}</h2>
            <p v-if="subtitle" class="text-xs text-muted-foreground mt-0.5">{{ subtitle }}</p>
          </div>
          <button
            @click="emit('close')"
            class="ml-4 flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- Body (scrollable) -->
        <div class="flex-1 overflow-y-auto px-6 py-4">
          <slot />
        </div>

        <!-- Footer -->
        <div class="flex justify-end gap-3 px-6 py-4 border-t border-border shrink-0">
          <slot name="footer">
            <button
              @click="emit('close')"
              :disabled="saving"
              class="px-4 py-2 rounded-lg text-sm font-medium border border-border hover:bg-accent transition-colors disabled:opacity-50"
            >
              {{ t('common.cancel') }}
            </button>
            <button
              @click="emit('submit')"
              :disabled="saving"
              class="px-4 py-2 rounded-lg text-sm font-medium bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              <span v-if="saving" class="flex items-center gap-2">
                <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                {{ t('common.loading') }}
              </span>
              <span v-else>{{ t('common.save') }}</span>
            </button>
          </slot>
        </div>
      </div>
    </div>
  </Teleport>
</template>
