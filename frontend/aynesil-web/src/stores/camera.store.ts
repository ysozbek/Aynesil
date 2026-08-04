/**
 * Camera Management store — cameras, assignments, authorizations, viewing logs.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { cameraService } from '@/services/camera.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  CameraListItemDto,
  CameraDto,
  ViewingAuthorizationDto,
  ViewingLogDto,
  CameraListQuery,
  ViewingAuthorizationQuery,
  ViewingLogQuery,
  CreateCameraPayload,
  UpdateCameraPayload,
  AssignRoomPayload,
  AssignSessionPayload,
  CreateAuthorizationPayload,
  StartViewingPayload,
} from '@/types/camera.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useCameraStore = defineStore('camera', () => {
  const cameraList = ref<PaginatedResult<CameraListItemDto>>(emptyPage<CameraListItemDto>())
  const currentCamera = ref<CameraDto | null>(null)
  const authorizations = ref<PaginatedResult<ViewingAuthorizationDto>>(emptyPage<ViewingAuthorizationDto>())
  const currentAuthorization = ref<ViewingAuthorizationDto | null>(null)
  const viewingLogs = ref<PaginatedResult<ViewingLogDto>>(emptyPage<ViewingLogDto>())
  const activeLogId = ref<number | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Cameras ────────────────────────────────────────────────────────────────

  async function fetchCameras(query: CameraListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await cameraService.list(query)
      if (res.success && res.data) cameraList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchCamera(id: string) {
    loading.value = true
    error.value = null
    try {
      const res = await cameraService.get(id)
      if (res.success && res.data) currentCamera.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createCamera(payload: CreateCameraPayload): Promise<CameraDto> {
    saving.value = true
    try {
      const res = await cameraService.create(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kamera oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateCamera(id: string, payload: UpdateCameraPayload): Promise<CameraDto> {
    saving.value = true
    try {
      const res = await cameraService.update(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kamera güncellenemedi.')
      currentCamera.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function toggleActive(id: string, isActive: boolean) {
    saving.value = true
    try {
      const res = await cameraService.setActive(id, isActive)
      if (!res.success) throw new Error(res.message ?? 'Durum güncellenemedi.')
      if (currentCamera.value?.id === id) currentCamera.value = { ...currentCamera.value, isActive }
      const item = cameraList.value.items.find(c => c.id === id)
      if (item) item.isActive = isActive
    } finally {
      saving.value = false
    }
  }

  // ── Assignments ────────────────────────────────────────────────────────────

  async function assignRoom(cameraId: string, payload: AssignRoomPayload) {
    saving.value = true
    try {
      const res = await cameraService.assignRoom(cameraId, payload)
      if (!res.success) throw new Error(res.message ?? 'Oda ataması yapılamadı.')
      await fetchCamera(cameraId)
    } finally {
      saving.value = false
    }
  }

  async function removeRoom(cameraId: string, roomId: string) {
    saving.value = true
    try {
      await cameraService.removeRoom(cameraId, roomId)
      await fetchCamera(cameraId)
    } finally {
      saving.value = false
    }
  }

  async function assignSession(cameraId: string, payload: AssignSessionPayload) {
    saving.value = true
    try {
      const res = await cameraService.assignSession(cameraId, payload)
      if (!res.success) throw new Error(res.message ?? 'Seans ataması yapılamadı.')
      await fetchCamera(cameraId)
    } finally {
      saving.value = false
    }
  }

  async function removeSession(cameraId: string, sessionId: string) {
    saving.value = true
    try {
      await cameraService.removeSession(cameraId, sessionId)
      await fetchCamera(cameraId)
    } finally {
      saving.value = false
    }
  }

  // ── Authorizations ────────────────────────────────────────────────────────

  async function fetchAuthorizations(query: ViewingAuthorizationQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await cameraService.listAuthorizations(query)
      if (res.success && res.data) authorizations.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchAuthorization(id: string) {
    loading.value = true
    try {
      const res = await cameraService.getAuthorization(id)
      if (res.success && res.data) currentAuthorization.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function createAuthorization(payload: CreateAuthorizationPayload): Promise<ViewingAuthorizationDto> {
    saving.value = true
    try {
      const res = await cameraService.createAuthorization(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Yetki oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function revokeAuthorization(id: string) {
    saving.value = true
    try {
      const res = await cameraService.revokeAuthorization(id)
      if (!res.success) throw new Error(res.message ?? 'Yetki iptal edilemedi.')
      await fetchAuthorizations({})
    } finally {
      saving.value = false
    }
  }

  // ── Viewing ────────────────────────────────────────────────────────────────

  async function startViewing(payload: StartViewingPayload) {
    saving.value = true
    try {
      const res = await cameraService.startViewing(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'İzleme başlatılamadı.')
      activeLogId.value = res.data.logId
    } finally {
      saving.value = false
    }
  }

  async function endViewing() {
    if (!activeLogId.value) return
    saving.value = true
    try {
      await cameraService.endViewing(activeLogId.value)
      activeLogId.value = null
    } finally {
      saving.value = false
    }
  }

  async function fetchViewingLogs(query: ViewingLogQuery) {
    loading.value = true
    try {
      const res = await cameraService.listViewingLogs(query)
      if (res.success && res.data) viewingLogs.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  function clearCurrent() {
    currentCamera.value = null
    currentAuthorization.value = null
  }

  return {
    cameraList, currentCamera, authorizations, currentAuthorization,
    viewingLogs, activeLogId, loading, saving, error,
    fetchCameras, fetchCamera, createCamera, updateCamera, toggleActive,
    assignRoom, removeRoom, assignSession, removeSession,
    fetchAuthorizations, fetchAuthorization, createAuthorization, revokeAuthorization,
    startViewing, endViewing, fetchViewingLogs, clearCurrent,
  }
})
