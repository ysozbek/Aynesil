<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('notification.list.title') }}</h1>
        <p class="text-sm text-gray-500">{{ $t('notification.list.subtitle') }}</p>
      </div>
      <button class="btn btn-ghost btn-sm" :disabled="store.saving" @click="markAll">
        {{ store.saving ? $t('common.saving') : $t('notification.actions.markAllRead') }}
      </button>
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap gap-3 items-end">
      <div>
        <label class="label label-text text-xs">{{ $t('notification.filter.isRead') }}</label>
        <select v-model="filters.isRead" class="select select-sm select-bordered" @change="load(1)">
          <option value="">{{ $t('common.allStatuses') }}</option>
          <option value="false">{{ $t('notification.filter.unreadOnly') }}</option>
          <option value="true">{{ $t('notification.filter.readOnly') }}</option>
        </select>
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('notification.filter.from') }}</label>
        <input v-model="filters.from" type="date" class="input input-sm input-bordered" @change="load(1)" />
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('notification.filter.to') }}</label>
        <input v-model="filters.to" type="date" class="input input-sm input-bordered" @change="load(1)" />
      </div>
    </div>

    <!-- List -->
    <div v-if="store.loading" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <div v-else class="space-y-2">
      <div v-if="!store.notificationList.items.length" class="card bg-base-100 shadow">
        <div class="card-body items-center text-center py-10">
          <div class="text-4xl mb-2">🔔</div>
          <p class="text-gray-500">{{ $t('common.noData') }}</p>
        </div>
      </div>

      <div
        v-for="n in store.notificationList.items"
        :key="n.id"
        :class="['card bg-base-100 shadow-sm border-l-4 cursor-pointer hover:shadow-md transition-shadow',
          n.isRead ? 'border-base-300' : 'border-primary']"
        @click="openDetail(n.id)"
      >
        <div class="card-body p-4">
          <div class="flex items-start justify-between gap-4">
            <div class="flex gap-3 flex-1">
              <div :class="['w-2.5 h-2.5 rounded-full mt-1.5 flex-shrink-0', n.isRead ? 'bg-gray-300' : 'bg-primary']"></div>
              <div class="flex-1">
                <p class="font-medium">{{ n.subject ?? $t('notification.noSubject') }}</p>
                <p class="text-sm text-gray-600 line-clamp-2 mt-0.5">{{ n.body }}</p>
                <p class="text-xs text-gray-400 mt-1">{{ formatDateTime(n.createdAt) }}</p>
              </div>
            </div>
            <div class="flex flex-col items-end gap-1 flex-shrink-0">
              <span v-if="!n.isRead" class="badge badge-primary badge-sm">{{ $t('notification.unread') }}</span>
              <span v-if="n.categoryCode" class="badge badge-ghost badge-xs">{{ n.categoryCode }}</span>
              <button
                v-if="!n.isRead"
                class="btn btn-ghost btn-xs"
                @click.stop="markOne(n.id)"
              >
                {{ $t('notification.actions.markRead') }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="store.notificationList.totalPages > 1" class="flex justify-center gap-2">
      <button class="btn btn-sm" :disabled="!store.notificationList.hasPreviousPage" @click="load(store.notificationList.page - 1)">«</button>
      <span class="btn btn-sm btn-disabled">{{ store.notificationList.page }} / {{ store.notificationList.totalPages }}</span>
      <button class="btn btn-sm" :disabled="!store.notificationList.hasNextPage" @click="load(store.notificationList.page + 1)">»</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useNotificationStore } from '@/stores/notification.store'

const store = useNotificationStore()
const router = useRouter()

const filters = ref({ isRead: '', from: '', to: '' })

function load(page = 1) {
  store.fetchNotifications({
    page,
    pageSize: 20,
    isRead: filters.value.isRead === '' ? undefined : filters.value.isRead === 'true',
    from: filters.value.from || undefined,
    to: filters.value.to || undefined,
  })
}

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

async function markOne(id: string) {
  await store.markRead(id)
}

async function markAll() {
  await store.markAllRead()
}

function openDetail(id: string) {
  router.push({ name: 'notification-detail', params: { id } })
}

onMounted(() => {
  load()
  store.fetchUnreadCount()
})
</script>
