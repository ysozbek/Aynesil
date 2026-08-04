<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/camps" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div v-if="campStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else-if="!camp" class="text-center py-20 text-muted">{{ $t('errors.notFound') }}</div>

    <div v-else>
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ camp.name }}</h1>
          <p class="text-muted mb-0">{{ camp.code }} · {{ camp.location ?? '—' }}</p>
        </div>
        <div class="d-flex gap-2">
          <span :class="camp.isActive ? 'badge badge-light-success fs-7' : 'badge badge-light-danger fs-7'">
            {{ camp.isActive ? $t('common.active') : $t('common.passive') }}
          </span>
          <RouterLink v-if="hasPermission('camp:update')" :to="`/camps/${camp.id}/edit`" class="btn btn-sm btn-light">
            <i class="ki-outline ki-pencil fs-4 me-1"></i>{{ $t('common.edit') }}
          </RouterLink>
        </div>
      </div>

      <!-- Info Cards -->
      <div class="row g-5 mb-6">
        <div class="col-xl-4">
          <div class="card h-100">
            <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('camp.detail.info') }}</h3></div>
            <div class="card-body pt-0">
              <div class="mb-3"><span class="text-muted fs-7">{{ $t('camp.fields.type') }}:</span> <span class="fw-semibold ms-2">{{ camp.campTypeCode ?? '—' }}</span></div>
              <div class="mb-3"><span class="text-muted fs-7">{{ $t('camp.fields.capacity') }}:</span> <span class="fw-semibold ms-2">{{ camp.capacity ?? '—' }}</span></div>
              <div class="mb-3"><span class="text-muted fs-7">{{ $t('camp.fields.location') }}:</span> <span class="fw-semibold ms-2">{{ camp.location ?? '—' }}</span></div>
              <div v-if="camp.description" class="mt-4">
                <div class="text-muted fs-7 mb-1">{{ $t('camp.fields.description') }}</div>
                <div class="text-gray-700">{{ camp.description }}</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-xl-8">
          <!-- Periods -->
          <div class="card h-100">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('camp.detail.periods') }}</h3>
              <div class="card-toolbar">
                <button
                  v-if="hasPermission('camp:update')"
                  class="btn btn-sm btn-light-primary"
                  @click="showPeriodModal = true"
                >
                  <i class="ki-outline ki-plus fs-4 me-1"></i>{{ $t('camp.detail.addPeriod') }}
                </button>
              </div>
            </div>
            <div class="card-body pt-0">
              <div v-if="camp.periods.length === 0" class="text-center py-8 text-muted">{{ $t('camp.detail.noPeriods') }}</div>
              <div v-else class="table-responsive">
                <table class="table table-row-dashed align-middle gs-0 gy-3">
                  <thead>
                    <tr class="fw-bold text-muted bg-light">
                      <th class="ps-3">{{ $t('camp.fields.periodName') }}</th>
                      <th>{{ $t('camp.fields.startDate') }}</th>
                      <th>{{ $t('camp.fields.endDate') }}</th>
                      <th class="text-center">{{ $t('camp.fields.capacity') }}</th>
                      <th class="text-center">{{ $t('camp.fields.enrolled') }}</th>
                      <th class="text-center">{{ $t('camp.fields.waitlist') }}</th>
                      <th class="text-end pe-3">{{ $t('common.actions') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="p in camp.periods" :key="p.id">
                      <td class="ps-3 fw-semibold">{{ p.name }}</td>
                      <td class="text-muted fs-7">{{ p.startDate }}</td>
                      <td class="text-muted fs-7">{{ p.endDate }}</td>
                      <td class="text-center">{{ p.capacity ?? '∞' }}</td>
                      <td class="text-center text-success fw-bold">{{ p.enrolledCount }}</td>
                      <td class="text-center text-warning">{{ p.waitlistCount }}</td>
                      <td class="text-end pe-3">
                        <RouterLink :to="`/camps/periods/${p.id}`" class="btn btn-sm btn-light-primary">
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
      </div>

      <!-- Educators -->
      <div class="card mb-6">
        <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('camp.detail.educators') }}</h3></div>
        <div class="card-body pt-0">
          <div v-if="campStore.educators.length === 0" class="text-muted text-center py-6">{{ $t('camp.detail.noEducators') }}</div>
          <div v-else class="d-flex flex-wrap gap-4">
            <div v-for="e in campStore.educators" :key="e.id" class="d-flex align-items-center p-3 rounded bg-light">
              <div class="symbol symbol-40px me-3">
                <span class="symbol-label bg-light-primary">
                  <i class="ki-outline ki-user fs-3 text-primary"></i>
                </span>
              </div>
              <div>
                <div class="fw-semibold">{{ e.educatorId }}</div>
                <div class="text-muted fs-8">{{ e.role }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Period Modal -->
    <div v-if="showPeriodModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('camp.detail.addPeriod') }}</h5>
            <button class="btn-close" @click="showPeriodModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="row g-3">
              <div class="col-12">
                <label class="form-label required">{{ $t('camp.fields.periodName') }}</label>
                <input v-model="periodForm.name" type="text" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label required">{{ $t('camp.fields.startDate') }}</label>
                <input v-model="periodForm.startDate" type="date" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label required">{{ $t('camp.fields.endDate') }}</label>
                <input v-model="periodForm.endDate" type="date" class="form-control" />
              </div>
              <div class="col-6">
                <label class="form-label">{{ $t('camp.fields.capacity') }}</label>
                <input v-model.number="periodForm.capacity" type="number" class="form-control" />
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showPeriodModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="campStore.saving" @click="doCreatePeriod">
              <span v-if="campStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useCampStore } from '@/stores/camp.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const campStore = useCampStore()
const authStore = useAuthStore()
const id = route.params.id as string
const camp = computed(() => campStore.currentCamp)
const showPeriodModal = ref(false)
const periodForm = reactive({ name: '', startDate: '', endDate: '', capacity: undefined as number | undefined })

function hasPermission(p: string) { return authStore.hasPermission(p) }

async function doCreatePeriod() {
  if (!camp.value) return
  await campStore.createPeriod(camp.value.id, periodForm)
  showPeriodModal.value = false
  Object.assign(periodForm, { name: '', startDate: '', endDate: '', capacity: undefined })
}

onMounted(async () => {
  campStore.clearCurrent()
  await campStore.fetchCamp(id)
  if (campStore.currentCamp) {
    await campStore.fetchEducators(id)
  }
})
</script>
