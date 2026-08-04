<template>
  <div class="container-xxl py-6">
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('consultancyContract.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('consultancyContract.subtitle') }}</p>
      </div>
      <RouterLink
        v-if="hasPermission('consultancy_agreement:create')"
        to="/consultancy/agreements/new"
        class="btn btn-primary"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('consultancyContract.new') }}
      </RouterLink>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.status" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="draft">{{ $t('consultancyContract.draft') }}</option>
              <option value="sent">{{ $t('consultancyContract.sent') }}</option>
              <option value="signed">{{ $t('consultancyContract.signed') }}</option>
              <option value="expired">{{ $t('consultancyContract.expired') }}</option>
              <option value="cancelled">{{ $t('consultancyContract.cancelled') }}</option>
            </select>
          </div>
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('consultancyContract.agreementType') }}</label>
            <select v-model="filters.agreementTypeId" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option v-for="t in agreementTypes" :key="t.id" :value="t.id">{{ t.label || t.code }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <button class="btn btn-sm btn-light w-100" @click="resetFilters">{{ $t('common.cancel') }}</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="store.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="store.agreements.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-document fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('consultancyContract.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('consultancyContract.fields.title') }}</th>
                <th>{{ $t('consultancyContract.fields.institution') }}</th>
                <th>{{ $t('consultancyContract.fields.plan') }}</th>
                <th>{{ $t('consultancyContract.fields.type') }}</th>
                <th>{{ $t('consultancyContract.fields.startDate') }}</th>
                <th>{{ $t('consultancyContract.fields.endDate') }}</th>
                <th>{{ $t('consultancyContract.fields.signedDate') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="a in store.agreements.items" :key="a.id">
                <td class="ps-4 fw-semibold">{{ a.title }}</td>
                <td class="text-muted fs-7">{{ a.institutionName }}</td>
                <td class="text-muted fs-7">{{ a.planName }}</td>
                <td>{{ a.agreementTypeCode ?? '—' }}</td>
                <td class="text-muted fs-7">{{ a.startDate ?? '—' }}</td>
                <td class="text-muted fs-7">{{ a.endDate ?? '—' }}</td>
                <td class="text-muted fs-7">{{ a.signedDate ?? '—' }}</td>
                <td>
                  <span :class="statusBadge(a.status)">{{ statusLabel(a.status) }}</span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/consultancy/agreements/${a.id}`" class="btn btn-sm btn-light-primary">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="store.agreements.totalPages > 1" class="d-flex justify-content-end pt-4">
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light" :disabled="!store.agreements.hasPreviousPage" @click="changePage(filters.page! - 1)">{{ $t('common.back') }}</button>
            <span class="btn btn-sm btn-light-primary">{{ filters.page }} / {{ store.agreements.totalPages }}</span>
            <button class="btn btn-sm btn-light" :disabled="!store.agreements.hasNextPage" @click="changePage(filters.page! + 1)">{{ $t('common.next') }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, computed, onMounted } from 'vue'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { useI18n } from 'vue-i18n'
import type { AgreementListQuery } from '@/types/consultancy.types'

const { t } = useI18n()
const store = useConsultancyStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const filters = reactive<AgreementListQuery>({
  page: 1, pageSize: 20, status: '', agreementTypeId: '',
  corporationId: authStore.user?.corporationId,
})

const agreementTypes = computed(() => refDataStore.getByCategory?.('agreement_type') ?? [])
function hasPermission(p: string) { return authStore.hasPermission(p) }

function statusBadge(s: string) {
  const map: Record<string, string> = {
    draft: 'badge badge-light-secondary',
    sent: 'badge badge-light-primary',
    signed: 'badge badge-light-success',
    expired: 'badge badge-light-dark',
    cancelled: 'badge badge-light-danger',
  }
  return map[s] ?? 'badge badge-light'
}

function statusLabel(s: string) {
  const map: Record<string, string> = {
    draft: t('consultancyContract.draft'),
    sent: t('consultancyContract.sent'),
    signed: t('consultancyContract.signed'),
    expired: t('consultancyContract.expired'),
    cancelled: t('consultancyContract.cancelled'),
  }
  return map[s] ?? s
}

async function doFetch() {
  filters.page = 1
  await store.fetchAgreements(filters)
}

function resetFilters() {
  filters.status = ''
  filters.agreementTypeId = ''
  filters.page = 1
  doFetch()
}

function changePage(page: number) {
  filters.page = page
  store.fetchAgreements(filters)
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('agreement_type')
  await doFetch()
})
</script>
