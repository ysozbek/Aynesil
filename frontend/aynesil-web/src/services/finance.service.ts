/**
 * Finance / Payment API service.
 * Wraps all /api/payments endpoints.
 */
import { apiService } from '@/services/api.service'
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
  CreditLedgerEntryDto,
  CreditSummaryDto,
  CreditLedgerQuery,
  ConsumeCreditPayload,
  GrantCreditPayload,
  RefundCreditPayload,
  AdjustCreditPayload,
  InvoiceDto,
  InvoiceListItemDto,
  InvoiceListQuery,
  CreateInvoicePayload,
  AddInvoiceLinePayload,
  PaymentDto,
  PaymentListItemDto,
  PaymentListQuery,
  CreatePaymentPayload,
  RefundDto,
  RefundListItemDto,
  RefundListQuery,
  CreateRefundPayload,
  DiscountDto,
  CreateDiscountPayload,
  ScholarshipDto,
  ScholarshipListItemDto,
  ScholarshipListQuery,
  CreateScholarshipPayload,
  UpdateScholarshipPayload,
  PromotionDto,
  PromotionListItemDto,
  PromotionListQuery,
  CreatePromotionPayload,
  UpdatePromotionPayload,
  ValidatePromotionResult,
  RevenueReportDto,
  PackageReportDto,
  CreditUsageReportDto,
  ReportQuery,
} from '@/types/finance.types'

function qs(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== '') q.set(k, String(v))
  }
  const s = q.toString()
  return s ? `?${s}` : ''
}

export const financeService = {
  // ── Package Definitions ────────────────────────────────────────────────────

  listPackageDefinitions: (query: PackageDefinitionListQuery) =>
    apiService.get<PaginatedResult<PackageDefinitionListItemDto>>(
      `/payments/package-definitions${qs(query as Record<string, unknown>)}`
    ),

  getPackageDefinition: (id: string) =>
    apiService.get<PackageDefinitionDto>(`/payments/package-definitions/${id}`),

  createPackageDefinition: (payload: CreatePackageDefinitionPayload) =>
    apiService.post<PackageDefinitionDto>('/payments/package-definitions', payload),

  updatePackageDefinition: (id: string, payload: UpdatePackageDefinitionPayload) =>
    apiService.put<PackageDefinitionDto>(`/payments/package-definitions/${id}`, payload),

  activatePackageDefinition: (id: string) =>
    apiService.post(`/payments/package-definitions/${id}/activate`),

  deactivatePackageDefinition: (id: string) =>
    apiService.post(`/payments/package-definitions/${id}/deactivate`),

  deletePackageDefinition: (id: string) =>
    apiService.delete(`/payments/package-definitions/${id}`),

  // ── Student Packages ───────────────────────────────────────────────────────

  listStudentPackages: (query: StudentPackageListQuery) =>
    apiService.get<PaginatedResult<StudentPackageListItemDto>>(
      `/payments/packages${qs(query as Record<string, unknown>)}`
    ),

  getStudentPackage: (id: string) =>
    apiService.get<StudentPackageDto>(`/payments/packages/${id}`),

  getPackageBalance: (id: string) =>
    apiService.get<PackageBalanceDto>(`/payments/packages/${id}/balance`),

  createStudentPackage: (payload: CreateStudentPackagePayload) =>
    apiService.post<StudentPackageDto>('/payments/packages', payload),

  cancelStudentPackage: (id: string, payload: { reason?: string }) =>
    apiService.post(`/payments/packages/${id}/cancel`, payload),

  // ── Credit Ledger ──────────────────────────────────────────────────────────

  listCredits: (query: CreditLedgerQuery) =>
    apiService.get<PaginatedResult<CreditLedgerEntryDto>>(
      `/payments/credits${qs(query as Record<string, unknown>)}`
    ),

  getCreditSummary: (studentId: string) =>
    apiService.get<CreditSummaryDto>(`/payments/credits/summary/${studentId}`),

  consumeCredits: (payload: ConsumeCreditPayload) =>
    apiService.post<CreditLedgerEntryDto>('/payments/credits/consume', payload),

  grantCredits: (payload: GrantCreditPayload) =>
    apiService.post<CreditLedgerEntryDto>('/payments/credits/grant', payload),

  refundCredits: (payload: RefundCreditPayload) =>
    apiService.post<CreditLedgerEntryDto>('/payments/credits/refund', payload),

  adjustCredits: (payload: AdjustCreditPayload) =>
    apiService.post<CreditLedgerEntryDto>('/payments/credits/adjust', payload),

  // ── Invoices ──────────────────────────────────────────────────────────────

  listInvoices: (query: InvoiceListQuery) =>
    apiService.get<PaginatedResult<InvoiceListItemDto>>(
      `/payments/invoices${qs(query as Record<string, unknown>)}`
    ),

  getInvoice: (id: string) =>
    apiService.get<InvoiceDto>(`/payments/invoices/${id}`),

  createInvoice: (payload: CreateInvoicePayload) =>
    apiService.post<InvoiceDto>('/payments/invoices', payload),

  addInvoiceLine: (id: string, payload: AddInvoiceLinePayload) =>
    apiService.post<InvoiceDto>(`/payments/invoices/${id}/lines`, payload),

  removeInvoiceLine: (id: string, lineId: string) =>
    apiService.delete(`/payments/invoices/${id}/lines/${lineId}`),

  issueInvoice: (id: string, payload: { rowVersion: string }) =>
    apiService.post<InvoiceDto>(`/payments/invoices/${id}/issue`, payload),

  voidInvoice: (id: string, payload: { reason?: string; rowVersion: string }) =>
    apiService.post<InvoiceDto>(`/payments/invoices/${id}/void`, payload),

  // ── Transactions (Payments) ────────────────────────────────────────────────

  listTransactions: (query: PaymentListQuery) =>
    apiService.get<PaginatedResult<PaymentListItemDto>>(
      `/payments/transactions${qs(query as Record<string, unknown>)}`
    ),

  getTransaction: (id: string) =>
    apiService.get<PaymentDto>(`/payments/transactions/${id}`),

  createTransaction: (payload: CreatePaymentPayload) =>
    apiService.post<PaymentDto>('/payments/transactions', payload),

  captureTransaction: (id: string, payload: { rowVersion: string }) =>
    apiService.post<PaymentDto>(`/payments/transactions/${id}/capture`, payload),

  failTransaction: (id: string, payload: { reason?: string; rowVersion: string }) =>
    apiService.post<PaymentDto>(`/payments/transactions/${id}/fail`, payload),

  // ── Refunds ───────────────────────────────────────────────────────────────

  listRefunds: (query: RefundListQuery) =>
    apiService.get<PaginatedResult<RefundListItemDto>>(
      `/payments/refunds${qs(query as Record<string, unknown>)}`
    ),

  getRefund: (id: string) =>
    apiService.get<RefundDto>(`/payments/refunds/${id}`),

  createRefund: (payload: CreateRefundPayload) =>
    apiService.post<RefundDto>('/payments/refunds', payload),

  processRefund: (id: string, payload: { rowVersion: string }) =>
    apiService.post<RefundDto>(`/payments/refunds/${id}/process`, payload),

  failRefund: (id: string, payload: { reason?: string; rowVersion: string }) =>
    apiService.post<RefundDto>(`/payments/refunds/${id}/fail`, payload),

  // ── Discounts ──────────────────────────────────────────────────────────────

  createDiscount: (payload: CreateDiscountPayload) =>
    apiService.post<DiscountDto>('/payments/discounts', payload),

  // ── Scholarships ──────────────────────────────────────────────────────────

  listScholarships: (query: ScholarshipListQuery) =>
    apiService.get<PaginatedResult<ScholarshipListItemDto>>(
      `/payments/scholarships${qs(query as Record<string, unknown>)}`
    ),

  getScholarship: (id: string) =>
    apiService.get<ScholarshipDto>(`/payments/scholarships/${id}`),

  createScholarship: (payload: CreateScholarshipPayload) =>
    apiService.post<ScholarshipDto>('/payments/scholarships', payload),

  updateScholarship: (id: string, payload: UpdateScholarshipPayload) =>
    apiService.put<ScholarshipDto>(`/payments/scholarships/${id}`, payload),

  // ── Promotions ────────────────────────────────────────────────────────────

  listPromotions: (query: PromotionListQuery) =>
    apiService.get<PaginatedResult<PromotionListItemDto>>(
      `/payments/promotions${qs(query as Record<string, unknown>)}`
    ),

  getPromotion: (id: string) =>
    apiService.get<PromotionDto>(`/payments/promotions/${id}`),

  validatePromotion: (code: string, corporationId: string) =>
    apiService.get<ValidatePromotionResult>(
      `/payments/promotions/validate${qs({ code, corporationId } as Record<string, unknown>)}`
    ),

  createPromotion: (payload: CreatePromotionPayload) =>
    apiService.post<PromotionDto>('/payments/promotions', payload),

  updatePromotion: (id: string, payload: UpdatePromotionPayload) =>
    apiService.put<PromotionDto>(`/payments/promotions/${id}`, payload),

  activatePromotion: (id: string) =>
    apiService.post(`/payments/promotions/${id}/activate`),

  deactivatePromotion: (id: string) =>
    apiService.post(`/payments/promotions/${id}/deactivate`),

  // ── Reports ───────────────────────────────────────────────────────────────

  getRevenueReport: (query: ReportQuery) =>
    apiService.get<RevenueReportDto>(
      `/payments/reports/revenue${qs(query as Record<string, unknown>)}`
    ),

  getPackageReport: (query: ReportQuery) =>
    apiService.get<PackageReportDto>(
      `/payments/reports/packages${qs(query as Record<string, unknown>)}`
    ),

  getCreditUsageReport: (query: ReportQuery) =>
    apiService.get<CreditUsageReportDto>(
      `/payments/reports/credit-usage${qs(query as Record<string, unknown>)}`
    ),
}
