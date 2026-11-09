<script setup lang="ts">
/**
 * Searchable single-select. Shows names to the user and emits the selected id.
 * Domain-specific pickers (StudentPicker, EducatorPicker, …) wrap this and supply
 * `search` / `resolve`.
 */
import { ref, watch, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PickerOption } from './types'

const props = withDefaults(defineProps<{
  modelValue: string
  search: (term: string) => Promise<PickerOption[]>
  /** Looks up the label of an id that was set from outside (e.g. route query). */
  resolve?: (id: string) => Promise<PickerOption | null>
  placeholder?: string
  disabled?: boolean
  invalid?: boolean
  size?: 'sm' | 'md'
}>(), {
  placeholder: '',
  disabled: false,
  invalid: false,
  size: 'md',
})

const emit = defineEmits<{
  'update:modelValue': [id: string]
  select: [option: PickerOption | null]
}>()

const { t } = useI18n()

const text = ref('')
const selectedLabel = ref('')
const open = ref(false)
const loading = ref(false)
const options = ref<PickerOption[]>([])
const highlighted = ref(-1)

let debounce: ReturnType<typeof setTimeout> | undefined
let requestSeq = 0

async function runSearch(term: string) {
  const seq = ++requestSeq
  loading.value = true
  try {
    const result = await props.search(term.trim())
    if (seq === requestSeq) {
      options.value = result
      highlighted.value = result.length ? 0 : -1
    }
  } finally {
    if (seq === requestSeq) loading.value = false
  }
}

function onInput() {
  open.value = true
  clearTimeout(debounce)
  debounce = setTimeout(() => runSearch(text.value), 250)
}

function onFocus() {
  if (props.disabled) return
  open.value = true
  if (text.value === selectedLabel.value) text.value = ''
  runSearch(text.value)
}

function onBlur() {
  // Delay so a click on an option registers before the list closes.
  setTimeout(() => {
    open.value = false
    text.value = selectedLabel.value
  }, 150)
}

function choose(option: PickerOption) {
  selectedLabel.value = option.label
  text.value = option.label
  open.value = false
  emit('update:modelValue', option.id)
  emit('select', option)
}

function clear() {
  selectedLabel.value = ''
  text.value = ''
  emit('update:modelValue', '')
  emit('select', null)
}

function onKeydown(e: KeyboardEvent) {
  if (!open.value) return
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlighted.value = Math.min(highlighted.value + 1, options.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlighted.value = Math.max(highlighted.value - 1, 0)
  } else if (e.key === 'Enter') {
    e.preventDefault()
    const option = options.value[highlighted.value]
    if (option) choose(option)
  } else if (e.key === 'Escape') {
    open.value = false
  }
}

watch(
  () => props.modelValue,
  async (id) => {
    if (!id) {
      selectedLabel.value = ''
      text.value = ''
      return
    }
    const known = options.value.find((o) => o.id === id)
    if (known) {
      selectedLabel.value = known.label
      text.value = known.label
      return
    }
    if (props.resolve) {
      const resolved = await props.resolve(id)
      if (resolved && props.modelValue === id) {
        selectedLabel.value = resolved.label
        text.value = resolved.label
      }
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => clearTimeout(debounce))

const heightClass = props.size === 'sm' ? 'h-9' : 'py-2'
</script>

<template>
  <div class="relative">
    <input
      v-model="text"
      type="text"
      autocomplete="off"
      :disabled="disabled"
      :placeholder="placeholder || t('common.searchToSelect')"
      @input="onInput"
      @focus="onFocus"
      @blur="onBlur"
      @keydown="onKeydown"
      :class="[
        'w-full px-3 pr-8 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary',
        heightClass,
        invalid ? 'border-red-400' : 'border-border',
        disabled ? 'opacity-50' : '',
      ]"
    />
    <button
      v-if="modelValue && !disabled"
      type="button"
      @mousedown.prevent="clear"
      class="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
      :title="t('common.clear')"
    >
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>

    <div
      v-if="open"
      class="absolute z-30 mt-1 w-full max-h-64 overflow-auto rounded-lg border border-border bg-card shadow-lg"
    >
      <div v-if="loading" class="px-3 py-2 text-sm text-muted-foreground">{{ t('common.loading') }}</div>
      <div v-else-if="options.length === 0" class="px-3 py-2 text-sm text-muted-foreground">{{ t('common.noResults') }}</div>
      <template v-else>
        <button
          v-for="(option, idx) in options"
          :key="option.id"
          type="button"
          @mousedown.prevent="choose(option)"
          :class="[
            'w-full text-left px-3 py-2 text-sm hover:bg-accent',
            idx === highlighted ? 'bg-accent' : '',
            option.id === modelValue ? 'font-medium text-primary' : 'text-foreground',
          ]"
        >
          <div>{{ option.label }}</div>
          <div v-if="option.sublabel" class="text-xs text-muted-foreground">{{ option.sublabel }}</div>
        </button>
      </template>
    </div>
  </div>
</template>
