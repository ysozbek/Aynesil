<script setup lang="ts">
/**
 * Benim öğrencilerim: giriş yapan eğitmenin bakım ekibinde olduğu öğrenciler (GET care-team/my-students).
 * Eğitmen, hesabı bir eğitmen kaydına bağlı olduğunda belirlenir; bağlı değilse liste boş döner.
 */
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { careTeamService } from '@/services/careTeam.service'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { CareTeamStudentListItemDto } from '@/types/careTeam.types'

const { t } = useI18n()
const router = useRouter()
const refData = useRefDataStore()

const rows = ref<CareTeamStudentListItemDto[]>([])
const roles = ref<RefValueItem[]>([])
const loading = ref(false)
const error = ref('')

const roleLabel = (id: string, code?: string | null) => roles.value.find((r) => r.id === id)?.label ?? code ?? '—'
const formatDate = (val?: string | null) => (val ? new Date(`${val}T00:00:00`).toLocaleDateString('tr-TR') : '…')

onMounted(async () => {
  loading.value = true
  try {
    roles.value = await refData.getValues('care_team_role')
    const res = await careTeamService.myStudents()
    rows.value = res.success && res.data ? res.data : []
  } catch (e: unknown) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <PageHeader :title="t('careTeam.my.title')" :description="t('careTeam.my.description')" />

    <p v-if="error" class="mb-3 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ error }}</p>
    <div v-if="loading" class="py-8 text-center text-sm text-muted-foreground">{{ t('common.loading') }}</div>
    <div v-else-if="!rows.length && !error" class="text-center py-12 text-muted-foreground">
      <p>{{ t('careTeam.my.empty') }}</p>
      <p class="mt-2 text-xs">{{ t('careTeam.my.notLinked') }}</p>
    </div>
    <div v-else class="space-y-3">
      <div
        v-for="r in rows"
        :key="r.assignmentId"
        class="rounded-xl border border-border bg-card p-4 shadow-sm flex items-center justify-between gap-4"
      >
        <div>
          <button
            class="font-medium text-foreground hover:underline text-left"
            @click="router.push({ name: 'student-detail', params: { id: r.studentId } })"
          >
            {{ r.studentFullName }}
          </button>
          <span
            v-if="r.isPrimary"
            class="ml-2 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700"
          >{{ t('careTeam.primary') }}</span>
          <StatusBadge entity="careAssignment" :status="r.status" class="ml-2" />
          <p class="text-xs text-muted-foreground mt-1">
            {{ roleLabel(r.roleId, r.roleCode) }} · {{ formatDate(r.activeFrom) }} – {{ formatDate(r.activeTo) }}
          </p>
        </div>
        <button
          class="px-3 py-1.5 text-xs rounded-lg border border-border hover:bg-accent shrink-0"
          @click="router.push({ name: 'student-dashboard', params: { id: r.studentId } })"
        >
          {{ t('student.tab.overview') }}
        </button>
      </div>
    </div>
  </div>
</template>
