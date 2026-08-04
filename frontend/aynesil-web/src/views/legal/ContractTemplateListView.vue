<template>
  <div class="container-xxl py-6">
    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('legal.template.contract.list.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('legal.template.contract.list.subtitle') }}</p>
      </div>
    </div>

    <div class="card">
      <div class="card-body py-3">
        <div v-if="contractStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="contractStore.templates.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('legal.template.contract.list.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('legal.template.fields.code') }}</th>
                <th>{{ $t('legal.template.fields.type') }}</th>
                <th class="text-center">{{ $t('legal.template.fields.version') }}</th>
                <th>{{ $t('legal.template.fields.effectiveFrom') }}</th>
                <th>{{ $t('legal.template.fields.current') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in contractStore.templates.items" :key="t.id">
                <td class="ps-4 fw-semibold font-monospace">{{ t.code }}</td>
                <td class="text-muted">{{ t.contractTypeCode ?? '—' }}</td>
                <td class="text-center">v{{ t.version }}</td>
                <td class="text-muted fs-7">{{ t.effectiveFrom ?? '—' }}</td>
                <td>
                  <span v-if="t.isCurrent" class="badge badge-light-success">{{ $t('legal.template.current') }}</span>
                  <span v-else class="badge badge-light-secondary">{{ $t('legal.template.historical') }}</span>
                </td>
                <td class="text-end pe-4">
                  <RouterLink :to="`/legal/contract-templates/${t.id}`" class="btn btn-sm btn-light-primary">
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
import { onMounted } from 'vue'
import { useContractStore } from '@/stores/contract.store'
import { useAuthStore } from '@/stores/auth.store'

const contractStore = useContractStore()
const authStore = useAuthStore()

onMounted(() => contractStore.fetchTemplates({ corporationId: authStore.user?.corporationId }))
</script>
