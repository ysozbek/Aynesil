<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <button class="btn btn-sm btn-light" @click="router.back()">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </button>
    </div>

    <div class="card mw-750px mx-auto">
      <div class="card-header border-0 pt-6">
        <h2 class="card-title fw-bold">
          {{ isEdit ? $t('followUp.form.editTitle') : $t('followUp.form.newTitle') }}
        </h2>
      </div>
      <div class="card-body">
        <form @submit.prevent="handleSubmit" novalidate>
          <div class="row g-4">
            <!-- Title -->
            <div class="col-12">
              <label class="form-label required">{{ $t('followUp.fields.title') }}</label>
              <input
                v-model="form.title"
                type="text"
                class="form-control"
                :class="{ 'is-invalid': errors.title }"
                required
              />
              <div v-if="errors.title" class="invalid-feedback">{{ errors.title }}</div>
            </div>

            <!-- Description -->
            <div class="col-12">
              <label class="form-label">{{ $t('followUp.fields.description') }}</label>
              <textarea v-model="form.description" class="form-control" rows="4"></textarea>
            </div>

            <!-- Due Date -->
            <div class="col-sm-6">
              <label class="form-label">{{ $t('followUp.dueDate') }}</label>
              <input v-model="form.dueDate" type="date" class="form-control" />
            </div>

            <!-- Assigned To -->
            <div class="col-sm-6">
              <label class="form-label">{{ $t('followUp.assignedTo') }} ID</label>
              <input v-model="form.assignedTo" type="text" class="form-control" />
            </div>

            <!-- Source (auto-filled from context, displayed read-only) -->
            <div v-if="contextLabel" class="col-12">
              <label class="form-label">{{ $t('followUp.fields.source') }}</label>
              <input type="text" class="form-control bg-light" :value="contextLabel" readonly />
            </div>

            <!-- Notes (edit only) -->
            <div v-if="isEdit" class="col-12">
              <label class="form-label">{{ $t('followUp.fields.notes') }}</label>
              <textarea v-model="form.notes" class="form-control" rows="3"></textarea>
            </div>
          </div>

          <!-- Error -->
          <div v-if="errorMsg" class="alert alert-danger mt-5">{{ errorMsg }}</div>

          <!-- Unsaved warning -->
          <div v-if="isDirty" class="alert alert-light-warning mt-4 fs-7">
            <i class="ki-outline ki-information-5 fs-3 me-2"></i>
            {{ $t('common.unsavedChanges') }}
          </div>

          <div class="d-flex justify-content-end gap-3 mt-6">
            <button type="button" class="btn btn-light" @click="router.back()">{{ $t('common.cancel') }}</button>
            <button type="submit" class="btn btn-primary" :disabled="store.saving">
              <span v-if="store.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const authStore = useAuthStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')

// Context from query params (pre-filled from plan/visit/observation detail page)
const planId = route.query.planId as string | undefined
const visitId = route.query.visitId as string | undefined
const observationId = route.query.observationId as string | undefined

const contextLabel = computed(() => {
  if (planId) return `Plan: ${planId}`
  if (visitId) return `Ziyaret: ${visitId}`
  return ''
})

const form = reactive({
  title: '',
  description: '',
  dueDate: '',
  assignedTo: '',
  notes: '',
})

const errors = reactive({ title: '' })
const initialForm = JSON.stringify(form)
const isDirty = computed(() => JSON.stringify(form) !== initialForm)

function validate(): boolean {
  errors.title = ''
  if (!form.title.trim()) { errors.title = 'Başlık zorunludur.'; return false }
  return true
}

async function handleSubmit() {
  if (!validate()) return
  errorMsg.value = ''
  try {
    if (isEdit.value && id) {
      const cur = store.currentFollowUp
      if (!cur) return
      await store.updateFollowUp(id, {
        title: form.title,
        description: form.description || undefined,
        dueDate: form.dueDate || undefined,
        assignedTo: form.assignedTo || undefined,
        notes: form.notes || undefined,
        rowVersion: cur.rowVersion,
      })
      router.push(`/consultancy/follow-ups/${id}`)
    } else {
      const result = await store.createFollowUp({
        corporationId: authStore.user?.corporationId ?? '',
        consultancyPlanId: planId,
        schoolVisitId: visitId,
        observationRecordId: observationId,
        title: form.title,
        description: form.description || undefined,
        dueDate: form.dueDate || undefined,
        assignedTo: form.assignedTo || undefined,
      })
      router.push(`/consultancy/follow-ups/${result.id}`)
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

// Unsaved changes guard
function handleBeforeUnload(e: BeforeUnloadEvent) {
  if (isDirty.value) { e.preventDefault() }
}
onMounted(async () => {
  window.addEventListener('beforeunload', handleBeforeUnload)
  if (isEdit.value && id) {
    await store.fetchFollowUp(id)
    const f = store.currentFollowUp
    if (f) {
      form.title = f.title
      form.description = f.description ?? ''
      form.dueDate = f.dueDate ?? ''
      form.assignedTo = f.assignedTo ?? ''
      form.notes = f.notes ?? ''
    }
  }
})
onBeforeUnmount(() => window.removeEventListener('beforeunload', handleBeforeUnload))
</script>
