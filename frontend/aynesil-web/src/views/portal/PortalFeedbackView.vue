<script setup lang="ts">
import { reactive, ref, watch, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSurveyStore } from '@/stores/survey.store'
import { useParentPortalStore } from '@/stores/parentPortal.store'
import { portalService } from '@/services/portal.service'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { PortalSessionDto } from '@/types/portal.types'

const { t } = useI18n()
const store = useSurveyStore()
const portal = useParentPortalStore()

const form = reactive({ studentId: '', sessionId: '', rating: 0, comment: '' })
const sessions = ref<PortalSessionDto[]>([])
const error = ref('')
const done = ref('')

const stars = (n?: number) => (n ? '★'.repeat(n) + '☆'.repeat(5 - n) : '—')

async function loadSessions() {
  form.sessionId = ''
  sessions.value = []
  if (!form.studentId) return
  try {
    const res = await portalService.getSessions({ studentId: form.studentId, page: 1, pageSize: 20 })
    // Yalnızca yapılmış seanslar puanlanabilir; erişim yoksa liste boş kalır (genel geri bildirim yine verilir).
    if (res.success && res.data) sessions.value = res.data.filter((s) => s.status === 'completed')
  } catch { sessions.value = [] }
}

async function submit() {
  error.value = ''; done.value = ''
  if (!form.rating && !form.comment.trim()) { error.value = t('surveys.portal.ratingOrComment'); return }
  try {
    await store.createFeedback({
      sessionId: form.sessionId || null,
      rating: form.rating || null,
      comment: form.comment.trim() || null,
    })
    done.value = t('surveys.portal.feedbackThanks')
    Object.assign(form, { sessionId: '', rating: 0, comment: '' })
    await store.fetchMyFeedback({ page: 1, pageSize: 20, sortDirection: 'desc' })
  } catch (e: unknown) {
    error.value = (e as Error).message
  }
}

watch(() => form.studentId, loadSessions)

onMounted(async () => {
  await portal.fetchMyStudents()
  form.studentId = portal.myStudents[0]?.studentId ?? ''
  await store.fetchMyFeedback({ page: 1, pageSize: 20, sortDirection: 'desc' })
})
</script>

<template>
  <div>
    <PageHeader :title="t('surveys.portal.feedbackTitle')" :description="t('surveys.portal.feedbackSubtitle')" />

    <form class="max-w-2xl rounded-xl border border-border bg-card shadow-sm p-6 space-y-4 mb-8" @submit.prevent="submit">
      <div v-if="portal.myStudents.length > 1">
        <label class="block text-sm font-medium mb-1">{{ t('surveys.portal.child') }}</label>
        <select v-model="form.studentId" class="h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
          <option v-for="s in portal.myStudents" :key="s.studentId" :value="s.studentId">{{ s.fullName }}</option>
        </select>
      </div>

      <div>
        <label class="block text-sm font-medium mb-1">{{ t('surveys.portal.session') }}</label>
        <select v-model="form.sessionId" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent">
          <option value="">{{ t('surveys.portal.generalFeedback') }}</option>
          <option v-for="s in sessions" :key="s.id" :value="s.id">
            {{ new Date(s.startsAt).toLocaleString('tr-TR') }}{{ s.title ? ' — ' + s.title : '' }}
          </option>
        </select>
        <p class="text-xs text-muted-foreground mt-1">{{ t('surveys.portal.sessionHint') }}</p>
      </div>

      <div>
        <label class="block text-sm font-medium mb-1">{{ t('surveys.feedback.rating') }}</label>
        <div class="flex gap-1 text-2xl">
          <button
            v-for="n in 5" :key="n" type="button"
            :class="n <= form.rating ? 'text-amber-500' : 'text-gray-300 hover:text-amber-300'"
            :aria-label="String(n)"
            @click="form.rating = form.rating === n ? 0 : n"
          >★</button>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium mb-1">{{ t('common.comment') }}</label>
        <textarea v-model="form.comment" rows="3" maxlength="2000" class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent" />
      </div>

      <p v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ error }}</p>
      <p v-if="done" class="text-sm text-emerald-700 bg-emerald-50 rounded-lg px-3 py-2">{{ done }}</p>

      <div class="flex justify-end">
        <button
          type="submit" :disabled="store.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ store.saving ? t('common.saving') : t('surveys.portal.send') }}
        </button>
      </div>
    </form>

    <h2 class="text-base font-semibold mb-3">{{ t('surveys.portal.myFeedback') }}</h2>
    <p v-if="!store.feedback.items.length" class="text-sm text-muted-foreground">{{ t('surveys.feedback.noData') }}</p>
    <ul class="space-y-3 max-w-2xl">
      <li v-for="f in store.feedback.items" :key="f.id" class="rounded-xl border border-border bg-card p-4">
        <div class="flex items-center justify-between text-sm">
          <span class="text-amber-500 tracking-wider">{{ stars(f.rating) }}</span>
          <span class="text-xs text-muted-foreground">{{ new Date(f.createdAt).toLocaleString('tr-TR') }}</span>
        </div>
        <p v-if="f.educatorName" class="text-xs text-muted-foreground mt-1">{{ t('surveys.feedback.educator') }}: {{ f.educatorName }}</p>
        <p v-if="f.comment" class="text-sm mt-2">{{ f.comment }}</p>
      </li>
    </ul>
  </div>
</template>
