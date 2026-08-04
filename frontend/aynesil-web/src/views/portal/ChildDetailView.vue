<template>
  <div class="p-6 space-y-6">
    <!-- Back + Header -->
    <div class="flex items-center gap-3">
      <button class="btn btn-ghost btn-sm" @click="$router.back()">
        ← {{ $t('common.back') }}
      </button>
    </div>

    <div v-if="store.loading && !store.currentStudent" class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <!-- 403 / Not found -->
    <div v-else-if="accessDenied" class="card bg-base-100 shadow">
      <div class="card-body items-center text-center py-12">
        <div class="text-5xl mb-4">🔒</div>
        <h2 class="card-title">{{ $t('errors.forbidden') }}</h2>
        <p class="text-gray-500">{{ $t('portal.errors.noAccess') }}</p>
      </div>
    </div>

    <template v-else-if="store.currentStudent">
      <!-- Student Header Card -->
      <div class="card bg-base-100 shadow">
        <div class="card-body">
          <div class="flex items-center gap-5">
            <div class="avatar placeholder">
              <div class="bg-primary text-primary-content rounded-full w-20">
                <span class="text-2xl font-bold">{{ initials(store.currentStudent.fullName) }}</span>
              </div>
            </div>
            <div>
              <h1 class="text-2xl font-bold">{{ store.currentStudent.fullName }}</h1>
              <p v-if="store.currentStudent.dateOfBirth" class="text-gray-500">
                {{ $t('student.dateOfBirth') }}: {{ formatDate(store.currentStudent.dateOfBirth) }}
              </p>
              <span class="badge badge-success mt-1">
                {{ store.currentStudent.enrollmentStatus ?? 'Aktif' }}
              </span>
            </div>

            <!-- Dashboard summary -->
            <div v-if="store.dashboard" class="ml-auto flex gap-6 text-center hidden md:flex">
              <div>
                <p class="text-2xl font-bold text-primary">{{ store.dashboard.upcomingSessions ?? 0 }}</p>
                <p class="text-xs text-gray-500">{{ $t('portal.dashboard.upcomingSessions') }}</p>
              </div>
              <div>
                <p class="text-2xl font-bold text-success">{{ store.dashboard.packageBalance ?? 0 }}</p>
                <p class="text-xs text-gray-500">{{ $t('portal.dashboard.packageBalance') }}</p>
              </div>
              <div>
                <p class="text-2xl font-bold text-info">{{ store.dashboard.activeGoals ?? 0 }}</p>
                <p class="text-xs text-gray-500">{{ $t('portal.dashboard.activeGoals') }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs tabs-boxed bg-base-200">
        <button
          v-for="tab in availableTabs"
          :key="tab.key"
          :class="['tab tab-lg', activeTab === tab.key ? 'tab-active' : '']"
          @click="activeTab = tab.key"
        >
          {{ tab.icon }} {{ tab.label }}
        </button>
      </div>

      <!-- Sessions Tab -->
      <div v-if="activeTab === 'sessions'">
        <ChildSessionsTab :student-id="studentId" />
      </div>

      <!-- Goals Tab -->
      <div v-if="activeTab === 'goals'">
        <ChildGoalsTab :student-id="studentId" />
      </div>

      <!-- Packages Tab -->
      <div v-if="activeTab === 'packages'">
        <ChildPackagesTab :student-id="studentId" />
      </div>

      <!-- Documents Tab -->
      <div v-if="activeTab === 'documents'">
        <ChildDocumentsTab :student-id="studentId" />
      </div>

      <!-- Meetings Tab -->
      <div v-if="activeTab === 'meetings'">
        <ChildMeetingsTab :student-id="studentId" />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useParentPortalStore } from '@/stores/parentPortal.store'
import { useI18n } from 'vue-i18n'
import ChildSessionsTab from './tabs/ChildSessionsTab.vue'
import ChildGoalsTab from './tabs/ChildGoalsTab.vue'
import ChildPackagesTab from './tabs/ChildPackagesTab.vue'
import ChildDocumentsTab from './tabs/ChildDocumentsTab.vue'
import ChildMeetingsTab from './tabs/ChildMeetingsTab.vue'

const route = useRoute()
const { t } = useI18n()
const store = useParentPortalStore()

const studentId = computed(() => route.params.studentId as string)
const activeTab = ref('sessions')
const accessDenied = ref(false)

const availableTabs = computed(() => {
  const student = store.currentStudent
  if (!student) return []
  const tabs = []
  if (student.canViewSessions) tabs.push({ key: 'sessions', label: t('portal.nav.sessions'), icon: '📅' })
  if (student.canViewGoals) tabs.push({ key: 'goals', label: t('portal.nav.goals'), icon: '🎯' })
  if (student.canViewFinance) tabs.push({ key: 'packages', label: t('portal.nav.packages'), icon: '📦' })
  if (student.canViewDocuments) tabs.push({ key: 'documents', label: t('portal.nav.documents'), icon: '📄' })
  tabs.push({ key: 'meetings', label: t('portal.nav.meetings'), icon: '🤝' })
  return tabs
})

function initials(name: string): string {
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2)
}

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

onMounted(async () => {
  try {
    await store.fetchStudent(studentId.value)
    await store.fetchDashboard(studentId.value)
    if (availableTabs.value.length) activeTab.value = availableTabs.value[0].key
  } catch (e: unknown) {
    const err = e as { response?: { status: number } }
    if (err?.response?.status === 403 || err?.response?.status === 404) {
      accessDenied.value = true
    }
  }
})
</script>
