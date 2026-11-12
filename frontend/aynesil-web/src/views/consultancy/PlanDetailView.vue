<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useConsultancyStore } from '@/stores/consultancy.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import ConfirmModal from '@/components/shared/ConfirmModal.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useConsultancyStore()
const { can } = usePermission()

const plan = computed(() => store.currentPlan)
const actionTarget = ref<'activate' | 'complete' | 'cancel' | null>(null)
const actionLoading = ref(false)

onMounted(() => store.fetchPlan(String(route.params.id)))

function formatDate(val?: string | null) {
  if (!val) return '—'
  return new Date(val).toLocaleDateString('tr-TR')
}

async function runAction() {
  if (!plan.value || !actionTarget.value) return
  actionLoading.value = true
  try {
    if (actionTarget.value === 'activate') await store.activatePlan(plan.value.id)
    else if (actionTarget.value === 'complete') await store.completePlan(plan.value.id)
    else if (actionTarget.value === 'cancel') await store.cancelPlan(plan.value.id)
    actionTarget.value = null
  } finally {
    actionLoading.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="plan?.name ?? t('consultancy.plan.detail.title')"
      :description="plan?.institutionName"
    >
      <button
        type="button"
        class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
        @click="router.push({ name: 'consultancy-plans' })"
      >
        {{ t('common.back') }}
      </button>
      <button
        v-if="plan?.status === 'draft' && can(Permissions.ConsultancyPlans.Activate)"
        type="button"
        class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
        @click="actionTarget = 'activate'"
      >
        {{ t('consultancy.plan.actions.activate') }}
      </button>
      <button
        v-if="plan?.status === 'active' && can(Permissions.ConsultancyPlans.Complete)"
        type="button"
        class="px-3 py-2 text-sm rounded-lg bg-primary text-primary-foreground hover:opacity-90"
        @click="actionTarget = 'complete'"
      >
        {{ t('consultancy.plan.actions.complete') }}
      </button>
      <button
        v-if="(plan?.status === 'draft' || plan?.status === 'active') && can(Permissions.ConsultancyPlans.Cancel)"
        type="button"
        class="px-3 py-2 text-sm rounded-lg border border-red-200 text-red-700 hover:bg-red-50"
        @click="actionTarget = 'cancel'"
      >
        {{ t('consultancy.plan.actions.cancel') }}
      </button>
    </PageHeader>

    <div v-if="store.loading && !plan" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>

    <div v-else-if="plan" class="bg-white rounded-xl border border-border p-6 space-y-6">
      <div class="flex items-center gap-2">
        <StatusBadge entity="consultancyPlan" :status="plan.status" />
        <span v-if="plan.consultancyTypeCode" class="text-sm text-muted-foreground">
          {{ plan.consultancyTypeCode }}
        </span>
      </div>

      <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <dt class="text-muted-foreground">{{ t('consultancy.plan.fields.institution') }}</dt>
          <dd class="font-medium mt-0.5">{{ plan.institutionName }}</dd>
        </div>
        <div>
          <dt class="text-muted-foreground">{{ t('consultancy.plan.fields.period') }}</dt>
          <dd class="font-medium mt-0.5">{{ formatDate(plan.periodStart) }} — {{ formatDate(plan.periodEnd) }}</dd>
        </div>
        <div class="sm:col-span-2">
          <dt class="text-muted-foreground">{{ t('consultancy.plan.fields.scope') }}</dt>
          <dd class="mt-0.5 whitespace-pre-wrap">{{ plan.scope || '—' }}</dd>
        </div>
      </dl>

      <div class="flex flex-wrap gap-3 pt-2 border-t border-border">
        <button
          type="button"
          class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          @click="router.push({ name: 'consultancy-visits', query: { planId: plan.id } })"
        >
          {{ t('consultancy.plan.actions.viewVisits') }}
        </button>
        <button
          type="button"
          class="px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent"
          @click="router.push({ name: 'consultancy-reports', query: { planId: plan.id } })"
        >
          {{ t('consultancy.plan.actions.viewReports') }}
        </button>
      </div>
    </div>

    <ConfirmModal
      :open="!!actionTarget"
      :title="actionTarget ? t(`consultancy.plan.actions.${actionTarget}`) : ''"
      :message="t('consultancy.plan.actions.confirm')"
      :loading="actionLoading"
      @confirm="runAction"
      @cancel="actionTarget = null"
    />
  </div>
</template>
