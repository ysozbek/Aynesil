<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { educatorService } from '@/services/educator.service'
import EntityPicker from './EntityPicker.vue'
import type { PickerOption } from './types'

defineProps<{
  modelValue: string
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
  const res = await educatorService.listEducators({
    corporationId: corporationId.value,
    isActive: true,
    search: term || undefined,
    page: 1,
    pageSize: 20,
  })
  if (!res.success || !res.data) return []
  return res.data.items.map((e) => ({
    id: e.id,
    label: e.fullName,
    sublabel: [e.titleLabel, e.primaryCampusName].filter(Boolean).join(' · ') || null,
  }))
}

async function resolve(id: string): Promise<PickerOption | null> {
  const res = await educatorService.getEducator(id)
  return res.success && res.data ? { id, label: res.data.fullName } : null
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
    :placeholder="placeholder || t('pickers.educator')"
    @update:model-value="(id) => emit('update:modelValue', id)"
    @select="(o) => emit('select', o)"
  />
</template>
