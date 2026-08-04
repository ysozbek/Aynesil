<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore, type RefValueItem } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const leaveStore = useLeaveStore()
const auth = useAuthStore()
const refData = useRefDataStore()

const isEdit = computed(() => !!route.params.id)
const id = route.params.id as string | undefined
const errorMsg = ref('')
const leaveTypes = ref<RefValueItem[]>([])

const form = reactive({
  leaveTypeId: '',
  unit: 'Day' as 'Day' | 'Hour',
  startsAt: '',
  endsAt: '',
  quantity: undefined as number | undefined,
  reason: '',
})

async function handleSubmit() {
  errorMsg.value = ''
  const corp = auth.user?.corporationId ?? ''
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
      router.push({ name: 'leave-detail', params: { id } })
    } else {
      const result = await leaveStore.createLeave({
        corporationId: corp,
        educatorId: auth.user?.id ?? '',
        leaveTypeId: form.leaveTypeId || undefined,
        unit: form.unit,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        quantity: form.quantity,
        reason: form.reason || undefined,
      })
      router.push({ name: 'leave-detail', params: { id: result.id } })
    }
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  leaveTypes.value = await refData.getValues('leave_type')
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

<template>
  <div>
    <PageHeader
      :title="isEdit ? t('leave.form.editTitle') : t('leave.form.newTitle')"
    >
      <button
        @click="router.push({ name: 'leave-list' })"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <form
      class="max-w-2xl rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-4"
      @submit.prevent="handleSubmit"
    >
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.leaveType') }} *</label>
          <select v-model="form.leaveTypeId" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="lt in leaveTypes" :key="lt.id" :value="lt.id">{{ lt.label || lt.code }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.unit') }} *</label>
          <select v-model="form.unit" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="Day">{{ t('leave.unit.day') }}</option>
            <option value="Hour">{{ t('leave.unit.hour') }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.startsAt') }} *</label>
          <input v-model="form.startsAt" type="datetime-local" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.endsAt') }} *</label>
          <input v-model="form.endsAt" type="datetime-local" required class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div v-if="form.unit === 'Hour'">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.quantity') }}</label>
          <input v-model.number="form.quantity" type="number" step="0.5" min="0.5" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.fields.reason') }}</label>
          <textarea v-model="form.reason" rows="4" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>

      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <div class="flex justify-end gap-2 pt-2">
        <button type="button" @click="router.push({ name: 'leave-list' })" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="leaveStore.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ leaveStore.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
