<template>
  <div class="p-6 space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('meeting.list.title') }}</h1>
        <p class="text-sm text-gray-500">{{ $t('meeting.list.subtitle') }}</p>
      </div>
      <router-link :to="{ name: 'meeting-new' }" class="btn btn-primary btn-sm">
        + {{ $t('meeting.actions.create') }}
      </router-link>
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap gap-3 items-end">
      <div>
        <label class="label label-text text-xs">{{ $t('common.status') }}</label>
        <select v-model="filters.status" class="select select-sm select-bordered" @change="load(1)">
          <option value="">{{ $t('common.allStatuses') }}</option>
          <option value="draft">Draft</option>
          <option value="scheduled">Scheduled</option>
          <option value="completed">Completed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('common.from') }}</label>
        <input v-model="filters.from" type="date" class="input input-sm input-bordered" @change="load(1)" />
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('common.to') }}</label>
        <input v-model="filters.to" type="date" class="input input-sm input-bordered" @change="load(1)" />
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('common.search') }}</label>
        <input v-model="filters.search" type="text" class="input input-sm input-bordered" :placeholder="$t('common.search')" @keyup.enter="load(1)" />
      </div>
      <button class="btn btn-primary btn-sm mt-5" @click="load(1)">{{ $t('common.filter') }}</button>
    </div>

    <!-- Table -->
    <div class="card bg-base-100 shadow overflow-hidden">
      <div v-if="store.loading" class="flex justify-center py-10">
        <span class="loading loading-spinner text-primary"></span>
      </div>
      <div v-else class="overflow-x-auto">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>{{ $t('meeting.fields.title') }}</th>
              <th>{{ $t('meeting.fields.type') }}</th>
              <th>{{ $t('meeting.fields.scheduledAt') }}</th>
              <th>{{ $t('meeting.fields.location') }}</th>
              <th>{{ $t('common.status') }}</th>
              <th>{{ $t('meeting.fields.participants') }}</th>
              <th>{{ $t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!store.meetingList.items.length">
              <td colspan="7" class="text-center py-8 text-gray-400">{{ $t('common.noData') }}</td>
            </tr>
            <tr v-for="m in store.meetingList.items" :key="m.id">
              <td class="font-medium">{{ m.title }}</td>
              <td>{{ m.meetingTypeCode ?? '-' }}</td>
              <td>{{ m.scheduledAt ? formatDateTime(m.scheduledAt) : '-' }}</td>
              <td>{{ m.location ?? '-' }}</td>
              <td>
                <span :class="['badge badge-sm', meetingStatusClass(m.status)]">{{ m.status }}</span>
              </td>
              <td>{{ m.participantCount }}</td>
              <td>
                <div class="flex gap-1">
                  <router-link :to="{ name: 'meeting-detail', params: { id: m.id } }" class="btn btn-ghost btn-xs">
                    {{ $t('common.view') }}
                  </router-link>
                  <router-link :to="{ name: 'meeting-edit', params: { id: m.id } }" class="btn btn-ghost btn-xs">
                    {{ $t('common.edit') }}
                  </router-link>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="store.meetingList.totalPages > 1" class="flex justify-center gap-2">
      <button class="btn btn-sm" :disabled="!store.meetingList.hasPreviousPage" @click="load(store.meetingList.page - 1)">«</button>
      <span class="btn btn-sm btn-disabled">{{ store.meetingList.page }} / {{ store.meetingList.totalPages }}</span>
      <button class="btn btn-sm" :disabled="!store.meetingList.hasNextPage" @click="load(store.meetingList.page + 1)">»</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useMeetingStore } from '@/stores/meeting.store'

const store = useMeetingStore()
const filters = ref({ status: '', from: '', to: '', search: '' })

function load(page = 1) {
  store.fetchMeetings({
    page,
    pageSize: 20,
    status: filters.value.status || undefined,
    from: filters.value.from || undefined,
    to: filters.value.to || undefined,
    search: filters.value.search || undefined,
  })
}

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}

function meetingStatusClass(s: string): string {
  const map: Record<string, string> = {
    draft: 'badge-ghost', scheduled: 'badge-info',
    completed: 'badge-success', cancelled: 'badge-error',
  }
  return map[s] ?? 'badge-ghost'
}

onMounted(() => load())
</script>
