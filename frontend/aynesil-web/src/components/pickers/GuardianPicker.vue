<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { guardianService } from '@/services/student.service'
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
  const res = await guardianService.listGuardians({
    corporationId: corporationId.value,
    search: term || undefined,
    page: 1,
    pageSize: 20,
  })
  if (!res.success || !res.data) return []
  return res.data.items.map((g) => ({
    id: g.id,
    label: g.fullName,
    sublabel: [g.phone, g.email].filter(Boolean).join(' · ') || null,
  }))
}

async function resolve(id: string): Promise<PickerOption | null> {
  const res = await guardianService.getGuardian(id)
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
    :placeholder="placeholder || t('pickers.guardian')"
    @update:model-value="(id) => emit('update:modelValue', id)"
    @select="(o) => emit('select', o)"
  />
</template>
