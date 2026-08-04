<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { useSessionStore } from '@/stores/session.store'
import { useRefDataStore } from '@/stores/refdata.store'
import PageHeader from '@/components/shared/PageHeader.vue'
import type { RefValueItem } from '@/stores/refdata.store'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const store = useSessionStore()
const refData = useRefDataStore()

const sessionId = route.params.id as string | undefined
const isEdit = !!sessionId
const corporationId = computed(() => auth.user?.corporationId ?? '')

const sessionTypes = ref<RefValueItem[]>([])

const form = reactive({
  campusId: '',
  roomId: '',
  sessionTypeId: '',
  title: '',
  startsAt: '',
  endsAt: '',
})

const errors = reactive<Record<string, string>>({})

onMounted(async () => {
  await refData.getValues('SESSION_TYPE').then(v => { sessionTypes.value = v })
  if (isEdit && sessionId) {
    await store.fetchSession(sessionId)
    const s = store.currentSession
    if (s) {
      form.campusId = s.campusId
      form.roomId = s.roomId ?? ''
      form.sessionTypeId = s.sessionTypeId
      form.title = s.title
      form.startsAt = s.startsAt.slice(0, 16)
      form.endsAt = s.endsAt.slice(0, 16)
    }
  }
})

function validate(): boolean {
  Object.keys(errors).forEach(k => delete (errors as Record<string, string>)[k])
  if (!form.title.trim()) errors.title = t('validation.required', { field: t('scheduling.session.titleField') })
  if (!form.sessionTypeId) errors.sessionTypeId = t('validation.required', { field: t('scheduling.session.type') })
  if (!form.startsAt) errors.startsAt = t('validation.required', { field: t('scheduling.session.startsAt') })
  if (!form.endsAt) errors.endsAt = t('validation.required', { field: t('scheduling.session.endsAt') })
  return Object.keys(errors).length === 0
}

async function submit() {
  if (!validate()) return
  try {
    if (isEdit && sessionId) {
      await store.rescheduleSession(sessionId, {
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
        roomId: form.roomId || undefined,
        rowVersion: store.currentSession!.rowVersion,
      })
      router.push({ name: 'session-detail', params: { id: sessionId } })
    } else {
      const created = await store.createSession({
        corporationId: corporationId.value,
        campusId: form.campusId,
        roomId: form.roomId || undefined,
        sessionTypeId: form.sessionTypeId,
        title: form.title,
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
      })
      router.push({ name: 'session-detail', params: { id: created.id } })
    }
  } catch (e: unknown) {
    errors.submit = (e as Error).message
  }
}
</script>

<template>
  <div>
    <PageHeader
      :title="isEdit ? t('scheduling.session.edit') : t('scheduling.session.create')"
      :description="isEdit ? t('scheduling.session.editDescription') : t('scheduling.session.createDescription')"
    />

    <div class="max-w-2xl">
      <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-5">

        <div v-if="errors.submit" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
          {{ errors.submit }}
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.titleField') }} <span class="text-red-500">*</span>
          </label>
          <input
            v-model="form.title"
            type="text"
            class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.title ? 'border-red-400' : 'border-border'"
          />
          <p v-if="errors.title" class="text-xs text-red-500 mt-1">{{ errors.title }}</p>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">
            {{ t('scheduling.session.type') }} <span class="text-red-500">*</span>
          </label>
          <select
            v-model="form.sessionTypeId"
            class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
            :class="errors.sessionTypeId ? 'border-red-400' : 'border-border'"
          >
            <option value="">{{ t('common.select') }}</option>
            <option v-for="st in sessionTypes" :key="st.id" :value="st.id">{{ st.label }}</option>
          </select>
          <p v-if="errors.sessionTypeId" class="text-xs text-red-500 mt-1">{{ errors.sessionTypeId }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('scheduling.session.startsAt') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.startsAt"
              type="datetime-local"
              class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.startsAt ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.startsAt" class="text-xs text-red-500 mt-1">{{ errors.startsAt }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-foreground mb-1.5">
              {{ t('scheduling.session.endsAt') }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.endsAt"
              type="datetime-local"
              class="w-full px-3 py-2 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
              :class="errors.endsAt ? 'border-red-400' : 'border-border'"
            />
            <p v-if="errors.endsAt" class="text-xs text-red-500 mt-1">{{ errors.endsAt }}</p>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-foreground mb-1.5">{{ t('scheduling.session.room') }}</label>
          <input
            v-model="form.roomId"
            type="text"
            :placeholder="t('scheduling.session.roomIdPlaceholder')"
            class="w-full px-3 py-2 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button
            @click="router.back()"
            class="px-4 py-2 text-sm border border-border rounded-lg hover:bg-accent"
          >{{ t('common.cancel') }}</button>
          <button
            @click="submit"
            :disabled="store.saving"
            class="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
          >
            {{ store.saving ? t('common.saving') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
