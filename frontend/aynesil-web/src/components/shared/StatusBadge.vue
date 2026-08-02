<script setup lang="ts">
defineProps<{
  value: string | boolean
  trueLabel?: string
  falseLabel?: string
}>()

function resolve(value: string | boolean): { label: string; classes: string } {
  if (typeof value === 'boolean') {
    return value
      ? { label: '', classes: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20' }
      : { label: '', classes: 'bg-red-50 text-red-700 ring-red-600/20' }
  }
  const v = value.toLowerCase()
  if (v === 'active' || v === 'aktif')
    return { label: value, classes: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20' }
  if (v === 'inactive' || v === 'pasif')
    return { label: value, classes: 'bg-gray-100 text-gray-600 ring-gray-500/20' }
  if (v === 'suspended' || v === 'askıda')
    return { label: value, classes: 'bg-amber-50 text-amber-700 ring-amber-600/20' }
  if (v === 'pending' || v === 'bekliyor')
    return { label: value, classes: 'bg-blue-50 text-blue-700 ring-blue-600/20' }
  return { label: value, classes: 'bg-gray-100 text-gray-600 ring-gray-500/20' }
}
</script>

<template>
  <span
    :class="[
      'inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset',
      resolve(value).classes,
    ]"
  >
    <template v-if="typeof value === 'boolean'">
      {{ value ? (trueLabel ?? 'Aktif') : (falseLabel ?? 'Pasif') }}
    </template>
    <template v-else>
      {{ resolve(value).label }}
    </template>
  </span>
</template>
