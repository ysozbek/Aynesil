<script setup lang="ts">
import { reactive, ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const authStore = useAuthStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')

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

<template>
  <div>
    <PageHeader :title="isEdit ? t('followUp.form.editTitle') : t('followUp.form.newTitle')">
      <button @click="router.back()" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <form
      class="max-w-2xl rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-4"
      @submit.prevent="handleSubmit"
    >
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.title') }} *</label>
          <input
            v-model="form.title"
            type="text"
            required
            :class="['w-full h-10 px-3 text-sm rounded-lg border bg-transparent', errors.title ? 'border-red-500' : 'border-border']"
          />
          <p v-if="errors.title" class="text-xs text-red-600 mt-1">{{ errors.title }}</p>
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.description') }}</label>
          <textarea v-model="form.description" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.dueDate') }}</label>
          <input v-model="form.dueDate" type="date" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.assignedTo') }} ID</label>
          <input v-model="form.assignedTo" type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div v-if="contextLabel" class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.source') }}</label>
          <input type="text" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-accent/50" :value="contextLabel" readonly />
        </div>
        <div v-if="isEdit" class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('followUp.fields.notes') }}</label>
          <textarea v-model="form.notes" rows="3" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>

      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <p v-if="isDirty" class="text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2">
        {{ t('common.unsavedChanges') }}
      </p>

      <div class="flex justify-end gap-2 pt-2">
        <button type="button" @click="router.back()" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="store.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ store.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
