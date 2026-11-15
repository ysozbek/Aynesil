<template>
  <div class="space-y-4">
    <div v-if="store.loading" class="flex justify-center py-8">
      <span class="loading loading-spinner text-primary"></span>
    </div>

    <template v-else>
      <p class="text-xs text-gray-500">{{ $t('portal.bep.hint') }}</p>

      <div v-if="store.bepList.length" class="space-y-3">
        <div v-for="plan in store.bepList" :key="plan.id" class="card bg-base-100 shadow-sm">
          <div class="card-body p-4">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1">
                <p class="font-medium">{{ plan.title || $t('portal.bep.untitled') }}</p>
                <div class="flex gap-2 mt-1 flex-wrap text-xs text-gray-500">
                  <span>{{ $t('portal.bep.version', { n: plan.version }) }}</span>
                  <span v-if="plan.approvedAt">• {{ $t('portal.bep.approvedOn') }}: {{ formatDate(plan.approvedAt) }}</span>
                  <span v-if="plan.goalCount">• {{ $t('portal.bep.goalCount', { n: plan.goalCount }) }}</span>
                </div>
              </div>
              <div class="flex flex-col items-end gap-1">
                <StatusBadge entity="educationPlan" :status="plan.status" />
                <span v-if="isCurrent(plan)" class="text-xs text-success">{{ $t('portal.bep.current') }}</span>
              </div>
            </div>
            <p class="mt-3 text-sm text-gray-600">
              {{ $t('portal.bep.period') }}:
              {{ plan.effectiveFrom ? formatDate(plan.effectiveFrom) : '—' }}
              –
              {{ plan.effectiveTo ? formatDate(plan.effectiveTo) : $t('portal.bep.openEnded') }}
            </p>
          </div>
        </div>
      </div>

      <div v-else class="card bg-base-100 shadow">
        <div class="card-body items-center text-center py-10">
          <p class="text-gray-500">{{ $t('portal.bep.none') }}</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useParentPortalStore } from '@/stores/parentPortal.store'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import type { PortalEducationPlanDto } from '@/types/portal.types'

const props = defineProps<{ studentId: string }>()
const store = useParentPortalStore()

function formatDate(d: string): string {
  return new Date(d).toLocaleDateString('tr-TR')
}

/** Bugün plan döneminin içindeyse "güncel" gösterilir. */
function isCurrent(plan: PortalEducationPlanDto): boolean {
  const today = new Date().toISOString().slice(0, 10)
  if (plan.effectiveFrom && plan.effectiveFrom.slice(0, 10) > today) return false
  if (plan.effectiveTo && plan.effectiveTo.slice(0, 10) < today) return false
  return true
}

onMounted(() => store.fetchBep(props.studentId))
</script>
