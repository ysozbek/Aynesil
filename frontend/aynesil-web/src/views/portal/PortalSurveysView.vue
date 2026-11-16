<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { PendingSurveyDto, QuestionAnswerPayload, SurveyQuestionDto } from '@/types/survey.types'

const { t } = useI18n()
const store = useSurveyStore()

interface Draft { studentId: string; values: Record<string, string | number | null> }

const open = ref<string | null>(null)
const drafts = reactive<Record<string, Draft>>({})
const error = ref('')
const done = ref('')

function openSurvey(s: PendingSurveyDto) {
  error.value = ''
  open.value = open.value === s.surveyId ? null : s.surveyId
  if (!drafts[s.surveyId]) drafts[s.surveyId] = { studentId: s.students[0]?.studentId ?? '', values: {} }
}

function isAnswered(q: SurveyQuestionDto, d: Draft) {
  const v = d.values[q.id]
  return v !== undefined && v !== null && String(v).trim() !== ''
}

function toAnswer(q: SurveyQuestionDto, d: Draft): QuestionAnswerPayload {
  const v = d.values[q.id]
  switch (q.questionType) {
    case 'rating': return { questionId: q.id, numericValue: v == null ? null : Number(v) }
    case 'multiple_choice': return { questionId: q.id, answerOptionId: (v as string) || null }
    case 'scale': return q.answerOptions.length
      ? { questionId: q.id, answerOptionId: (v as string) || null }
      : { questionId: q.id, numericValue: v == null ? null : Number(v) }
    default: return { questionId: q.id, answerText: v == null ? null : String(v).trim() }
  }
}

async function submit(s: PendingSurveyDto) {
  const d = drafts[s.surveyId]
  error.value = ''
  if (!d.studentId) { error.value = t('surveys.portal.pickChild'); return }
  const missing = s.questions.find((q) => q.isRequired && !isAnswered(q, d))
  if (missing) { error.value = t('surveys.portal.requiredMissing', { question: missing.questionText }); return }
  try {
    await store.submitResponse({
      surveyId: s.surveyId,
      studentId: d.studentId,
      answers: s.questions.filter((q) => isAnswered(q, d)).map((q) => toAnswer(q, d)),
    })
    done.value = t('surveys.portal.thanks', { title: s.title })
    delete drafts[s.surveyId]
    open.value = null
    await store.fetchPending()
  } catch (e: unknown) {
    error.value = (e as Error).message
  }
}

onMounted(() => store.fetchPending())
</script>

<template>
  <div>
    <PageHeader :title="t('surveys.portal.title')" :description="t('surveys.portal.subtitle')" />

    <p v-if="done" class="mb-4 text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">{{ done }}</p>
    <p v-if="store.error" class="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ store.error }}</p>

    <p v-if="!store.loading && !store.pending.length" class="text-sm text-muted-foreground rounded-xl border border-border bg-card p-8 text-center">
      {{ t('surveys.portal.none') }}
    </p>

    <div class="space-y-4 max-w-3xl">
      <section v-for="s in store.pending" :key="s.surveyId" class="rounded-xl border border-border bg-card shadow-sm">
        <button type="button" class="w-full flex items-center justify-between gap-4 p-5 text-left" @click="openSurvey(s)">
          <div>
            <h2 class="text-base font-semibold text-foreground">{{ s.title }}</h2>
            <p v-if="s.description" class="text-sm text-muted-foreground mt-1">{{ s.description }}</p>
            <p class="text-xs text-muted-foreground mt-1">{{ t('surveys.portal.pendingFor', { names: s.students.map((x) => x.studentName).join(', ') }) }}</p>
          </div>
          <span class="text-sm text-primary shrink-0">{{ open === s.surveyId ? t('common.close') : t('surveys.portal.fill') }}</span>
        </button>

        <form v-if="open === s.surveyId && drafts[s.surveyId]" class="border-t border-border p-5 space-y-5" @submit.prevent="submit(s)">
          <div v-if="s.students.length > 1">
            <label class="block text-sm font-medium mb-1">{{ t('surveys.portal.child') }}</label>
            <select v-model="drafts[s.surveyId].studentId" class="h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
              <option v-for="st in s.students" :key="st.studentId" :value="st.studentId">{{ st.studentName }}</option>
            </select>
          </div>

          <div v-for="(q, qi) in s.questions" :key="q.id" class="space-y-2">
            <p class="text-sm font-medium text-foreground">
              {{ qi + 1 }}. {{ q.questionText }}<span v-if="q.isRequired" class="text-red-600"> *</span>
            </p>

            <div v-if="q.questionType === 'rating'" class="flex gap-2">
              <button
                v-for="n in 5" :key="n" type="button"
                :class="['w-10 h-10 rounded-lg border text-sm font-medium', drafts[s.surveyId].values[q.id] === n ? 'bg-primary text-primary-foreground border-primary' : 'border-border hover:bg-accent']"
                @click="drafts[s.surveyId].values[q.id] = n"
              >{{ n }}</button>
            </div>

            <div v-else-if="q.questionType === 'yes_no'" class="flex gap-4 text-sm">
              <label v-for="opt in ['yes', 'no']" :key="opt" class="flex items-center gap-2">
                <input v-model="drafts[s.surveyId].values[q.id]" type="radio" :name="q.id" :value="opt" />
                {{ t(`common.${opt}`) }}
              </label>
            </div>

            <div v-else-if="q.questionType === 'multiple_choice' || (q.questionType === 'scale' && q.answerOptions.length)" class="space-y-1 text-sm">
              <label v-for="o in q.answerOptions" :key="o.id" class="flex items-center gap-2">
                <input v-model="drafts[s.surveyId].values[q.id]" type="radio" :name="q.id" :value="o.id" />
                {{ o.optionText }}
              </label>
            </div>

            <div v-else-if="q.questionType === 'scale'" class="flex flex-wrap gap-2">
              <button
                v-for="n in 10" :key="n" type="button"
                :class="['w-9 h-9 rounded-lg border text-sm', drafts[s.surveyId].values[q.id] === n ? 'bg-primary text-primary-foreground border-primary' : 'border-border hover:bg-accent']"
                @click="drafts[s.surveyId].values[q.id] = n"
              >{{ n }}</button>
            </div>

            <textarea
              v-else v-model="drafts[s.surveyId].values[q.id] as string" rows="3"
              class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent"
            />
          </div>

          <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ error }}</p>

          <div class="flex justify-end">
            <button
              type="submit" :disabled="store.saving"
              class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
            >
              {{ store.saving ? t('common.saving') : t('surveys.portal.submit') }}
            </button>
          </div>
        </form>
      </section>
    </div>
  </div>
</template>
