<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useMeetingStore } from '@/stores/meeting.store'
import { useAuthStore } from '@/stores/auth.store'
import PageHeader from '@/components/shared/PageHeader.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const store = useMeetingStore()
const auth = useAuthStore()

const isEdit = computed(() => !!route.params.id)
const submitError = ref<string | null>(null)

const form = ref({
  title: '',
  scheduledAt: '',
  endsAt: '',
  location: '',
  participants: [] as Array<{ participantType: string; externalName?: string }>,
})
const errors = ref<Record<string, string>>({})

function addParticipant() {
  form.value.participants.push({ participantType: 'user' })
}

function removeParticipant(idx: number) {
  form.value.participants.splice(idx, 1)
}

function validate(): boolean {
  errors.value = {}
  if (!form.value.title.trim()) {
    errors.value.title = 'Başlık zorunludur.'
  }
  return Object.keys(errors.value).length === 0
}

async function submit() {
  if (!validate()) return
  submitError.value = null
  try {
    if (isEdit.value) {
      const m = store.currentMeeting!
      await store.updateMeeting(route.params.id as string, {
        title: form.value.title,
        location: form.value.location || undefined,
        scheduledAt: form.value.scheduledAt || undefined,
        endsAt: form.value.endsAt || undefined,
        rowVersion: m.rowVersion,
      })
    } else {
      const created = await store.scheduleMeeting({
        corporationId: auth.user!.corporationId!,
        title: form.value.title,
        location: form.value.location || undefined,
        scheduledAt: form.value.scheduledAt || undefined,
        endsAt: form.value.endsAt || undefined,
        participants: form.value.participants.length ? form.value.participants : undefined,
      })
      router.push({ name: 'meeting-detail', params: { id: created.id } })
      return
    }
    router.push({ name: 'meeting-detail', params: { id: route.params.id } })
  } catch (e: unknown) {
    submitError.value = (e as Error).message
  }
}

onMounted(async () => {
  if (isEdit.value) {
    await store.fetchMeeting(route.params.id as string)
    if (store.currentMeeting) {
      const m = store.currentMeeting
      form.value.title = m.title
      form.value.location = m.location ?? ''
      form.value.scheduledAt = m.scheduledAt ? m.scheduledAt.slice(0, 16) : ''
      form.value.endsAt = m.endsAt ? m.endsAt.slice(0, 16) : ''
    }
  }
})
</script>

<template>
  <div>
    <PageHeader :title="isEdit ? t('meeting.form.editTitle') : t('meeting.form.createTitle')">
      <button
        @click="router.back()"
        class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent"
      >
        {{ t('common.back') }}
      </button>
    </PageHeader>

    <div v-if="store.loading && isEdit" class="py-16 text-center text-sm text-muted-foreground">
      {{ t('common.loading') }}
    </div>

    <form
      v-else
      class="max-w-2xl rounded-xl border border-border bg-[--color-card] shadow-sm p-6 space-y-4"
      @submit.prevent="submit"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.fields.title') }} *</label>
        <input
          v-model="form.title"
          type="text"
          :placeholder="t('meeting.form.titlePlaceholder')"
          :class="[
            'w-full h-10 px-3 text-sm rounded-lg border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary',
            errors.title ? 'border-red-400' : 'border-border',
          ]"
        />
        <p v-if="errors.title" class="text-xs text-red-600 mt-1">{{ errors.title }}</p>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.fields.scheduledAt') }}</label>
          <input v-model="form.scheduledAt" type="datetime-local" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.fields.endsAt') }}</label>
          <input v-model="form.endsAt" type="datetime-local" class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent" />
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('meeting.fields.location') }}</label>
        <input
          v-model="form.location"
          type="text"
          :placeholder="t('meeting.form.locationPlaceholder')"
          class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent"
        />
      </div>

      <div v-if="!isEdit" class="pt-2 border-t border-border space-y-4">
        <div class="flex items-center justify-between">
          <h3 class="font-semibold text-foreground text-sm">{{ t('meeting.participants.title') }}</h3>
          <button
            type="button"
            @click="addParticipant"
            class="text-xs text-primary hover:underline"
          >
            + {{ t('meeting.participants.add') }}
          </button>
        </div>
        <div
          v-for="(p, idx) in form.participants"
          :key="idx"
          class="rounded-lg border border-border p-3 space-y-3"
        >
          <div class="flex items-center justify-between gap-3">
            <div class="flex-1">
              <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('meeting.participants.type') }}</label>
              <select v-model="p.participantType" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent">
                <option value="user">User</option>
                <option value="guardian">Guardian</option>
                <option value="lead">Lead</option>
                <option value="external">External</option>
              </select>
            </div>
            <button
              type="button"
              @click="removeParticipant(idx)"
              class="mt-5 px-2 py-1 text-xs rounded-lg text-red-600 hover:bg-red-50"
            >
              {{ t('common.delete') }}
            </button>
          </div>
          <div v-if="p.participantType === 'external'">
            <label class="block text-xs font-medium text-muted-foreground mb-1">{{ t('meeting.participants.externalName') }}</label>
            <input v-model="p.externalName" type="text" class="w-full h-9 px-3 text-sm rounded-lg border border-border bg-transparent" />
          </div>
        </div>
      </div>

      <p v-if="submitError" class="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{{ submitError }}</p>

      <div class="flex justify-end gap-2 pt-2">
        <button type="button" @click="router.back()" class="px-4 py-2 text-sm rounded-lg border border-border hover:bg-accent">
          {{ t('common.cancel') }}
        </button>
        <button
          type="submit"
          :disabled="store.saving"
          class="px-4 py-2 text-sm rounded-lg bg-primary text-primary-foreground font-medium hover:opacity-90 disabled:opacity-50"
        >
          {{ store.saving ? t('common.saving') : t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>
