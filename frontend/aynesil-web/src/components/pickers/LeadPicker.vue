<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { leadService } from '@/services/lead.service'
import EntityPicker from './EntityPicker.vue'
import type { PickerOption } from './types'

const props = defineProps<{
  modelValue: string
  /** When true, already-converted leads are excluded. */
  openOnly?: boolean
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

function label(contactName: string, childName: string | null): string {
  return childName ? `${childName} (${contactName})` : contactName
}

async function search(term: string): Promise<PickerOption[]> {
  const res = await leadService.list({
    isConverted: props.openOnly ? false : undefined,
    search: term || undefined,
    page: 1,
    pageSize: 20,
  })
  if (!res.success || !res.data) return []
  return res.data.items.map((l) => ({
    id: l.id,
    label: label(l.contactName, l.childName),
    sublabel: l.statusName,
  }))
}

async function resolve(id: string): Promise<PickerOption | null> {
  const res = await leadService.getById(id)
  return res.success && res.data ? { id, label: label(res.data.contactName, res.data.childName) } : null
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
    :placeholder="placeholder || t('pickers.lead')"
    @update:model-value="(id) => emit('update:modelValue', id)"
    @select="(o) => emit('select', o)"
  />
</template>
