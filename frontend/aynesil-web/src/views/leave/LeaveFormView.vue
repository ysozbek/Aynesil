<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/leave/requests" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">
          {{ isEdit ? $t('leave.form.editTitle') : $t('leave.form.newTitle') }}
        </h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit">
          <div class="row g-4">
            <!-- Leave Type -->
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('leave.fields.leaveType') }}</label>
              <select v-model="form.leaveTypeId" class="form-select" required>
                <option value="">{{ $t('common.select') }}</option>
                <option
                  v-for="lt in leaveTypes"
                  :key="lt.id"
                  :value="lt.id"
                >{{ lt.label || lt.code }}</option>
              </select>
            </div>
            <!-- Unit -->
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('leave.fields.unit') }}</label>
              <select v-model="form.unit" class="form-select" required>
                <option value="Day">{{ $t('leave.unit.day') }}</option>
                <option value="Hour">{{ $t('leave.unit.hour') }}</option>
              </select>
            </div>
            <!-- Starts At -->
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('leave.fields.startsAt') }}</label>
              <input
                v-model="form.startsAt"
                type="datetime-local"
                class="form-control"
                required
              />
            </div>
            <!-- Ends At -->
            <div class="col-sm-6">
              <label class="form-label required">{{ $t('leave.fields.endsAt') }}</label>
              <input
                v-model="form.endsAt"
                type="datetime-local"
                class="form-control"
                required
              />
            </div>
            <!-- Quantity (hourly) -->
            <div v-if="form.unit === 'Hour'" class="col-sm-6">
              <label class="form-label">{{ $t('leave.fields.quantity') }}</label>
              <input
                v-model.number="form.quantity"
                type="number"
                step="0.5"
                min="0.5"
                class="form-control"
              />
            </div>
            <!-- Reason -->
            <div class="col-12">
              <label class="form-label">{{ $t('leave.fields.reason') }}</label>
              <textarea v-model="form.reason" class="form-control" rows="4"></textarea>
            </div>
          </div>

          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>

          <div class="d-flex justify-content-end gap-3 mt-6">
            <RouterLink to="/leave/requests" class="btn btn-light">
              {{ $t('common.cancel') }}
            </RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="leaveStore.saving">
              <span v-if="leaveStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore } from '@/stores/refdata.store'

const route = useRoute()
const router = useRouter()
const leaveStore = useLeaveStore()
const authStore = useAuthStore()
const refDataStore = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')

const form = reactive({
  leaveTypeId: '',
  unit: 'Day' as 'Day' | 'Hour',
  startsAt: '',
  endsAt: '',
  quantity: undefined as number | undefined,
  reason: '',
})

const leaveTypes = computed(() =>
  refDataStore.getByCategory?.('leave_type') ?? []
)

async function handleSubmit() {
  errorMsg.value = ''
  const corp = authStore.user?.corporationId ?? ''
  try {
    if (isEdit.value && id) {
      const current = leaveStore.currentLeave
      if (!current) return
      await leaveStore.updateLeave(id, {
        leaveTypeId: form.leaveTypeId || undefined,
        unit: form.unit,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        quantity: form.quantity,
        reason: form.reason || undefined,
        rowVersion: current.rowVersion,
      })
      router.push(`/leave/requests/${id}`)
    } else {
      const result = await leaveStore.createLeave({
        corporationId: corp,
        educatorId: authStore.user?.id ?? '',
        leaveTypeId: form.leaveTypeId || undefined,
        unit: form.unit,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        quantity: form.quantity,
        reason: form.reason || undefined,
      })
      router.push(`/leave/requests/${result.id}`)
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  await refDataStore.fetchCategory?.('leave_type')
  if (isEdit.value && id) {
    await leaveStore.fetchLeave(id)
    const l = leaveStore.currentLeave
    if (l) {
      form.leaveTypeId = l.leaveTypeId ?? ''
      form.unit = l.unit as 'Day' | 'Hour'
      form.startsAt = l.startsAt.substring(0, 16)
      form.endsAt = l.endsAt.substring(0, 16)
      form.quantity = l.quantity
      form.reason = l.reason ?? ''
    }
  }
})
</script>
