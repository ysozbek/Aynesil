<script setup lang="ts">
/**
 * Sözleşme / onam şablonu formu — oluştur, düzenle, yeni sürüm.
 *  - create: kod + tür + geçerlilik + (onamda zorunluluk) + başlık/metin
 *  - edit: güncel sürümün türü/geçerliliği/metni (kullanılmış sürümde backend reddeder; hata gösterilir)
 *  - newVersion: güncel sürüm arşivlenir, metin kopyalanır; burada değiştirilen metin yeni sürüme yazılır
 * Metin yalnızca arayüz dilinde (tr) düzenlenir; diğer dillerdeki çeviriler olduğu gibi kalır.
 */
import { reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import FormModal from '@/components/shared/FormModal.vue'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import type { ConsentTemplateDto, ContractTemplateDto } from '@/types/legal.types'

export type TemplateMode = 'create' | 'edit' | 'newVersion'
export interface TemplateFormValue {
  code: string
  typeId?: string
  effectiveFrom?: string
  isMandatory: boolean
  title: string
  body: string
}

const LOCALE = 'tr'

const props = defineProps<{
  open: boolean
  kind: 'contract' | 'consent'
  mode: TemplateMode
  template: ContractTemplateDto | ConsentTemplateDto | null
  saving: boolean
  error: string
}>()
const emit = defineEmits<{ submit: [value: TemplateFormValue]; close: [] }>()

const { t } = useI18n()
const refData = useRefDataStore()
const types = ref<RefValueItem[]>([])
const form = reactive<TemplateFormValue>({ code: '', typeId: '', effectiveFrom: '', isMandatory: true, title: '', body: '' })
const localError = ref('')

watch(() => props.open, async (open) => {
  if (!open) return
  localError.value = ''
  types.value = await refData.getValues(props.kind === 'contract' ? 'contract_type' : 'consent_type')
  const tpl = props.template
  const tr = tpl?.translations.find((x) => x.locale === LOCALE)
  Object.assign(form, {
    code: props.mode === 'create' ? '' : tpl?.code ?? '',
    typeId: (props.kind === 'contract'
      ? (tpl as ContractTemplateDto | null)?.contractTypeId
      : (tpl as ConsentTemplateDto | null)?.consentTypeId) ?? '',
    effectiveFrom: tpl?.effectiveFrom ?? '',
    isMandatory: props.kind === 'consent' ? ((tpl as ConsentTemplateDto | null)?.isMandatory ?? true) : true,
    title: tr?.title ?? '',
    body: tr?.body ?? '',
  })
})

function submit() {
  localError.value = ''
  if (props.mode === 'create' && !/^[a-z0-9_]+$/.test(form.code)) { localError.value = t('legal.template.form.codeFormat'); return }
  if (!form.title.trim() || !form.body.trim()) { localError.value = t('legal.template.form.textRequired'); return }
  emit('submit', { ...form, typeId: form.typeId || undefined, effectiveFrom: form.effectiveFrom || undefined })
}

const inputClass = 'w-full px-3 py-2 text-sm rounded-lg border border-border focus:outline-none focus:ring-1 focus:ring-primary'
</script>

<template>
  <FormModal
    wide
    :open="open"
    :saving="saving"
    :title="t(`legal.template.form.title.${mode}`)"
    :subtitle="mode === 'newVersion' && template ? t('legal.template.form.newVersionHint', { version: template.version + 1 }) : undefined"
    @submit="submit"
    @close="emit('close')"
  >
    <div class="space-y-4">
      <p v-if="localError || error" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ localError || error }}</p>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.template.fields.code') }} *</label>
          <input v-model="form.code" type="text" :disabled="mode !== 'create'" :class="inputClass" placeholder="ornek_kod" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.template.fields.type') }}</label>
          <select v-model="form.typeId" :disabled="mode === 'newVersion'" :class="inputClass">
            <option value="">—</option>
            <option v-for="v in types" :key="v.id" :value="v.id">{{ v.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.template.fields.effectiveFrom') }}</label>
          <input v-model="form.effectiveFrom" type="date" :class="inputClass" />
        </div>
        <label v-if="kind === 'consent'" class="flex items-center gap-2 text-sm text-foreground mt-6">
          <input v-model="form.isMandatory" type="checkbox" :disabled="mode === 'newVersion'" />
          {{ t('legal.consent.mandatory') }}
        </label>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.template.form.titleField') }} *</label>
        <input v-model="form.title" type="text" maxlength="300" :class="inputClass" />
      </div>
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('legal.template.form.body') }} *</label>
        <textarea v-model="form.body" rows="12" :class="inputClass + ' font-mono'" />
      </div>
    </div>
  </FormModal>
</template>
