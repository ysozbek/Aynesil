<script setup lang="ts">
/**
 * Activities view — global follow-up tracker across all leads.
 */
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useLeadActivityStore } from '@/stores/leadActivity.store'
import { useBranchStore } from '@/stores/branch.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import Pagination from '@/components/shared/Pagination.vue'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const activityStore = useLeadActivityStore()
const branchStore = useBranchStore()

const corporationId = computed(() => auth.user?.corporationId ?? '')

const query = reactive({
  corporationId: corporationId.value,
  campusId: '',
  page: 1,
  pageSize: 20,
})

onMounted(async () => {
  if (branchStore.list.items.length === 0) await branchStore.fetchList({ pageSize: 200 })
  await loadFollowUps()
})

watch(() => [query.campusId, query.page, query.pageSize], loadFollowUps)

async function loadFollowUps() {
  await activityStore.fetchFollowUps({
    corporationId: query.corporationId,
    campusId: query.campusId || undefined,
    page: query.page,
    pageSize: query.pageSize,
  })
}

function formatDateTime(val: string | null): string {
  if (!val) return '—'
  return new Date(val).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function isOverdue(val: string | null): boolean {
  if (!val) return false
  return new Date(val) < new Date()
}
</script>

<template>
  <div>
    <PageHeader :title="t('crm.activity.title')" :description="t('crm.activity.description')" />

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3">
      <select v-model="query.campusId" @change="query.page = 1"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('common.allCampuses') }}</option>
        <option v-for="c in branchStore.list.items" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </div>

    <!-- Loading -->
    <div v-if="activityStore.loading" class="space-y-3">
      <div v-for="i in 5" :key="i" class="h-16 rounded-xl bg-accent animate-pulse" />
    </div>

    <!-- Empty -->
    <div v-else-if="activityStore.followUps.items.length === 0" class="text-center py-24">
      <svg class="w-12 h-12 mx-auto mb-4 text-muted-foreground/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
      <p class="text-muted-foreground text-sm">{{ t('crm.activity.noFollowUps') }}</p>
    </div>

    <!-- Follow-up list -->
    <div v-else class="space-y-2">
      <div
        v-for="act in activityStore.followUps.items"
        :key="act.id"
        :class="['rounded-xl border bg-[--color-card] p-4 shadow-sm hover:shadow transition-shadow cursor-pointer', isOverdue(act.followUpAt) ? 'border-red-200 bg-red-50/30' : 'border-border']"
        @click="router.push({ name: 'lead-detail', params: { id: act.leadId } })"
      >
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-foreground truncate">{{ act.subject ?? t('crm.activity.noSubject') }}</p>
            <p class="text-xs text-muted-foreground mt-0.5">
              {{ act.activityTypeName ?? '—' }}
              <span v-if="act.direction"> · {{ act.direction === 'inbound' ? t('crm.activity.inbound') : t('crm.activity.outbound') }}</span>
            </p>
            <p v-if="act.body" class="mt-1 text-xs text-muted-foreground line-clamp-2">{{ act.body }}</p>
          </div>
          <div class="text-right flex-none">
            <p :class="['text-xs font-medium', isOverdue(act.followUpAt) ? 'text-red-600' : 'text-amber-600']">
              {{ formatDateTime(act.followUpAt) }}
            </p>
            <p v-if="isOverdue(act.followUpAt)" class="text-xs text-red-500 mt-0.5">{{ t('crm.activity.overdue') }}</p>
          </div>
        </div>
      </div>
    </div>

    <div class="mt-4">
      <Pagination
        :page="activityStore.followUps.page"
        :page-size="activityStore.followUps.pageSize"
        :total-count="activityStore.followUps.totalCount"
        :total-pages="activityStore.followUps.totalPages"
        :has-previous-page="activityStore.followUps.hasPreviousPage"
        :has-next-page="activityStore.followUps.hasNextPage"
        @update:page="(p) => { query.page = p }"
        @update:page-size="(s) => { query.pageSize = s; query.page = 1 }"
      />
    </div>
  </div>
</template>
