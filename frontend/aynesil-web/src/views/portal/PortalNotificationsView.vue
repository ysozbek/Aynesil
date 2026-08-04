<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('portal.notifications.title') }}</h1>
        <p class="text-sm text-gray-500">{{ $t('portal.notifications.subtitle') }}</p>
      </div>
    </div>

    <div v-if="store.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <div v-else-if="!store.portalNotifications.items.length" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-12">
        <div class="text-5xl mb-3">🔔</div>
        <p class="text-gray-500">{{ $t('portal.notifications.noNotifications') }}</p>
      </div>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="n in store.portalNotifications.items"
        :key="n.id"
        :class="['card bg-base-100 shadow-sm border-l-4 transition-all', n.isRead ? 'border-base-300 opacity-70' : 'border-primary']"
      >
        <div class="card-body p-4">
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1">
              <p v-if="n.subject" class="font-semibold">{{ n.subject }}</p>
              <p class="text-sm text-gray-600 mt-1">{{ n.body }}</p>
              <p class="text-xs text-gray-400 mt-2">{{ formatDateTime(n.createdAt) }}</p>
            </div>
            <div class="flex flex-col items-end gap-1">
              <span v-if="!n.isRead" class="badge badge-primary badge-sm">{{ $t('portal.notifications.unread') }}</span>
              <span v-if="n.categoryCode" class="badge badge-ghost badge-xs">{{ n.categoryCode }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="store.portalNotifications.totalPages > 1" class="flex justify-center gap-2 mt-4">
        <button class="btn btn-sm" :disabled="!store.portalNotifications.hasPreviousPage" @click="load(store.portalNotifications.page - 1)">«</button>
        <span class="btn btn-sm btn-disabled">{{ store.portalNotifications.page }} / {{ store.portalNotifications.totalPages }}</span>
        <button class="btn btn-sm" :disabled="!store.portalNotifications.hasNextPage" @click="load(store.portalNotifications.page + 1)">»</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const store = useParentPortalStore()

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

function load(page = 1) {
  store.fetchPortalNotifications(page, 20)
}

onMounted(() => load())
</script>
