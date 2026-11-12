<template>
  <div class="space-y-4">
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <div v-else-if="!store.meetingHistory.length" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-10">
        <div class="text-4xl mb-2">🤝</div>
        <p class="text-gray-500">{{ $t('portal.meetings.noMeetings') }}</p>
      </div>
    </div>

    <div v-else class="overflow-x-auto">
      <table class="table table-sm">
        <thead>
          <tr>
            <th>{{ $t('portal.meetings.title') }}</th>
            <th>{{ $t('portal.meetings.date') }}</th>
            <th>{{ $t('common.status') }}</th>
            <th>{{ $t('portal.meetings.attendance') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in store.meetingHistory" :key="m.id">
            <td>{{ m.title }}</td>
            <td>{{ m.scheduledAt ? formatDateTime(m.scheduledAt) : '-' }}</td>
            <td><StatusBadge entity="meeting" :status="m.status" /></td>
            <td>{{ m.guardianAttendance ?? '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'
import StatusBadge from '@/components/shared/StatusBadge.vue'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

function formatDateTime(d: string): string {
  return new Date(d).toLocaleString('tr-TR', { dateStyle: 'medium', timeStyle: 'short' })
}
onMounted(() => store.fetchMeetings(props.studentId))
</script>
