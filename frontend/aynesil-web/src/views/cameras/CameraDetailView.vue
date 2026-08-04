<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const cameraStore = useCameraStore()
const { can } = usePermission()

const id = route.params.id as string
const cam = computed(() => cameraStore.currentCamera)
const showRoomModal = ref(false)
const roomIdInput = ref('')

function formatDate(dt: string) {
  return new Date(dt).toLocaleString('tr-TR')
}

async function doAssignRoom() {
  if (!cam.value || !roomIdInput.value) return
  await cameraStore.assignRoom(cam.value.id, { roomId: roomIdInput.value })
  showRoomModal.value = false
  roomIdInput.value = ''
}

onMounted(() => {
  cameraStore.clearCurrent()
  cameraStore.fetchCamera(id)
})
</script>

<template>
  <div>
    <div v-if="cameraStore.loading" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('common.loading') }}
    </div>
    <div v-else-if="!cam" class="py-16 text-center text-muted-foreground text-sm">
      {{ t('errors.notFound') }}
    </div>
    <template v-else>
      <PageHeader :title="cam.name" :description="cam.code">
        <div class="flex flex-wrap items-center gap-2">
          <span
            :class="[
              'px-2.5 py-1 rounded-full text-xs font-medium',
              cam.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700',
            ]"
          >
            {{ cam.isActive ? t('common.active') : t('common.passive') }}
          </span>
          <button
            @click="router.push({ name: 'cameras' })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.back') }}
          </button>
          <button
            v-if="can('camera:update')"
            @click="router.push({ name: 'camera-edit', params: { id: cam.id } })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm p-5">
          <h3 class="font-semibold text-foreground mb-4">{{ t('camera.detail.info') }}</h3>
          <dl class="space-y-4 text-sm">
            <div>
              <dt class="text-xs text-muted-foreground mb-1">{{ t('camera.fields.code') }}</dt>
              <dd class="font-medium text-foreground">{{ cam.code }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-foreground mb-1">{{ t('camera.fields.type') }}</dt>
              <dd class="font-medium text-foreground">{{ cam.cameraTypeCode ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-foreground mb-1">{{ t('camera.fields.campus') }}</dt>
              <dd class="font-medium text-foreground">{{ cam.campusName ?? '—' }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted-foreground mb-1">{{ t('camera.fields.streamRef') }}</dt>
              <dd class="font-medium text-foreground truncate">{{ cam.streamRef ?? '—' }}</dd>
            </div>
          </dl>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
          <div class="flex items-center justify-between p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('camera.detail.roomAssignments') }}</h3>
            <button
              v-if="can('camera:update')"
              @click="showRoomModal = true"
              class="p-1.5 rounded-lg hover:bg-accent text-primary"
              :title="t('camera.detail.assignRoom')"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </button>
          </div>
          <div v-if="cam.roomAssignments.length === 0" class="py-10 text-center text-sm text-muted-foreground">
            {{ t('camera.detail.noRooms') }}
          </div>
          <div v-else class="divide-y divide-border">
            <div
              v-for="r in cam.roomAssignments"
              :key="r.id"
              class="flex items-center justify-between px-4 py-3"
            >
              <div class="min-w-0">
                <p class="text-sm font-medium text-foreground truncate">{{ r.roomName ?? r.roomCode }}</p>
                <p class="text-xs text-muted-foreground">{{ r.roomCode }}</p>
              </div>
              <button
                v-if="can('camera:update')"
                @click="cameraStore.removeRoom(cam.id, r.roomId)"
                class="p-1.5 rounded-lg hover:bg-red-50 text-red-600 shrink-0"
                :title="t('common.delete')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-border bg-[--color-card] shadow-sm overflow-hidden">
          <div class="p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('camera.detail.sessionAssignments') }}</h3>
          </div>
          <div v-if="cam.sessionAssignments.length === 0" class="py-10 text-center text-sm text-muted-foreground">
            {{ t('camera.detail.noSessions') }}
          </div>
          <div v-else class="divide-y divide-border">
            <div
              v-for="s in cam.sessionAssignments"
              :key="s.id"
              class="flex items-center justify-between px-4 py-3"
            >
              <div class="min-w-0">
                <p class="text-sm font-medium text-foreground">{{ formatDate(s.sessionStartsAt) }}</p>
                <p class="text-xs text-muted-foreground">{{ formatDate(s.sessionEndsAt) }}</p>
              </div>
              <button
                v-if="can('camera:update')"
                @click="cameraStore.removeSession(cam.id, s.sessionId)"
                class="p-1.5 rounded-lg hover:bg-red-50 text-red-600 shrink-0"
                :title="t('common.delete')"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>

      <div
        v-if="cam.streamRef && cam.isActive"
        class="mt-6 rounded-xl border border-red-200 bg-red-50/50 p-5 flex items-center justify-between"
      >
        <div class="flex items-center gap-3">
          <span class="relative flex h-3 w-3">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75" />
            <span class="relative inline-flex rounded-full h-3 w-3 bg-red-500" />
          </span>
          <h3 class="font-semibold text-red-700">{{ t('camera.detail.liveView') }}</h3>
        </div>
        <button
          @click="router.push(`/cameras/${cam.id}/live`)"
          class="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
          {{ t('camera.detail.watchLive') }}
        </button>
      </div>
    </template>

    <FormModal
      :open="showRoomModal"
      :title="t('camera.detail.assignRoom')"
      :saving="cameraStore.saving"
      @close="showRoomModal = false"
      @submit="doAssignRoom"
    >
      <div>
        <label class="block text-sm font-medium text-foreground mb-1">{{ t('camera.fields.room') }} *</label>
        <input
          v-model="roomIdInput"
          type="text"
          :placeholder="t('camera.detail.roomIdPlaceholder')"
          class="w-full h-10 px-3 text-sm rounded-lg border border-border bg-transparent focus:outline-none focus:ring-1 focus:ring-primary"
        />
      </div>
    </FormModal>
  </div>
</template>
