<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useStudentStore } from '@/stores/student.store'
import { useCaseStore } from '@/stores/case.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const studentStore = useStudentStore()
const caseStore = useCaseStore()

const id = computed(() => route.params.id as string)
const student = computed(() => studentStore.current)

function formatDate(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatDateTime(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

type TimelineEntry = {
  id: string
  type: 'status' | 'case_note' | 'medical_report' | 'diagnosis' | 'campus'
  date: string
  title: string
  detail: string | null
  color: string
}

const timeline = computed<TimelineEntry[]>(() => {
  const entries: TimelineEntry[] = []

  for (const h of studentStore.statusHistory) {
    entries.push({
      id: h.id,
      type: 'status',
      date: h.changedAt,
      title: `${t('student.changeStatus')}: ${h.statusLabel ?? '—'}`,
      detail: h.reason ?? null,
      color: 'bg-blue-500',
    })
  }

  for (const n of caseStore.caseNotes.items) {
    entries.push({
      id: n.id,
      type: 'case_note',
      date: n.createdAt,
      title: `${t('student.caseNote.title')}: ${n.noteType ?? t('common.none')}`,
      detail: n.body.length > 120 ? n.body.substring(0, 120) + '…' : n.body,
      color: n.isConfidential ? 'bg-red-400' : 'bg-green-500',
    })
  }

  for (const r of caseStore.medicalReports) {
    entries.push({
      id: r.id,
      type: 'medical_report',
      date: r.createdAt,
      title: `${t('student.medicalReport.title')}: ${r.title}`,
      detail: r.summary ?? null,
      color: 'bg-orange-500',
    })
  }

  for (const d of studentStore.diagnoses) {
    entries.push({
      id: d.id,
      type: 'diagnosis',
      date: d.createdAt,
      title: `${t('student.diagnosis.title')}: ${d.categoryLabel ?? d.icdCode ?? '—'}`,
      detail: d.description ?? null,
      color: 'bg-purple-500',
    })
  }

  for (const c of studentStore.campuses) {
    entries.push({
      id: c.id,
      type: 'campus',
      date: c.activeFrom + 'T00:00:00Z',
      title: `${t('student.campus.enroll')}: ${c.campusName ?? '—'}${c.isPrimary ? ` (${t('student.campus.primary')})` : ''}`,
      detail: c.activeTo ? `${t('student.campus.activeFrom')}: ${formatDate(c.activeFrom)} — ${t('student.campus.activeTo')}: ${formatDate(c.activeTo)}` : `${t('student.campus.activeFrom')}: ${formatDate(c.activeFrom)}`,
      color: 'bg-teal-500',
    })
  }

  return entries.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
})

onMounted(async () => {
  await Promise.all([
    studentStore.fetchOne(id.value),
    studentStore.fetchStatusHistory(id.value),
    caseStore.fetchCaseNotes({ studentId: id.value, page: 1, pageSize: 50 }),
    caseStore.fetchMedicalReports(id.value),
    caseStore.fetchDevelopmentReports(id.value),
    caseStore.fetchExternalReports(id.value),
  ])
})

onUnmounted(() => {
  caseStore.clearAll()
})

function typeIcon(type: TimelineEntry['type']): string {
  const icons: Record<TimelineEntry['type'], string> = {
    status: '🔄',
    case_note: '📝',
    medical_report: '🏥',
    diagnosis: '🔬',
    campus: '🏫',
  }
  return icons[type] ?? '📌'
}
</script>

<template>
  <div class="p-6 space-y-6">
    <PageHeader
      :title="t('student.timeline')"
      :description="student?.fullName ?? ''"
    >
      <template #actions>
        <button
          class="btn btn-sm btn-light"
          @click="router.push({ name: 'student-detail', params: { id } })"
        >
          ← {{ t('student.backToList') }}
        </button>
      </template>
    </PageHeader>

    <!-- Loading -->
    <div v-if="studentStore.loading" class="flex items-center justify-center py-16">
      <div class="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin" />
    </div>

    <!-- Empty -->
    <div v-else-if="timeline.length === 0" class="text-center py-16 text-muted-foreground">
      {{ t('common.noData') }}
    </div>

    <!-- Timeline -->
    <div v-else class="relative">
      <!-- vertical line -->
      <div class="absolute left-6 top-0 bottom-0 w-0.5 bg-border" />

      <ul class="space-y-6 pl-14">
        <li
          v-for="entry in timeline"
          :key="entry.id"
          class="relative"
        >
          <!-- dot -->
          <span
            :class="[entry.color, 'absolute -left-[2.4rem] top-1 w-4 h-4 rounded-full flex items-center justify-center text-white text-[10px]']"
          >{{ typeIcon(entry.type) }}</span>

          <div class="bg-[--color-card] border border-border rounded-xl p-4 shadow-sm">
            <div class="flex items-start justify-between gap-4">
              <p class="font-medium text-sm text-foreground">{{ entry.title }}</p>
              <span class="text-xs text-muted-foreground whitespace-nowrap flex-shrink-0">
                {{ formatDateTime(entry.date) }}
              </span>
            </div>
            <p v-if="entry.detail" class="mt-1 text-sm text-muted-foreground">{{ entry.detail }}</p>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>
