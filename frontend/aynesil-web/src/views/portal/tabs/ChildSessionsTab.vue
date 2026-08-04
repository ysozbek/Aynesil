<template>
  <div class="space-y-4">
    <!-- Filters -->
    <div class="flex flex-wrap gap-3 items-end">
      <div>
        <label class="label label-text text-xs">{{ $t('common.from') }}</label>
        <input v-model="filters.from" type="date" class="input input-bordered input-sm" />
      </div>
      <div>
        <label class="label label-text text-xs">{{ $t('common.to') }}</label>
        <input v-model="filters.to" type="date" class="input input-bordered input-sm" />
      </div>
      <button class="btn btn-sm btn-primary mt-5" @click="load(1)">{{ $t('common.filter') }}</button>
    </div>

    <!-- Loading -->
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <template v-else>
      <!-- Upcoming Sessions -->
      <div>
        <h3 class="text-sm font-semibold text-gray-600 mb-2 uppercase tracking-wide">{{ $t('portal.sessions.upcoming') }}</h3>
        <div v-if="upcomingSessions.length" class="space-y-2">
          <div v-for="s in upcomingSessions" :key="s.id" class="card bg-base-100 shadow-sm border-l-4 border-primary">
            <div class="card-body p-4 flex-row items-center justify-between">
              <div>
                <p class="font-semibold">{{ s.title ?? $t('portal.sessions.session') }}</p>
                <p class="text-sm text-gray-500">{{ formatDateTime(s.startsAt) }} – {{ formatTime(s.endsAt) }}</p>
              </div>
              <span :class="['badge', sessionStatusClass(s.status)]">{{ s.status }}</span>
            </div>
          </div>
        </div>
        <p v-else class="text-sm text-gray-500">{{ $t('portal.sessions.noUpcoming') }}</p>
      </div>

      <!-- Session History -->
      <div>
        <h3 class="text-sm font-semibold text-gray-600 mb-2 uppercase tracking-wide">{{ $t('portal.sessions.history') }}</h3>
        <div class="overflow-x-auto">
          <table class="table table-sm">
            <thead>
              <tr>
                <th>{{ $t('portal.sessions.title') }}</th>
                <th>{{ $t('portal.sessions.date') }}</th>
                <th>{{ $t('common.status') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!store.sessionList.items.length">
                <td colspan="3" class="text-center text-gray-400 py-6">{{ $t('common.noData') }}</td>
              </tr>
              <tr v-for="s in store.sessionList.items" :key="s.id">
                <td>{{ s.title ?? $t('portal.sessions.session') }}</td>
                <td>{{ formatDateTime(s.startsAt) }}</td>
                <td><span :class="['badge badge-sm', sessionStatusClass(s.status)]">{{ s.status }}</span></td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="store.sessionList.totalPages > 1" class="flex justify-center mt-4 gap-2">
          <button class="btn btn-sm" :disabled="!store.sessionList.hasPreviousPage" @click="load(store.sessionList.page - 1)">«</button>
          <span class="btn btn-sm btn-disabled">{{ store.sessionList.page }} / {{ store.sessionList.totalPages }}</span>
          <button class="btn btn-sm" :disabled="!store.sessionList.hasNextPage" @click="load(store.sessionList.page + 1)">»</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

const filters = ref({ from: '', to: '' })

const upcomingSessions = computed(() =>
  store.sessionList.items.filter(s => {
    const start = new Date(s.startsAt)
    return start >= new Date() && s.status !== 'cancelled'
  })
)

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}
function formatTime(d: string): string {
  return new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
}
function sessionStatusClass(s: string): string {
  const map: Record<string, string> = {
    scheduled: 'badge-info',
    completed: 'badge-success',
    cancelled: 'badge-error',
    no_show: 'badge-warning',
  }
  return map[s] ?? 'badge-ghost'
}

async function load(page = 1) {
  await store.fetchSessions({
    studentId: props.studentId,
    page,
    pageSize: 20,
    from: filters.value.from || undefined,
    to: filters.value.to || undefined,
  })
}

onMounted(() => load())
</script>
