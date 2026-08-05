/**
 * School Consultancy Management store.
 * Extended with Agreement and Follow-up Activity actions.
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { consultancyService } from '@/services/consultancy.service'
import type { PaginatedResult } from '@/types/api.types'
import type {
  InstitutionListItemDto, InstitutionDto,
  ConsultancyPlanListItemDto, ConsultancyPlanDto,
  SchoolVisitListItemDto, SchoolVisitDto,
  ConsultancyReportListItemDto,
  ConsultancyAgreementListItemDto, ConsultancyAgreementDto,
  FollowUpActivityListItemDto, FollowUpActivityDto,
  AgreementSummaryDto, OpenFollowUpReportItemDto,
  InstitutionReportDto, ConsultancyOutcomesDto,
  InstitutionListQuery, PlanListQuery, VisitListQuery,
  AgreementListQuery, FollowUpListQuery,
  OpenFollowUpReportQuery, AgreementSummaryQuery,
  CreateInstitutionPayload, UpdateInstitutionPayload,
  CreatePlanPayload, CreateVisitPayload, AddObservationPayload,
  CreateAgreementPayload, UpdateAgreementPayload, SignAgreementPayload,
  CreateFollowUpPayload, UpdateFollowUpPayload,
  CompleteFollowUpPayload,
} from '@/types/consultancy.types'

const emptyPage = <T>(): PaginatedResult<T> => ({
  items: [], totalCount: 0, page: 1, pageSize: 20,
  totalPages: 0, hasPreviousPage: false, hasNextPage: false,
})

export const useConsultancyStore = defineStore('consultancy', () => {
  const institutions = ref<PaginatedResult<InstitutionListItemDto>>(emptyPage<InstitutionListItemDto>())
  const currentInstitution = ref<InstitutionDto | null>(null)
  const plans = ref<PaginatedResult<ConsultancyPlanListItemDto>>(emptyPage<ConsultancyPlanListItemDto>())
  const currentPlan = ref<ConsultancyPlanDto | null>(null)
  const visits = ref<PaginatedResult<SchoolVisitListItemDto>>(emptyPage<SchoolVisitListItemDto>())
  const currentVisit = ref<SchoolVisitDto | null>(null)
  const reports = ref<PaginatedResult<ConsultancyReportListItemDto>>(emptyPage<ConsultancyReportListItemDto>())
  // ── Agreement state ──────────────────────────────────────────────────────────
  const agreements = ref<PaginatedResult<ConsultancyAgreementListItemDto>>(emptyPage<ConsultancyAgreementListItemDto>())
  const currentAgreement = ref<ConsultancyAgreementDto | null>(null)
  const agreementSummary = ref<AgreementSummaryDto[]>([])
  // ── Follow-up state ──────────────────────────────────────────────────────────
  const followUps = ref<PaginatedResult<FollowUpActivityListItemDto>>(emptyPage<FollowUpActivityListItemDto>())
  const currentFollowUp = ref<FollowUpActivityDto | null>(null)
  const openFollowUps = ref<OpenFollowUpReportItemDto[]>([])
  // ── Analytics state ──────────────────────────────────────────────────────────
  const institutionReport = ref<InstitutionReportDto[]>([])
  const outcomes = ref<ConsultancyOutcomesDto[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // ── Institutions ───────────────────────────────────────────────────────────
  async function fetchInstitutions(query: InstitutionListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listInstitutions(query)
      if (res.success && res.data) institutions.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchInstitution(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getInstitution(id)
      if (res.success && res.data) currentInstitution.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createInstitution(payload: CreateInstitutionPayload): Promise<InstitutionDto> {
    saving.value = true
    try {
      const res = await consultancyService.createInstitution(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kurum oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateInstitution(id: string, payload: UpdateInstitutionPayload): Promise<InstitutionDto> {
    saving.value = true
    try {
      const res = await consultancyService.updateInstitution(id, payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Kurum güncellenemedi.')
      currentInstitution.value = res.data
      return res.data
    } finally { saving.value = false }
  }

  // ── Plans ──────────────────────────────────────────────────────────────────
  async function fetchPlans(query: PlanListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listPlans(query)
      if (res.success && res.data) plans.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchPlan(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getPlan(id)
      if (res.success && res.data) currentPlan.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createPlan(payload: CreatePlanPayload): Promise<ConsultancyPlanDto> {
    saving.value = true
    try {
      const res = await consultancyService.createPlan(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Plan oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function activatePlan(id: string) {
    saving.value = true
    try {
      await consultancyService.activatePlan(id)
      await fetchPlan(id)
    } finally { saving.value = false }
  }

  async function completePlan(id: string) {
    saving.value = true
    try {
      await consultancyService.completePlan(id)
      await fetchPlan(id)
    } finally { saving.value = false }
  }

  async function cancelPlan(id: string) {
    saving.value = true
    try {
      await consultancyService.cancelPlan(id)
      await fetchPlan(id)
    } finally { saving.value = false }
  }

  // ── Visits ─────────────────────────────────────────────────────────────────
  async function fetchVisits(query: VisitListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listVisits(query)
      if (res.success && res.data) visits.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchVisit(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getVisit(id)
      if (res.success && res.data) currentVisit.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createVisit(payload: CreateVisitPayload): Promise<SchoolVisitDto> {
    saving.value = true
    try {
      const res = await consultancyService.createVisit(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Ziyaret oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function completeVisit(id: string) {
    saving.value = true
    try {
      await consultancyService.completeVisit(id)
      await fetchVisit(id)
    } finally { saving.value = false }
  }

  async function addObservation(visitId: string, payload: AddObservationPayload) {
    saving.value = true
    try {
      const res = await consultancyService.addObservation(visitId, payload)
      if (!res.success) throw new Error(res.message ?? 'Gözlem eklenemedi.')
      await fetchVisit(visitId)
    } finally { saving.value = false }
  }

  // ── Reports ────────────────────────────────────────────────────────────────
  async function fetchReports(query: Record<string, unknown>) {
    loading.value = true
    try {
      const res = await consultancyService.listReports(query)
      if (res.success && res.data) reports.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createReport(payload: {
    corporationId: string
    title: string
    consultancyPlanId?: string
    schoolVisitId?: string
    summary?: string
    fileId?: string
  }) {
    saving.value = true
    try {
      const res = await consultancyService.createReport(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Rapor oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  // ── Agreements ─────────────────────────────────────────────────────────────

  async function fetchAgreements(query: AgreementListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listAgreements(query)
      if (res.success && res.data) agreements.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchAgreement(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getAgreement(id)
      if (res.success && res.data) currentAgreement.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createAgreement(payload: CreateAgreementPayload): Promise<ConsultancyAgreementDto> {
    saving.value = true
    try {
      const res = await consultancyService.createAgreement(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Sözleşme oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateAgreement(id: string, payload: UpdateAgreementPayload) {
    saving.value = true
    try {
      const res = await consultancyService.updateAgreement(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme güncellenemedi.')
      await fetchAgreement(id)
    } finally { saving.value = false }
  }

  async function sendAgreement(id: string) {
    saving.value = true
    try {
      const res = await consultancyService.sendAgreement(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme gönderilemedi.')
      await fetchAgreement(id)
    } finally { saving.value = false }
  }

  async function signAgreement(id: string, payload: SignAgreementPayload) {
    saving.value = true
    try {
      const res = await consultancyService.signAgreement(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme imzalanamadı.')
      await fetchAgreement(id)
    } finally { saving.value = false }
  }

  async function expireAgreement(id: string) {
    saving.value = true
    try {
      const res = await consultancyService.expireAgreement(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme süresi doldu olarak işaretlenemedi.')
      await fetchAgreement(id)
    } finally { saving.value = false }
  }

  async function cancelAgreement(id: string) {
    saving.value = true
    try {
      const res = await consultancyService.cancelAgreement(id)
      if (!res.success) throw new Error(res.message ?? 'Sözleşme iptal edilemedi.')
      await fetchAgreement(id)
    } finally { saving.value = false }
  }

  async function deleteAgreement(id: string) {
    saving.value = true
    try {
      await consultancyService.deleteAgreement(id)
      currentAgreement.value = null
    } finally { saving.value = false }
  }

  async function fetchAgreementSummary(query: AgreementSummaryQuery) {
    loading.value = true
    try {
      const res = await consultancyService.getAgreementSummary(query)
      if (res.success && res.data) agreementSummary.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  // ── Follow-Ups ─────────────────────────────────────────────────────────────

  async function fetchFollowUps(query: FollowUpListQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.listFollowUps(query)
      if (res.success && res.data) followUps.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchFollowUp(id: string) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getFollowUp(id)
      if (res.success && res.data) currentFollowUp.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function createFollowUp(payload: CreateFollowUpPayload): Promise<FollowUpActivityDto> {
    saving.value = true
    try {
      const res = await consultancyService.createFollowUp(payload)
      if (!res.success || !res.data) throw new Error(res.message ?? 'Takip görevi oluşturulamadı.')
      return res.data
    } finally { saving.value = false }
  }

  async function updateFollowUp(id: string, payload: UpdateFollowUpPayload) {
    saving.value = true
    try {
      const res = await consultancyService.updateFollowUp(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi güncellenemedi.')
      await fetchFollowUp(id)
    } finally { saving.value = false }
  }

  async function startFollowUp(id: string) {
    saving.value = true
    try {
      const res = await consultancyService.startFollowUp(id)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi başlatılamadı.')
      await fetchFollowUp(id)
    } finally { saving.value = false }
  }

  async function completeFollowUp(id: string, payload: CompleteFollowUpPayload) {
    saving.value = true
    try {
      const res = await consultancyService.completeFollowUp(id, payload)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi tamamlanamadı.')
      await fetchFollowUp(id)
    } finally { saving.value = false }
  }

  async function cancelFollowUp(id: string) {
    saving.value = true
    try {
      const res = await consultancyService.cancelFollowUp(id)
      if (!res.success) throw new Error(res.message ?? 'Takip görevi iptal edilemedi.')
      await fetchFollowUp(id)
    } finally { saving.value = false }
  }

  async function deleteFollowUp(id: string) {
    saving.value = true
    try {
      await consultancyService.deleteFollowUp(id)
      currentFollowUp.value = null
    } finally { saving.value = false }
  }

  async function fetchOpenFollowUps(query: OpenFollowUpReportQuery) {
    loading.value = true; error.value = null
    try {
      const res = await consultancyService.getOpenFollowUpsReport(query)
      if (res.success && res.data) openFollowUps.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  // ── Analytics ─────────────────────────────────────────────────────────────
  async function fetchInstitutionReport(query: { corporationId?: string; institutionTypeId?: string }) {
    loading.value = true
    try {
      const res = await consultancyService.getInstitutionReport(query)
      if (res.success && res.data) institutionReport.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  async function fetchOutcomes(query: { corporationId?: string; institutionId?: string; status?: string }) {
    loading.value = true
    try {
      const res = await consultancyService.getOutcomes(query)
      if (res.success && res.data) outcomes.value = res.data
    } catch (e: unknown) { error.value = (e as Error).message }
    finally { loading.value = false }
  }

  function clearCurrent() {
    currentInstitution.value = null
    currentPlan.value = null
    currentVisit.value = null
    currentAgreement.value = null
    currentFollowUp.value = null
  }

  return {
    institutions, currentInstitution, plans, currentPlan,
    visits, currentVisit, reports,
    agreements, currentAgreement, agreementSummary,
    followUps, currentFollowUp, openFollowUps,
    institutionReport, outcomes, loading, saving, error,
    fetchInstitutions, fetchInstitution, createInstitution, updateInstitution,
    fetchPlans, fetchPlan, createPlan, activatePlan, completePlan, cancelPlan,
    fetchVisits, fetchVisit, createVisit, completeVisit, addObservation,
    fetchReports, createReport,
    fetchAgreements, fetchAgreement, createAgreement, updateAgreement,
    sendAgreement, signAgreement, expireAgreement, cancelAgreement, deleteAgreement,
    fetchAgreementSummary,
    fetchFollowUps, fetchFollowUp, createFollowUp, updateFollowUp,
    startFollowUp, completeFollowUp, cancelFollowUp, deleteFollowUp,
    fetchOpenFollowUps,
    fetchInstitutionReport, fetchOutcomes, clearCurrent,
  }
})
