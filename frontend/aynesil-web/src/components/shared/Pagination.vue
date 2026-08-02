<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}>()

const emit = defineEmits<{
  'update:page': [page: number]
  'update:pageSize': [size: number]
}>()

const { t } = useI18n()

const from = computed(() => (props.page - 1) * props.pageSize + 1)
const to = computed(() => Math.min(props.page * props.pageSize, props.totalCount))

const pageSizeOptions = [10, 20, 50, 100]

const visiblePages = computed(() => {
  const pages: (number | '...')[] = []
  const total = props.totalPages
  const current = props.page

  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i)
  } else {
    pages.push(1)
    if (current > 3) pages.push('...')
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      pages.push(i)
    }
    if (current < total - 2) pages.push('...')
    pages.push(total)
  }

  return pages
})
</script>

<template>
  <div v-if="totalCount > 0" class="flex items-center justify-between gap-4 px-1">
    <div class="flex items-center gap-2 text-sm text-muted-foreground">
      <span>{{ t('pagination.showing', { from, to, total: totalCount }) }}</span>
      <select
        :value="pageSize"
        @change="emit('update:pageSize', +($event.target as HTMLSelectElement).value)"
        class="h-8 rounded-md border border-border bg-transparent px-2 text-xs focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <option v-for="s in pageSizeOptions" :key="s" :value="s">{{ s }}</option>
      </select>
    </div>

    <div class="flex items-center gap-1">
      <button
        :disabled="!hasPreviousPage"
        @click="emit('update:page', page - 1)"
        class="flex h-8 w-8 items-center justify-center rounded-md border border-border text-sm disabled:opacity-40 hover:bg-accent transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>

      <template v-for="(p, i) in visiblePages" :key="i">
        <span v-if="p === '...'" class="flex h-8 w-8 items-center justify-center text-sm text-muted-foreground">
          …
        </span>
        <button
          v-else
          :class="[
            'flex h-8 w-8 items-center justify-center rounded-md border text-sm transition-colors',
            p === page
              ? 'bg-primary text-primary-foreground border-primary font-medium'
              : 'border-border hover:bg-accent',
          ]"
          @click="emit('update:page', p as number)"
        >
          {{ p }}
        </button>
      </template>

      <button
        :disabled="!hasNextPage"
        @click="emit('update:page', page + 1)"
        class="flex h-8 w-8 items-center justify-center rounded-md border border-border text-sm disabled:opacity-40 hover:bg-accent transition-colors"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>
  </div>
</template>
