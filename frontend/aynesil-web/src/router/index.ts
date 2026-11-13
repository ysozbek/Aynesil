/**
 * Vue Router
 * Route guards:
 *  - requiresAuth: redirect to /login if not authenticated
 *  - requiresPermission: redirect to /403 if permission missing
 *  - requiresGuest: redirect to / if already authenticated (login page)
 * Business module routes are lazy-loaded and added dynamically via
 * addBusinessModuleRoutes() as modules are enabled per tenant.
 */
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { Permissions } from '@/constants/permissions'

const routes: RouteRecordRaw[] = [
  // ── Public routes ─────────────────────────────────────────────────────────
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { requiresGuest: true, layout: 'auth' },
  },
  {
    path: '/forgot-password',
    name: 'forgot-password',
    component: () => import('@/views/auth/ForgotPasswordView.vue'),
    meta: { requiresGuest: true, layout: 'auth' },
  },
  {
    path: '/reset-password',
    name: 'reset-password',
    component: () => import('@/views/auth/ResetPasswordView.vue'),
    meta: { requiresGuest: true, layout: 'auth' },
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/views/errors/ForbiddenView.vue'),
    meta: { layout: 'blank' },
  },
  {
    path: '/404',
    name: 'not-found',
    component: () => import('@/views/errors/NotFoundView.vue'),
    meta: { layout: 'blank' },
  },

  // ── Authenticated shell ───────────────────────────────────────────────────
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      // Own password (no permission: every signed-in user, incl. portal parents)
      {
        path: 'account/password',
        name: 'account-password',
        component: () => import('@/views/account/ChangePasswordView.vue'),
      },
      {
        path: 'settings',
        name: 'settings',
        component: () => import('@/views/settings/SettingsView.vue'),
        meta: { permission: Permissions.Settings.Read },
      },

      // ── Corporation & Campus ───────────────────────────────────────────────
      {
        path: 'corporations',
        name: 'corporations',
        component: () => import('@/views/corporations/CorporationListView.vue'),
        meta: { permission: Permissions.Corporation.Read },
      },
      {
        path: 'corporations/:id',
        name: 'corporation-detail',
        component: () => import('@/views/corporations/CorporationDetailView.vue'),
        meta: { permission: Permissions.Corporation.Read },
      },
      {
        path: 'corporations/:id/settings',
        name: 'corporation-settings',
        component: () => import('@/views/corporations/CorporationSettingsView.vue'),
        meta: { permission: Permissions.Corporation.Read },
      },
      {
        path: 'campuses',
        name: 'campuses',
        component: () => import('@/views/campuses/CampusListView.vue'),
        meta: { permission: Permissions.Campus.Read },
      },

      // ── User Management ───────────────────────────────────────────────────
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UsersView.vue'),
        meta: { permission: Permissions.Users.Read },
      },
      {
        path: 'users/:id',
        name: 'user-detail',
        component: () => import('@/views/users/UserDetailView.vue'),
        meta: { permission: Permissions.Users.Read },
      },

      // ── Role & Permission Management ──────────────────────────────────────
      {
        path: 'roles',
        name: 'roles',
        component: () => import('@/views/roles/RolesView.vue'),
        meta: { permission: Permissions.Roles.Read },
      },
      {
        path: 'roles/:id',
        name: 'role-detail',
        component: () => import('@/views/roles/RoleDetailView.vue'),
        meta: { permission: Permissions.Roles.Read },
      },
      {
        path: 'permissions',
        name: 'permissions',
        component: () => import('@/views/permissions/PermissionsView.vue'),
        meta: { permission: Permissions.Roles.Read },
      },

      // ── Dynamic Menu Management ───────────────────────────────────────────
      {
        path: 'menus',
        name: 'menus',
        component: () => import('@/views/menus/MenusView.vue'),
        meta: { permission: Permissions.Menu.Read },
      },

      // ── Reference Data ────────────────────────────────────────────────────
      {
        path: 'reference-data',
        name: 'reference-data',
        component: () => import('@/views/refdata/RefDataView.vue'),
        meta: { permission: Permissions.RefData.Read },
      },

      // ── CRM & Lead Management ──────────────────────────────────────────────
      {
        path: 'crm',
        name: 'crm-dashboard',
        component: () => import('@/views/crm/CrmDashboardView.vue'),
        meta: { permission: Permissions.Leads.Read },
      },
      {
        path: 'crm/leads',
        name: 'leads',
        component: () => import('@/views/crm/LeadsView.vue'),
        meta: { permission: Permissions.Leads.Read },
      },
      {
        path: 'crm/leads/new',
        name: 'leads-new',
        component: () => import('@/views/crm/LeadFormView.vue'),
        meta: { permission: Permissions.Leads.Create },
      },
      {
        path: 'crm/leads/:id',
        name: 'lead-detail',
        component: () => import('@/views/crm/LeadDetailView.vue'),
        meta: { permission: Permissions.Leads.Read },
      },
      {
        path: 'crm/leads/:id/edit',
        name: 'lead-edit',
        component: () => import('@/views/crm/LeadFormView.vue'),
        meta: { permission: Permissions.Leads.Update },
      },
      {
        path: 'crm/leads/:id/convert',
        name: 'lead-convert',
        component: () => import('@/views/crm/LeadConvertView.vue'),
        meta: { permission: Permissions.Leads.Convert },
      },
      {
        path: 'crm/pipeline',
        name: 'crm-pipeline',
        component: () => import('@/views/crm/PipelineView.vue'),
        meta: { permission: Permissions.Leads.Read },
      },
      {
        path: 'crm/activities',
        name: 'crm-activities',
        component: () => import('@/views/crm/ActivitiesView.vue'),
        meta: { permission: Permissions.LeadActivities.Read },
      },
      {
        path: 'crm/reports',
        name: 'crm-reports',
        component: () => import('@/views/crm/ReportsView.vue'),
        meta: { permission: Permissions.Leads.Read },
      },

      // ── Assessment & Evaluation ────────────────────────────────────────────
      {
        path: 'assessment',
        name: 'assessment-dashboard',
        component: () => import('@/views/assessment/AssessmentDashboardView.vue'),
        meta: { permission: Permissions.AssessmentSessions.Read },
      },
      {
        path: 'assessment/templates',
        name: 'assessment-templates',
        component: () => import('@/views/assessment/TemplateListView.vue'),
        meta: { permission: Permissions.AssessmentTemplates.Read },
      },
      {
        path: 'assessment/templates/new',
        name: 'assessment-template-new',
        component: () => import('@/views/assessment/TemplateFormView.vue'),
        meta: { permission: Permissions.AssessmentTemplates.Create },
      },
      {
        path: 'assessment/templates/:id',
        name: 'assessment-template-detail',
        component: () => import('@/views/assessment/TemplateDetailView.vue'),
        meta: { permission: Permissions.AssessmentTemplates.Read },
      },
      {
        path: 'assessment/templates/:id/edit',
        name: 'assessment-template-edit',
        component: () => import('@/views/assessment/TemplateFormView.vue'),
        meta: { permission: Permissions.AssessmentTemplates.Update },
      },
      {
        path: 'assessment/sessions',
        name: 'assessment-sessions',
        component: () => import('@/views/assessment/SessionListView.vue'),
        meta: { permission: Permissions.AssessmentSessions.Read },
      },
      {
        path: 'assessment/sessions/new',
        name: 'assessment-sessions-new',
        component: () => import('@/views/assessment/SessionFormView.vue'),
        meta: { permission: Permissions.AssessmentSessions.Create },
      },
      {
        path: 'assessment/sessions/:id',
        name: 'assessment-session-detail',
        component: () => import('@/views/assessment/SessionDetailView.vue'),
        meta: { permission: Permissions.AssessmentSessions.Read },
      },
      {
        path: 'assessment/sessions/:id/edit',
        name: 'assessment-session-edit',
        component: () => import('@/views/assessment/SessionFormView.vue'),
        meta: { permission: Permissions.AssessmentSessions.Update },
      },

      // ── Student Lifecycle ──────────────────────────────────────────────────────
      {
        path: 'students',
        name: 'students',
        component: () => import('@/views/students/StudentListView.vue'),
        meta: { permission: Permissions.Students.Read },
      },
      {
        path: 'students/new',
        name: 'student-new',
        component: () => import('@/views/students/StudentFormView.vue'),
        meta: { permission: Permissions.Students.Create },
      },
      {
        path: 'students/:id',
        name: 'student-detail',
        component: () => import('@/views/students/StudentDetailView.vue'),
        meta: { permission: Permissions.Students.Read },
      },
      {
        path: 'students/:id/edit',
        name: 'student-edit',
        component: () => import('@/views/students/StudentFormView.vue'),
        meta: { permission: Permissions.Students.Update },
      },
      {
        path: 'students/:id/dashboard',
        name: 'student-dashboard',
        component: () => import('@/views/students/StudentDashboardView.vue'),
        meta: { permission: Permissions.Students.Read },
      },
      {
        path: 'students/:id/timeline',
        name: 'student-timeline',
        component: () => import('@/views/students/StudentTimelineView.vue'),
        meta: { permission: Permissions.Students.Read },
      },

      // ── Guardian Management ────────────────────────────────────────────────────
      {
        path: 'guardians',
        name: 'guardians',
        component: () => import('@/views/guardians/GuardianListView.vue'),
        meta: { permission: Permissions.Guardians.Read },
      },
      {
        path: 'guardians/new',
        name: 'guardian-new',
        component: () => import('@/views/guardians/GuardianFormView.vue'),
        meta: { permission: Permissions.Guardians.Create },
      },
      {
        path: 'guardians/:id',
        name: 'guardian-detail',
        component: () => import('@/views/guardians/GuardianDetailView.vue'),
        meta: { permission: Permissions.Guardians.Read },
      },
      {
        path: 'guardians/:id/edit',
        name: 'guardian-edit',
        component: () => import('@/views/guardians/GuardianFormView.vue'),
        meta: { permission: Permissions.Guardians.Update },
      },

      // ── Educator Management ────────────────────────────────────────────────────
      {
        path: 'educators',
        name: 'educators',
        component: () => import('@/views/educators/EducatorListView.vue'),
        meta: { permission: Permissions.Educators.Read },
      },
      {
        path: 'educators/new',
        name: 'educator-new',
        component: () => import('@/views/educators/EducatorFormView.vue'),
        meta: { permission: Permissions.Educators.Create },
      },
      {
        path: 'educators/:id',
        name: 'educator-detail',
        component: () => import('@/views/educators/EducatorDetailView.vue'),
        meta: { permission: Permissions.Educators.Read },
      },
      {
        path: 'educators/:id/edit',
        name: 'educator-edit',
        component: () => import('@/views/educators/EducatorFormView.vue'),
        meta: { permission: Permissions.Educators.Update },
      },
      {
        path: 'educators/:id/availability',
        name: 'educator-availability',
        component: () => import('@/views/educators/EducatorDashboardView.vue'),
        meta: { permission: Permissions.Educators.Read },
      },

      // ── Program Management ─────────────────────────────────────────────────────
      {
        path: 'programs',
        name: 'programs',
        component: () => import('@/views/programs/ProgramListView.vue'),
        meta: { permission: Permissions.Programs.Read },
      },
      {
        path: 'programs/new',
        name: 'program-new',
        component: () => import('@/views/programs/ProgramFormView.vue'),
        meta: { permission: Permissions.Programs.Create },
      },
      // Static paths before :id so "enrollments" is not captured as a program id.
      {
        path: 'programs/enrollments',
        name: 'enrollments',
        component: () => import('@/views/programs/EnrollmentListView.vue'),
        meta: { permission: Permissions.Enrollments.Read },
      },
      {
        path: 'programs/enrollments/:id',
        name: 'enrollment-detail',
        component: () => import('@/views/programs/EnrollmentDetailView.vue'),
        meta: { permission: Permissions.Enrollments.Read },
      },
      {
        path: 'programs/:id',
        name: 'program-detail',
        component: () => import('@/views/programs/ProgramDetailView.vue'),
        meta: { permission: Permissions.Programs.Read },
      },
      {
        path: 'programs/:id/edit',
        name: 'program-edit',
        component: () => import('@/views/programs/ProgramFormView.vue'),
        meta: { permission: Permissions.Programs.Update },
      },

      // ── BEP / IEP (Education Plans) ────────────────────────────────────────────
      {
        path: 'bep',
        name: 'bep-list',
        component: () => import('@/views/bep/EducationPlanListView.vue'),
        meta: { permission: Permissions.EducationPlans.Read },
      },
      {
        path: 'bep/new',
        name: 'bep-new',
        component: () => import('@/views/bep/EducationPlanFormView.vue'),
        meta: { permission: Permissions.EducationPlans.Create },
      },
      {
        path: 'bep/:id',
        name: 'bep-detail',
        component: () => import('@/views/bep/EducationPlanDetailView.vue'),
        meta: { permission: Permissions.EducationPlans.Read },
      },
      {
        path: 'bep/:id/edit',
        name: 'bep-edit',
        component: () => import('@/views/bep/EducationPlanFormView.vue'),
        meta: { permission: Permissions.EducationPlans.Update },
      },
      // Menu seed `/education-plans/*` (db/migrations/V12) still points to the
      // older URL base. Keep both URLs working so permissioned menu rows do not
      // 404 while we align menu paths in a follow-up migration.
      { path: 'education-plans', redirect: '/bep' },
      { path: 'education-plans/:id', redirect: (to) => `/bep/${to.params.id}` },
      { path: 'education-plans/:id/edit', redirect: (to) => `/bep/${to.params.id}/edit` },

      // ── Scheduling & Sessions ──────────────────────────────────────────────────
      {
        path: 'scheduling',
        name: 'scheduling-dashboard',
        component: () => import('@/views/scheduling/SchedulingDashboardView.vue'),
        meta: { permission: Permissions.Sessions.Read },
      },
      {
        path: 'scheduling/calendar',
        name: 'scheduling-calendar',
        component: () => import('@/views/scheduling/CalendarView.vue'),
        meta: { permission: Permissions.Sessions.Read },
      },
      {
        path: 'scheduling/sessions',
        name: 'sessions',
        component: () => import('@/views/scheduling/SessionListView.vue'),
        meta: { permission: Permissions.Sessions.Read },
      },
      {
        path: 'scheduling/sessions/new',
        name: 'session-new',
        component: () => import('@/views/scheduling/SessionFormView.vue'),
        meta: { permission: Permissions.Sessions.Create },
      },
      {
        path: 'scheduling/sessions/:id',
        name: 'session-detail',
        component: () => import('@/views/scheduling/SessionDetailView.vue'),
        meta: { permission: Permissions.Sessions.Read },
      },
      {
        path: 'scheduling/sessions/:id/edit',
        name: 'session-edit',
        component: () => import('@/views/scheduling/SessionFormView.vue'),
        meta: { permission: Permissions.Sessions.Update },
      },
      {
        path: 'scheduling/rooms',
        name: 'rooms',
        component: () => import('@/views/scheduling/RoomListView.vue'),
        meta: { permission: Permissions.Rooms.Read },
      },
      {
        // Alias keeps V13 menu path (/scheduling/recurring) working if V31 not applied.
        path: 'scheduling/recurring-schedules',
        alias: ['scheduling/recurring'],
        name: 'recurring-schedules',
        component: () => import('@/views/scheduling/RecurringScheduleListView.vue'),
        // API gates recurring schedules with session:* (no recurring_schedule:* catalog).
        meta: { permission: Permissions.Sessions.Read },
      },
      {
        path: 'scheduling/recurring-schedules/new',
        name: 'recurring-schedule-new',
        component: () => import('@/views/scheduling/RecurringScheduleFormView.vue'),
        meta: { permission: Permissions.Sessions.Create },
      },
      {
        path: 'scheduling/attendance',
        name: 'attendance-dashboard',
        component: () => import('@/views/scheduling/AttendanceDashboardView.vue'),
        meta: { permission: Permissions.Attendance.Read },
      },
      {
        path: 'scheduling/makeup-requests',
        name: 'makeup-requests',
        component: () => import('@/views/scheduling/MakeupRequestListView.vue'),
        meta: { permission: Permissions.MakeupRequests.Read },
      },
      {
        path: 'scheduling/makeup-requests/:id',
        name: 'makeup-request-detail',
        component: () => import('@/views/scheduling/MakeupRequestListView.vue'),
        meta: { permission: Permissions.MakeupRequests.Read },
      },

      // ── Finance & Payments ─────────────────────────────────────────────────────
      {
        path: 'finance',
        name: 'finance-dashboard',
        // Backend catalog: Permissions.FinancePayments.Read = Permissions.FinancePayments.Read.
        component: () => import('@/views/finance/FinanceDashboardView.vue'),
        meta: { permission: Permissions.FinancePayments.Read },
      },
      {
        path: 'finance/packages',
        name: 'packages',
        component: () => import('@/views/finance/PackageDefinitionListView.vue'),
        meta: { permission: Permissions.PackageDefinitions.Read },
      },
      {
        path: 'finance/packages/new',
        name: 'package-new',
        component: () => import('@/views/finance/PackageDefinitionFormView.vue'),
        meta: { permission: Permissions.PackageDefinitions.Create },
      },
      {
        path: 'finance/packages/:id',
        name: 'package-detail',
        component: () => import('@/views/finance/PackageDefinitionListView.vue'),
        meta: { permission: Permissions.PackageDefinitions.Read },
      },
      {
        path: 'finance/packages/:id/edit',
        name: 'package-edit',
        component: () => import('@/views/finance/PackageDefinitionFormView.vue'),
        meta: { permission: Permissions.PackageDefinitions.Update },
      },
      {
        path: 'finance/student-packages',
        name: 'student-packages',
        component: () => import('@/views/finance/StudentPackageListView.vue'),
        meta: { permission: Permissions.StudentPackages.Read },
      },
      {
        path: 'finance/student-packages/new',
        name: 'student-package-new',
        component: () => import('@/views/finance/StudentPackageFormView.vue'),
        meta: { permission: Permissions.StudentPackages.Purchase },
      },
      {
        path: 'finance/student-packages/:id',
        name: 'student-package-detail',
        component: () => import('@/views/finance/StudentPackageListView.vue'),
        meta: { permission: Permissions.StudentPackages.Read },
      },
      {
        path: 'finance/credits',
        name: 'credit-ledger',
        // Backend catalog: Permissions.CreditLedger.Read = Permissions.CreditLedger.Read.
        component: () => import('@/views/finance/CreditLedgerView.vue'),
        meta: { permission: Permissions.CreditLedger.Read },
      },
      {
        path: 'finance/invoices',
        name: 'invoices',
        component: () => import('@/views/finance/InvoiceListView.vue'),
        meta: { permission: Permissions.Invoices.Read },
      },
      {
        path: 'finance/invoices/new',
        name: 'invoice-new',
        component: () => import('@/views/finance/InvoiceDetailView.vue'),
        meta: { permission: Permissions.Invoices.Create },
      },
      {
        path: 'finance/invoices/:id',
        name: 'invoice-detail',
        component: () => import('@/views/finance/InvoiceDetailView.vue'),
        meta: { permission: Permissions.Invoices.Read },
      },
      {
        path: 'finance/payments',
        name: 'payments',
        // Permissions.FinancePayments.* uses the finance_payment:* prefix.
        component: () => import('@/views/finance/PaymentListView.vue'),
        meta: { permission: Permissions.FinancePayments.Read },
      },
      {
        path: 'finance/refunds',
        name: 'refunds',
        component: () => import('@/views/finance/RefundListView.vue'),
        meta: { permission: Permissions.Refunds.Read },
      },
      {
        path: 'finance/payments/new',
        name: 'payment-new',
        component: () => import('@/views/finance/PaymentFormView.vue'),
        meta: { permission: Permissions.FinancePayments.Record },
      },
      {
        path: 'finance/payments/:id',
        name: 'payment-detail',
        component: () => import('@/views/finance/PaymentDetailView.vue'),
        meta: { permission: Permissions.FinancePayments.Read },
      },
      {
        path: 'finance/scholarships',
        name: 'scholarships',
        component: () => import('@/views/finance/ScholarshipListView.vue'),
        meta: { permission: Permissions.Scholarships.Read },
      },
      {
        path: 'finance/promotions',
        name: 'promotions',
        component: () => import('@/views/finance/PromotionListView.vue'),
        meta: { permission: Permissions.Promotions.Read },
      },

      // ── Notification Management ───────────────────────────────────────────────
      {
        path: 'notifications',
        name: 'notification-dashboard',
        component: () => import('@/views/notifications/NotificationDashboardView.vue'),
        meta: { permission: Permissions.Notifications.Read },
      },
      {
        path: 'notifications/inbox',
        name: 'notification-list',
        component: () => import('@/views/notifications/NotificationListView.vue'),
        meta: { permission: Permissions.Notifications.Read },
      },
      {
        path: 'notifications/inbox/:id',
        name: 'notification-detail',
        component: () => import('@/views/notifications/NotificationDetailView.vue'),
        meta: { permission: Permissions.Notifications.Read },
      },
      {
        path: 'notifications/templates',
        name: 'notification-templates',
        component: () => import('@/views/notifications/NotificationTemplateListView.vue'),
        meta: { permission: Permissions.NotificationTemplates.Read },
      },
      {
        path: 'notifications/templates/new',
        name: 'notification-template-new',
        component: () => import('@/views/notifications/NotificationTemplateFormView.vue'),
        meta: { permission: Permissions.NotificationTemplates.Create },
      },
      {
        path: 'notifications/templates/:id/edit',
        name: 'notification-template-edit',
        component: () => import('@/views/notifications/NotificationTemplateFormView.vue'),
        meta: { permission: Permissions.NotificationTemplates.Update },
      },
      {
        path: 'notifications/preferences',
        name: 'notification-preferences',
        component: () => import('@/views/notifications/NotificationPreferencesView.vue'),
        meta: { requiresAuth: true },
      },

      // ── Meeting Management ─────────────────────────────────────────────────────
      {
        path: 'meetings',
        name: 'meeting-dashboard',
        component: () => import('@/views/meetings/MeetingDashboardView.vue'),
        meta: { permission: Permissions.Meetings.Read },
      },
      {
        path: 'meetings/list',
        name: 'meetings',
        component: () => import('@/views/meetings/MeetingListView.vue'),
        meta: { permission: Permissions.Meetings.Read },
      },
      {
        path: 'meetings/new',
        name: 'meeting-new',
        component: () => import('@/views/meetings/MeetingFormView.vue'),
        meta: { permission: Permissions.Meetings.Create },
      },
      {
        path: 'meetings/calendar',
        name: 'meeting-calendar',
        component: () => import('@/views/meetings/MeetingCalendarView.vue'),
        meta: { permission: Permissions.Meetings.Read },
      },
      {
        path: 'meetings/follow-ups',
        name: 'follow-ups',
        component: () => import('@/views/meetings/FollowUpListView.vue'),
        meta: { permission: Permissions.Meetings.Read },
      },
      {
        path: 'meetings/:id',
        name: 'meeting-detail',
        component: () => import('@/views/meetings/MeetingDetailView.vue'),
        meta: { permission: Permissions.Meetings.Read },
      },
      {
        path: 'meetings/:id/edit',
        name: 'meeting-edit',
        component: () => import('@/views/meetings/MeetingFormView.vue'),
        meta: { permission: Permissions.Meetings.Update },
      },

      // ── Parent Portal ──────────────────────────────────────────────────────────
      {
        path: 'portal',
        name: 'portal-dashboard',
        component: () => import('@/views/portal/PortalDashboardView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children',
        name: 'portal-children',
        component: () => import('@/views/portal/MyChildrenView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children/:studentId',
        name: 'portal-child-detail',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children/:studentId/sessions',
        name: 'portal-sessions',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children/:studentId/goals',
        name: 'portal-goals',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children/:studentId/packages',
        name: 'portal-packages',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/children/:studentId/documents',
        name: 'portal-documents',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },
      {
        path: 'portal/notifications',
        name: 'portal-notifications',
        component: () => import('@/views/portal/PortalNotificationsView.vue'),
        meta: { permission: Permissions.Portal.Access },
      },

      // ── Leave Management ───────────────────────────────────────────────────────
      {
        path: 'leave',
        name: 'leave-dashboard',
        component: () => import('@/views/leave/LeaveDashboardView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },
      {
        path: 'leave/requests',
        name: 'leave-list',
        component: () => import('@/views/leave/LeaveListView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },
      {
        path: 'leave/requests/new',
        name: 'leave-new',
        component: () => import('@/views/leave/LeaveFormView.vue'),
        meta: { permission: Permissions.LeaveRequests.Submit },
      },
      {
        path: 'leave/requests/:id',
        name: 'leave-detail',
        component: () => import('@/views/leave/LeaveDetailView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },
      {
        path: 'leave/requests/:id/edit',
        name: 'leave-edit',
        component: () => import('@/views/leave/LeaveFormView.vue'),
        meta: { permission: Permissions.LeaveRequests.Update },
      },
      {
        path: 'leave/calendar',
        name: 'leave-calendar',
        component: () => import('@/views/leave/LeaveCalendarView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },
      {
        path: 'leave/balances',
        name: 'leave-balances',
        component: () => import('@/views/leave/LeaveBalanceView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },
      {
        path: 'leave/reports',
        name: 'leave-reports',
        component: () => import('@/views/leave/LeaveReportsView.vue'),
        meta: { permission: Permissions.LeaveRequests.Read },
      },

      // ── Camera Management ──────────────────────────────────────────────────────
      {
        path: 'cameras',
        name: 'camera-dashboard',
        component: () => import('@/views/cameras/CameraDashboardView.vue'),
        meta: { permission: Permissions.Camera.Read },
      },
      {
        path: 'cameras/list',
        name: 'cameras',
        component: () => import('@/views/cameras/CameraListView.vue'),
        meta: { permission: Permissions.Camera.Read },
      },
      {
        path: 'cameras/new',
        name: 'camera-new',
        component: () => import('@/views/cameras/CameraFormView.vue'),
        meta: { permission: Permissions.Camera.Create },
      },
      {
        path: 'cameras/authorizations',
        name: 'camera-authorizations',
        component: () => import('@/views/cameras/ViewingAuthorizationsView.vue'),
        meta: { permission: Permissions.ViewingAuthorization.Read },
      },
      {
        path: 'cameras/viewing-history',
        name: 'camera-viewing-history',
        component: () => import('@/views/cameras/ViewingHistoryView.vue'),
        meta: { permission: Permissions.ViewingLog.Read },
      },
      {
        path: 'cameras/:id',
        name: 'camera-detail',
        component: () => import('@/views/cameras/CameraDetailView.vue'),
        meta: { permission: Permissions.Camera.Read },
      },
      {
        path: 'cameras/:id/edit',
        name: 'camera-edit',
        component: () => import('@/views/cameras/CameraFormView.vue'),
        meta: { permission: Permissions.Camera.Update },
      },

      // ── Camp Management ────────────────────────────────────────────────────────
      {
        path: 'camps',
        name: 'camp-dashboard',
        component: () => import('@/views/camps/CampDashboardView.vue'),
        meta: { permission: Permissions.Camps.Read },
      },
      {
        path: 'camps/list',
        name: 'camps',
        component: () => import('@/views/camps/CampListView.vue'),
        meta: { permission: Permissions.Camps.Read },
      },
      {
        path: 'camps/new',
        name: 'camp-new',
        component: () => import('@/views/camps/CampFormView.vue'),
        meta: { permission: Permissions.Camps.Create },
      },
      {
        path: 'camps/:id',
        name: 'camp-detail',
        component: () => import('@/views/camps/CampDetailView.vue'),
        meta: { permission: Permissions.Camps.Read },
      },
      {
        path: 'camps/:id/edit',
        name: 'camp-edit',
        component: () => import('@/views/camps/CampFormView.vue'),
        meta: { permission: Permissions.Camps.Update },
      },
      {
        path: 'camps/periods/:periodId/enrollments',
        name: 'camp-enrollment',
        component: () => import('@/views/camps/CampEnrollmentView.vue'),
        meta: { permission: Permissions.CampEnrollments.Read },
      },

      // ── School Consultancy Management ──────────────────────────────────────────
      {
        path: 'consultancy',
        name: 'consultancy-dashboard',
        component: () => import('@/views/consultancy/ConsultancyDashboardView.vue'),
        meta: { permission: Permissions.Institutions.Read },
      },
      {
        path: 'consultancy/institutions',
        name: 'institutions',
        component: () => import('@/views/consultancy/InstitutionListView.vue'),
        meta: { permission: Permissions.Institutions.Read },
      },
      {
        path: 'consultancy/institutions/new',
        name: 'institution-new',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: Permissions.Institutions.Create },
      },
      {
        path: 'consultancy/institutions/:id',
        name: 'institution-detail',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: Permissions.Institutions.Read },
      },
      {
        path: 'consultancy/institutions/:id/edit',
        name: 'institution-edit',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: Permissions.Institutions.Update },
      },
      {
        path: 'consultancy/plans',
        name: 'consultancy-plans',
        component: () => import('@/views/consultancy/PlanListView.vue'),
        meta: { permission: Permissions.ConsultancyPlans.Read },
      },
      {
        path: 'consultancy/plans/:id',
        name: 'consultancy-plan-detail',
        component: () => import('@/views/consultancy/PlanDetailView.vue'),
        meta: { permission: Permissions.ConsultancyPlans.Read },
      },
      {
        path: 'consultancy/visits',
        name: 'consultancy-visits',
        component: () => import('@/views/consultancy/VisitListView.vue'),
        meta: { permission: Permissions.SchoolVisits.Read },
      },
      {
        path: 'consultancy/visits/:id',
        name: 'consultancy-visit-detail',
        component: () => import('@/views/consultancy/VisitListView.vue'),
        meta: { permission: Permissions.SchoolVisits.Read },
      },
      {
        path: 'consultancy/observations',
        name: 'consultancy-observations',
        component: () => import('@/views/consultancy/ObservationListView.vue'),
        meta: { permission: Permissions.Observations.Read },
      },
      {
        path: 'consultancy/reports',
        name: 'consultancy-reports',
        component: () => import('@/views/consultancy/ReportListView.vue'),
        meta: { permission: Permissions.ConsultancyReports.Read },
      },
      {
        path: 'consultancy/agreements',
        name: 'consultancy-agreements',
        component: () => import('@/views/consultancy/AgreementListView.vue'),
        meta: { permission: Permissions.ConsultancyAgreements.Read },
      },
      {
        path: 'consultancy/agreements/new',
        name: 'agreement-new',
        component: () => import('@/views/consultancy/AgreementFormView.vue'),
        meta: { permission: Permissions.ConsultancyAgreements.Create },
      },
      {
        path: 'consultancy/agreements/:id',
        name: 'agreement-detail',
        component: () => import('@/views/consultancy/AgreementDetailView.vue'),
        meta: { permission: Permissions.ConsultancyAgreements.Read },
      },
      {
        path: 'consultancy/agreements/:id/edit',
        name: 'agreement-edit',
        component: () => import('@/views/consultancy/AgreementFormView.vue'),
        meta: { permission: Permissions.ConsultancyAgreements.Update },
      },
      {
        path: 'consultancy/follow-ups',
        name: 'consultancy-follow-ups',
        component: () => import('@/views/consultancy/FollowUpListView.vue'),
        meta: { permission: Permissions.FollowUps.Read },
      },
      {
        path: 'consultancy/follow-ups/open',
        name: 'follow-ups-open-report',
        component: () => import('@/views/consultancy/OpenFollowUpReportView.vue'),
        meta: { permission: Permissions.FollowUps.Read },
      },
      {
        path: 'consultancy/follow-ups/new',
        name: 'follow-up-new',
        component: () => import('@/views/consultancy/FollowUpFormView.vue'),
        meta: { permission: Permissions.FollowUps.Create },
      },
      {
        path: 'consultancy/follow-ups/:id',
        name: 'follow-up-detail',
        component: () => import('@/views/consultancy/FollowUpDetailView.vue'),
        meta: { permission: Permissions.FollowUps.Read },
      },
      {
        path: 'consultancy/follow-ups/:id/edit',
        name: 'follow-up-edit',
        component: () => import('@/views/consultancy/FollowUpFormView.vue'),
        meta: { permission: Permissions.FollowUps.Update },
      },

      // ── KPI & Performance Management ───────────────────────────────────────────
      {
        path: 'kpi',
        name: 'kpi-dashboard',
        component: () => import('@/views/kpi/KpiDashboardView.vue'),
        meta: { permission: Permissions.KpiDashboard.Read },
      },
      {
        path: 'kpi/definitions',
        name: 'kpi-definitions',
        component: () => import('@/views/kpi/KpiDefinitionListView.vue'),
        meta: { permission: Permissions.Kpi.Read },
      },
      {
        path: 'kpi/definitions/:id',
        name: 'kpi-definition-detail',
        component: () => import('@/views/kpi/KpiDefinitionListView.vue'),
        meta: { permission: Permissions.Kpi.Read },
      },
      {
        path: 'kpi/snapshots',
        name: 'kpi-snapshots',
        component: () => import('@/views/kpi/PerformanceSnapshotListView.vue'),
        meta: { permission: Permissions.KpiSnapshots.Read },
      },

      // ── Legal (Contract & Consent) Management ──────────────────────────────────
      {
        path: 'legal',
        name: 'legal-dashboard',
        component: () => import('@/views/legal/LegalDashboardView.vue'),
        meta: { permission: Permissions.StudentContracts.Read },
      },
      {
        path: 'legal/contracts',
        name: 'contracts',
        component: () => import('@/views/legal/ContractListView.vue'),
        meta: { permission: Permissions.StudentContracts.Read },
      },
      {
        path: 'legal/contracts/new',
        name: 'contract-new',
        component: () => import('@/views/legal/ContractListView.vue'),
        meta: { permission: Permissions.StudentContracts.Generate },
      },
      {
        path: 'legal/contracts/:id',
        name: 'contract-detail',
        component: () => import('@/views/legal/ContractDetailView.vue'),
        meta: { permission: Permissions.StudentContracts.Read },
      },
      {
        path: 'legal/contract-templates',
        name: 'contract-templates',
        component: () => import('@/views/legal/TemplateListView.vue'),
        meta: { permission: Permissions.ContractTemplates.Read, templateKind: 'contract' },
      },
      {
        path: 'legal/consent-templates',
        name: 'consent-templates',
        component: () => import('@/views/legal/TemplateListView.vue'),
        meta: { permission: Permissions.ConsentTemplates.Read, templateKind: 'consent' },
      },
      {
        path: 'legal/consents',
        name: 'consents',
        component: () => import('@/views/legal/ConsentListView.vue'),
        meta: { permission: Permissions.StudentConsents.Read },
      },
      {
        path: 'legal/consents/:id',
        name: 'consent-detail',
        component: () => import('@/views/legal/ConsentListView.vue'),
        meta: { permission: Permissions.StudentConsents.Read },
      },
      {
        path: 'legal/reports',
        name: 'legal-reports',
        component: () => import('@/views/legal/LegalReportsView.vue'),
        meta: { permission: Permissions.LegalReports.Read },
      },

      // ── Goal Management ────────────────────────────────────────────────────────
      {
        path: 'goals',
        name: 'goal-dashboard',
        component: () => import('@/views/goals/GoalDashboardView.vue'),
        meta: { permission: Permissions.StudentGoals.Read },
      },
      {
        path: 'goals/libraries',
        name: 'goal-libraries',
        component: () => import('@/views/goals/GoalLibraryListView.vue'),
        meta: { permission: Permissions.GoalLibraries.Read },
      },
      {
        path: 'goals/templates',
        name: 'goal-template-list',
        component: () => import('@/views/goals/GoalTemplateListView.vue'),
        meta: { permission: Permissions.GoalTemplates.Read },
      },
      {
        path: 'goals/templates/new',
        name: 'goal-template-new',
        component: () => import('@/views/goals/GoalTemplateFormView.vue'),
        meta: { permission: Permissions.GoalTemplates.Create },
      },
      {
        path: 'goals/templates/:id',
        name: 'goal-template-detail',
        component: () => import('@/views/goals/GoalTemplateDetailView.vue'),
        meta: { permission: Permissions.GoalTemplates.Read },
      },
      {
        path: 'goals/templates/:id/edit',
        name: 'goal-template-edit',
        component: () => import('@/views/goals/GoalTemplateFormView.vue'),
        meta: { permission: Permissions.GoalTemplates.Update },
      },
      {
        path: 'goals/student-goals',
        name: 'student-goal-list',
        component: () => import('@/views/goals/StudentGoalListView.vue'),
        meta: { permission: Permissions.StudentGoals.Read },
      },
      {
        path: 'goals/student-goals/new',
        name: 'student-goal-new',
        component: () => import('@/views/goals/StudentGoalFormView.vue'),
        meta: { permission: Permissions.StudentGoals.Create },
      },
      {
        path: 'goals/student-goals/:id',
        name: 'student-goal-detail',
        component: () => import('@/views/goals/StudentGoalDetailView.vue'),
        meta: { permission: Permissions.StudentGoals.Read },
      },
      {
        path: 'goals/student-goals/:id/edit',
        name: 'student-goal-edit',
        component: () => import('@/views/goals/StudentGoalFormView.vue'),
        meta: { permission: Permissions.StudentGoals.Update },
      },
    ],
  },

  // ── Catch-all ─────────────────────────────────────────────────────────────
  { path: '/:pathMatch(.*)*', redirect: '/404' },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

// ── Navigation guards ─────────────────────────────────────────────────────────
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Use to.matched.some() so that parent-level meta.requiresAuth is inherited by all children.
  // A plain to.meta.requiresAuth check only reads the deepest matched route's meta,
  // causing child routes to bypass the auth guard when only the parent has requiresAuth.
  const requiresAuth = to.matched.some((r) => r.meta.requiresAuth)

  if (requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  // İlk giriş parolası değiştirilene kadar yalnızca şifre değiştirme sayfası (backend de diğer uçları reddeder).
  if (auth.isAuthenticated && auth.mustChangePassword && to.name !== 'account-password') {
    return { name: 'account-password' }
  }

  if (to.meta.requiresGuest && auth.isAuthenticated) {
    return { path: '/' }
  }

  if (to.meta.permission && !auth.hasPermission(to.meta.permission as string)) {
    return { name: 'forbidden' }
  }
})

export default router
