<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { ParentFeedbackListItemDto, ParentFeedbackListQuery } from '@/types/survey.types'

const { t } = useI18n()
const router = useRouter()
const store = useSurveyStore()
const { visible } = useModule(Modules.Surveys)

const filters = reactive<ParentFeedbackListQuery>({ page: 1, pageSize: 20, sortDirection: 'desc' })

const columns: Column<ParentFeedbackListItemDto>[] = [
  { key: 'createdAt', label: t('common.date'), width: '150px' },
  { key: 'guardianName', label: t('surveys.feedback.guardian'), width: '180px' },
  { key: 'educatorName', label: t('surveys.feedback.educator'), width: '180px' },
  { key: 'rating', label: t('surveys.feedback.rating'), width: '130px' },
  { key: 'comment', label: t('common.comment') },
]

const stars = (n?: number) => (n ? '★'.repeat(n) + '☆'.repeat(5 - n) : '—')

watch(() => filters.page, () => store.fetchFeedback(filters))
onMounted(() => store.fetchFeedback(filters))
</script>

<template>
  <div>
    <PageHeader :title="t('surveys.feedback.title')" :description="t('surveys.feedback.subtitle')">
      <button
        v-if="visible(Permissions.Surveys.Read)" type="button"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        @click="router.push({ name: 'surveys' })"
      >
        {{ t('surveys.list.title') }}
      </button>
    </PageHeader>

    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <DataTable :columns="columns" :rows="store.feedback.items" :loading="store.loading" :empty-text="t('surveys.feedback.noData')">
      <template #cell-createdAt="{ value }">{{ new Date(String(value)).toLocaleString('tr-TR') }}</template>
      <template #cell-guardianName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-educatorName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-rating="{ value }"><span class="text-amber-500 tracking-wider">{{ stars(value as number | undefined) }}</span></template>
      <template #cell-comment="{ value }">{{ value ?? '—' }}</template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.feedback.page" :page-size="store.feedback.pageSize" :total-count="store.feedback.totalCount"
        :total-pages="store.feedback.totalPages" :has-previous-page="store.feedback.hasPreviousPage" :has-next-page="store.feedback.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; filters.page = 1; store.fetchFeedback(filters) }"
      />
    </div>
  </div>
</template>
