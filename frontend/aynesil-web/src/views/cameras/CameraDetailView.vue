<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCameraStore } from '@/stores/camera.store'
import { usePermission } from '@/composables/usePermission'
import PageHeader from '@/components/shared/PageHeader.vue'
import FormModal from '@/components/shared/FormModal.vue'
import RoomPicker from '@/components/pickers/RoomPicker.vue'
import StatusBadge from '@/components/shared/StatusBadge.vue'
import { Permissions } from '@/constants/permissions'

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
          <StatusBadge :value="Boolean(cam.isActive)" />
          <button
            @click="router.push({ name: 'cameras' })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.back') }}
          </button>
          <button
            v-if="can(Permissions.Camera.Update)"
            @click="router.push({ name: 'camera-edit', params: { id: cam.id } })"
            class="px-3 py-1.5 text-sm rounded-lg border border-border hover:bg-accent"
          >
            {{ t('common.edit') }}
          </button>
        </div>
      </PageHeader>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="rounded-xl border border-border bg-card shadow-sm p-5">
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

        <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
          <div class="flex items-center justify-between p-4 border-b border-border">
            <h3 class="font-semibold text-foreground">{{ t('camera.detail.roomAssignments') }}</h3>
            <button
              v-if="can(Permissions.Camera.Update)"
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
                v-if="can(Permissions.Camera.Update)"
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

        <div class="rounded-xl border border-border bg-card shadow-sm overflow-hidden">
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
                v-if="can(Permissions.Camera.Update)"
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

      <!--
        "Canlı İzle" düğmesi kaldırıldı: /cameras/:id/live diye bir sayfa yok (router'da tanımsız → 404) ve
        uygulamada akış oynatıcısı bulunmuyor. streamRef yalnızca bir akış referansı (URL/ID) saklıyor;
        backend'deki izleme günlüğü uçları (/cameras/viewing/start|end) bir oynatıcı ekranı yazıldığında
        oradan çağrılacak. O zamana kadar düğme kullanıcıyı boş sayfaya göndermesin diye gösterilmiyor.
      -->
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
        <RoomPicker v-model="roomIdInput" :campus-id="cam?.campusId" />
      </div>
    </FormModal>
  </div>
</template>
