<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { usePermissionStore } from '@/stores/permission.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { PermissionListItemDto } from '@/types/permission.types'

const { t } = useI18n()
const permStore = usePermissionStore()

const searchQuery = ref('')
const selectedResource = ref('')

onMounted(() => permStore.loadCatalog())

const resources = computed(() => Object.keys(permStore.catalogByResource).sort())

const filteredCatalog = computed(() => {
  const q = searchQuery.value.toLowerCase()
  const result: Record<string, PermissionListItemDto[]> = {}
  for (const [resource, perms] of Object.entries(permStore.catalogByResource)) {
    if (selectedResource.value && resource !== selectedResource.value) continue
    const filtered = q
      ? perms.filter((p) => p.code.toLowerCase().includes(q) || p.action.toLowerCase().includes(q) || p.description?.toLowerCase().includes(q))
      : perms
    if (filtered.length) result[resource] = filtered
  }
  return result
})

const totalCount = computed(() => permStore.catalog.length)
const filteredCount = computed(() => Object.values(filteredCatalog.value).reduce((s, arr) => s + arr.length, 0))
</script>

<template>
  <div>
    <PageHeader :title="t('permission.title')" :description="t('permission.description')" />

    <!-- Stats bar -->
    <div class="mb-4 flex items-center gap-4 text-sm text-muted-foreground">
      <span>{{ t('permission.total') }}: <strong class="text-foreground">{{ totalCount }}</strong></span>
      <span v-if="filteredCount !== totalCount">{{ t('permission.filtered') }}: <strong class="text-foreground">{{ filteredCount }}</strong></span>
      <span>{{ t('permission.resources') }}: <strong class="text-foreground">{{ resources.length }}</strong></span>
    </div>

    <!-- Filters -->
    <div class="mb-4 flex items-center gap-3 flex-wrap">
      <div class="relative flex-1 min-w-[200px] max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input v-model="searchQuery" type="search" :placeholder="t('permission.search')"
          class="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary" />
      </div>

      <select v-model="selectedResource"
        class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary">
        <option value="">{{ t('permission.allResources') }}</option>
        <option v-for="r in resources" :key="r" :value="r" class="capitalize">{{ r }}</option>
      </select>
    </div>

    <!-- Loading -->
    <div v-if="permStore.catalogLoading" class="space-y-3">
      <div v-for="i in 6" :key="i" class="bg-[--color-card] rounded-xl p-5 border border-border animate-pulse">
        <div class="h-4 w-28 bg-accent rounded mb-4" />
        <div class="grid grid-cols-4 gap-2">
          <div v-for="j in 4" :key="j" class="h-8 bg-accent rounded-lg" />
        </div>
      </div>
    </div>

    <!-- Permission groups by resource -->
    <div v-else class="space-y-3">
      <div v-for="(perms, resource) in filteredCatalog" :key="resource"
        class="bg-[--color-card] rounded-xl border border-border shadow-sm overflow-hidden">
        <!-- Resource header -->
        <div class="px-5 py-3 border-b border-border bg-accent/20 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <div class="w-2 h-2 rounded-full bg-primary" />
            <h3 class="text-sm font-bold text-foreground capitalize">{{ resource }}</h3>
          </div>
          <span class="text-xs text-muted-foreground">{{ perms.length }} {{ t('permission.items') }}</span>
        </div>

        <!-- Permission list -->
        <div class="px-5 py-4">
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-2">
            <div
              v-for="perm in perms"
              :key="perm.id"
              class="flex flex-col gap-0.5 rounded-lg bg-accent/30 px-3 py-2"
            >
              <span class="text-xs font-mono font-semibold text-foreground">{{ perm.action }}</span>
              <span class="text-xs text-muted-foreground font-mono">{{ perm.code }}</span>
              <span v-if="perm.description" class="text-xs text-muted-foreground mt-0.5 leading-snug">
                {{ perm.description }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div v-if="Object.keys(filteredCatalog).length === 0" class="text-center py-16 text-muted-foreground text-sm">
        <svg class="w-10 h-10 mx-auto mb-3 opacity-30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        {{ t('permission.noResults') }}
      </div>
    </div>
  </div>
</template>
