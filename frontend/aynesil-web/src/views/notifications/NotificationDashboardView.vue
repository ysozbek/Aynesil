<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ $t('notification.dashboard.title') }}</h1>
      <p class="text-sm text-gray-500">{{ $t('notification.dashboard.subtitle') }}</p>
    </div>

    <!-- Summary cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="card bg-primary text-primary-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('notification.dashboard.total') }}</p>
          <p class="text-3xl font-bold">{{ store.notificationList.totalCount }}</p>
        </div>
      </div>
      <div class="card bg-warning text-warning-content shadow">
        <div class="card-body p-4">
          <p class="text-sm opacity-80">{{ $t('notification.dashboard.unread') }}</p>
          <p class="text-3xl font-bold">{{ store.unreadCount }}</p>
        </div>
      </div>
      <div class="card bg-base-100 shadow">
        <div class="card-body p-4">
          <p class="text-sm text-gray-600">{{ $t('notification.dashboard.readRate') }}</p>
          <p class="text-3xl font-bold">{{ readRate }}%</p>
        </div>
      </div>
      <div class="card bg-base-100 shadow">
        <div class="card-body p-4">
          <p class="text-sm text-gray-600">{{ $t('notification.dashboard.templates') }}</p>
          <p class="text-3xl font-bold">{{ templateStore.templateList.totalCount }}</p>
        </div>
      </div>
    </div>

    <!-- Recent notifications -->
    <div class="card bg-base-100 shadow">
      <div class="card-header border-b px-6 py-4 flex items-center justify-between">
        <h2 class="font-semibold">{{ $t('notification.dashboard.recent') }}</h2>
        <router-link :to="{ name: 'notification-list' }" class="btn btn-ghost btn-sm">
          {{ $t('common.viewAll') }}
        </router-link>
      </div>
      <div class="card-body p-0">
        <div v-if="store.loading" class="flex justify-center py-8">
          <span class="loading loading-spinner text-primary"></span>
        </div>
        <div v-else class="divide-y">
          <div
            v-for="n in store.notificationList.items.slice(0, 10)"
            :key="n.id"
            class="px-6 py-3 flex items-start gap-3 hover:bg-base-50"
          >
            <div :class="['w-2 h-2 rounded-full mt-2 flex-shrink-0', n.isRead ? 'bg-gray-300' : 'bg-primary']"></div>
            <div class="flex-1 min-w-0">
              <p class="font-medium truncate">{{ n.subject ?? n.body.slice(0, 60) }}</p>
              <p class="text-xs text-gray-500 mt-0.5">{{ formatDateTime(n.createdAt) }}</p>
            </div>
            <span v-if="n.categoryCode" class="badge badge-ghost badge-xs flex-shrink-0">{{ n.categoryCode }}</span>
          </div>
          <div v-if="!store.notificationList.items.length" class="px-6 py-8 text-center text-gray-400">
            {{ $t('common.noData') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Quick actions -->
    <div class="flex flex-wrap gap-3">
      <router-link :to="{ name: 'notification-templates' }" class="btn btn-outline btn-sm">
        {{ $t('notification.nav.templates') }}
      </router-link>
      <router-link :to="{ name: 'notification-preferences' }" class="btn btn-outline btn-sm">
        {{ $t('notification.nav.preferences') }}
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useNotificationStore } from '@/stores/notification.store'
import { useNotificationTemplateStore } from '@/stores/notificationTemplate.store'

const store = useNotificationStore()
const templateStore = useNotificationTemplateStore()

const readRate = computed(() => {
  const total = store.notificationList.totalCount
  if (!total) return 0
  const read = store.notificationList.items.filter(n => n.isRead).length
  return Math.round((read / store.notificationList.items.length) * 100) || 0
})

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

onMounted(async () => {
  await Promise.all([
    store.fetchNotifications({ page: 1, pageSize: 20 }),
    store.fetchUnreadCount(),
    templateStore.fetchTemplates({ page: 1, pageSize: 1 }),
  ])
})
</script>
