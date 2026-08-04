<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <button class="btn btn-sm btn-light" @click="router.back()">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </button>
    </div>

    <div class="d-flex align-items-center justify-content-between mb-6">
      <div>
        <h1 class="text-gray-900 fw-bold fs-2">{{ $t('camp.enrollment.title') }}</h1>
        <p class="text-muted mb-0">{{ $t('camp.enrollment.subtitle') }}</p>
      </div>
      <button
        v-if="hasPermission('camp_enrollment:enroll')"
        class="btn btn-primary"
        @click="showEnrollModal = true"
      >
        <i class="ki-outline ki-plus fs-2 me-1"></i>{{ $t('camp.enrollment.enroll') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div v-if="campStore.enrollmentSummary" class="row g-5 mb-6">
      <div class="col-sm-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-primary">{{ campStore.enrollmentSummary.totalEnrolled }}</div>
            <div class="text-muted fs-7">{{ $t('camp.enrollment.enrolled') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-warning">{{ campStore.enrollmentSummary.totalWaitlist }}</div>
            <div class="text-muted fs-7">{{ $t('camp.enrollment.waitlist') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-success">{{ campStore.enrollmentSummary.totalCompleted }}</div>
            <div class="text-muted fs-7">{{ $t('camp.enrollment.completed') }}</div>
          </div>
        </div>
      </div>
      <div class="col-sm-3">
        <div class="card card-flush h-100">
          <div class="card-body text-center py-5">
            <div class="fs-2 fw-bold text-danger">{{ campStore.enrollmentSummary.totalWithdrawn }}</div>
            <div class="text-muted fs-7">{{ $t('camp.enrollment.withdrawn') }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Enrollment Table -->
    <div class="card">
      <div class="card-body py-3">
        <div v-if="campStore.loading" class="text-center py-15">
          <div class="spinner-border text-primary"></div>
        </div>
        <div v-else-if="campStore.enrollments.items.length === 0" class="text-center py-15 text-muted">
          {{ $t('camp.enrollment.noData') }}
        </div>
        <div v-else class="table-responsive">
          <table class="table table-row-dashed align-middle gs-0 gy-4">
            <thead>
              <tr class="fw-bold text-muted bg-light">
                <th class="ps-4">{{ $t('camp.fields.student') }}</th>
                <th>{{ $t('common.status') }}</th>
                <th>{{ $t('camp.enrollment.enrolledAt') }}</th>
                <th class="text-end pe-4">{{ $t('common.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="e in campStore.enrollments.items" :key="e.id">
                <td class="ps-4 fw-semibold">{{ e.studentId }}</td>
                <td>
                  <span :class="enrollmentStatusBadge(e.status)">{{ e.status }}</span>
                </td>
                <td class="text-muted fs-7">{{ formatDate(e.enrolledAt) }}</td>
                <td class="text-end pe-4">
                  <button
                    v-if="e.status === 'Waitlist' && hasPermission('camp_enrollment:manage')"
                    class="btn btn-sm btn-light-success me-2"
                    @click="campStore.promoteFromWaitlist(e.id, periodId)"
                  >{{ $t('camp.enrollment.promote') }}</button>
                  <button
                    v-if="e.status === 'Enrolled' && hasPermission('camp_enrollment:withdraw')"
                    class="btn btn-sm btn-light-danger"
                    @click="campStore.withdraw(e.id, periodId)"
                  >{{ $t('camp.enrollment.withdraw') }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Enroll Modal -->
    <div v-if="showEnrollModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('camp.enrollment.enroll') }}</h5>
            <button class="btn-close" @click="showEnrollModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label required">{{ $t('camp.fields.student') }} ID</label>
            <input v-model="enrollStudentId" type="text" class="form-control" />
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showEnrollModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="campStore.saving || !enrollStudentId" @click="doEnroll">
              <span v-if="campStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('camp.enrollment.enroll') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCampStore } from '@/stores/camp.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const router = useRouter()
const campStore = useCampStore()
const authStore = useAuthStore()
const periodId = route.params.periodId as string
const showEnrollModal = ref(false)
const enrollStudentId = ref('')

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleDateString('tr-TR') }

function enrollmentStatusBadge(status: string) {
  const map: Record<string, string> = {
    Enrolled: 'badge badge-light-success', Waitlist: 'badge badge-light-warning',
    Completed: 'badge badge-light-primary', Withdrawn: 'badge badge-light-danger',
  }
  return map[status] ?? 'badge badge-light'
}

async function doEnroll() {
  await campStore.enrollStudent(periodId, { studentId: enrollStudentId.value })
  showEnrollModal.value = false
  enrollStudentId.value = ''
}

onMounted(async () => {
  await Promise.all([
    campStore.fetchEnrollments(periodId),
    campStore.fetchEnrollmentSummary(periodId),
  ])
})
</script>
