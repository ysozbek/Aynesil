<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.consent.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('legal.consent.list.subtitle') }}</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="card mb-6">
      <div class="card-body py-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-3">
            <label class="form-label fs-7">{{ $t('common.status') }}</label>
            <select v-model="filters.state" class="form-select form-select-sm" @change="doFetch">
              <option value="">{{ $t('common.allStatuses') }}</option>
              <option value="Granted">{{ $t('legal.consent.state.granted') }}</option>
              <option value="Withdrawn">{{ $t('legal.consent.state.withdrawn') }}</option>
              <option value="Pending">{{ $t('legal.consent.state.pending') }}</option>
            </select>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="consentStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="consentStore.consents.items.length === 0" class="text-center py-15 text-muted">
          <i class="ki-outline ki-shield-tick fs-3x mb-4 d-block text-gray-300"></i>
          {{ $t('legal.consent.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.consent.fields.student') }}</th>
                <th>{{ $t('legal.consent.fields.consentType') }}</th>
                <th>{{ $t('legal.consent.fields.template') }}</th>
                <th>{{ $t('legal.consent.fields.grantedAt') }}</th>
                <th>{{ $t('legal.consent.fields.validUntil') }}</th>
                <th>{{ $t('legal.consent.fields.mandatory') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in consentStore.consents.items" :key="c.id">
                <td class="ps-4 fw-semibold">{{ c.studentFullName ?? '—' }}</td>
                <td class="text-muted">{{ c.consentTypeCode ?? '—' }}</td>
                <td class="text-muted fs-7">{{ c.templateCode ? `${c.templateCode} v${c.templateVersion}` : '—' }}</td>
                <td class="text-muted fs-7">{{ c.grantedAt ? formatDate(c.grantedAt) : '—' }}</td>
                <td class="text-muted fs-7">{{ c.validUntil ?? '—' }}</td>
                <td>
                  <i v-if="false" class="ki-outline ki-check fs-4 text-success"></i>
                </td>
                <td><span :class="stateBadge(c.state)">{{ c.state }}</span></td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/legal/consents/${c.id}`" class="btn btn-sm btn-light-primary">
                    <i class="ki-outline ki-eye fs-4"></i>
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue'
import { useConsentStore } from '@/stores/consent.store'
import { useAuthStore } from '@/stores/auth.store'
import type { ConsentListQuery } from '@/types/legal.types'

const consentStore = useConsentStore()
const authStore = useAuthStore()

const filters = reactive<ConsentListQuery>({
  page: 1, pageSize: 20, state: '',
  corporationId: authStore.user?.corporationId,
})

function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

function stateBadge(s: string) {
  const map: Record<string, string> = {
    Granted: 'badge badge-light-success', Withdrawn: 'badge badge-light-danger', Pending: 'badge badge-light-warning',
  }
  return map[s] ?? 'badge badge-light'
}

async function doFetch() { filters.page = 1; await consentStore.fetchConsents(filters) }

onMounted(doFetch)
</script>
