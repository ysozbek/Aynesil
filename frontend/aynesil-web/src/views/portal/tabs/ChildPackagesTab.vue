<template>
  <div class="space-y-4">
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <div v-else-if="!store.packages.length" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-10">
        <div class="text-4xl mb-2">📦</div>
        <p class="text-gray-500">{{ $t('portal.packages.noPackages') }}</p>
      </div>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="pkg in store.packages"
        :key="pkg.id"
        class="card bg-base-100 shadow"
      >
        <div class="card-body p-5">
          <div class="flex items-center justify-between">
            <div>
              <span :class="['badge', packageStatusClass(pkg.status)]">{{ pkg.status }}</span>
              <p v-if="pkg.expiresOn" class="text-sm text-gray-500 mt-1">
                {{ $t('portal.packages.expiresOn') }}: {{ formatDate(pkg.expiresOn) }}
              </p>
            </div>
            <div class="text-right">
              <p class="text-3xl font-bold text-primary">{{ pkg.remainingCredits }}</p>
              <p class="text-xs text-gray-500">{{ $t('portal.packages.remainingCredits') }} / {{ pkg.totalCredits }}</p>
            </div>
          </div>

          <!-- Credits progress -->
          <div class="mt-3">
            <div class="w-full bg-base-200 rounded-full h-3">
              <div
                class="h-3 rounded-full transition-all bg-primary"
                :style="{ width: `${Math.min((pkg.remainingCredits / pkg.totalCredits) * 100, 100)}%` }"
              ></div>
            </div>
            <p class="text-xs text-gray-400 mt-1 text-right">
              {{ Math.round((pkg.remainingCredits / pkg.totalCredits) * 100) }}% {{ $t('portal.packages.remaining') }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

function packageStatusClass(s: string): string {
  const map: Record<string, string> = { active: 'badge-success', expired: 'badge-error', depleted: 'badge-warning' }
  return map[s] ?? 'badge-ghost'
}

onMounted(() => store.fetchPackages(props.studentId))
</script>
