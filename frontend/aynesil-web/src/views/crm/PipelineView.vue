<script setup lang="ts">
/**
 * Kanban Pipeline View.
 * Displays leads grouped by pipeline stage.
 * Stage movement uses ChangeLeadStatusCommand on the backend (POST /api/leads/{id}/status).
 * Drag-and-drop (native HTML5 DnD) with optimistic UI + server sync.
 */
import { ref, computed, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useLeadStore } from '@/stores/lead.store'
import { useLeadPipelineStore } from '@/stores/leadPipeline.store'
import { useBranchStore } from '@/stores/branch.store'
import { useRefDataStore } from '@/stores/refdata.store'
import { usePermission } from '@/composables/usePermission'
import type { LeadListItemDto } from '@/types/crm.types'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const leadStore = useLeadStore()
const pipelineStore = useLeadPipelineStore()
const branchStore = useBranchStore()
const refData = useRefDataStore()
const { can } = usePermission()

const corporationId = computed(() => auth.user?.corporationId ?? '')
const stages = ref<RefValueItem[]>([])
const campusFilter = ref('')

// Leads grouped by stage
const leadsByStage = reactive<Record<string, LeadListItemDto[]>>({})
const loadingStages = ref(true)

onMounted(async () => {
  await Promise.all([
    refData.getValues('LEAD_PIPELINE_STAGE').then(v => { stages.value = v }),
    branchStore.list.items.length === 0 ? branchStore.fetchList({ pageSize: 200 }) : Promise.resolve(),
  ])
  await loadBoard()
})

async function loadBoard() {
  loadingStages.value = true
  try {
    await Promise.all([
      pipelineStore.fetchSummary(corporationId.value, campusFilter.value || undefined),
      leadStore.fetchList({
        corporationId: corporationId.value,
        campusId: campusFilter.value || undefined,
        isConverted: false,
        page: 1,
        pageSize: 200,
        sortBy: 'createdAt',
        sortDirection: 'desc',
      }),
    ])
    // Group by stage
    stages.value.forEach(s => { leadsByStage[s.id] = [] })
    leadStore.list.items.forEach(lead => {
      const stageId = lead.pipelineStageCode
        ? stages.value.find(s => s.code === lead.pipelineStageCode)?.id
        : null
      if (stageId && leadsByStage[stageId]) {
        leadsByStage[stageId].push(lead)
      } else {
        const unassignedKey = '__unassigned__'
        if (!leadsByStage[unassignedKey]) leadsByStage[unassignedKey] = []
        leadsByStage[unassignedKey].push(lead)
      }
    })
  } finally {
    loadingStages.value = false
  }
}

// ── Drag & Drop ────────────────────────────────────────────────────────────────
const dragging = ref<{ lead: LeadListItemDto; fromStageId: string } | null>(null)
const dragOverStage = ref<string | null>(null)

function onDragStart(lead: LeadListItemDto, stageId: string) {
  dragging.value = { lead, fromStageId: stageId }
}

function onDragOver(e: DragEvent, stageId: string) {
  e.preventDefault()
  dragOverStage.value = stageId
}

function onDragLeave() {
  dragOverStage.value = null
}

async function onDrop(e: DragEvent, toStageId: string) {
  e.preventDefault()
  dragOverStage.value = null
  if (!dragging.value || dragging.value.fromStageId === toStageId) {
    dragging.value = null
    return
  }
  if (!can('lead:update')) { dragging.value = null; return }

  const { lead, fromStageId } = dragging.value
  dragging.value = null

  // Optimistic update
  const idx = leadsByStage[fromStageId].findIndex(l => l.id === lead.id)
  if (idx >= 0) leadsByStage[fromStageId].splice(idx, 1)
  if (!leadsByStage[toStageId]) leadsByStage[toStageId] = []
  leadsByStage[toStageId].unshift(lead)

  try {
    await leadStore.fetchOne(lead.id)
    if (leadStore.current) {
      await leadStore.changeStatus(lead.id, {
        newStatusId: leadStore.current.statusId ?? '',
        newPipelineStageId: toStageId,
        rowVersion: leadStore.current.rowVersion,
      })
    }
  } catch {
    // Rollback on error
    const toIdx = leadsByStage[toStageId].findIndex(l => l.id === lead.id)
    if (toIdx >= 0) leadsByStage[toStageId].splice(toIdx, 1)
    if (!leadsByStage[fromStageId]) leadsByStage[fromStageId] = []
    leadsByStage[fromStageId].unshift(lead)
  }
}

function stageCount(stageId: string): number {
  return leadsByStage[stageId]?.length ?? 0
}

function stageCardColor(idx: number): string {
  const colors = [
    'border-l-blue-400', 'border-l-indigo-400', 'border-l-violet-400',
    'border-l-amber-400', 'border-l-orange-400', 'border-l-rose-400',
  ]
  return colors[idx % colors.length]
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between gap-4">
      <div>
        <h1 class="text-xl font-bold text-foreground">{{ t('crm.pipeline.title') }}</h1>
        <p class="text-sm text-muted-foreground">{{ t('crm.pipeline.description') }}</p>
      </div>
      <div class="flex items-center gap-3">
        <select v-model="campusFilter" @change="loadBoard"
          class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
          <option value="">{{ t('common.allCampuses') }}</option>
          <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
        <button @click="loadBoard" class="h-9 w-9 flex items-center justify-center rounded-lg border border-border hover:bg-accent transition-colors" :title="t('common.filter')">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Pipeline Summary Stats -->
    <div v-if="pipelineStore.summary" class="mb-6 flex items-center gap-4 text-sm text-muted-foreground">
      <span>{{ t('crm.pipeline.total') }}: <strong class="text-foreground">{{ pipelineStore.summary.totalLeads }}</strong></span>
      <span>{{ t('crm.pipeline.converted') }}: <strong class="text-emerald-600">{{ pipelineStore.summary.convertedLeads }}</strong></span>
      <span>{{ t('crm.pipeline.lost') }}: <strong class="text-red-500">{{ pipelineStore.summary.lostLeads }}</strong></span>
    </div>

    <!-- Loading -->
    <div v-if="loadingStages" class="flex gap-4 overflow-x-auto pb-4">
      <div v-for="i in 5" :key="i" class="flex-none w-64 h-96 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Kanban Board -->
    <div v-else class="flex gap-4 overflow-x-auto pb-6 items-start">
      <div
        v-for="(stage, idx) in stages"
        :key="stage.id"
        class="flex-none w-64"
        @dragover="onDragOver($event, stage.id)"
        @dragleave="onDragLeave"
        @drop="onDrop($event, stage.id)"
      >
        <!-- Stage Header -->
        <div :class="['rounded-t-xl border border-b-0 border-border bg-[--color-card] px-3 py-2.5 flex items-center justify-between', dragOverStage === stage.id ? 'ring-2 ring-primary' : '']">
          <span class="text-sm font-semibold text-foreground">{{ stage.label }}</span>
          <span class="text-xs font-medium px-2 py-0.5 rounded-full bg-accent text-muted-foreground">{{ stageCount(stage.id) }}</span>
        </div>

        <!-- Stage Cards -->
        <div :class="['min-h-[200px] rounded-b-xl border border-t-0 border-border bg-accent/20 p-2 space-y-2 transition-colors', dragOverStage === stage.id ? 'bg-primary/5 border-primary' : '']">
          <div
            v-for="lead in leadsByStage[stage.id] ?? []"
            :key="lead.id"
            draggable="true"
            @dragstart="onDragStart(lead, stage.id)"
            @click="router.push({ name: 'lead-detail', params: { id: lead.id } })"
            :class="['rounded-lg border-l-4 border border-border bg-[--color-card] p-3 cursor-pointer hover:shadow-md transition-shadow', stageCardColor(idx)]"
          >
            <p class="text-sm font-medium text-foreground truncate">{{ lead.contactName }}</p>
            <p v-if="lead.childName" class="text-xs text-muted-foreground truncate">{{ lead.childName }}</p>
            <div class="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
              <span v-if="lead.sourceName" class="bg-accent rounded px-1.5 py-0.5">{{ lead.sourceName }}</span>
              <span v-if="lead.score !== null && lead.score !== undefined" class="ml-auto font-mono">{{ lead.score }}</span>
            </div>
            <p v-if="lead.assignedToName" class="mt-1 text-xs text-muted-foreground">{{ lead.assignedToName }}</p>
          </div>

          <div v-if="!leadsByStage[stage.id]?.length" class="text-center py-8 text-xs text-muted-foreground opacity-60">
            {{ t('crm.pipeline.emptyStage') }}
          </div>
        </div>
      </div>

      <!-- Add new lead shortcut -->
      <div v-if="can('lead:create')" class="flex-none w-64">
        <button
          @click="router.push({ name: 'leads-new' })"
          class="w-full rounded-xl border-2 border-dashed border-border hover:border-primary hover:bg-primary/5 transition-colors p-6 text-muted-foreground hover:text-primary text-sm font-medium flex flex-col items-center gap-2"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          {{ t('crm.lead.create') }}
        </button>
      </div>
    </div>
  </div>
</template>
