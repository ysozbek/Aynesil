<template>
  <div class="p-6 space-y-6 max-w-3xl mx-auto">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ $t('notification.preferences.title') }}</h1>
      <p class="text-sm text-gray-500">{{ $t('notification.preferences.subtitle') }}</p>
    </div>

    <div v-if="prefStore.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <template v-else>
      <div v-if="!prefStore.preferences.length" class="card bg-base-100 shadow">
        <div class="card-body items-center text-center py-10">
          <p class="text-gray-500">{{ $t('notification.preferences.noPreferences') }}</p>
        </div>
      </div>

      <form v-else @submit.prevent="save" class="space-y-4">
        <!-- Group by category -->
        <div
          v-for="(group, category) in groupedPreferences"
          :key="category"
          class="card bg-base-100 shadow"
        >
          <div class="card-body">
            <h2 class="font-semibold text-base border-b pb-2 mb-3">
              {{ category || $t('notification.preferences.defaultCategory') }}
            </h2>
            <div class="space-y-3">
              <div
                v-for="pref in group"
                :key="pref.id"
                class="flex items-center justify-between py-2"
              >
                <div>
                  <p class="text-sm font-medium">{{ pref.channelCode ?? $t('notification.preferences.allChannels') }}</p>
                </div>
                <label class="cursor-pointer">
                  <input
                    v-model="localPrefs[pref.id]"
                    type="checkbox"
                    class="toggle toggle-primary toggle-sm"
                  />
                </label>
              </div>
            </div>
          </div>
        </div>

        <div v-if="saveError" class="alert alert-error">
          <span>{{ saveError }}</span>
        </div>

        <div class="flex justify-end">
          <button type="submit" class="btn btn-primary" :disabled="prefStore.saving">
            <span v-if="prefStore.saving" class="loading loading-spinner loading-sm"></span>
            {{ prefStore.saving ? $t('common.saving') : $t('common.save') }}
          </button>
        </div>
      </form>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useNotificationPreferenceStore } from '@/stores/notificationPreference.store'

const prefStore = useNotificationPreferenceStore()
const localPrefs = ref<Record<string, boolean>>({})
const saveError = ref<string | null>(null)

const groupedPreferences = computed(() => {
  const groups: Record<string, typeof prefStore.preferences> = {}
  for (const p of prefStore.preferences) {
    const key = p.categoryCode ?? ''
    if (!groups[key]) groups[key] = []
    groups[key].push(p)
  }
  return groups
})

async function save() {
  saveError.value = null
  try {
    await prefStore.savePreferences({
      preferences: prefStore.preferences.map(p => ({
        categoryId: p.categoryId,
        channelId: p.channelId,
        isEnabled: localPrefs.value[p.id] ?? p.isEnabled,
      })),
    })
  } catch (e: unknown) {
    saveError.value = (e as Error).message
  }
}

onMounted(async () => {
  await prefStore.fetchPreferences()
  for (const p of prefStore.preferences) {
    localPrefs.value[p.id] = p.isEnabled
  }
})
</script>
