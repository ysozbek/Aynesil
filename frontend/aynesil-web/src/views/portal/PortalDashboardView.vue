<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">{{ $t('portal.dashboard.title') }}</h1>
        <p class="text-sm text-gray-500 mt-1">{{ $t('portal.dashboard.subtitle') }}</p>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="store.loading && !store.myStudents.length" class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <!-- No students (ABAC: no assigned students for this guardian) -->
    <div v-else-if="!store.loading && !store.myStudents.length" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-12">
        <div class="text-5xl mb-4">👶</div>
        <h2 class="card-title">{{ $t('portal.dashboard.noChildren') }}</h2>
        <p class="text-gray-500">{{ $t('portal.dashboard.noChildrenDesc') }}</p>
      </div>
    </div>

    <template v-else>
      <!-- Student selector (if more than 1 child) -->
      <div v-if="store.myStudents.length > 1" class="flex gap-3 flex-wrap">
        <button
          v-for="s in store.myStudents"
          :key="s.id"
          :class="['btn btn-sm', selectedStudentId === s.id ? 'btn-primary' : 'btn-ghost border']"
          @click="selectStudent(s.id)"
        >
          {{ s.fullName }}
        </button>
      </div>

      <!-- Dashboard widgets -->
      <template v-if="selectedStudentId && store.dashboard">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <!-- Upcoming Sessions -->
          <div class="card bg-primary text-primary-content shadow">
            <div class="card-body p-4">
              <p class="text-sm opacity-80">{{ $t('portal.dashboard.upcomingSessions') }}</p>
              <p class="text-3xl font-bold">{{ store.dashboard.upcomingSessions ?? 0 }}</p>
            </div>
          </div>
          <!-- Unread Notifications -->
          <div class="card bg-warning text-warning-content shadow">
            <div class="card-body p-4">
              <p class="text-sm opacity-80">{{ $t('portal.dashboard.unreadNotifications') }}</p>
              <p class="text-3xl font-bold">{{ store.dashboard.unreadNotifications }}</p>
            </div>
          </div>
          <!-- Package Balance -->
          <div class="card bg-success text-success-content shadow">
            <div class="card-body p-4">
              <p class="text-sm opacity-80">{{ $t('portal.dashboard.packageBalance') }}</p>
              <p class="text-3xl font-bold">{{ store.dashboard.packageBalance ?? 0 }}</p>
            </div>
          </div>
          <!-- Active Goals -->
          <div class="card bg-info text-info-content shadow">
            <div class="card-body p-4">
              <p class="text-sm opacity-80">{{ $t('portal.dashboard.activeGoals') }}</p>
              <p class="text-3xl font-bold">{{ store.dashboard.activeGoals ?? 0 }}</p>
            </div>
          </div>
        </div>

        <!-- Quick links -->
        <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
          <router-link :to="{ name: 'portal-sessions', params: { studentId: selectedStudentId } }" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
            <div class="card-body p-5">
              <div class="text-3xl mb-2">📅</div>
              <h3 class="font-semibold">{{ $t('portal.nav.sessions') }}</h3>
              <p class="text-xs text-gray-500">{{ $t('portal.dashboard.sessionDesc') }}</p>
            </div>
          </router-link>
          <router-link :to="{ name: 'portal-goals', params: { studentId: selectedStudentId } }" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
            <div class="card-body p-5">
              <div class="text-3xl mb-2">🎯</div>
              <h3 class="font-semibold">{{ $t('portal.nav.goals') }}</h3>
              <p class="text-xs text-gray-500">{{ $t('portal.dashboard.goalsDesc') }}</p>
            </div>
          </router-link>
          <router-link :to="{ name: 'portal-packages', params: { studentId: selectedStudentId } }" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
            <div class="card-body p-5">
              <div class="text-3xl mb-2">📦</div>
              <h3 class="font-semibold">{{ $t('portal.nav.packages') }}</h3>
              <p class="text-xs text-gray-500">{{ $t('portal.dashboard.packagesDesc') }}</p>
            </div>
          </router-link>
          <router-link :to="{ name: 'portal-documents', params: { studentId: selectedStudentId } }" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
            <div class="card-body p-5">
              <div class="text-3xl mb-2">📄</div>
              <h3 class="font-semibold">{{ $t('portal.nav.documents') }}</h3>
              <p class="text-xs text-gray-500">{{ $t('portal.dashboard.documentsDesc') }}</p>
            </div>
          </router-link>
          <router-link :to="{ name: 'portal-notifications' }" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
            <div class="card-body p-5">
              <div class="text-3xl mb-2">🔔</div>
              <h3 class="font-semibold">{{ $t('portal.nav.notifications') }}</h3>
              <p class="text-xs text-gray-500">{{ $t('portal.dashboard.notificationsDesc') }}</p>
            </div>
          </router-link>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'

const store = useParentPortalStore()
const selectedStudentId = ref<string | null>(null)

async function selectStudent(id: string) {
  selectedStudentId.value = id
  await store.fetchDashboard(id)
}

onMounted(async () => {
  await store.fetchMyStudents()
  if (store.myStudents.length > 0) {
    await selectStudent(store.myStudents[0].id)
  }
})
</script>
