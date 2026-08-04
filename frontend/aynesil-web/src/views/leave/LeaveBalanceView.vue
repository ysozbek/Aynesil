<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLeaveStore } from '@/stores/leave.store'
import { useAuthStore } from '@/stores/auth.store'
import { useRefDataStore, type RefValueItem } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import DataTable from '@/components/shared/DataTable.vue'
import FormModal from '@/components/shared/FormModal.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { LeaveBalanceDto } from '@/types/leave.types'

const { t } = useI18n()
const leaveStore = useLeaveStore()
const auth = useAuthStore()
const refData = useRefDataStore()
const { can } = usePermission()

const filters = reactive({ periodYear: new Date().getFullYear(), leaveTypeId: '' })
const leaveTypes = ref<RefValueItem[]>([])
const showEntitlementModal = ref(false)
const showCarryForwardModal = ref(false)
const selectedBalance = ref<LeaveBalanceDto | null>(null)
const newEntitled = ref(0)
const cfFrom = ref(new Date().getFullYear() - 1)
const cfTo = ref(new Date().getFullYear())

const columns: Column<LeaveBalanceDto>[] = [
  { key: 'educatorFullName', label: t('leave.fields.educator') },
  { key: 'leaveTypeCode', label: t('leave.fields.leaveType'), width: '120px' },
  { key: 'periodYear', label: t('leave.fields.periodYear'), width: '90px' },
  { key: 'entitled', label: t('leave.balance.entitled'), width: '80px', align: 'right' },
  { key: 'used', label: t('leave.balance.used'), width: '80px', align: 'right' },
  { key: 'remaining', label: t('leave.balance.remaining'), width: '80px', align: 'right' },
  { key: 'unit', label: t('leave.fields.unit'), width: '80px' },
]

async function doFetch() {
  await leaveStore.fetchBalances({
    corporationId: auth.user?.corporationId,
    periodYear: filters.periodYear || undefined,
    leaveTypeId: filters.leaveTypeId || undefined,
  })
}

function openEntitlement(b: LeaveBalanceDto) {
  selectedBalance.value = b
  newEntitled.value = b.entitled
  showEntitlementModal.value = true
}

async function doSetEntitlement() {
  if (!selectedBalance.value) return
  await leaveStore.setEntitlement(selectedBalance.value.id, { entitled: newEntitled.value })
  showEntitlementModal.value = false
  await doFetch()
}

async function doCarryForward() {
  await leaveStore.carryForward({
    corporationId: auth.user?.corporationId ?? '',
    fromYear: cfFrom.value,
    toYear: cfTo.value,
  })
  showCarryForwardModal.value = false
  await doFetch()
}

onMounted(async () => {
  leaveTypes.value = await refData.getValues('leave_type')
  await doFetch()
})
</script>

<template>
  <div>
    <PageHeader :title="t('leave.balance.title')" :description="t('leave.balance.subtitle')">
      <button
        v-if="can('leave_balance:manage') || can('leave_request:approve')"
        @click="showCarryForwardModal = true"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent font-medium"
      >
        {{ t('leave.balance.carryForward') }}
      </button>
    </PageHeader>

    <div class="mb-4 flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('leave.fields.periodYear') }}</label>
        <input
          v-model.number="filters.periodYear"
          type="number"
          class="h-9 w-28 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        />
      </div>
      <div>
        <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('leave.fields.leaveType') }}</label>
        <select
          v-model="filters.leaveTypeId"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent"
          @change="doFetch"
        >
          <option value="">{{ t('common.allStatuses') }}</option>
          <option v-for="lt in leaveTypes" :key="lt.id" :value="lt.id">{{ lt.label || lt.code }}</option>
        </select>
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="leaveStore.balances"
      :loading="leaveStore.loading"
      :empty-text="t('leave.balance.noData')"
    >
      <template #cell-educatorFullName="{ value }">
        <span class="font-medium text-foreground">{{ value ?? '—' }}</span>
      </template>
      <template #cell-used="{ value }">
        <span class="text-amber-600">{{ value }}</span>
      </template>
      <template #cell-remaining="{ value }">
        <span class="font-semibold text-green-600">{{ value }}</span>
      </template>
      <template #cell-unit="{ value }">
        {{ value === 'Day' ? t('leave.unit.day') : t('leave.unit.hour') }}
      </template>
      <template #actions="{ row }">
        <div class="flex justify-end" @click.stop>
          <button
            v-if="can('leave_balance:manage') || can('leave_request:approve')"
            @click="openEntitlement(row)"
            class="p-1.5 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground"
            :title="t('leave.balance.setEntitlement')"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>
        </div>
      </template>
    </DataTable>

    <FormModal
      :open="showEntitlementModal"
      :title="t('leave.balance.setEntitlement')"
      :subtitle="selectedBalance ? `${selectedBalance.educatorFullName} · ${selectedBalance.leaveTypeCode}` : undefined"
      :saving="leaveStore.saving"
      @submit="doSetEntitlement"
      @close="showEntitlementModal = false"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.balance.entitled') }} *</label>
        <input v-model.number="newEntitled" type="number" step="0.5" min="0" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
      </div>
    </FormModal>

    <FormModal
      :open="showCarryForwardModal"
      :title="t('leave.balance.carryForward')"
      :saving="leaveStore.saving"
      @submit="doCarryForward"
      @close="showCarryForwardModal = false"
    >
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.balance.fromYear') }} *</label>
          <input v-model.number="cfFrom" type="number" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('leave.balance.toYear') }} *</label>
          <input v-model.number="cfTo" type="number" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>
    </FormModal>
  </div>
</template>
