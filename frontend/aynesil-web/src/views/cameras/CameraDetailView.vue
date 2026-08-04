<template>
  <div class="container-xxl py-6">
    <div class="mb-5">
      <RouterLink to="/cameras" class="btn btn-sm btn-light">
        <i class="ki-outline ki-arrow-left fs-4 me-1"></i>{{ $t('common.back') }}
      </RouterLink>
    </div>

    <div v-if="cameraStore.loading" class="text-center py-20">
      <div class="spinner-border text-primary"></div>
    </div>

    <div v-else-if="!cam" class="text-center py-20 text-muted">{{ $t('errors.notFound') }}</div>

    <div v-else>
      <div class="d-flex align-items-center justify-content-between mb-6">
        <div>
          <h1 class="text-gray-900 fw-bold fs-2">{{ cam.name }}</h1>
          <p class="text-muted mb-0">{{ cam.code }}</p>
        </div>
        <div class="d-flex gap-2">
          <span :class="cam.isActive ? 'badge badge-light-success fs-7' : 'badge badge-light-danger fs-7'">
            {{ cam.isActive ? $t('common.active') : $t('common.passive') }}
          </span>
          <RouterLink v-if="hasPermission('camera:update')" :to="`/cameras/${cam.id}/edit`" class="btn btn-sm btn-light">
            <i class="ki-outline ki-pencil fs-4 me-1"></i>{{ $t('common.edit') }}
          </RouterLink>
        </div>
      </div>

      <div class="row g-6">
        <!-- Info -->
        <div class="col-xl-4">
          <div class="card mb-6">
            <div class="card-header border-0"><h3 class="card-title fw-bold">{{ $t('camera.detail.info') }}</h3></div>
            <div class="card-body pt-0">
              <div class="mb-4">
                <div class="text-muted fs-7 mb-1">{{ $t('camera.fields.code') }}</div>
                <div class="fw-semibold">{{ cam.code }}</div>
              </div>
              <div class="mb-4">
                <div class="text-muted fs-7 mb-1">{{ $t('camera.fields.type') }}</div>
                <div class="fw-semibold">{{ cam.cameraTypeCode ?? '—' }}</div>
              </div>
              <div class="mb-4">
                <div class="text-muted fs-7 mb-1">{{ $t('camera.fields.campus') }}</div>
                <div class="fw-semibold">{{ cam.campusName ?? '—' }}</div>
              </div>
              <div class="mb-4">
                <div class="text-muted fs-7 mb-1">{{ $t('camera.fields.streamRef') }}</div>
                <div class="fw-semibold text-truncate">{{ cam.streamRef ?? '—' }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Room Assignments -->
        <div class="col-xl-4">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('camera.detail.roomAssignments') }}</h3>
              <div v-if="hasPermission('camera:update')" class="card-toolbar">
                <button class="btn btn-sm btn-light-primary" @click="showRoomModal = true">
                  <i class="ki-outline ki-plus fs-4"></i>
                </button>
              </div>
            </div>
            <div class="card-body pt-0">
              <div v-if="cam.roomAssignments.length === 0" class="text-muted text-center py-6">{{ $t('camera.detail.noRooms') }}</div>
              <div v-else>
                <div v-for="r in cam.roomAssignments" :key="r.id" class="d-flex align-items-center justify-content-between mb-3 p-3 rounded bg-light">
                  <div>
                    <div class="fw-semibold">{{ r.roomName ?? r.roomCode }}</div>
                    <div class="text-muted fs-8">{{ r.roomCode }}</div>
                  </div>
                  <button v-if="hasPermission('camera:update')" class="btn btn-sm btn-icon btn-light-danger" @click="cameraStore.removeRoom(cam!.id, r.roomId)">
                    <i class="ki-outline ki-cross fs-4"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Session Assignments -->
        <div class="col-xl-4">
          <div class="card mb-6">
            <div class="card-header border-0">
              <h3 class="card-title fw-bold">{{ $t('camera.detail.sessionAssignments') }}</h3>
            </div>
            <div class="card-body pt-0">
              <div v-if="cam.sessionAssignments.length === 0" class="text-muted text-center py-6">{{ $t('camera.detail.noSessions') }}</div>
              <div v-else>
                <div v-for="s in cam.sessionAssignments" :key="s.id" class="d-flex align-items-center justify-content-between mb-3 p-3 rounded bg-light">
                  <div>
                    <div class="fw-semibold">{{ formatDate(s.sessionStartsAt) }}</div>
                    <div class="text-muted fs-8">{{ formatDate(s.sessionEndsAt) }}</div>
                  </div>
                  <button v-if="hasPermission('camera:update')" class="btn btn-sm btn-icon btn-light-danger" @click="cameraStore.removeSession(cam!.id, s.sessionId)">
                    <i class="ki-outline ki-cross fs-4"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Live View -->
      <div v-if="cam.streamRef && cam.isActive" class="card mt-2">
        <div class="card-header border-0">
          <h3 class="card-title fw-bold text-danger">
            <span class="pulse pulse-danger me-2"><span class="pulse-ring"></span></span>
            {{ $t('camera.detail.liveView') }}
          </h3>
          <div class="card-toolbar">
            <RouterLink :to="`/cameras/${cam.id}/live`" class="btn btn-danger">
              <i class="ki-outline ki-eye fs-4 me-1"></i>{{ $t('camera.detail.watchLive') }}
            </RouterLink>
          </div>
        </div>
      </div>
    </div>

    <!-- Room Assignment Modal -->
    <div v-if="showRoomModal" class="modal fade show d-block" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">{{ $t('camera.detail.assignRoom') }}</h5>
            <button class="btn-close" @click="showRoomModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label required">{{ $t('camera.fields.room') }}</label>
            <input v-model="roomIdInput" type="text" class="form-control" :placeholder="$t('camera.detail.roomIdPlaceholder')" />
          </div>
          <div class="modal-footer">
            <button class="btn btn-light" @click="showRoomModal = false">{{ $t('common.cancel') }}</button>
            <button class="btn btn-primary" :disabled="cameraStore.saving || !roomIdInput" @click="doAssignRoom">
              <span v-if="cameraStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ $t('common.save') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useCameraStore } from '@/stores/camera.store'
import { useAuthStore } from '@/stores/auth.store'

const route = useRoute()
const cameraStore = useCameraStore()
const authStore = useAuthStore()
const id = route.params.id as string
const cam = computed(() => cameraStore.currentCamera)
const showRoomModal = ref(false)
const roomIdInput = ref('')

function hasPermission(p: string) { return authStore.hasPermission(p) }
function formatDate(dt: string) { return new Date(dt).toLocaleString('tr-TR') }

async function doAssignRoom() {
  if (!cam.value) return
  await cameraStore.assignRoom(cam.value.id, { roomId: roomIdInput.value })
  showRoomModal.value = false
  roomIdInput.value = ''
}

onMounted(() => {
  cameraStore.clearCurrent()
  cameraStore.fetchCamera(id)
})
</script>
