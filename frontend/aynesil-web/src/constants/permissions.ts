/**
 * İzin kodları — ÜRETİLMİŞ DOSYA, elle düzenlemeyin.
 * Kaynak: src/Aynesil.Shared/Constants/Permissions.cs
 * Yeniden üretmek için: python3 scripts/gen-permissions.py
 */
export const Permissions = {
  // Corporation (admin)
  Corporation: {
    Read: 'corporation:read',
    Create: 'corporation:create',
    Update: 'corporation:update',
    Delete: 'corporation:delete',
  },
  // Campus
  Campus: {
    Read: 'campus:read',
    Create: 'campus:create',
    Update: 'campus:update',
    Delete: 'campus:delete',
  },
  // IAM
  Users: {
    Read: 'user:read',
    Create: 'user:create',
    Update: 'user:update',
    Delete: 'user:delete',
    ResetPassword: 'user:reset_password',
  },
  Roles: {
    Read: 'role:read',
    Create: 'role:create',
    Update: 'role:update',
    Delete: 'role:delete',
    AssignPermission: 'role:assign_permission',
  },
  // Reference Data
  RefData: {
    Read: 'ref_data:read',
    Manage: 'ref_data:manage',
  },
  // Settings
  Settings: {
    Read: 'settings:read',
    Manage: 'settings:manage',
  },
  // Menu
  Menu: {
    Read: 'menu:read',
    Manage: 'menu:manage',
  },
  // Notifications
  Notifications: {
    Read: 'notification:read',
    Send: 'notification:send',
  },
  // Notification Templates
  NotificationTemplates: {
    Read: 'notification_template:read',
    Create: 'notification_template:create',
    Update: 'notification_template:update',
    Delete: 'notification_template:delete',
  },
  // Notification Triggers
  NotificationTriggers: {
    Manage: 'notification_trigger:manage',
  },
  // Surveys
  Surveys: {
    Read: 'survey:read',
    Create: 'survey:create',
    Update: 'survey:update',
    Delete: 'survey:delete',
    Respond: 'survey:respond',
  },
  // Parent Feedback
  ParentFeedback: {
    Read: 'parent_feedback:read',
    Create: 'parent_feedback:create',
    Submit: 'parent_feedback:submit',
  },
  // Parent Portal
  Portal: {
    Access: 'portal:access',
  },
  // Files
  Files: {
    Read: 'file:read',
    Upload: 'file:upload',
    Delete: 'file:delete',
  },
  // Reports
  Reports: {
    Read: 'report:read',
    Run: 'report:run',
    Manage: 'report:manage',
    Export: 'report:export',
  },
  // Audit
  Audit: {
    Read: 'audit:read',
  },
  // Integrations
  Integrations: {
    Read: 'integration:read',
    Manage: 'integration:manage',
  },
  // CRM / Leads
  Leads: {
    Read: 'lead:read',
    Create: 'lead:create',
    Update: 'lead:update',
    Delete: 'lead:delete',
    Convert: 'lead:convert',
    Assign: 'lead:assign',
  },
  LeadActivities: {
    Read: 'lead_activity:read',
    Create: 'lead_activity:create',
  },
  Interviews: {
    Read: 'interview:read',
    Create: 'interview:create',
    Update: 'interview:update',
    Manage: 'interview:manage',
  },
  // Assessment & Evaluation
  AssessmentTemplates: {
    Read: 'assessment_template:read',
    Create: 'assessment_template:create',
    Update: 'assessment_template:update',
    Delete: 'assessment_template:delete',
    Publish: 'assessment_template:publish',
    Version: 'assessment_template:version',
  },
  AssessmentSessions: {
    Read: 'assessment_session:read',
    Create: 'assessment_session:create',
    Update: 'assessment_session:update',
    Delete: 'assessment_session:delete',
    Start: 'assessment_session:start',
    Complete: 'assessment_session:complete',
    Cancel: 'assessment_session:cancel',
    SubmitResponses: 'assessment_session:submit_responses',
  },
  AssessmentReports: {
    Read: 'assessment_report:read',
    Create: 'assessment_report:create',
    Update: 'assessment_report:update',
    Finalize: 'assessment_report:finalize',
  },
  ProgramRecommendations: {
    Read: 'program_recommendation:read',
    Create: 'program_recommendation:create',
    Update: 'program_recommendation:update',
  },
  // Students (Layer 2 — SPED)
  Students: {
    Read: 'student:read',
    Write: 'student:write',
    Create: 'student:create',
    Update: 'student:update',
    Delete: 'student:delete',
    ChangeStatus: 'student:change_status',
  },
  Guardians: {
    Read: 'guardian:read',
    Create: 'guardian:create',
    Update: 'guardian:update',
    Delete: 'guardian:delete',
    ManagePortal: 'guardian:manage_portal',
  },
  CaseNotes: {
    Read: 'case_note:read',
    Create: 'case_note:create',
    Update: 'case_note:update',
    Delete: 'case_note:delete',
    ReadConfidential: 'case_note:read_confidential',
  },
  // Educators
  Educators: {
    Read: 'educator:read',
    Create: 'educator:create',
    Update: 'educator:update',
    Delete: 'educator:delete',
    ManageSpecialties: 'educator:manage_specialties',
    ManageCampuses: 'educator:manage_campuses',
    ManageCertifications: 'educator:manage_certifications',
    ManageHierarchy: 'educator:manage_hierarchy',
  },
  // Programs (Education)
  Programs: {
    Read: 'program:read',
    Create: 'program:create',
    Update: 'program:update',
    Delete: 'program:delete',
  },
  // Enrollments
  Enrollments: {
    Read: 'enrollment:read',
    Create: 'enrollment:create',
    Update: 'enrollment:update',
    ManagePrograms: 'enrollment:manage_programs',
  },
  // Goal Library
  GoalLibraries: {
    Read: 'goal_library:read',
    Create: 'goal_library:create',
    Update: 'goal_library:update',
    Delete: 'goal_library:delete',
  },
  // Goal Templates
  GoalTemplates: {
    Read: 'goal_template:read',
    Create: 'goal_template:create',
    Update: 'goal_template:update',
    Delete: 'goal_template:delete',
    Translate: 'goal_template:translate',
  },
  // Student Goals
  StudentGoals: {
    Read: 'student_goal:read',
    Create: 'student_goal:create',
    Update: 'student_goal:update',
    Delete: 'student_goal:delete',
    ChangeStatus: 'student_goal:change_status',
  },
  // Goal Progress
  GoalProgress: {
    Read: 'goal_progress:read',
    Record: 'goal_progress:record',
  },
  // Academic Periods
  AcademicPeriods: {
    Read: 'academic_period:read',
    Manage: 'academic_period:manage',
  },
  // Education Plans (BEP/IEP)
  EducationPlans: {
    Read: 'education_plan:read',
    Create: 'education_plan:create',
    Update: 'education_plan:update',
    Delete: 'education_plan:delete',
    Submit: 'education_plan:submit',
    Approve: 'education_plan:approve',
    Revise: 'education_plan:revise',
    ManageGoals: 'education_plan:manage_goals',
    AddReview: 'education_plan:add_review',
    GuardianView: 'education_plan:guardian_view',
  },
  // Goal Reports
  GoalReports: {
    Read: 'goal_report:read',
    Export: 'goal_report:export',
  },
  // Rooms
  Rooms: {
    Read: 'room:read',
    Create: 'room:create',
    Update: 'room:update',
    Delete: 'room:delete',
  },
  // Sessions
  Sessions: {
    Read: 'session:read',
    Create: 'session:create',
    Update: 'session:update',
    Delete: 'session:delete',
    Reschedule: 'session:reschedule',
    Complete: 'session:complete',
    Cancel: 'session:cancel',
    ManageParticipants: 'session:manage_participants',
    ManageEducators: 'session:manage_educators',
    ManageGoals: 'session:manage_goals',
    ManageCalendar: 'session:manage_calendar',
    BulkGenerate: 'session:bulk_generate',
    BulkCancel: 'session:bulk_cancel',
    BulkReassign: 'session:bulk_reassign',
  },
  // Session Notes
  SessionNotes: {
    Write: 'session_note:write',
    Delete: 'session_note:delete',
  },
  // Attendance
  Attendance: {
    Read: 'attendance:read',
    Record: 'attendance:record',
  },
  // Makeup Requests
  MakeupRequests: {
    Read: 'makeup_request:read',
    Request: 'makeup_request:request',
    Manage: 'makeup_request:manage',
  },
  // Package Definitions
  PackageDefinitions: {
    Read: 'package_definition:read',
    Create: 'package_definition:create',
    Update: 'package_definition:update',
    Delete: 'package_definition:delete',
  },
  // Student Packages
  StudentPackages: {
    Read: 'student_package:read',
    Purchase: 'student_package:purchase',
    Cancel: 'student_package:cancel',
  },
  // Credit Ledger
  CreditLedger: {
    Read: 'credit_ledger:read',
    Consume: 'credit_ledger:consume',
    Grant: 'credit_ledger:grant',
    Adjust: 'credit_ledger:adjust',
  },
  // Invoices
  Invoices: {
    Read: 'invoice:read',
    Create: 'invoice:create',
    Update: 'invoice:update',
    Void: 'invoice:void',
  },
  // Finance Payments
  FinancePayments: {
    Read: 'finance_payment:read',
    Record: 'finance_payment:record',
    Capture: 'finance_payment:capture',
  },
  // Refunds
  Refunds: {
    Read: 'refund:read',
    Request: 'refund:request',
    Process: 'refund:process',
  },
  // Discounts
  Discounts: {
    Read: 'discount:read',
    Apply: 'discount:apply',
  },
  // Scholarships
  Scholarships: {
    Read: 'scholarship:read',
    Grant: 'scholarship:grant',
    Update: 'scholarship:update',
  },
  // Promotions
  Promotions: {
    Read: 'promotion:read',
    Create: 'promotion:create',
    Update: 'promotion:update',
  },
  // Finance Reports
  FinanceReports: {
    Read: 'finance_report:read',
  },
  // Meetings
  Meetings: {
    Read: 'meeting:read',
    Create: 'meeting:create',
    Update: 'meeting:update',
    Delete: 'meeting:delete',
    Complete: 'meeting:complete',
    Cancel: 'meeting:cancel',
    ManageParticipants: 'meeting:manage_participants',
    RecordAttendance: 'meeting:record_attendance',
    RecordOutcome: 'meeting:record_outcome',
    ManageFollowUps: 'meeting:manage_follow_ups',
  },
  // Care Team (ABAC Phase 2–4)
  CareTeam: {
    Read: 'care_team:read',
    Assign: 'care_team:assign',
    Bypass: 'care_team:bypass',
  },
  // Leave Management
  LeaveRequests: {
    Read: 'leave_request:read',
    Submit: 'leave_request:submit',
    Update: 'leave_request:update',
    Cancel: 'leave_request:cancel',
    Approve: 'leave_request:approve',
    Reject: 'leave_request:reject',
  },
  LeaveBalances: {
    Read: 'leave_balance:read',
    Manage: 'leave_balance:manage',
  },
  LeaveReports: {
    Read: 'leave_report:read',
  },
  // Camp Management
  Camps: {
    Read: 'camp:read',
    Create: 'camp:create',
    Update: 'camp:update',
    Delete: 'camp:delete',
    Activate: 'camp:activate',
    ManagePeriods: 'camp:manage_periods',
  },
  CampEnrollments: {
    Read: 'camp_enrollment:read',
    Enroll: 'camp_enrollment:enroll',
    Manage: 'camp_enrollment:manage',
    Withdraw: 'camp_enrollment:withdraw',
    Complete: 'camp_enrollment:complete',
  },
  CampAttendance: {
    Read: 'camp_attendance:read',
    Record: 'camp_attendance:record',
  },
  CampReports: {
    Read: 'camp_report:read',
    Create: 'camp_report:create',
  },
  CampActivities: {
    Read: 'camp_activity:read',
    Create: 'camp_activity:create',
    Update: 'camp_activity:update',
    Delete: 'camp_activity:delete',
  },
  CampEducators: {
    Read: 'camp_educator:read',
    Manage: 'camp_educator:manage',
  },
  CampParticipation: {
    Read: 'camp_participation:read',
    Record: 'camp_participation:record',
  },
  // Educator Performance & KPI
  Kpi: {
    Read: 'kpi:read',
    Manage: 'kpi:manage',
    Compute: 'kpi:compute',
  },
  KpiSnapshots: {
    Read: 'kpi_snapshot:read',
    Manage: 'kpi_snapshot:manage',
  },
  KpiReports: {
    Read: 'kpi_report:read',
  },
  KpiDashboard: {
    Read: 'kpi_dashboard:read',
  },
  // School Consultancy Management
  Institutions: {
    Read: 'institution:read',
    Create: 'institution:create',
    Update: 'institution:update',
    Delete: 'institution:delete',
  },
  ConsultancyPlans: {
    Read: 'consultancy_plan:read',
    Create: 'consultancy_plan:create',
    Update: 'consultancy_plan:update',
    Delete: 'consultancy_plan:delete',
    Activate: 'consultancy_plan:activate',
    Complete: 'consultancy_plan:complete',
    Cancel: 'consultancy_plan:cancel',
  },
  SchoolVisits: {
    Read: 'school_visit:read',
    Create: 'school_visit:create',
    Update: 'school_visit:update',
    Delete: 'school_visit:delete',
    Complete: 'school_visit:complete',
    Cancel: 'school_visit:cancel',
  },
  Observations: {
    Read: 'observation:read',
    Create: 'observation:create',
    Update: 'observation:update',
    Delete: 'observation:delete',
  },
  ConsultancyReports: {
    Read: 'consultancy_report:read',
    Create: 'consultancy_report:create',
    Delete: 'consultancy_report:delete',
  },
  ConsultancyAgreements: {
    Read: 'consultancy_agreement:read',
    Create: 'consultancy_agreement:create',
    Update: 'consultancy_agreement:update',
    Delete: 'consultancy_agreement:delete',
    Send: 'consultancy_agreement:send',
    Sign: 'consultancy_agreement:sign',
    Expire: 'consultancy_agreement:expire',
    Cancel: 'consultancy_agreement:cancel',
  },
  FollowUps: {
    Read: 'follow_up:read',
    Create: 'follow_up:create',
    Update: 'follow_up:update',
    Delete: 'follow_up:delete',
    Start: 'follow_up:start',
    Complete: 'follow_up:complete',
    Cancel: 'follow_up:cancel',
  },
  // Contract & Consent Management
  ContractTemplates: {
    Read: 'contract_template:read',
    Create: 'contract_template:create',
    Update: 'contract_template:update',
    Delete: 'contract_template:delete',
    Version: 'contract_template:version',
  },
  StudentContracts: {
    Read: 'student_contract:read',
    Generate: 'student_contract:generate',
    Update: 'student_contract:update',
    Delete: 'student_contract:delete',
    Send: 'student_contract:send',
    Sign: 'student_contract:sign',
    Activate: 'student_contract:activate',
    Expire: 'student_contract:expire',
    Terminate: 'student_contract:terminate',
  },
  ConsentTemplates: {
    Read: 'consent_template:read',
    Create: 'consent_template:create',
    Update: 'consent_template:update',
    Delete: 'consent_template:delete',
    Version: 'consent_template:version',
  },
  StudentConsents: {
    Read: 'student_consent:read',
    Grant: 'student_consent:grant',
    Withdraw: 'student_consent:withdraw',
    AttachEvidence: 'student_consent:attach_evidence',
  },
  LegalReports: {
    Read: 'legal_report:read',
  },
  // Camera Management
  Camera: {
    Read: 'camera:read',
    Create: 'camera:create',
    Update: 'camera:update',
    Delete: 'camera:delete',
  },
  CameraAssignment: {
    Read: 'camera_assignment:read',
    Manage: 'camera_assignment:manage',
  },
  ViewingAuthorization: {
    Read: 'viewing_authorization:read',
    Grant: 'viewing_authorization:grant',
    Revoke: 'viewing_authorization:revoke',
  },
  ViewingSession: {
    Start: 'viewing_session:start',
    End: 'viewing_session:end',
  },
  ViewingLog: {
    Read: 'viewing_log:read',
  },
} as const

type Values<T> = T[keyof T]
export type PermissionCode = Values<{ [G in keyof typeof Permissions]: Values<(typeof Permissions)[G]> }>
