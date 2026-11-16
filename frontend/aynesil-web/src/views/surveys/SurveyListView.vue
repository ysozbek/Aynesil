<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { useModule } from '@/composables/useModule'
import { Modules } from '@/constants/modules'
import { Permissions } from '@/constants/permissions'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { SurveyListItemDto, SurveyListQuery } from '@/types/survey.types'

const { t } = useI18n()
const router = useRouter()
const store = useSurveyStore()
const refData = useRefDataStore()
const { visible } = useModule(Modules.Surveys)

const filters = reactive<SurveyListQuery>({ page: 1, pageSize: 20, search: '', target: '', isActive: '' })
const typeLabels = ref<Record<string, string>>({})
const actionError = ref('')

const columns: Column<SurveyListItemDto>[] = [
  { key: 'title', label: t('surveys.fields.title') },
  { key: 'typeId', label: t('surveys.fields.type'), width: '170px' },
  { key: 'target', label: t('surveys.fields.target'), width: '120px' },
  { key: 'isActive', label: t('common.status'), width: '100px' },
  { key: 'questionCount', label: t('surveys.fields.questionCount'), width: '90px' },
  { key: 'responseCount', label: t('surveys.fields.responseCount'), width: '90px' },
  { key: 'updatedAt', label: t('common.updatedAt'), width: '120px' },
]

function fetch(resetPage = false) {
  if (resetPage) filters.page = 1
  const q: SurveyListQuery = { ...filters }
  if (q.isActive === '') delete q.isActive
  return store.fetchSurveys(q)
}

let debounceTimer: ReturnType<typeof setTimeout>
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => fetch(true), 400)
}

watch(() => filters.page, () => fetch())

onMounted(async () => {
  const types = await refData.getValues('survey_type', false)
  typeLabels.value = Object.fromEntries(types.map((v) => [v.id, v.label || v.code]))
  await fetch()
})

// ── Delete ────────────────────────────────────────────────────────────────────
const deleteTarget = ref<SurveyListItemDto | null>(null)

async function doDelete() {
  if (!deleteTarget.value) return
  actionError.value = ''
  try {
    await store.remove(deleteTarget.value.id)
    deleteTarget.value = null
    await fetch()
  } catch (e: unknown) {
    actionError.value = (e as Error).message
    deleteTarget.value = null
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('surveys.list.title')" :description="t('surveys.list.subtitle')">
      <button
        v-if="visible(Permissions.ParentFeedback.Read)"
        type="button"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        @click="router.push({ name: 'survey-feedback' })"
      >
        {{ t('surveys.feedback.title') }}
      </button>
      <button
        v-if="visible(Permissions.Surveys.Create)"
        type="button"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
        @click="router.push({ name: 'survey-new' })"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('surveys.new') }}
      </button>
    </PageHeader>

    <p v-if="actionError" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ actionError }}</p>
    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div class="flex-1 min-w-[160px]">
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.search') }}</label>
        <input
          v-model="filters.search"
          type="text"
          class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          @input="debouncedFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('surveys.fields.target') }}</label>
        <select v-model="filters.target" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="fetch(true)">
          <option value="">{{ t('common.all') }}</option>
          <option v-for="tg in ['guardian', 'educator', 'student']" :key="tg" :value="tg">{{ t(`surveys.target.${tg}`) }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('common.status') }}</label>
        <select v-model="filters.isActive" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent" @change="fetch(true)">
          <option value="">{{ t('common.allStatuses') }}</option>
          <option :value="true">{{ t('common.active') }}</option>
          <option :value="false">{{ t('common.passive') }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.surveys.items"
      :loading="store.loading"
      :empty-text="t('surveys.list.noData')"
      @row-click="(row) => router.push({ name: 'survey-results', params: { id: row.id } })"
    >
      <template #cell-typeId="{ value }">
        <span class="text-muted-foreground">{{ value ? (typeLabels[String(value)] ?? '—') : '—' }}</span>
      </template>
      <template #cell-target="{ value }">{{ t(`surveys.target.${String(value)}`) }}</template>
      <template #cell-isActive="{ value }">
        <StatusBadge :value="Boolean(value)" :true-label="t('common.active')" :false-label="t('common.passive')" />
      </template>
      <template #cell-updatedAt="{ value }">{{ new Date(String(value)).toLocaleDateString('tr-TR') }}</template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-2">
          <button type="button" class="text-xs text-primary hover:underline" @click.stop="router.push({ name: 'survey-results', params: { id: row.id } })">
            {{ t('surveys.results.open') }}
          </button>
          <button
            v-if="visible(Permissions.Surveys.Update)"
            type="button" class="text-xs text-primary hover:underline"
            @click.stop="router.push({ name: 'survey-edit', params: { id: row.id } })"
          >
            {{ t('common.edit') }}
          </button>
          <button
            v-if="visible(Permissions.Surveys.Delete)"
            type="button" class="text-xs text-red-600 hover:underline"
            @click.stop="deleteTarget = row"
          >
            {{ t('common.delete') }}
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.surveys.page"
        :page-size="store.surveys.pageSize"
        :total-count="store.surveys.totalCount"
        :total-pages="store.surveys.totalPages"
        :has-previous-page="store.surveys.hasPreviousPage"
        :has-next-page="store.surveys.hasNextPage"
        @update:page="(p) => { filters.page = p }"
        @update:page-size="(s) => { filters.pageSize = s; fetch(true) }"
      />
    </div>

    <ConfirmModal
      :open="!!deleteTarget"
      :title="t('surveys.delete.title')"
      :message="deleteTarget && deleteTarget.responseCount > 0
        ? t('surveys.delete.messageWithResponses', { name: deleteTarget.title })
        : t('surveys.delete.message', { name: deleteTarget?.title ?? '' })"
      :confirm-label="t('common.delete')"
      :loading="store.saving"
      @confirm="doDelete"
      @cancel="deleteTarget = null"
    />
  </div>
</template>
