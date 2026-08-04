<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { usePackageStore } from '@/stores/package.store'
import { usePermission } from '@/composables/usePermission'
import DataTable from '@/components/shared/DataTable.vue'
import Pagination from '@/components/shared/Pagination.vue'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { StudentPackageListItemDto } from '@/types/finance.types'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const store = usePackageStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
  studentId: '',
  status: '',
  page: 1,
  pageSize: 20,
})

watch(() => [query.status, query.studentId, query.page], () => loadList())
onMounted(() => loadList())

async function loadList() {
  await store.fetchStudentPackages({
    ...query,
    corporationId: corporationId.value,
    studentId: query.studentId || undefined,
    status: query.status || undefined,
  })
}

const columns: Column<StudentPackageListItemDto>[] = [
  { key: 'studentFullName', label: t('student.fullName') },
  { key: 'packageName', label: t('finance.package.name') },
  { key: 'purchasedOn', label: t('finance.studentPackage.purchasedOn'), width: '120px' },
  { key: 'expiresOn', label: t('finance.studentPackage.expiresOn'), width: '120px' },
  { key: 'totalCredits', label: t('finance.studentPackage.totalCredits'), width: '90px', align: 'center' },
  { key: 'remainingCredits', label: t('finance.studentPackage.remainingCredits'), width: '100px', align: 'center' },
  { key: 'status', label: t('common.status'), width: '100px' },
]

function formatDate(val: unknown): string {
  if (!val) return '—'
  return new Date(String(val)).toLocaleDateString('tr-TR')
}

function statusColor(status: string): string {
  const map: Record<string, string> = {
    active: 'bg-green-100 text-green-700',
    expired: 'bg-gray-100 text-gray-600',
    cancelled: 'bg-red-100 text-red-700',
    exhausted: 'bg-amber-100 text-amber-700',
  }
  return map[status] ?? 'bg-gray-100 text-gray-600'
}

const cancelTarget = ref<StudentPackageListItemDto | null>(null)
const cancelLoading = ref(false)

async function doCancel() {
  if (!cancelTarget.value) return
  cancelLoading.value = true
  try {
    await store.cancelStudentPackage(cancelTarget.value.id)
    cancelTarget.value = null
    await loadList()
  } finally {
    cancelLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader :title="t('finance.studentPackage.title')" :description="t('finance.studentPackage.description')">
      <button
        v-if="can('student_package:create')"
        @click="router.push({ name: 'student-package-new' })"
        class="flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:opacity-90"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        {{ t('finance.studentPackage.assign') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <input
        v-model="query.studentId"
        type="text"
        :placeholder="t('student.fullName') + ' ID'"
        @input="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
      />
      <select v-model="query.status" @change="query.page = 1" class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allStatuses') }}</option>
        <option value="active">{{ t('finance.studentPackage.status.active') }}</option>
        <option value="expired">{{ t('finance.studentPackage.status.expired') }}</option>
        <option value="cancelled">{{ t('finance.studentPackage.status.cancelled') }}</option>
        <option value="exhausted">{{ t('finance.studentPackage.status.exhausted') }}</option>
      </select>
    </div>

    <DataTable
      :columns="columns"
      :rows="store.studentPackageList.items"
      :loading="store.loading"
      @row-click="(row) => router.push({ name: 'student-package-detail', params: { id: row.id } })"
    >
      <template #cell-purchasedOn="{ value }">{{ formatDate(value) }}</template>
      <template #cell-expiresOn="{ value }">
        <span :class="value && new Date(String(value)) < new Date() ? 'text-red-600' : ''">{{ formatDate(value) }}</span>
      </template>
      <template #cell-remainingCredits="{ row }">
        <div class="flex items-center gap-2">
          <div class="w-20 bg-gray-200 rounded-full h-1.5">
            <div
              class="bg-primary h-1.5 rounded-full"
              :style="{ width: `${row.totalCredits > 0 ? (row.remainingCredits / row.totalCredits) * 100 : 0}%` }"
            />
          </div>
          <span class="text-xs font-mono">{{ row.remainingCredits }}</span>
        </div>
      </template>
      <template #cell-status="{ row }">
        <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusColor(row.status)]">
          {{ t(`finance.studentPackage.status.${row.status}`) }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center justify-end gap-1" @click.stop>
          <button
            @click="router.push({ name: 'student-package-detail', params: { id: row.id } })"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('common.view')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zM2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
          <button
            v-if="can('student_package:update') && row.status === 'active'"
            @click="cancelTarget = row"
            class="p-1.5 rounded-lg hover:bg-red-50 text-muted-foreground hover:text-red-600"
            :title="t('finance.studentPackage.cancel')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <div class="mt-4">
      <Pagination
        :page="store.studentPackageList.page"
        :page-size="store.studentPackageList.pageSize"
        :total-count="store.studentPackageList.totalCount"
        :total-pages="store.studentPackageList.totalPages"
        :has-previous-page="store.studentPackageList.hasPreviousPage"
        :has-next-page="store.studentPackageList.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>

    <ConfirmModal
      :open="!!cancelTarget"
      :title="t('finance.studentPackage.cancelTitle')"
      :message="t('finance.studentPackage.cancelMessage', { name: cancelTarget?.packageName })"
      :confirm-label="t('finance.studentPackage.cancel')"
      :loading="cancelLoading"
      @confirm="doCancel"
      @cancel="cancelTarget = null"
    />
  </div>
</template>
