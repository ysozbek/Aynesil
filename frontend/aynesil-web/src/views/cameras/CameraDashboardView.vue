<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const router = useRouter()
const cameraStore = useCameraStore()
const auth = useAuthStore()
const { can } = usePermission()

function formatDatetime(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

const totalCameras = computed(() => cameraStore.cameraList.totalCount)
const activeCameras = computed(() => cameraStore.cameraList.items.filter((c) => c.isActive).length)
const activeAuthorizations = computed(() =>
  cameraStore.authorizations.items.filter((a) => a.isCurrentlyValid && !a.isRevoked).length
)

onMounted(async () => {
  const corp = auth.user?.corporationId
  await Promise.all([
    cameraStore.fetchCameras({ corporationId: corp, pageSize: 50 }),
    cameraStore.fetchAuthorizations({ corporationId: corp, isCurrentlyValid: true }),
    cameraStore.fetchViewingLogs({ corporationId: corp, pageSize: 20 }),
  ])
})
</script>

<template>
  <div>
    <PageHeader :title="t('camera.dashboard.title')" :description="t('camera.dashboard.subtitle')">
      <button
        v-if="can('camera:create')"
        @click="router.push({ name: 'camera-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('camera.new') }}
      </button>
    </PageHeader>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-primary">{{ totalCameras }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('camera.dashboard.totalCameras') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-green-600">{{ activeCameras }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('camera.dashboard.activeCameras') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-amber-600">{{ activeAuthorizations }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('camera.dashboard.activeAuthorizations') }}</p>
      </div>
      <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
        <p class="text-2xl font-bold text-sky-600">{{ cameraStore.viewingLogs.totalCount }}</p>
        <p class="text-xs text-muted-foreground mt-1">{{ t('camera.dashboard.recentViews') }}</p>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('camera.dashboard.cameraStatus') }}</h3>
          <button
            @click="router.push({ name: 'cameras' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="cameraStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="cameraStore.cameraList.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('camera.list.noData') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="c in cameraStore.cameraList.items"
            :key="c.id"
            class="flex items-center gap-4 px-4 py-3 hover:bg-accent/30 cursor-pointer"
            @click="router.push({ name: 'camera-detail', params: { id: c.id } })"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">{{ c.name }}</p>
              <p class="text-xs text-muted-foreground">{{ c.code }} · {{ c.campusName ?? '—' }}</p>
            </div>
            <span
              :class="[
                'px-2 py-0.5 rounded-full text-xs font-medium shrink-0',
                c.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700',
              ]"
            >
              {{ c.isActive ? t('common.active') : t('common.passive') }}
            </span>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="font-semibold text-foreground">{{ t('camera.dashboard.accessLogs') }}</h3>
          <button
            @click="router.push({ name: 'camera-viewing-history' })"
            class="text-xs text-primary hover:underline"
          >
            {{ t('common.viewAll') }}
          </button>
        </div>

        <div v-if="cameraStore.loading" class="p-4 space-y-3">
          <div v-for="i in 4" :key="i" class="h-12 rounded-lg bg-accent animate-pulse" />
        </div>
        <div v-else-if="cameraStore.viewingLogs.items.length === 0" class="py-10 text-center text-sm text-muted-foreground">
          {{ t('camera.dashboard.noLogs') }}
        </div>
        <div v-else class="divide-y divide-border">
          <div
            v-for="log in cameraStore.viewingLogs.items.slice(0, 8)"
            :key="log.id"
            class="flex items-center gap-3 px-4 py-3"
          >
            <div class="w-8 h-8 rounded-lg bg-sky-100 flex items-center justify-center shrink-0">
              <svg class="w-4 h-4 text-sky-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-foreground truncate">
                {{ log.guardianFullName ?? t('camera.dashboard.staffViewer') }}
              </p>
              <p class="text-xs text-muted-foreground truncate">
                {{ log.cameraCode ?? '—' }} · {{ formatDatetime(log.startedAt) }}
              </p>
            </div>
            <span class="text-xs text-muted-foreground shrink-0">
              {{ log.durationSeconds ? `${Math.round(log.durationSeconds / 60)} dk` : '—' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
