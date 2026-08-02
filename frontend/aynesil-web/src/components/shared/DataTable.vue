<script setup lang="ts" generic="T extends Record<string, unknown>">
import { useI18n } from 'vue-i18n'

export interface Column<T> {
  key: string
  label: string
  sortable?: boolean
  width?: string
  align?: 'left' | 'right' | 'center'
  render?: (row: T) => string
}

const props = withDefaults(defineProps<{
  columns: Column<T>[]
  rows: T[]
  loading?: boolean
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  rowKey?: keyof T
  emptyText?: string
}>(), {
  loading: false,
  emptyText: undefined,
  sortBy: undefined,
  sortDirection: 'asc',
  rowKey: 'id' as keyof T,
})

const emit = defineEmits<{
  'sort': [key: string, direction: 'asc' | 'desc']
  'row-click': [row: T]
}>()

const { t } = useI18n()

function handleSort(col: Column<T>) {
  if (!col.sortable) return
  const direction = props.sortBy === col.key && props.sortDirection === 'asc' ? 'desc' : 'asc'
  emit('sort', col.key, direction)
}

function alignClass(align?: string) {
  if (align === 'right') return 'text-right'
  if (align === 'center') return 'text-center'
  return 'text-left'
}
</script>

<template>
  <div class="w-full overflow-x-auto rounded-xl border border-border bg-[--color-card] shadow-sm">
    <table class="w-full text-sm">
      <thead>
        <tr class="border-b border-border bg-accent/40">
          <th
            v-for="col in columns"
            :key="col.key"
            :style="col.width ? `width:${col.width}` : ''"
            :class="[
              'px-4 py-3 font-semibold text-foreground whitespace-nowrap select-none',
              alignClass(col.align),
              col.sortable ? 'cursor-pointer hover:text-primary' : '',
            ]"
            @click="handleSort(col)"
          >
            <span class="inline-flex items-center gap-1">
              {{ col.label }}
              <span v-if="col.sortable" class="text-muted-foreground">
                <svg
                  v-if="sortBy === col.key"
                  class="w-3.5 h-3.5"
                  :class="sortDirection === 'desc' ? 'rotate-180' : ''"
                  fill="currentColor" viewBox="0 0 20 20"
                >
                  <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.17l3.71-3.94a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                </svg>
                <svg v-else class="w-3.5 h-3.5 opacity-30" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M10 3a.75.75 0 01.55.24l3.25 3.5a.75.75 0 11-1.1 1.02L10 4.852 7.3 7.76a.75.75 0 01-1.1-1.02l3.25-3.5A.75.75 0 0110 3zm-3.76 9.2a.75.75 0 011.06.04l2.7 2.908 2.7-2.908a.75.75 0 111.1 1.02l-3.25 3.5a.75.75 0 01-1.1 0l-3.25-3.5a.75.75 0 01.04-1.06z" clip-rule="evenodd" />
                </svg>
              </span>
            </span>
          </th>
          <!-- Actions slot header -->
          <th v-if="$slots.actions" class="px-4 py-3 font-semibold text-right text-foreground w-px whitespace-nowrap">
            {{ t('common.actions') }}
          </th>
        </tr>
      </thead>

      <tbody>
        <!-- Loading skeleton -->
        <template v-if="loading">
          <tr v-for="i in 5" :key="`skeleton-${i}`" class="border-b border-border last:border-0">
            <td v-for="col in columns" :key="col.key" class="px-4 py-3">
              <div class="h-4 rounded bg-accent animate-pulse" />
            </td>
            <td v-if="$slots.actions" class="px-4 py-3">
              <div class="h-4 w-16 ml-auto rounded bg-accent animate-pulse" />
            </td>
          </tr>
        </template>

        <!-- Empty state -->
        <tr v-else-if="rows.length === 0">
          <td :colspan="columns.length + ($slots.actions ? 1 : 0)" class="py-16 text-center text-muted-foreground">
            <svg class="w-10 h-10 mx-auto mb-3 opacity-30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0H4m8 0v4" />
            </svg>
            <p class="text-sm">{{ emptyText ?? t('common.noData') }}</p>
          </td>
        </tr>

        <!-- Data rows -->
        <template v-else>
          <tr
            v-for="row in rows"
            :key="String(row[rowKey])"
            class="border-b border-border last:border-0 hover:bg-accent/30 transition-colors"
            @click="emit('row-click', row)"
          >
            <td
              v-for="col in columns"
              :key="col.key"
              :class="['px-4 py-3 text-foreground', alignClass(col.align)]"
            >
              <slot :name="`cell-${col.key}`" :row="row" :value="row[col.key]">
                {{ col.render ? col.render(row) : row[col.key] }}
              </slot>
            </td>
            <td v-if="$slots.actions" class="px-4 py-3 text-right">
              <slot name="actions" :row="row" />
            </td>
          </tr>
        </template>
      </tbody>
    </table>
  </div>
</template>
