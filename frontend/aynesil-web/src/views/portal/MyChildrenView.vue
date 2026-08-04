<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">{{ $t('portal.children.title') }}</h1>
      <p class="text-sm text-gray-500 mt-1">{{ $t('portal.children.subtitle') }}</p>
    </div>

    <div v-if="store.loading" class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <div v-else-if="!store.myStudents.length" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-12">
        <div class="text-5xl mb-4">👶</div>
        <h2 class="card-title">{{ $t('portal.children.noChildren') }}</h2>
        <p class="text-gray-500">{{ $t('portal.children.noChildrenDesc') }}</p>
      </div>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="student in store.myStudents"
        :key="student.id"
        class="card bg-base-100 shadow hover:shadow-lg transition-shadow cursor-pointer"
        @click="$router.push({ name: 'portal-child-detail', params: { studentId: student.id } })"
      >
        <div class="card-body">
          <div class="flex items-center gap-4">
            <div class="avatar placeholder">
              <div class="bg-primary text-primary-content rounded-full w-14">
                <span class="text-xl font-bold">{{ initials(student.fullName) }}</span>
              </div>
            </div>
            <div class="flex-1">
              <h2 class="card-title text-base">{{ student.fullName }}</h2>
              <p v-if="student.dateOfBirth" class="text-sm text-gray-500">
                {{ formatDate(student.dateOfBirth) }}
              </p>
              <span
                class="badge badge-sm mt-1"
                :class="statusBadgeClass(student.enrollmentStatus)"
              >
                {{ student.enrollmentStatus ?? '-' }}
              </span>
            </div>
          </div>

          <!-- Permission indicators -->
          <div class="flex flex-wrap gap-1 mt-3">
            <span v-if="student.canViewSessions" class="badge badge-ghost badge-xs">📅 {{ $t('portal.nav.sessions') }}</span>
            <span v-if="student.canViewGoals" class="badge badge-ghost badge-xs">🎯 {{ $t('portal.nav.goals') }}</span>
            <span v-if="student.canViewFinance" class="badge badge-ghost badge-xs">💳 {{ $t('portal.nav.packages') }}</span>
            <span v-if="student.canViewDocuments" class="badge badge-ghost badge-xs">📄 {{ $t('portal.nav.documents') }}</span>
            <span v-if="student.canViewReports" class="badge badge-ghost badge-xs">📊 {{ $t('portal.nav.reports') }}</span>
          </div>

          <div class="card-actions justify-end mt-2">
            <router-link
              :to="{ name: 'portal-child-detail', params: { studentId: student.id } }"
              class="btn btn-sm btn-primary"
            >
              {{ $t('common.view') }}
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const store = useParentPortalStore()

function initials(name: string): string {
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2)
}

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

function statusBadgeClass(status?: string): string {
  if (!status) return 'badge-ghost'
  const map: Record<string, string> = {
    active: 'badge-success',
    enrolled: 'badge-success',
    inactive: 'badge-ghost',
    graduated: 'badge-info',
  }
  return map[status.toLowerCase()] ?? 'badge-ghost'
}

onMounted(() => store.fetchMyStudents())
</script>
