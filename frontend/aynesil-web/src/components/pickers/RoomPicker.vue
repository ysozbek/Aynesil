<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { schedulingService } from '@/services/scheduling.service'
import EntityPicker from './EntityPicker.vue'
import type { PickerOption } from './types'

const props = defineProps<{
  modelValue: string
  /** Restricts results to one campus when set. */
  campusId?: string
  disabled?: boolean
  invalid?: boolean
  placeholder?: string
  size?: 'sm' | 'md'
}>()

const emit = defineEmits<{
  'update:modelValue': [id: string]
  select: [option: PickerOption | null]
}>()

const { t } = useI18n()
const auth = useAuthStore()
const corporationId = computed(() => auth.user?.corporationId ?? '')

async function search(term: string): Promise<PickerOption[]> {
  const res = await schedulingService.listRooms({
    corporationId: corporationId.value,
    campusId: props.campusId || undefined,
    isActive: true,
    search: term || undefined,
    page: 1,
    pageSize: 50,
  })
  if (!res.success || !res.data) return []
  return res.data.items.map((r) => ({
    id: r.id,
    label: r.name,
    sublabel: [r.campusName, r.capacity ? `${r.capacity} ${t('pickers.capacitySuffix')}` : null].filter(Boolean).join(' · ') || null,
  }))
}

async function resolve(id: string): Promise<PickerOption | null> {
  const res = await schedulingService.getRoom(id)
  return res.success && res.data ? { id, label: res.data.name } : null
}
</script>

<template>
  <EntityPicker
    :model-value="modelValue"
    :search="search"
    :resolve="resolve"
    :disabled="disabled"
    :invalid="invalid"
    :size="size"
    :placeholder="placeholder || t('pickers.room')"
    @update:model-value="(id) => emit('update:modelValue', id)"
    @select="(o) => emit('select', o)"
  />
</template>
