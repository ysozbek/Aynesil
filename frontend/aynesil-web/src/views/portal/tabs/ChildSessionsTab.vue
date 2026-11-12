<template>
  <div class="space-y-4">
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
              <tr v-if="!store.sessionList.length">
                <td colspan="3" class="text-center text-gray-400 py-6">{{ $t('common.noData') }}</td>
              </tr>
              <tr v-for="s in store.sessionList" :key="s.id">
                <td>{{ s.title ?? $t('portal.sessions.session') }}</td>
                <td>{{ formatDateTime(s.startsAt) }}</td>
                <td><span :class="['badge badge-sm', sessionStatusClass(s.status)]">{{ s.status }}</span></td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="store.sessionPage > 1 || store.sessionHasNext" class="flex justify-center mt-4 gap-2">
          <button class="btn btn-sm" :disabled="store.sessionPage <= 1" @click="load(store.sessionPage - 1)">«</button>
          <span class="btn btn-sm btn-disabled">{{ store.sessionPage }}</span>
          <button class="btn btn-sm" :disabled="!store.sessionHasNext" @click="load(store.sessionPage + 1)">»</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

const upcomingSessions = computed(() =>
  store.sessionList.filter(s => {
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
  })
}

onMounted(() => load())
</script>
