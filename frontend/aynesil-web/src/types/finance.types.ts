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
  packageTypeLabel?: string
  programId?: string
  programName?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  isActive: boolean
  description?: string
  rowVersion: string
}

export interface PackageDefinitionListItemDto {
  id: string
  code: string
  name: string
  packageTypeLabel?: string
  programName?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  isActive: boolean
}

export interface PackageDefinitionListQuery extends PagedQuery {
  corporationId: string
  isActive?: boolean
  packageTypeId?: string
  programId?: string
}

export interface CreatePackageDefinitionPayload {
  corporationId: string
  code: string
  name: string
  packageTypeId: string
  programId?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  description?: string
}

export interface UpdatePackageDefinitionPayload {
  name: string
  packageTypeId: string
  programId?: string
  totalCredits: number
  validityDays: number
  listPrice: number
  currency: string
  description?: string
  rowVersion: string
}

// ── Student Packages ───────────────────────────────────────────────────────────

export interface StudentPackageDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  packageDefinitionId: string
  packageName: string
  purchasedOn: string
  expiresOn?: string
  totalCredits: number
  remainingCredits: number
  consumedCredits: number
  price: number
  currency: string
  status: string
  invoiceId?: string
  rowVersion: string
}

export interface StudentPackageListItemDto {
  id: string
  studentFullName: string
  packageName: string
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
  corporationId: string
  studentId?: string
  status?: string
  packageDefinitionId?: string
}

export interface CreateStudentPackagePayload {
  corporationId: string
  studentId: string
  packageDefinitionId: string
  purchasedOn: string
  price: number
  currency: string
  invoiceId?: string
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
  recordedByName: string
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
  corporationId: string
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
  sessionId?: string
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
  discountAmount: number
  lineTotal: number
}

export interface InvoiceDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  invoiceNo: string
  issueDate?: string
  dueDate?: string
  subtotal: number
  discountTotal: number
  total: number
  paidAmount: number
  balance: number
  currency: string
  status: string
  lines: InvoiceLineDto[]
  rowVersion: string
}

export interface InvoiceListItemDto {
  id: string
  studentFullName: string
  invoiceNo: string
  issueDate?: string
  dueDate?: string
  total: number
  paidAmount: number
  balance: number
  currency: string
  status: string
}

export interface InvoiceListQuery extends PagedQuery {
  corporationId: string
  studentId?: string
  status?: string
  from?: string
  to?: string
}

export interface CreateInvoicePayload {
  corporationId: string
  studentId: string
  dueDate?: string
  currency: string
}

export interface AddInvoiceLinePayload {
  description: string
  quantity: number
  unitPrice: number
  discountAmount?: number
}

// ── Transactions (Payments) ────────────────────────────────────────────────────

export interface PaymentDto {
  id: string
  corporationId: string
  studentId: string
  studentFullName: string
  invoiceId?: string
  invoiceNo?: string
  amount: number
  currency: string
  status: string
  paymentMethodId: string
  paymentMethodLabel?: string
  gatewayReference?: string
  paidAt?: string
  notes?: string
  rowVersion: string
}

export interface PaymentListItemDto {
  id: string
  studentFullName: string
  invoiceNo?: string
  amount: number
  currency: string
  status: string
  paymentMethodLabel?: string
  paidAt?: string
}

export interface PaymentListQuery extends PagedQuery {
  corporationId: string
  studentId?: string
  invoiceId?: string
  status?: string
  from?: string
  to?: string
}

export interface CreatePaymentPayload {
  corporationId: string
  studentId: string
  invoiceId?: string
  amount: number
  currency: string
  paymentMethodId: string
  gatewayReference?: string
  paidAt?: string
  notes?: string
}

// ── Refunds ───────────────────────────────────────────────────────────────────

export interface RefundDto {
  id: string
  corporationId: string
  paymentId: string
  paymentAmount: number
  amount: number
  currency: string
  reason?: string
  status: string
  processedAt?: string
  processedByName?: string
  rowVersion: string
}

export interface RefundListItemDto {
  id: string
  paymentId: string
  amount: number
  currency: string
  reason?: string
  status: string
  processedAt?: string
}

export interface RefundListQuery extends PagedQuery {
  corporationId: string
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
  discountTypeId: string
  discountTypeLabel?: string
  isPercentage: boolean
  value: number
  appliedAt: string
  appliedByName: string
}

export interface CreateDiscountPayload {
  corporationId: string
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
  scholarshipTypeId: string
  scholarshipTypeLabel?: string
  percentage?: number
  amount?: number
  currency?: string
  validFrom: string
  validTo?: string
  notes?: string
  rowVersion: string
}

export interface ScholarshipListItemDto {
  id: string
  studentFullName: string
  scholarshipTypeLabel?: string
  percentage?: number
  amount?: number
  currency?: string
  validFrom: string
  validTo?: string
}

export interface ScholarshipListQuery extends PagedQuery {
  corporationId: string
  studentId?: string
  scholarshipTypeId?: string
}

export interface CreateScholarshipPayload {
  corporationId: string
  studentId: string
  scholarshipTypeId: string
  percentage?: number
  amount?: number
  currency?: string
  validFrom: string
  validTo?: string
  notes?: string
}

export interface UpdateScholarshipPayload {
  percentage?: number
  amount?: number
  validFrom: string
  validTo?: string
  notes?: string
  rowVersion: string
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
  redemptionCount: number
  validFrom?: string
  validTo?: string
  isActive: boolean
  rowVersion: string
}

export interface PromotionListItemDto {
  id: string
  code: string
  name: string
  value: number
  isPercentage: boolean
  redemptionCount: number
  maxRedemptions?: number
  isActive: boolean
  validTo?: string
}

export interface PromotionListQuery extends PagedQuery {
  corporationId: string
  isActive?: boolean
}

export interface CreatePromotionPayload {
  corporationId: string
  code: string
  name: string
  value: number
  isPercentage: boolean
  maxRedemptions?: number
  validFrom?: string
  validTo?: string
}

export interface UpdatePromotionPayload {
  name: string
  value: number
  isPercentage: boolean
  maxRedemptions?: number
  validFrom?: string
  validTo?: string
  rowVersion: string
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

export interface RevenueByMethodDto {
  paymentMethodLabel: string
  totalAmount: number
  count: number
}

export interface RevenueReportDto {
  from: string
  to: string
  totalRevenue: number
  totalTransactions: number
  byMethod: RevenueByMethodDto[]
}

export interface TopPackageDto {
  packageName: string
  soldCount: number
  totalRevenue: number
}

export interface PackageReportDto {
  from: string
  to: string
  totalPackagesSold: number
  totalRevenue: number
  topPackages: TopPackageDto[]
}

export interface CreditUsageByStudentDto {
  studentFullName: string
  totalConsumed: number
  totalGranted: number
}

export interface CreditUsageReportDto {
  from: string
  to: string
  totalConsumed: number
  totalGranted: number
  byStudent: CreditUsageByStudentDto[]
}

export interface ReportQuery {
  corporationId: string
  from: string
  to: string
  campusId?: string
}
