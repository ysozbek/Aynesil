<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useEducatorStore } from '@/stores/educator.store'
import { useAuthStore } from '@/stores/auth.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useEducatorStore()
const auth = useAuthStore()

const id = computed(() => route.params.id as string)
const educator = computed(() => store.current)
const availability = computed(() => store.availability)

onMounted(async () => {
  await Promise.all([
    store.fetchOne(id.value),
    store.fetchAvailability(id.value),
    store.fetchUtilization({
      corporationId: auth.user?.corporationId ?? '',
      activeOnly: true,
    }),
  ])
})

onUnmounted(() => {
  store.clearCurrent()
})

const utilizationEntry = computed(() =>
  store.utilization.find(u => u.id === id.value)
)

const utilizationPercent = computed(() => {
  const u = utilizationEntry.value
  if (!u || u.totalStudentProgramCount === 0) return 0
  return Math.min(100, Math.round((u.activeStudentProgramCount / u.totalStudentProgramCount) * 100))
})

function utilizationColor(pct: number): string {
  if (pct >= 90) return 'bg-red-500'
  if (pct >= 70) return 'bg-amber-500'
  return 'bg-emerald-500'
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="store.loading && !educator" class="space-y-4">
      <div class="h-8 w-64 rounded bg-accent animate-pulse" />
      <div class="h-48 rounded-xl bg-accent animate-pulse" />
    </div>

    <template v-else-if="educator">
      <!-- Back + header -->
      <div class="mb-6 flex items-start justify-between gap-4">
        <div>
          <button @click="router.push({ name: 'educator-detail', params: { id: educator.id } })" class="text-sm text-muted-foreground hover:text-foreground mb-2 flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            {{ t('educator.backToList') }}
          </button>
          <h1 class="text-2xl font-bold text-foreground">{{ educator.fullName }}</h1>
          <div class="flex items-center gap-2 mt-1">
            <span v-if="educator.titleLabel" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
              {{ educator.titleLabel }}
            </span>
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', educator.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600']">
              {{ educator.isActive ? t('common.active') : t('common.inactive') }}
            </span>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <!-- Availability Panel -->
        <div class="md:col-span-2 space-y-5">
          <!-- Active Campuses -->
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <h3 class="font-semibold text-foreground mb-3">{{ t('educator.tab.campuses') }}</h3>
            <div v-if="!availability || availability.activeCampuses.length === 0" class="text-sm text-muted-foreground">
              {{ t('common.noData') }}
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="campus in availability.activeCampuses"
                :key="campus.id"
                class="flex items-center justify-between text-sm"
              >
                <span class="text-foreground">{{ campus.campusName ?? '—' }}</span>
                <span v-if="campus.isPrimary" class="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
                  {{ t('common.primary') }}
                </span>
              </div>
            </div>
          </div>

          <!-- Specialties -->
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <h3 class="font-semibold text-foreground mb-3">{{ t('educator.tab.specialties') }}</h3>
            <div v-if="!availability || availability.specialties.length === 0" class="text-sm text-muted-foreground">
              {{ t('common.noData') }}
            </div>
            <div v-else class="flex flex-wrap gap-2">
              <span
                v-for="spec in availability.specialties"
                :key="spec.id"
                class="px-2 py-1 rounded-lg text-xs font-medium bg-indigo-50 text-indigo-700 border border-indigo-200"
              >
                {{ spec.specialtyLabel ?? '—' }}
              </span>
            </div>
          </div>

          <!-- Assigned Students Placeholder -->
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <h3 class="font-semibold text-foreground mb-3">{{ t('common.assignedStudents') }}</h3>
            <p class="text-sm text-muted-foreground">{{ t('common.comingSoon') }}</p>
          </div>
        </div>

        <!-- Utilization Widget -->
        <div>
          <div class="rounded-xl border border-border bg-[--color-card] p-5 shadow-sm">
            <h3 class="font-semibold text-foreground mb-4">{{ t('common.utilization') }}</h3>

            <div class="space-y-4">
              <div class="text-center">
                <p class="text-4xl font-bold text-foreground">
                  {{ availability?.activeStudentProgramCount ?? 0 }}
                </p>
                <p class="text-sm text-muted-foreground mt-1">{{ t('common.activePrograms') }}</p>
              </div>

              <div v-if="utilizationEntry">
                <div class="flex justify-between text-xs text-muted-foreground mb-1">
                  <span>{{ t('common.active') }}: {{ utilizationEntry.activeStudentProgramCount }}</span>
                  <span>{{ t('common.total') }}: {{ utilizationEntry.totalStudentProgramCount }}</span>
                </div>
                <div class="w-full bg-gray-100 rounded-full h-2.5">
                  <div
                    :class="['h-2.5 rounded-full transition-all', utilizationColor(utilizationPercent)]"
                    :style="{ width: `${utilizationPercent}%` }"
                  />
                </div>
                <p class="text-xs text-muted-foreground text-center mt-1">{{ utilizationPercent }}%</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <div v-else-if="!store.loading" class="text-center py-24">
      <p class="text-muted-foreground">{{ t('errors.notFound') }}</p>
      <button @click="router.push({ name: 'educators' })" class="mt-4 text-sm text-primary hover:underline">
        ← {{ t('educator.backToList') }}
      </button>
    </div>
  </div>
</template>
