<template>
  <div class="p-6 space-y-6 max-w-3xl mx-auto">
    <div class="flex items-center gap-3">
      <button class="btn btn-ghost btn-sm" @click="$router.back()">← {{ $t('common.back') }}</button>
      <h1 class="text-2xl font-bold text-gray-900">
        {{ isEdit ? $t('meeting.form.editTitle') : $t('meeting.form.createTitle') }}
      </h1>
    </div>

    <div v-if="store.loading && isEdit" class="flex justify-center py-10">
      <span class="loading loading-spinner loading-lg text-primary"></span>
    </div>

    <form v-else @submit.prevent="submit" class="space-y-6">
      <div class="card bg-base-100 shadow">
        <div class="card-body space-y-4">
          <!-- Title -->
          <div class="form-control">
            <label class="label"><span class="label-text">{{ $t('meeting.fields.title') }} *</span></label>
            <input
              v-model="form.title"
              type="text"
              :class="['input input-bordered', errors.title ? 'input-error' : '']"
              :placeholder="$t('meeting.form.titlePlaceholder')"
            />
            <label v-if="errors.title" class="label"><span class="label-text-alt text-error">{{ errors.title }}</span></label>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <!-- Scheduled At -->
            <div class="form-control">
              <label class="label"><span class="label-text">{{ $t('meeting.fields.scheduledAt') }}</span></label>
              <input v-model="form.scheduledAt" type="datetime-local" class="input input-bordered" />
            </div>
            <!-- Ends At -->
            <div class="form-control">
              <label class="label"><span class="label-text">{{ $t('meeting.fields.endsAt') }}</span></label>
              <input v-model="form.endsAt" type="datetime-local" class="input input-bordered" />
            </div>
          </div>

          <!-- Location -->
          <div class="form-control">
            <label class="label"><span class="label-text">{{ $t('meeting.fields.location') }}</span></label>
            <input v-model="form.location" type="text" class="input input-bordered" :placeholder="$t('meeting.form.locationPlaceholder')" />
          </div>
        </div>
      </div>

      <!-- Participants (new meeting only) -->
      <div v-if="!isEdit" class="card bg-base-100 shadow">
        <div class="card-body space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold text-base">{{ $t('meeting.participants.title') }}</h2>
            <button type="button" class="btn btn-ghost btn-sm" @click="addParticipant">
              + {{ $t('meeting.participants.add') }}
            </button>
          </div>
          <div
            v-for="(p, idx) in form.participants"
            :key="idx"
            class="border rounded-lg p-3 space-y-2"
          >
            <div class="flex items-center justify-between">
              <div class="form-control w-40">
                <label class="label label-text text-xs">{{ $t('meeting.participants.type') }}</label>
                <select v-model="p.participantType" class="select select-sm select-bordered">
                  <option value="user">User</option>
                  <option value="guardian">Guardian</option>
                  <option value="lead">Lead</option>
                  <option value="external">External</option>
                </select>
              </div>
              <button type="button" class="btn btn-ghost btn-xs text-error" @click="removeParticipant(idx)">{{ $t('common.delete') }}</button>
            </div>
            <div v-if="p.participantType === 'external'" class="form-control">
              <label class="label label-text text-xs">{{ $t('meeting.participants.externalName') }}</label>
              <input v-model="p.externalName" type="text" class="input input-sm input-bordered" />
            </div>
          </div>
        </div>
      </div>

      <div v-if="submitError" class="alert alert-error"><span>{{ submitError }}</span></div>

      <div class="flex justify-end gap-3">
        <button type="button" class="btn btn-ghost" @click="$router.back()">{{ $t('common.cancel') }}</button>
        <button type="submit" class="btn btn-primary" :disabled="store.saving">
          <span v-if="store.saving" class="loading loading-spinner loading-sm"></span>
          {{ store.saving ? $t('common.saving') : $t('common.save') }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useMeetingStore } from '@/stores/meeting.store'
import { useAuthStore } from '@/stores/auth.store'

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
  if (!form.value.title.trim()) errors.value.title = 'Başlık zorunludur.'
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
