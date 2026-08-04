/**
 * Package store — package definitions and student packages.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { financeService } from '@/services/finance.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  PackageDefinitionDto,
  PackageDefinitionListItemDto,
  PackageDefinitionListQuery,
  CreatePackageDefinitionPayload,
  UpdatePackageDefinitionPayload,
  StudentPackageDto,
  StudentPackageListItemDto,
  StudentPackageListQuery,
  PackageBalanceDto,
  CreateStudentPackagePayload,
} from '@/types/finance.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const usePackageStore = defineStore('package', () => {
  const definitionList = ref<PaginatedResult<PackageDefinitionListItemDto>>(emptyPage<PackageDefinitionListItemDto>())
  const currentDefinition = ref<PackageDefinitionDto | null>(null)
  const studentPackageList = ref<PaginatedResult<StudentPackageListItemDto>>(emptyPage<StudentPackageListItemDto>())
  const currentStudentPackage = ref<StudentPackageDto | null>(null)
  const packageBalance = ref<PackageBalanceDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Package Definitions ──────────────────────────────────────────────────────

  async function fetchDefinitions(query: PackageDefinitionListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listPackageDefinitions(query)
      if (res.success && res.data) definitionList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchDefinition(id: string) {
    loading.value = true
    try {
      const res = await financeService.getPackageDefinition(id)
      if (res.success && res.data) currentDefinition.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createDefinition(payload: CreatePackageDefinitionPayload): Promise<PackageDefinitionDto> {
    saving.value = true
    try {
      const res = await financeService.createPackageDefinition(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Paket tanımı oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function updateDefinition(id: string, payload: UpdatePackageDefinitionPayload): Promise<PackageDefinitionDto> {
    saving.value = true
    try {
      const res = await financeService.updatePackageDefinition(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Paket tanımı güncellenemedi.')
      currentDefinition.value = res.data
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function activateDefinition(id: string) {
    saving.value = true
    try {
      await financeService.activatePackageDefinition(id)
      if (currentDefinition.value?.id === id) await fetchDefinition(id)
    } finally {
      saving.value = false
    }
  }

  async function deactivateDefinition(id: string) {
    saving.value = true
    try {
      await financeService.deactivatePackageDefinition(id)
      if (currentDefinition.value?.id === id) await fetchDefinition(id)
    } finally {
      saving.value = false
    }
  }

  async function deleteDefinition(id: string) {
    saving.value = true
    try {
      await financeService.deletePackageDefinition(id)
      if (currentDefinition.value?.id === id) currentDefinition.value = null
    } finally {
      saving.value = false
    }
  }

  // ── Student Packages ─────────────────────────────────────────────────────────

  async function fetchStudentPackages(query: StudentPackageListQuery) {
    loading.value = true
    error.value = null
    try {
      const res = await financeService.listStudentPackages(query)
      if (res.success && res.data) studentPackageList.value = res.data
    } catch (e: unknown) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  async function fetchStudentPackage(id: string) {
    loading.value = true
    try {
      const res = await financeService.getStudentPackage(id)
      if (res.success && res.data) currentStudentPackage.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function fetchPackageBalance(id: string) {
    loading.value = true
    try {
      const res = await financeService.getPackageBalance(id)
      if (res.success && res.data) packageBalance.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function createStudentPackage(payload: CreateStudentPackagePayload): Promise<StudentPackageDto> {
    saving.value = true
    try {
      const res = await financeService.createStudentPackage(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Öğrenci paketi oluşturulamadı.')
      return res.data
    } finally {
      saving.value = false
    }
  }

  async function cancelStudentPackage(id: string, reason?: string) {
    saving.value = true
    try {
      await financeService.cancelStudentPackage(id, { reason })
      if (currentStudentPackage.value?.id === id) await fetchStudentPackage(id)
    } finally {
      saving.value = false
    }
  }

  function clearCurrent() {
    currentDefinition.value = null
    currentStudentPackage.value = null
    packageBalance.value = null
  }

  return {
    definitionList, currentDefinition,
    studentPackageList, currentStudentPackage, packageBalance,
    loading, saving, error,
    fetchDefinitions, fetchDefinition, createDefinition, updateDefinition,
    activateDefinition, deactivateDefinition, deleteDefinition,
    fetchStudentPackages, fetchStudentPackage, fetchPackageBalance,
    createStudentPackage, cancelStudentPackage,
    clearCurrent,
  }
})
