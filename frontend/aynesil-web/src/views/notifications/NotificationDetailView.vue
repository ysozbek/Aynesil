<template>
  <div class="p-6 space-y-6 max-w-3xl mx-auto">
    <div class="flex items-center gap-3">
      <button class="btn btn-ghost btn-sm" @click="$router.back()">
        ← {{ $t('common.back') }}
      </button>
    </div>

    <div v-if="store.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <div v-else-if="notification" class="card bg-base-100 shadow">
      <div class="card-body">
        <!-- Header -->
        <div class="flex items-start justify-between">
          <div>
            <h1 class="text-xl font-bold">{{ notification.subject ?? $t('notification.noSubject') }}</h1>
            <div class="flex gap-2 mt-2 flex-wrap">
              <span v-if="notification.categoryCode" class="badge badge-outline badge-sm">{{ notification.categoryCode }}</span>
              <span :class="['badge badge-sm', notification.isRead ? 'badge-ghost' : 'badge-primary']">
                {{ notification.isRead ? $t('notification.read') : $t('notification.unread') }}
              </span>
            </div>
          </div>
          <button
            v-if="!notification.isRead"
            class="btn btn-sm btn-outline"
            :disabled="store.saving"
            @click="markRead"
          >
            {{ $t('notification.actions.markRead') }}
          </button>
        </div>

        <div class="divider my-3"></div>

        <!-- Body -->
        <div class="prose max-w-none">
          <p class="whitespace-pre-wrap text-gray-700">{{ notification.body }}</p>
        </div>

        <div class="divider my-3"></div>

        <!-- Metadata -->
        <div class="grid grid-cols-2 gap-4 text-sm text-gray-500">
          <div>
            <span class="font-medium text-gray-600">{{ $t('common.createdAt') }}:</span>
            {{ formatDateTime(notification.createdAt) }}
          </div>
          <div v-if="notification.readAt">
            <span class="font-medium text-gray-600">{{ $t('notification.readAt') }}:</span>
            {{ formatDateTime(notification.readAt) }}
          </div>
          <div>
            <span class="font-medium text-gray-600">{{ $t('common.status') }}:</span>
            {{ notification.status }}
          </div>
        </div>
      </div>
    </div>

    <div v-else class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-12">
        <p class="text-gray-400">{{ $t('errors.notFound') }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useNotificationStore } from '@/stores/notification.store'
import type { NotificationListItemDto } from '@/types/notification.types'

const route = useRoute()
const store = useNotificationStore()
const id = computed(() => route.params.id as string)

const notification = computed<NotificationListItemDto | undefined>(() =>
  store.notificationList.items.find(n => n.id === id.value)
)

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'long', timeStyle: 'short' })
}

async function markRead() {
  await store.markRead(id.value)
}

onMounted(async () => {
  if (!notification.value) {
    await store.fetchNotifications({ page: 1, pageSize: 100 })
  }
  if (notification.value && !notification.value.isRead) {
    await markRead()
  }
})
</script>
