<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import { surveyService } from '@/services/survey.service'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import type { PaginatedResult } from '@/types/api.types'
import type { SurveyQuestionResultDto, SurveyResponseListItemDto } from '@/types/survey.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useSurveyStore()
const { visible } = useModule(Modules.Surveys)
const id = route.params.id as string

const responses = ref<PaginatedResult<SurveyResponseListItemDto> | null>(null)
const responseError = ref('')
const query = reactive({ page: 1, pageSize: 10 })

const columns: Column<SurveyResponseListItemDto>[] = [
  { key: 'guardianName', label: t('surveys.results.guardian') },
  { key: 'studentName', label: t('surveys.results.student') },
  { key: 'submittedAt', label: t('surveys.results.submittedAt'), width: '170px' },
]

async function fetchResponses() {
  responseError.value = ''
  try {
    const res = await surveyService.listResponses({ surveyId: id, isSubmitted: true, page: query.page, pageSize: query.pageSize })
    if (res.success && res.data) responses.value = res.data
  } catch (e: unknown) { responseError.value = (e as Error).message }
}

watch(() => query.page, fetchResponses)

onMounted(async () => {
  await Promise.all([store.fetchResults(id), fetchResponses()])
})

function pct(q: SurveyQuestionResultDto, count: number) {
  const max = Math.max(1, ...q.distribution.map((b) => b.count))
  return Math.round((count / max) * 100)
}

function bucketLabel(q: SurveyQuestionResultDto, label: string) {
  return q.questionType === 'yes_no' ? t(`common.${label}`) : label
}

function dt(v?: string) {
  return v ? new Date(v).toLocaleString('tr-TR') : '—'
}
</script>

<template>
  <div>
    <PageHeader :title="store.results?.title ?? t('surveys.results.title')" :description="t('surveys.results.subtitle')">
      <button type="button" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'surveys' })">
        {{ t('common.back') }}
      </button>
      <button
        v-if="visible(Permissions.Surveys.Update)" type="button"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        @click="router.push({ name: 'survey-edit', params: { id } })"
      >
        {{ t('common.edit') }}
      </button>
    </PageHeader>

    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <template v-if="store.results">
      <div class="mb-6 grid grid-cols-2 md:grid-cols-3 gap-4">
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground">{{ t('surveys.results.responseCount') }}</p>
          <p class="text-2xl font-bold">{{ store.results.responseCount }}</p>
        </div>
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground">{{ t('surveys.results.lastResponse') }}</p>
          <p class="text-sm font-medium mt-2">{{ dt(store.results.lastSubmittedAt) }}</p>
        </div>
        <div class="rounded-xl border border-border bg-card p-4">
          <p class="text-xs text-muted-foreground">{{ t('surveys.fields.target') }}</p>
          <p class="text-sm font-medium mt-2">{{ t(`surveys.target.${store.results.target}`) }}</p>
        </div>
      </div>

      <p v-if="store.results.responseCount === 0" class="mb-6 text-sm text-muted-foreground">{{ t('surveys.results.noResponses') }}</p>

      <div class="space-y-4 mb-8">
        <section v-for="(q, qi) in store.results.questions" :key="q.questionId" class="rounded-xl border border-border bg-card shadow-sm p-5">
          <div class="flex items-start justify-between gap-4">
            <h3 class="text-sm font-semibold text-foreground">{{ qi + 1 }}. {{ q.questionText }}</h3>
            <span class="text-xs text-muted-foreground shrink-0">
              {{ t(`surveys.questionType.${q.questionType}`) }} · {{ t('surveys.results.answered', { count: q.answeredCount }) }}
            </span>
          </div>

          <p v-if="q.average != null" class="mt-2 text-sm">
            {{ t('surveys.results.average') }}: <span class="font-semibold">{{ q.average }}</span>
          </p>

          <ul v-if="q.distribution.length" class="mt-3 space-y-2">
            <li v-for="b in q.distribution" :key="b.label" class="flex items-center gap-3 text-sm">
              <span class="w-40 truncate text-foreground">{{ bucketLabel(q, b.label) }}</span>
              <div class="flex-1 h-2.5 rounded-full bg-muted overflow-hidden">
                <div class="h-full rounded-full bg-primary" :style="{ width: pct(q, b.count) + '%' }" />
              </div>
              <span class="w-10 text-right tabular-nums text-muted-foreground">{{ b.count }}</span>
            </li>
          </ul>

          <ul v-if="q.questionType === 'text'" class="mt-3 space-y-2">
            <li v-for="(a, i) in q.textAnswers" :key="i" class="text-sm rounded-lg bg-muted/50 px-3 py-2">{{ a }}</li>
            <li v-if="!q.textAnswers.length" class="text-sm text-muted-foreground">{{ t('surveys.results.noText') }}</li>
          </ul>
        </section>
      </div>
    </template>

    <h2 class="text-base font-semibold mb-3">{{ t('surveys.results.responses') }}</h2>
    <p v-if="responseError" class="mb-3 text-sm text-red-600">{{ responseError }}</p>
    <DataTable :columns="columns" :rows="responses?.items ?? []" :loading="false" :empty-text="t('surveys.results.noResponses')">
      <template #cell-guardianName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-studentName="{ value }">{{ value ?? '—' }}</template>
      <template #cell-submittedAt="{ value }">{{ dt(value ? String(value) : undefined) }}</template>
    </DataTable>
    <div v-if="responses" class="mt-4">
      <Pagination
        :page="responses.page" :page-size="responses.pageSize" :total-count="responses.totalCount"
        :total-pages="responses.totalPages" :has-previous-page="responses.hasPreviousPage" :has-next-page="responses.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1; fetchResponses() }"
      />
    </div>
  </div>
</template>
