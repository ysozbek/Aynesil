<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCorporationStore } from '@/stores/corporation.store'
import { useBranchStore } from '@/stores/branch.store'
import { usePermission } from '@/composables/usePermission'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import DataTable from '@/components/shared/DataTable.vue'
import type { Column } from '@/components/shared/DataTable.vue'
import type { CampusListItemDto } from '@/types/campus.types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const store = useCorporationStore()
const branchStore = useBranchStore()
const { can } = usePermission()

const id = computed(() => route.params.id as string)

onMounted(async () => {
  await store.fetchOne(id.value)
  await branchStore.fetchList({ corporationId: id.value, pageSize: 100 })
})

const campusColumns: Column<CampusListItemDto>[] = [
  { key: 'code', label: t('campus.code'), width: '80px' },
  { key: 'name', label: t('campus.name'), sortable: true },
  { key: 'city', label: t('campus.city') },
  { key: 'phone', label: t('campus.phone') },
  { key: 'isActive', label: t('common.status'), width: '100px' },
]

function formatDate(val: string | undefined) {
  if (!val) return '-'
  return new Date(val).toLocaleDateString('tr-TR', { year: 'numeric', month: 'long', day: 'numeric' })
}
</script>

<template>
  <div>
    <!-- Back + title -->
    <div class="mb-6 flex items-center gap-3">
      <button
        @click="router.push({ name: 'corporations' })"
        class="flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent text-muted-foreground hover:text-foreground transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
      <div class="flex-1">
        <div class="flex items-center gap-3">
          <h1 class="text-xl font-bold text-foreground">{{ store.current?.displayName }}</h1>
          <StatusBadge v-if="store.current" :value="store.current.status" />
        </div>
        <p class="text-sm text-muted-foreground">{{ store.current?.code }}</p>
      </div>

      <div class="flex items-center gap-2">
        <button
          v-if="can('corporation:update')"
          @click="router.push({ name: 'corporation-settings', params: { id } })"
          class="flex items-center gap-2 px-3 py-2 text-sm rounded-lg border border-border hover:bg-accent transition-colors"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          {{ t('corporation.settings') }}
        </button>
      </div>
    </div>

    <!-- Loading skeleton -->
    <div v-if="store.loading" class="grid grid-cols-2 gap-5">
      <div v-for="i in 4" :key="i" class="bg-[--color-card] rounded-xl p-5 border border-border animate-pulse">
        <div class="h-4 w-24 bg-accent rounded mb-3" />
        <div class="h-5 w-40 bg-accent rounded" />
      </div>
    </div>

    <template v-else-if="store.current">
      <!-- Info cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('corporation.legalName') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.legalName }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('corporation.locale') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.defaultLocale }} / {{ store.current.defaultCurrency }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('corporation.timezone') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.timezone }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('corporation.taxOffice') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.taxOffice || '-' }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('corporation.taxNumber') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ store.current.taxNumber || '-' }}</p>
        </div>
        <div class="bg-[--color-card] rounded-xl p-4 border border-border shadow-sm">
          <p class="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-1">{{ t('common.createdAt') }}</p>
          <p class="text-sm font-semibold text-foreground">{{ formatDate(store.current.createdAt) }}</p>
        </div>
      </div>

      <!-- Campuses / Branches -->
      <div class="flex items-center justify-between mb-3">
        <h2 class="text-base font-semibold text-foreground">
          {{ t('campus.title') }}
          <span class="ml-2 text-xs font-normal text-muted-foreground">({{ branchStore.list.totalCount }})</span>
        </h2>
        <button
          v-if="can('campus:read')"
          @click="router.push({ name: 'campuses', query: { corporationId: id } })"
          class="text-xs text-primary hover:underline"
        >
          {{ t('common.viewAll') }}
        </button>
      </div>

      <DataTable
        :columns="campusColumns"
        :rows="branchStore.list.items"
        :loading="branchStore.loading"
      >
        <template #cell-isActive="{ value }">
          <StatusBadge :value="!!value" />
        </template>
      </DataTable>
    </template>

    <div v-else class="text-center py-16 text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
  </div>
</template>
