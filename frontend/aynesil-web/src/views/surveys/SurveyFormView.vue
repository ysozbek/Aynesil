<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import { useRefDataStore } from '@/stores/refdata.store'
import type { RefValueItem } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { QuestionPayload, SurveyQuestionType, SurveyTarget } from '@/types/survey.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useSurveyStore()
const refData = useRefDataStore()

const id = route.params.id as string | undefined
const isEdit = computed(() => !!id)
const types = ref<RefValueItem[]>([])
const errorMsg = ref('')
const fieldError = ref('')
/** Yanıt alınmış anketin soruları değiştirilemez (sunucu 422 verir); ekran da kilitler. */
const locked = ref(false)

const questionTypes: SurveyQuestionType[] = ['rating', 'yes_no', 'multiple_choice', 'scale', 'text']
const hasOptions = (type: SurveyQuestionType) => type === 'multiple_choice' || type === 'scale'

interface OptionRow { id?: string; optionText: string; optionValue: string }
interface QuestionRow { id?: string; questionText: string; questionType: SurveyQuestionType; isRequired: boolean; options: OptionRow[] }

const form = reactive({
  title: '',
  description: '',
  target: 'guardian' as SurveyTarget,
  typeId: '',
  isActive: true,
})
const questions = ref<QuestionRow[]>([newQuestion()])

function newQuestion(): QuestionRow {
  return { questionText: '', questionType: 'rating', isRequired: true, options: [] }
}

function addQuestion() { questions.value.push(newQuestion()) }
function removeQuestion(i: number) { questions.value.splice(i, 1) }
function move(i: number, d: -1 | 1) {
  const j = i + d
  if (j < 0 || j >= questions.value.length) return
  const [q] = questions.value.splice(i, 1)
  questions.value.splice(j, 0, q)
}
function addOption(q: QuestionRow) { q.options.push({ optionText: '', optionValue: '' }) }
function removeOption(q: QuestionRow, i: number) { q.options.splice(i, 1) }
function onTypeChange(q: QuestionRow) {
  if (hasOptions(q.questionType) && q.options.length === 0 && q.questionType === 'multiple_choice') {
    q.options = [{ optionText: '', optionValue: '' }, { optionText: '', optionValue: '' }]
  }
}

function toPayload(): QuestionPayload[] {
  return questions.value.map((q, qi) => ({
    id: q.id,
    questionText: q.questionText.trim(),
    questionType: q.questionType,
    isRequired: q.isRequired,
    sortOrder: qi + 1,
    answerOptions: hasOptions(q.questionType)
      ? q.options.map((o, oi) => ({ id: o.id, optionText: o.optionText.trim(), optionValue: o.optionValue.trim() || null, sortOrder: oi + 1 }))
      : [],
  }))
}

function validate(): boolean {
  fieldError.value = ''
  if (!form.title.trim()) { fieldError.value = t('validation.required', { field: t('surveys.fields.title') }); return false }
  if (locked.value) return true
  if (questions.value.length === 0) { fieldError.value = t('surveys.form.needQuestion'); return false }
  for (const q of questions.value) {
    if (!q.questionText.trim()) { fieldError.value = t('surveys.form.questionTextRequired'); return false }
    if (q.questionType === 'multiple_choice' && q.options.filter((o) => o.optionText.trim()).length < 2) {
      fieldError.value = t('surveys.form.needTwoOptions'); return false
    }
    if (hasOptions(q.questionType) && q.options.some((o) => !o.optionText.trim())) {
      fieldError.value = t('surveys.form.optionTextRequired'); return false
    }
  }
  return true
}

async function submit() {
  errorMsg.value = ''
  if (!validate()) return
  try {
    if (isEdit.value && id) {
      const cur = store.current
      if (!cur) return
      await store.update(id, {
        title: form.title.trim(),
        description: form.description.trim() || null,
        target: form.target,
        typeId: form.typeId || null,
        isActive: form.isActive,
        rowVersion: cur.rowVersion,
        questions: locked.value ? undefined : toPayload(),
      })
    } else {
      await store.create({
        title: form.title.trim(),
        description: form.description.trim() || null,
        target: form.target,
        typeId: form.typeId || null,
        questions: toPayload(),
      })
    }
    router.push({ name: 'surveys' })
  } catch (e: unknown) {
    errorMsg.value = (e as Error).message
  }
}

onMounted(async () => {
  types.value = await refData.getValues('survey_type')
  if (!isEdit.value || !id) return
  await store.fetchSurvey(id)
  const s = store.current
  if (!s) { errorMsg.value = store.error ?? t('common.error'); return }
  Object.assign(form, { title: s.title, description: s.description ?? '', target: s.target, typeId: s.typeId ?? '', isActive: s.isActive })
  locked.value = s.responseCount > 0
  questions.value = s.questions.map((q) => ({
    id: q.id,
    questionText: q.questionText,
    questionType: q.questionType,
    isRequired: q.isRequired,
    options: q.answerOptions.map((o) => ({ id: o.id, optionText: o.optionText, optionValue: o.optionValue ?? '' })),
  }))
})
</script>

<template>
  <div>
    <PageHeader :title="isEdit ? t('surveys.form.editTitle') : t('surveys.form.newTitle')">
      <button type="button" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'surveys' })">
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <form class="max-w-3xl space-y-6" @submit.prevent="submit">
      <section class="rounded-xl border border-border bg-card shadow-sm p-6 grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium mb-1">{{ t('surveys.fields.title') }} *</label>
          <input v-model="form.title" type="text" maxlength="500" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('surveys.fields.target') }}</label>
          <select v-model="form.target" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option v-for="tg in ['guardian', 'educator', 'student']" :key="tg" :value="tg">{{ t(`surveys.target.${tg}`) }}</option>
          </select>
          <p v-if="form.target !== 'guardian'" class="text-xs text-muted-foreground mt-1">{{ t('surveys.form.targetHint') }}</p>
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">{{ t('surveys.fields.type') }}</label>
          <select v-model="form.typeId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
            <option value="">{{ t('common.select') }}</option>
            <option v-for="ty in types" :key="ty.id" :value="ty.id">{{ ty.label || ty.code }}</option>
          </select>
        </div>
        <div class="sm:col-span-2">
          <label class="block text-sm font-medium mb-1">{{ t('surveys.fields.description') }}</label>
          <textarea v-model="form.description" rows="2" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <label v-if="isEdit" class="flex items-center gap-2 text-sm sm:col-span-2">
          <input v-model="form.isActive" type="checkbox" />
          {{ t('surveys.form.isActive') }}
        </label>
      </section>

      <p v-if="locked" class="text-sm text-amber-700 bg-amber-50 rounded-lg px-3 py-2">{{ t('surveys.form.locked') }}</p>

      <section class="space-y-3">
        <h2 class="text-base font-semibold text-foreground">{{ t('surveys.form.questions') }}</h2>

        <div v-for="(q, qi) in questions" :key="q.id ?? qi" class="rounded-xl border border-border bg-card shadow-sm p-4 space-y-3">
          <div class="flex items-start gap-3">
            <span class="mt-2 text-sm font-semibold text-muted-foreground w-6">{{ qi + 1 }}.</span>
            <div class="flex-1 space-y-3">
              <input
                v-model="q.questionText" type="text" :disabled="locked" :placeholder="t('surveys.form.questionText')"
                class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
              />
              <div class="flex flex-wrap items-center gap-4">
                <select
                  v-model="q.questionType" :disabled="locked"
                  class="h-9 px-3 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
                  @change="onTypeChange(q)"
                >
                  <option v-for="qt in questionTypes" :key="qt" :value="qt">{{ t(`surveys.questionType.${qt}`) }}</option>
                </select>
                <label class="flex items-center gap-2 text-sm">
                  <input v-model="q.isRequired" type="checkbox" :disabled="locked" />
                  {{ t('surveys.fields.required') }}
                </label>
              </div>

              <div v-if="hasOptions(q.questionType)" class="space-y-2">
                <div v-for="(o, oi) in q.options" :key="o.id ?? oi" class="flex items-center gap-2">
                  <input
                    v-model="o.optionText" type="text" :disabled="locked" :placeholder="t('surveys.form.optionText')"
                    class="flex-1 h-9 px-3 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
                  />
                  <input
                    v-model="o.optionValue" type="text" :disabled="locked" :placeholder="t('surveys.form.optionValue')"
                    class="w-28 h-9 px-3 text-sm rounded-lg border border-border bg-transparent disabled:opacity-60"
                  />
                  <button v-if="!locked" type="button" class="text-xs text-red-600 hover:underline" @click="removeOption(q, oi)">
                    {{ t('common.delete') }}
                  </button>
                </div>
                <button v-if="!locked" type="button" class="text-xs text-primary hover:underline" @click="addOption(q)">
                  + {{ t('surveys.form.addOption') }}
                </button>
                <p v-if="q.questionType === 'scale'" class="text-xs text-muted-foreground">{{ t('surveys.form.scaleHint') }}</p>
              </div>
            </div>
            <div v-if="!locked" class="flex flex-col gap-1 text-xs">
              <button type="button" class="text-primary hover:underline disabled:opacity-30" :disabled="qi === 0" @click="move(qi, -1)">↑</button>
              <button type="button" class="text-primary hover:underline disabled:opacity-30" :disabled="qi === questions.length - 1" @click="move(qi, 1)">↓</button>
              <button type="button" class="text-red-600 hover:underline" @click="removeQuestion(qi)">✕</button>
            </div>
          </div>
        </div>

        <button
          v-if="!locked" type="button"
          class="px-4 py-2 text-sm rounded-lg border border-dashed border-border hover:bg-accent"
          @click="addQuestion"
        >
          + {{ t('surveys.form.addQuestion') }}
        </button>
      </section>

      <p v-if="fieldError" class="text-sm text-red-600">{{ fieldError }}</p>
      <p v-if="errorMsg" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ errorMsg }}</p>

      <div class="flex justify-end gap-2">
        <button type="button" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent" @click="router.push({ name: 'surveys' })">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit" :disabled="store.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ store.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
