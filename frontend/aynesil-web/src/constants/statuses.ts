/**
 * Durum / tür sabitleri — kaynak: DB check constraint'leri (db/ altındaki layer dizinleri ve db/migrations/).
 * DB ve backend küçük harfli (snake_case) kod döndürür; frontend'de durum değerlerini
 * string literal yerine bu sabitlerle karşılaştırın.
 */

/** iam.user_account.status (04_identity_access.sql) */
export const UserStatus = {
  Invited: 'invited',
  Active: 'active',
  Suspended: 'suspended',
  Disabled: 'disabled',
} as const
export type UserStatus = (typeof UserStatus)[keyof typeof UserStatus]

/** core.corporation.status (02_tenancy.sql) */
export const CorporationStatus = {
  Active: 'active',
  Suspended: 'suspended',
  Closed: 'closed',
} as const
export type CorporationStatus = (typeof CorporationStatus)[keyof typeof CorporationStatus]

/** legal.student_contract.status (08_legal.sql) */
export const ContractStatus = {
  Draft: 'draft',
  Sent: 'sent',
  Signed: 'signed',
  Active: 'active',
  Expired: 'expired',
  Terminated: 'terminated',
} as const
export type ContractStatus = (typeof ContractStatus)[keyof typeof ContractStatus]

/** legal.student_consent.state (08_legal.sql) */
export const ConsentState = {
  Granted: 'granted',
  Withdrawn: 'withdrawn',
  Expired: 'expired',
} as const
export type ConsentState = (typeof ConsentState)[keyof typeof ConsentState]

/** camps.camp_enrollment.status (11_camps.sql) */
export const CampEnrollmentStatus = {
  Enrolled: 'enrolled',
  Waitlist: 'waitlist',
  Withdrawn: 'withdrawn',
  Completed: 'completed',
} as const
export type CampEnrollmentStatus = (typeof CampEnrollmentStatus)[keyof typeof CampEnrollmentStatus]

/** ops.meeting.status (10_ops.sql) */
export const MeetingStatus = {
  Scheduled: 'scheduled',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type MeetingStatus = (typeof MeetingStatus)[keyof typeof MeetingStatus]

/** ops.meeting_follow_up.status (10_ops.sql) */
export const MeetingFollowUpStatus = {
  Open: 'open',
  InProgress: 'in_progress',
  Done: 'done',
  Cancelled: 'cancelled',
} as const
export type MeetingFollowUpStatus = (typeof MeetingFollowUpStatus)[keyof typeof MeetingFollowUpStatus]

/** consultancy.follow_up_activity.status (12_consultancy.sql / V26) */
export const ConsultancyFollowUpStatus = {
  Pending: 'pending',
  InProgress: 'in_progress',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type ConsultancyFollowUpStatus =
  (typeof ConsultancyFollowUpStatus)[keyof typeof ConsultancyFollowUpStatus]

/** consultancy.consultancy_plan.status (12_consultancy.sql) */
export const ConsultancyPlanStatus = {
  Draft: 'draft',
  Active: 'active',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type ConsultancyPlanStatus = (typeof ConsultancyPlanStatus)[keyof typeof ConsultancyPlanStatus]

/** consultancy.school_visit.status (12_consultancy.sql) */
export const SchoolVisitStatus = {
  Planned: 'planned',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type SchoolVisitStatus = (typeof SchoolVisitStatus)[keyof typeof SchoolVisitStatus]

/** scheduling.session.status (06_scheduling.sql) */
export const SessionStatus = {
  Scheduled: 'scheduled',
  Completed: 'completed',
  Cancelled: 'cancelled',
  NoShow: 'no_show',
  Rescheduled: 'rescheduled',
} as const
export type SessionStatus = (typeof SessionStatus)[keyof typeof SessionStatus]

/** ops.leave_request.status (10_ops.sql) */
export const LeaveStatus = {
  Pending: 'pending',
  Approved: 'approved',
  Rejected: 'rejected',
  Cancelled: 'cancelled',
} as const
export type LeaveStatus = (typeof LeaveStatus)[keyof typeof LeaveStatus]

/** ops.leave_request.unit / leave_balance.unit (10_ops.sql) */
export const LeaveUnit = {
  Day: 'day',
  Hour: 'hour',
} as const
export type LeaveUnit = (typeof LeaveUnit)[keyof typeof LeaveUnit]

/** finance.credit_ledger.entry_type (07_finance.sql) */
export const CreditEntryType = {
  Grant: 'grant',
  Consume: 'consume',
  Refund: 'refund',
  Adjustment: 'adjustment',
  Expire: 'expire',
} as const
export type CreditEntryType = (typeof CreditEntryType)[keyof typeof CreditEntryType]

/** education.student_program.status (05_education.sql) */
export const StudentProgramStatus = {
  Active: 'active',
  Paused: 'paused',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type StudentProgramStatus = (typeof StudentProgramStatus)[keyof typeof StudentProgramStatus]

/** education.student_goal.status (05_education.sql) */
export const StudentGoalStatus = {
  Active: 'active',
  Achieved: 'achieved',
  Discontinued: 'discontinued',
  OnHold: 'on_hold',
} as const
export type StudentGoalStatus = (typeof StudentGoalStatus)[keyof typeof StudentGoalStatus]

/** education.education_plan.status (05_education.sql) */
export const EducationPlanStatus = {
  Draft: 'draft',
  InReview: 'in_review',
  Approved: 'approved',
  Active: 'active',
  Revised: 'revised',
  Closed: 'closed',
} as const
export type EducationPlanStatus = (typeof EducationPlanStatus)[keyof typeof EducationPlanStatus]

/** assessment.assessment_session.status (04_assessment.sql) */
export const AssessmentSessionStatus = {
  Planned: 'planned',
  InProgress: 'in_progress',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const
export type AssessmentSessionStatus = (typeof AssessmentSessionStatus)[keyof typeof AssessmentSessionStatus]

/** scheduling.attendance.status (06_scheduling.sql) */
export const AttendanceStatus = {
  Present: 'present',
  Absent: 'absent',
  Late: 'late',
  Excused: 'excused',
  LeftEarly: 'left_early',
} as const
export type AttendanceStatus = (typeof AttendanceStatus)[keyof typeof AttendanceStatus]

/** scheduling.makeup_request.status (06_scheduling.sql) */
export const MakeupRequestStatus = {
  Requested: 'requested',
  Approved: 'approved',
  Scheduled: 'scheduled',
  Completed: 'completed',
  Rejected: 'rejected',
  Expired: 'expired',
} as const
export type MakeupRequestStatus = (typeof MakeupRequestStatus)[keyof typeof MakeupRequestStatus]

/** consultancy.consultancy_agreement.status (12_consultancy.sql / V26) */
export const ConsultancyAgreementStatus = {
  Draft: 'draft',
  Sent: 'sent',
  Signed: 'signed',
  Expired: 'expired',
  Cancelled: 'cancelled',
} as const
export type ConsultancyAgreementStatus = (typeof ConsultancyAgreementStatus)[keyof typeof ConsultancyAgreementStatus]

/** finance.student_package.status (07_finance.sql) */
export const StudentPackageStatus = {
  Active: 'active',
  Exhausted: 'exhausted',
  Expired: 'expired',
  Cancelled: 'cancelled',
} as const
export type StudentPackageStatus = (typeof StudentPackageStatus)[keyof typeof StudentPackageStatus]

/** finance.invoice.status (07_finance.sql) */
export const InvoiceStatus = {
  Draft: 'draft',
  Issued: 'issued',
  Paid: 'paid',
  Partial: 'partial',
  Overdue: 'overdue',
  Void: 'void',
} as const
export type InvoiceStatus = (typeof InvoiceStatus)[keyof typeof InvoiceStatus]

/** finance.payment.status (07_finance.sql) */
export const PaymentStatus = {
  Pending: 'pending',
  Authorized: 'authorized',
  Captured: 'captured',
  Failed: 'failed',
  Refunded: 'refunded',
} as const
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus]

/** crm.interview.status (03_crm.sql) */
export const LeadInterviewStatus = {
  Scheduled: 'scheduled',
  Completed: 'completed',
  NoShow: 'no_show',
  Cancelled: 'cancelled',
} as const
export type LeadInterviewStatus = (typeof LeadInterviewStatus)[keyof typeof LeadInterviewStatus]

/** Türetilmiş durum (DB sütunu değil): ViewingAuthorizationDto.isRevoked / isCurrentlyValid */
export const ViewingAuthorizationState = {
  Active: 'active',
  Expired: 'expired',
  Revoked: 'revoked',
} as const
export type ViewingAuthorizationState = (typeof ViewingAuthorizationState)[keyof typeof ViewingAuthorizationState]

/** ops.meeting_participant.attendance (10_ops.sql) */
export const MeetingAttendance = {
  Invited: 'invited',
  Attended: 'attended',
  Absent: 'absent',
  Tentative: 'tentative',
} as const
export type MeetingAttendance = (typeof MeetingAttendance)[keyof typeof MeetingAttendance]

/** education.education_plan_approval.decision (05_education.sql) */
export const EducationPlanDecision = {
  Approved: 'approved',
  Rejected: 'rejected',
  ChangesRequested: 'changes_requested',
} as const
export type EducationPlanDecision = (typeof EducationPlanDecision)[keyof typeof EducationPlanDecision]

/**
 * StatusBadge gibi genel bileşenlerin varlık adından durum kümesine ulaşması için kayıt.
 * Anahtar = `entity` prop değeri; değer = o varlığın durum sabitleri.
 */
export const StatusEntities = {
  user: UserStatus,
  corporation: CorporationStatus,
  contract: ContractStatus,
  consent: ConsentState,
  campEnrollment: CampEnrollmentStatus,
  meeting: MeetingStatus,
  meetingFollowUp: MeetingFollowUpStatus,
  consultancyFollowUp: ConsultancyFollowUpStatus,
  consultancyPlan: ConsultancyPlanStatus,
  schoolVisit: SchoolVisitStatus,
  session: SessionStatus,
  leave: LeaveStatus,
  creditEntry: CreditEntryType,
  studentProgram: StudentProgramStatus,
  studentGoal: StudentGoalStatus,
  educationPlan: EducationPlanStatus,
  assessmentSession: AssessmentSessionStatus,
  attendance: AttendanceStatus,
  makeupRequest: MakeupRequestStatus,
  consultancyAgreement: ConsultancyAgreementStatus,
  studentPackage: StudentPackageStatus,
  invoice: InvoiceStatus,
  payment: PaymentStatus,
  leadInterview: LeadInterviewStatus,
  viewingAuthorization: ViewingAuthorizationState,
  meetingAttendance: MeetingAttendance,
  educationPlanDecision: EducationPlanDecision,
} as const
export type StatusEntity = keyof typeof StatusEntities
export type StatusOf<E extends StatusEntity> = Extract<
  (typeof StatusEntities)[E][keyof (typeof StatusEntities)[E]],
  string
>
