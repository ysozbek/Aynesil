/**
 * Finance / Payment type definitions.
 * Mirrors Aynesil.Application.Features.Finance.Dtos.FinanceDtos
 */
import type { PagedQuery } from './api.types'

// ── Package Definitions ────────────────────────────────────────────────────────

export interface PackageDefinitionDto {
  id: string
  corporationId: string
  code: string
  name: string
  packageTypeId: string
  programId?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  isActive: boolean
  rowVersion: number
}

/** Mirrors PackageDefinitionListItemDto. `packageTypeId` is resolved to a label via ref-data (package_type). */
export interface PackageDefinitionListItemDto {
  id: string
  corporationId: string
  code: string
  name: string
  packageTypeId?: string
  packageTypeName?: string
  programName?: string
  totalCredits?: number
  validityDays?: number
  listPrice: number
  currency: string
  isActive: boolean
}

export interface PackageDefinitionListQuery extends PagedQuery {
  isActive?: boolean
  packageTypeId?: string
  programId?: string
}

export interface CreatePackageDefinitionPayload {
  code: string
  name: string
  packageTypeId: string
  programId?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
}

export interface UpdatePackageDefinitionPayload {
  code: string
  name: string
  packageTypeId: string
  programId?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  rowVersion: number
}

// ── Student Packages ───────────────────────────────────────────────────────────

/** Mirrors StudentPackageDto on the API. */
export interface StudentPackageDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  packageDefinitionId?: string
  packageDefinitionName?: string
  purchasedOn: string
  expiresOn?: string
  totalCredits: number
  remainingCredits: number
  price: number
  currency: string
  status: string
  createdAt: string
  updatedAt: string
  rowVersion: number
}

/** Mirrors StudentPackageListItemDto on the API. */
export interface StudentPackageListItemDto {
  id: string
  studentId: string
  studentFullName: string
  packageDefinitionId?: string
  packageDefinitionName?: string
  purchasedOn: string
  expiresOn?: string
  totalCredits: number
  remainingCredits: number
  status: string
}

export interface PackageBalanceDto {
  studentPackageId: string
  totalCredits: number
  remainingCredits: number
  consumedCredits: number
  expiresOn?: string
}

export interface StudentPackageListQuery extends PagedQuery {
  studentId?: string
  status?: string
  packageDefinitionId?: string
}

/** Mirrors PurchasePackageRequest on the API. */
export interface CreateStudentPackagePayload {
  studentId: string
  totalCredits: number
  price: number
  packageDefinitionId?: string
  expiresOn?: string
  currency: string
}

// ── Credit Ledger ──────────────────────────────────────────────────────────────

export interface CreditLedgerEntryDto {
  id: string
  studentPackageId: string
  entryType: string
  delta: number
  runningBalance: number
  sessionId?: string
  sessionTitle?: string
  reason?: string
  occurredAt: string
  createdBy?: string
  recordedByName?: string
}

export interface CreditSummaryDto {
  studentId: string
  activePackages: number
  totalGranted: number
  totalConsumed: number
  totalRemaining: number
  expiringWithin30Days: number
}

export interface CreditLedgerQuery extends PagedQuery {
  studentPackageId?: string
  studentId?: string
  entryType?: string
  from?: string
  to?: string
}

export interface ConsumeCreditPayload {
  studentPackageId: string
  sessionId: string
  amount: number
  reason?: string
}

export interface GrantCreditPayload {
  studentPackageId: string
  amount: number
  reason?: string
}

export interface RefundCreditPayload {
  studentPackageId: string
  amount: number
  reason?: string
}

export interface AdjustCreditPayload {
  studentPackageId: string
  delta: number
  reason: string
}

// ── Invoices ──────────────────────────────────────────────────────────────────

export interface InvoiceLineDto {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface InvoiceDto {
  id: string
  corporationId: string
  studentId: string | null
  studentFullName: string | null
  /** null until the invoice is issued (draft invoices have no number). */
  invoiceNo: string | null
  issueDate?: string
  dueDate?: string
  subtotal: number
  discountTotal: number
  taxTotal: number
  grandTotal: number
  paidAmount: number
  balance: number
  currency: string
  status: string
  lines: InvoiceLineDto[]
  rowVersion: number
}

export interface InvoiceListItemDto {
  id: string
  studentFullName: string | null
  /** null until the invoice is issued (draft invoices have no number). */
  invoiceNo: string | null
  issueDate?: string
  dueDate?: string
  grandTotal: number
  paidAmount: number
  balance: number
  currency: string
  status: string
}

export interface InvoiceListQuery extends PagedQuery {
  studentId?: string
  status?: string
  overdue?: boolean
  issuedFrom?: string
  issuedTo?: string
}

export interface CreateInvoicePayload {
  /** `YYYY-MM-DD`; required by the backend (DateOnly, no default). */
  issueDate: string
  studentId: string
  dueDate?: string
  currency: string
}

export interface AddInvoiceLinePayload {
  description: string
  quantity: number
  unitPrice: number
}

// ── Transactions (Payments) ────────────────────────────────────────────────────

export interface PaymentDto {
  id: string
  corporationId: string
  studentId: string | null
  studentFullName: string | null
  invoiceId?: string
  invoiceNo?: string
  amount: number
  currency: string
  status: string
  paymentMethodId?: string
  paymentMethodName?: string
  gatewayReference?: string
  paidAt?: string
  rowVersion: number
}

export interface PaymentListItemDto {
  id: string
  studentFullName: string | null
  invoiceNo?: string
  amount: number
  currency: string
  status: string
  paymentMethodId?: string
  paymentMethodName?: string
  paidAt?: string
}

/** Backend GetPaymentsQuery has no free-text search → `search` is omitted. */
export interface PaymentListQuery extends Omit<PagedQuery, 'search'> {
  studentId?: string
  invoiceId?: string
  paymentMethodId?: string
  status?: string
  paidFrom?: string
  paidTo?: string
}

export interface CreatePaymentPayload {
  studentId: string
  invoiceId?: string
  amount: number
  currency: string
  paymentMethodId: string
  gatewayReference?: string
  paidAt?: string
}

// ── Refunds ───────────────────────────────────────────────────────────────────

export interface RefundDto {
  id: string
  corporationId: string
  paymentId: string
  amount: number
  reason?: string
  status: string
  processedAt?: string
  createdAt: string
}

export interface RefundListItemDto {
  id: string
  paymentId: string
  amount: number
  reason?: string
  status: string
  processedAt?: string
}

export interface RefundListQuery extends PagedQuery {
  paymentId?: string
  status?: string
}

export interface CreateRefundPayload {
  paymentId: string
  amount: number
  reason?: string
}

// ── Discounts ──────────────────────────────────────────────────────────────────

export interface DiscountDto {
  id: string
  corporationId: string
  invoiceId?: string
  studentPackageId?: string
  discountTypeId: string | null
  isPercentage: boolean
  value: number
  reason?: string | null
  createdAt: string
}

export interface CreateDiscountPayload {
  invoiceId?: string
  studentPackageId?: string
  discountTypeId: string
  isPercentage: boolean
  value: number
}

// ── Scholarships ──────────────────────────────────────────────────────────────

export interface ScholarshipDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  scholarshipTypeId?: string
  percentage?: number
  amount?: number
  validFrom?: string
  validTo?: string
  approvedBy?: string
  note?: string
  rowVersion: number
}

export interface ScholarshipListItemDto {
  id: string
  studentId: string
  studentFullName: string
  scholarshipTypeId?: string
  scholarshipTypeName?: string
  percentage?: number
  amount?: number
  validFrom?: string
  validTo?: string
}

export interface ScholarshipListQuery extends PagedQuery {
  studentId?: string
  scholarshipTypeId?: string
}

export interface CreateScholarshipPayload {
  studentId: string
  scholarshipTypeId: string
  percentage?: number
  amount?: number
  validFrom: string
  validTo?: string
  note?: string
}

export interface UpdateScholarshipPayload {
  scholarshipTypeId?: string
  percentage?: number
  amount?: number
  validFrom?: string
  validTo?: string
  note?: string
  rowVersion: number
}

// ── Promotions ────────────────────────────────────────────────────────────────

export interface PromotionDto {
  id: string
  corporationId: string
  code: string
  name: string
  value: number
  isPercentage: boolean
  maxRedemptions?: number
  validFrom?: string
  validTo?: string
  isActive: boolean
}

export interface PromotionListItemDto {
  id: string
  code: string
  name: string
  value: number
  isPercentage: boolean
  isActive: boolean
  validFrom?: string
  validTo?: string
}

export interface PromotionListQuery extends PagedQuery {
  isActive?: boolean
}

export interface CreatePromotionPayload {
  code: string
  name: string
  value: number
  isPercentage: boolean
  maxRedemptions?: number
  validFrom?: string
  validTo?: string
}

export interface UpdatePromotionPayload {
  code: string
  name: string
  value: number
  isPercentage: boolean
  maxRedemptions?: number
  validFrom?: string
  validTo?: string
}

export interface ValidatePromotionResult {
  isValid: boolean
  promotionId?: string
  name?: string
  value?: number
  isPercentage?: boolean
  message?: string
}

// ── Reports ───────────────────────────────────────────────────────────────────

/** Mirrors RevenueByMethodDto. `methodName` carries the raw id, so labels come from ref-data via `paymentMethodId`. */
export interface RevenueByMethodDto {
  paymentMethodId?: string
  methodName: string
  totalAmount: number
  count: number
}

/** Mirrors RevenueReportDto. Money fields are decimal amounts (no ratios). */
export interface RevenueReportDto {
  periodStart: string
  periodEnd: string
  totalRevenue: number
  totalRefunded: number
  netRevenue: number
  paymentCount: number
  refundCount: number
  byMethod: RevenueByMethodDto[]
}

/** Mirrors PackageSalesSummaryDto. */
export interface TopPackageDto {
  packageDefinitionId?: string
  packageName: string
  unitsSold: number
  totalRevenue: number
}

/** Mirrors PackageReportDto — an all-time snapshot; the endpoint takes no period. */
export interface PackageReportDto {
  corporationId: string
  totalActive: number
  totalExhausted: number
  totalExpired: number
  totalCancelled: number
  expiringIn30Days: number
  totalCreditsGranted: number
  totalCreditsConsumed: number
  totalCreditsRemaining: number
  topPackages: TopPackageDto[]
}

export interface CreditUsageByStudentDto {
  studentId: string
  studentFullName: string
  granted: number
  consumed: number
  remaining: number
}

export interface CreditUsageReportDto {
  periodStart: string
  periodEnd: string
  totalGranted: number
  totalConsumed: number
  totalExpired: number
  totalRefunded: number
  totalAdjusted: number
  byStudent: CreditUsageByStudentDto[]
}

/** Backend expects `periodStart` / `periodEnd` (DateOnly, yyyy-MM-dd). */
export interface ReportQuery {
  periodStart: string
  periodEnd: string
}
