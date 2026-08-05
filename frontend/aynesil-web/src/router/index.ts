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

const routes: RouteRecordRaw[] = [
  // ── Public routes ─────────────────────────────────────────────────────────
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
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
      {
        path: 'settings',
        name: 'settings',
        component: () => import('@/views/settings/SettingsView.vue'),
        meta: { permission: 'settings:read' },
      },

      // ── Corporation & Campus ───────────────────────────────────────────────
      {
        path: 'corporations',
        name: 'corporations',
        component: () => import('@/views/corporations/CorporationListView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'corporations/:id',
        name: 'corporation-detail',
        component: () => import('@/views/corporations/CorporationDetailView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'corporations/:id/settings',
        name: 'corporation-settings',
        component: () => import('@/views/corporations/CorporationSettingsView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'campuses',
        name: 'campuses',
        component: () => import('@/views/campuses/CampusListView.vue'),
        meta: { permission: 'campus:read' },
      },

      // ── User Management ───────────────────────────────────────────────────
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UsersView.vue'),
        meta: { permission: 'user:read' },
      },
      {
        path: 'users/:id',
        name: 'user-detail',
        component: () => import('@/views/users/UserDetailView.vue'),
        meta: { permission: 'user:read' },
      },

      // ── Role & Permission Management ──────────────────────────────────────
      {
        path: 'roles',
        name: 'roles',
        component: () => import('@/views/roles/RolesView.vue'),
        meta: { permission: 'role:read' },
      },
      {
        path: 'roles/:id',
        name: 'role-detail',
        component: () => import('@/views/roles/RoleDetailView.vue'),
        meta: { permission: 'role:read' },
      },
      {
        path: 'permissions',
        name: 'permissions',
        component: () => import('@/views/permissions/PermissionsView.vue'),
        meta: { permission: 'role:read' },
      },

      // ── Dynamic Menu Management ───────────────────────────────────────────
      {
        path: 'menus',
        name: 'menus',
        component: () => import('@/views/menus/MenusView.vue'),
        meta: { permission: 'menu:read' },
      },

      // ── Reference Data ────────────────────────────────────────────────────
      {
        path: 'reference-data',
        name: 'reference-data',
        component: () => import('@/views/refdata/RefDataView.vue'),
        meta: { permission: 'ref_data:read' },
      },

      // ── CRM & Lead Management ──────────────────────────────────────────────
      {
        path: 'crm',
        name: 'crm-dashboard',
        component: () => import('@/views/crm/CrmDashboardView.vue'),
        meta: { permission: 'lead:read' },
      },
      {
        path: 'crm/leads',
        name: 'leads',
        component: () => import('@/views/crm/LeadsView.vue'),
        meta: { permission: 'lead:read' },
      },
      {
        path: 'crm/leads/new',
        name: 'leads-new',
        component: () => import('@/views/crm/LeadFormView.vue'),
        meta: { permission: 'lead:create' },
      },
      {
        path: 'crm/leads/:id',
        name: 'lead-detail',
        component: () => import('@/views/crm/LeadDetailView.vue'),
        meta: { permission: 'lead:read' },
      },
      {
        path: 'crm/leads/:id/edit',
        name: 'lead-edit',
        component: () => import('@/views/crm/LeadFormView.vue'),
        meta: { permission: 'lead:update' },
      },
      {
        path: 'crm/leads/:id/convert',
        name: 'lead-convert',
        component: () => import('@/views/crm/LeadConvertView.vue'),
        meta: { permission: 'lead:convert' },
      },
      {
        path: 'crm/pipeline',
        name: 'crm-pipeline',
        component: () => import('@/views/crm/PipelineView.vue'),
        meta: { permission: 'lead:read' },
      },
      {
        path: 'crm/activities',
        name: 'crm-activities',
        component: () => import('@/views/crm/ActivitiesView.vue'),
        meta: { permission: 'lead_activity:read' },
      },
      {
        path: 'crm/reports',
        name: 'crm-reports',
        component: () => import('@/views/crm/ReportsView.vue'),
        meta: { permission: 'lead:read' },
      },

      // ── Assessment & Evaluation ────────────────────────────────────────────
      {
        path: 'assessment',
        name: 'assessment-dashboard',
        component: () => import('@/views/assessment/AssessmentDashboardView.vue'),
        meta: { permission: 'assessment_session:read' },
      },
      {
        path: 'assessment/templates',
        name: 'assessment-templates',
        component: () => import('@/views/assessment/TemplateListView.vue'),
        meta: { permission: 'assessment_template:read' },
      },
      {
        path: 'assessment/templates/new',
        name: 'assessment-template-new',
        component: () => import('@/views/assessment/TemplateFormView.vue'),
        meta: { permission: 'assessment_template:create' },
      },
      {
        path: 'assessment/templates/:id',
        name: 'assessment-template-detail',
        component: () => import('@/views/assessment/TemplateDetailView.vue'),
        meta: { permission: 'assessment_template:read' },
      },
      {
        path: 'assessment/templates/:id/edit',
        name: 'assessment-template-edit',
        component: () => import('@/views/assessment/TemplateFormView.vue'),
        meta: { permission: 'assessment_template:update' },
      },
      {
        path: 'assessment/sessions',
        name: 'assessment-sessions',
        component: () => import('@/views/assessment/SessionListView.vue'),
        meta: { permission: 'assessment_session:read' },
      },
      {
        path: 'assessment/sessions/new',
        name: 'assessment-sessions-new',
        component: () => import('@/views/assessment/SessionFormView.vue'),
        meta: { permission: 'assessment_session:create' },
      },
      {
        path: 'assessment/sessions/:id',
        name: 'assessment-session-detail',
        component: () => import('@/views/assessment/SessionDetailView.vue'),
        meta: { permission: 'assessment_session:read' },
      },
      {
        path: 'assessment/sessions/:id/edit',
        name: 'assessment-session-edit',
        component: () => import('@/views/assessment/SessionFormView.vue'),
        meta: { permission: 'assessment_session:update' },
      },

      // ── Student Lifecycle ──────────────────────────────────────────────────────
      {
        path: 'students',
        name: 'students',
        component: () => import('@/views/students/StudentListView.vue'),
        meta: { permission: 'student:read' },
      },
      {
        path: 'students/new',
        name: 'student-new',
        component: () => import('@/views/students/StudentFormView.vue'),
        meta: { permission: 'student:create' },
      },
      {
        path: 'students/:id',
        name: 'student-detail',
        component: () => import('@/views/students/StudentDetailView.vue'),
        meta: { permission: 'student:read' },
      },
      {
        path: 'students/:id/edit',
        name: 'student-edit',
        component: () => import('@/views/students/StudentFormView.vue'),
        meta: { permission: 'student:update' },
      },
      {
        path: 'students/:id/dashboard',
        name: 'student-dashboard',
        component: () => import('@/views/students/StudentDashboardView.vue'),
        meta: { permission: 'student:read' },
      },
      {
        path: 'students/:id/timeline',
        name: 'student-timeline',
        component: () => import('@/views/students/StudentTimelineView.vue'),
        meta: { permission: 'student:read' },
      },

      // ── Guardian Management ────────────────────────────────────────────────────
      {
        path: 'guardians',
        name: 'guardians',
        component: () => import('@/views/guardians/GuardianListView.vue'),
        meta: { permission: 'guardian:read' },
      },
      {
        path: 'guardians/new',
        name: 'guardian-new',
        component: () => import('@/views/guardians/GuardianFormView.vue'),
        meta: { permission: 'guardian:create' },
      },
      {
        path: 'guardians/:id',
        name: 'guardian-detail',
        component: () => import('@/views/guardians/GuardianDetailView.vue'),
        meta: { permission: 'guardian:read' },
      },
      {
        path: 'guardians/:id/edit',
        name: 'guardian-edit',
        component: () => import('@/views/guardians/GuardianFormView.vue'),
        meta: { permission: 'guardian:update' },
      },

      // ── Educator Management ────────────────────────────────────────────────────
      {
        path: 'educators',
        name: 'educators',
        component: () => import('@/views/educators/EducatorListView.vue'),
        meta: { permission: 'educator:read' },
      },
      {
        path: 'educators/new',
        name: 'educator-new',
        component: () => import('@/views/educators/EducatorFormView.vue'),
        meta: { permission: 'educator:create' },
      },
      {
        path: 'educators/:id',
        name: 'educator-detail',
        component: () => import('@/views/educators/EducatorDetailView.vue'),
        meta: { permission: 'educator:read' },
      },
      {
        path: 'educators/:id/edit',
        name: 'educator-edit',
        component: () => import('@/views/educators/EducatorFormView.vue'),
        meta: { permission: 'educator:update' },
      },
      {
        path: 'educators/:id/availability',
        name: 'educator-availability',
        component: () => import('@/views/educators/EducatorDashboardView.vue'),
        meta: { permission: 'educator:read' },
      },

      // ── Program Management ─────────────────────────────────────────────────────
      {
        path: 'programs',
        name: 'programs',
        component: () => import('@/views/programs/ProgramListView.vue'),
        meta: { permission: 'program:read' },
      },
      {
        path: 'programs/new',
        name: 'program-new',
        component: () => import('@/views/programs/ProgramFormView.vue'),
        meta: { permission: 'program:create' },
      },
      {
        path: 'programs/:id',
        name: 'program-detail',
        component: () => import('@/views/programs/ProgramDetailView.vue'),
        meta: { permission: 'program:read' },
      },
      {
        path: 'programs/:id/edit',
        name: 'program-edit',
        component: () => import('@/views/programs/ProgramFormView.vue'),
        meta: { permission: 'program:update' },
      },

      // ── BEP / IEP (Education Plans) ────────────────────────────────────────────
      {
        path: 'bep',
        name: 'bep-list',
        component: () => import('@/views/bep/EducationPlanListView.vue'),
        meta: { permission: 'education_plan:read' },
      },
      {
        path: 'bep/new',
        name: 'bep-new',
        component: () => import('@/views/bep/EducationPlanFormView.vue'),
        meta: { permission: 'education_plan:create' },
      },
      {
        path: 'bep/:id',
        name: 'bep-detail',
        component: () => import('@/views/bep/EducationPlanDetailView.vue'),
        meta: { permission: 'education_plan:read' },
      },
      {
        path: 'bep/:id/edit',
        name: 'bep-edit',
        component: () => import('@/views/bep/EducationPlanFormView.vue'),
        meta: { permission: 'education_plan:update' },
      },

      // ── Scheduling & Sessions ──────────────────────────────────────────────────
      {
        path: 'scheduling',
        name: 'scheduling-dashboard',
        component: () => import('@/views/scheduling/SchedulingDashboardView.vue'),
        meta: { permission: 'session:read' },
      },
      {
        path: 'scheduling/calendar',
        name: 'scheduling-calendar',
        component: () => import('@/views/scheduling/CalendarView.vue'),
        meta: { permission: 'session:read' },
      },
      {
        path: 'scheduling/sessions',
        name: 'sessions',
        component: () => import('@/views/scheduling/SessionListView.vue'),
        meta: { permission: 'session:read' },
      },
      {
        path: 'scheduling/sessions/new',
        name: 'session-new',
        component: () => import('@/views/scheduling/SessionFormView.vue'),
        meta: { permission: 'session:create' },
      },
      {
        path: 'scheduling/sessions/:id',
        name: 'session-detail',
        component: () => import('@/views/scheduling/SessionDetailView.vue'),
        meta: { permission: 'session:read' },
      },
      {
        path: 'scheduling/sessions/:id/edit',
        name: 'session-edit',
        component: () => import('@/views/scheduling/SessionFormView.vue'),
        meta: { permission: 'session:update' },
      },
      {
        path: 'scheduling/rooms',
        name: 'rooms',
        component: () => import('@/views/scheduling/RoomListView.vue'),
        meta: { permission: 'room:read' },
      },
      {
        // Alias keeps V13 menu path (/scheduling/recurring) working if V31 not applied.
        path: 'scheduling/recurring-schedules',
        alias: ['scheduling/recurring'],
        name: 'recurring-schedules',
        component: () => import('@/views/scheduling/RecurringScheduleListView.vue'),
        // API gates recurring schedules with session:* (no recurring_schedule:* catalog).
        meta: { permission: 'session:read' },
      },
      {
        path: 'scheduling/recurring-schedules/new',
        name: 'recurring-schedule-new',
        component: () => import('@/views/scheduling/RecurringScheduleFormView.vue'),
        meta: { permission: 'session:create' },
      },
      {
        path: 'scheduling/attendance',
        name: 'attendance-dashboard',
        component: () => import('@/views/scheduling/AttendanceDashboardView.vue'),
        meta: { permission: 'attendance:read' },
      },
      {
        path: 'scheduling/makeup-requests',
        name: 'makeup-requests',
        component: () => import('@/views/scheduling/MakeupRequestListView.vue'),
        meta: { permission: 'makeup_request:read' },
      },
      {
        path: 'scheduling/makeup-requests/:id',
        name: 'makeup-request-detail',
        component: () => import('@/views/scheduling/MakeupRequestListView.vue'),
        meta: { permission: 'makeup_request:read' },
      },

      // ── Finance & Payments ─────────────────────────────────────────────────────
      {
        path: 'finance',
        name: 'finance-dashboard',
        component: () => import('@/views/finance/FinanceDashboardView.vue'),
        meta: { permission: 'payment:read' },
      },
      {
        path: 'finance/packages',
        name: 'packages',
        component: () => import('@/views/finance/PackageDefinitionListView.vue'),
        meta: { permission: 'package_definition:read' },
      },
      {
        path: 'finance/packages/new',
        name: 'package-new',
        component: () => import('@/views/finance/PackageDefinitionFormView.vue'),
        meta: { permission: 'package_definition:create' },
      },
      {
        path: 'finance/packages/:id',
        name: 'package-detail',
        component: () => import('@/views/finance/PackageDefinitionListView.vue'),
        meta: { permission: 'package_definition:read' },
      },
      {
        path: 'finance/packages/:id/edit',
        name: 'package-edit',
        component: () => import('@/views/finance/PackageDefinitionFormView.vue'),
        meta: { permission: 'package_definition:update' },
      },
      {
        path: 'finance/student-packages',
        name: 'student-packages',
        component: () => import('@/views/finance/StudentPackageListView.vue'),
        meta: { permission: 'student_package:read' },
      },
      {
        path: 'finance/student-packages/:id',
        name: 'student-package-detail',
        component: () => import('@/views/finance/StudentPackageListView.vue'),
        meta: { permission: 'student_package:read' },
      },
      {
        path: 'finance/credits',
        name: 'credit-ledger',
        component: () => import('@/views/finance/CreditLedgerView.vue'),
        meta: { permission: 'credit:read' },
      },
      {
        path: 'finance/invoices',
        name: 'invoices',
        component: () => import('@/views/finance/InvoiceListView.vue'),
        meta: { permission: 'invoice:read' },
      },
      {
        path: 'finance/invoices/new',
        name: 'invoice-new',
        component: () => import('@/views/finance/InvoiceDetailView.vue'),
        meta: { permission: 'invoice:create' },
      },
      {
        path: 'finance/invoices/:id',
        name: 'invoice-detail',
        component: () => import('@/views/finance/InvoiceDetailView.vue'),
        meta: { permission: 'invoice:read' },
      },
      {
        path: 'finance/payments',
        name: 'payments',
        component: () => import('@/views/finance/PaymentListView.vue'),
        meta: { permission: 'payment:read' },
      },
      {
        path: 'finance/payments/new',
        name: 'payment-new',
        component: () => import('@/views/finance/PaymentFormView.vue'),
        meta: { permission: 'payment:create' },
      },
      {
        path: 'finance/payments/:id',
        name: 'payment-detail',
        component: () => import('@/views/finance/PaymentDetailView.vue'),
        meta: { permission: 'payment:read' },
      },
      {
        path: 'finance/scholarships',
        name: 'scholarships',
        component: () => import('@/views/finance/ScholarshipListView.vue'),
        meta: { permission: 'scholarship:read' },
      },
      {
        path: 'finance/promotions',
        name: 'promotions',
        component: () => import('@/views/finance/PromotionListView.vue'),
        meta: { permission: 'promotion:read' },
      },

      // ── Notification Management ───────────────────────────────────────────────
      {
        path: 'notifications',
        name: 'notification-dashboard',
        component: () => import('@/views/notifications/NotificationDashboardView.vue'),
        meta: { permission: 'notification:read' },
      },
      {
        path: 'notifications/inbox',
        name: 'notification-list',
        component: () => import('@/views/notifications/NotificationListView.vue'),
        meta: { permission: 'notification:read' },
      },
      {
        path: 'notifications/inbox/:id',
        name: 'notification-detail',
        component: () => import('@/views/notifications/NotificationDetailView.vue'),
        meta: { permission: 'notification:read' },
      },
      {
        path: 'notifications/templates',
        name: 'notification-templates',
        component: () => import('@/views/notifications/NotificationTemplateListView.vue'),
        meta: { permission: 'notification_template:read' },
      },
      {
        path: 'notifications/templates/new',
        name: 'notification-template-new',
        component: () => import('@/views/notifications/NotificationTemplateFormView.vue'),
        meta: { permission: 'notification_template:create' },
      },
      {
        path: 'notifications/templates/:id/edit',
        name: 'notification-template-edit',
        component: () => import('@/views/notifications/NotificationTemplateFormView.vue'),
        meta: { permission: 'notification_template:update' },
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
        meta: { permission: 'meeting:read' },
      },
      {
        path: 'meetings/list',
        name: 'meetings',
        component: () => import('@/views/meetings/MeetingListView.vue'),
        meta: { permission: 'meeting:read' },
      },
      {
        path: 'meetings/new',
        name: 'meeting-new',
        component: () => import('@/views/meetings/MeetingFormView.vue'),
        meta: { permission: 'meeting:create' },
      },
      {
        path: 'meetings/calendar',
        name: 'meeting-calendar',
        component: () => import('@/views/meetings/MeetingCalendarView.vue'),
        meta: { permission: 'meeting:read' },
      },
      {
        path: 'meetings/follow-ups',
        name: 'follow-ups',
        component: () => import('@/views/meetings/FollowUpListView.vue'),
        meta: { permission: 'meeting:read' },
      },
      {
        path: 'meetings/:id',
        name: 'meeting-detail',
        component: () => import('@/views/meetings/MeetingDetailView.vue'),
        meta: { permission: 'meeting:read' },
      },
      {
        path: 'meetings/:id/edit',
        name: 'meeting-edit',
        component: () => import('@/views/meetings/MeetingFormView.vue'),
        meta: { permission: 'meeting:update' },
      },

      // ── Parent Portal ──────────────────────────────────────────────────────────
      {
        path: 'portal',
        name: 'portal-dashboard',
        component: () => import('@/views/portal/PortalDashboardView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children',
        name: 'portal-children',
        component: () => import('@/views/portal/MyChildrenView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children/:studentId',
        name: 'portal-child-detail',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children/:studentId/sessions',
        name: 'portal-sessions',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children/:studentId/goals',
        name: 'portal-goals',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children/:studentId/packages',
        name: 'portal-packages',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/children/:studentId/documents',
        name: 'portal-documents',
        component: () => import('@/views/portal/ChildDetailView.vue'),
        meta: { permission: 'portal:access' },
      },
      {
        path: 'portal/notifications',
        name: 'portal-notifications',
        component: () => import('@/views/portal/PortalNotificationsView.vue'),
        meta: { permission: 'portal:access' },
      },

      // ── Leave Management ───────────────────────────────────────────────────────
      {
        path: 'leave',
        name: 'leave-dashboard',
        component: () => import('@/views/leave/LeaveDashboardView.vue'),
        meta: { permission: 'leave_request:read' },
      },
      {
        path: 'leave/requests',
        name: 'leave-list',
        component: () => import('@/views/leave/LeaveListView.vue'),
        meta: { permission: 'leave_request:read' },
      },
      {
        path: 'leave/requests/new',
        name: 'leave-new',
        component: () => import('@/views/leave/LeaveFormView.vue'),
        meta: { permission: 'leave_request:submit' },
      },
      {
        path: 'leave/requests/:id',
        name: 'leave-detail',
        component: () => import('@/views/leave/LeaveDetailView.vue'),
        meta: { permission: 'leave_request:read' },
      },
      {
        path: 'leave/requests/:id/edit',
        name: 'leave-edit',
        component: () => import('@/views/leave/LeaveFormView.vue'),
        meta: { permission: 'leave_request:update' },
      },
      {
        path: 'leave/calendar',
        name: 'leave-calendar',
        component: () => import('@/views/leave/LeaveCalendarView.vue'),
        meta: { permission: 'leave_request:read' },
      },
      {
        path: 'leave/balances',
        name: 'leave-balances',
        component: () => import('@/views/leave/LeaveBalanceView.vue'),
        meta: { permission: 'leave_request:read' },
      },
      {
        path: 'leave/reports',
        name: 'leave-reports',
        component: () => import('@/views/leave/LeaveReportsView.vue'),
        meta: { permission: 'leave_request:read' },
      },

      // ── Camera Management ──────────────────────────────────────────────────────
      {
        path: 'cameras',
        name: 'camera-dashboard',
        component: () => import('@/views/cameras/CameraDashboardView.vue'),
        meta: { permission: 'camera:read' },
      },
      {
        path: 'cameras/list',
        name: 'cameras',
        component: () => import('@/views/cameras/CameraListView.vue'),
        meta: { permission: 'camera:read' },
      },
      {
        path: 'cameras/new',
        name: 'camera-new',
        component: () => import('@/views/cameras/CameraFormView.vue'),
        meta: { permission: 'camera:create' },
      },
      {
        path: 'cameras/authorizations',
        name: 'camera-authorizations',
        component: () => import('@/views/cameras/ViewingAuthorizationsView.vue'),
        meta: { permission: 'viewing_authorization:read' },
      },
      {
        path: 'cameras/viewing-history',
        name: 'camera-viewing-history',
        component: () => import('@/views/cameras/ViewingHistoryView.vue'),
        meta: { permission: 'viewing_log:read' },
      },
      {
        path: 'cameras/:id',
        name: 'camera-detail',
        component: () => import('@/views/cameras/CameraDetailView.vue'),
        meta: { permission: 'camera:read' },
      },
      {
        path: 'cameras/:id/edit',
        name: 'camera-edit',
        component: () => import('@/views/cameras/CameraFormView.vue'),
        meta: { permission: 'camera:update' },
      },

      // ── Camp Management ────────────────────────────────────────────────────────
      {
        path: 'camps',
        name: 'camp-dashboard',
        component: () => import('@/views/camps/CampDashboardView.vue'),
        meta: { permission: 'camp:read' },
      },
      {
        path: 'camps/list',
        name: 'camps',
        component: () => import('@/views/camps/CampListView.vue'),
        meta: { permission: 'camp:read' },
      },
      {
        path: 'camps/new',
        name: 'camp-new',
        component: () => import('@/views/camps/CampFormView.vue'),
        meta: { permission: 'camp:create' },
      },
      {
        path: 'camps/:id',
        name: 'camp-detail',
        component: () => import('@/views/camps/CampDetailView.vue'),
        meta: { permission: 'camp:read' },
      },
      {
        path: 'camps/:id/edit',
        name: 'camp-edit',
        component: () => import('@/views/camps/CampFormView.vue'),
        meta: { permission: 'camp:update' },
      },
      {
        path: 'camps/periods/:periodId/enrollments',
        name: 'camp-enrollment',
        component: () => import('@/views/camps/CampEnrollmentView.vue'),
        meta: { permission: 'camp_enrollment:read' },
      },

      // ── School Consultancy Management ──────────────────────────────────────────
      {
        path: 'consultancy',
        name: 'consultancy-dashboard',
        component: () => import('@/views/consultancy/ConsultancyDashboardView.vue'),
        meta: { permission: 'institution:read' },
      },
      {
        path: 'consultancy/institutions',
        name: 'institutions',
        component: () => import('@/views/consultancy/InstitutionListView.vue'),
        meta: { permission: 'institution:read' },
      },
      {
        path: 'consultancy/institutions/new',
        name: 'institution-new',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: 'institution:create' },
      },
      {
        path: 'consultancy/institutions/:id',
        name: 'institution-detail',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: 'institution:read' },
      },
      {
        path: 'consultancy/institutions/:id/edit',
        name: 'institution-edit',
        component: () => import('@/views/consultancy/InstitutionFormView.vue'),
        meta: { permission: 'institution:update' },
      },
      {
        path: 'consultancy/plans',
        name: 'consultancy-plans',
        component: () => import('@/views/consultancy/PlanListView.vue'),
        meta: { permission: 'consultancy_plan:read' },
      },
      {
        path: 'consultancy/plans/:id',
        name: 'consultancy-plan-detail',
        component: () => import('@/views/consultancy/PlanDetailView.vue'),
        meta: { permission: 'consultancy_plan:read' },
      },
      {
        path: 'consultancy/visits',
        name: 'consultancy-visits',
        component: () => import('@/views/consultancy/VisitListView.vue'),
        meta: { permission: 'school_visit:read' },
      },
      {
        path: 'consultancy/visits/:id',
        name: 'consultancy-visit-detail',
        component: () => import('@/views/consultancy/VisitListView.vue'),
        meta: { permission: 'school_visit:read' },
      },
      {
        path: 'consultancy/observations',
        name: 'consultancy-observations',
        component: () => import('@/views/consultancy/ObservationListView.vue'),
        meta: { permission: 'observation:read' },
      },
      {
        path: 'consultancy/reports',
        name: 'consultancy-reports',
        component: () => import('@/views/consultancy/ReportListView.vue'),
        meta: { permission: 'consultancy_report:read' },
      },
      {
        path: 'consultancy/agreements',
        name: 'consultancy-agreements',
        component: () => import('@/views/consultancy/AgreementListView.vue'),
        meta: { permission: 'consultancy_agreement:read' },
      },
      {
        path: 'consultancy/agreements/new',
        name: 'agreement-new',
        component: () => import('@/views/consultancy/AgreementFormView.vue'),
        meta: { permission: 'consultancy_agreement:create' },
      },
      {
        path: 'consultancy/agreements/:id',
        name: 'agreement-detail',
        component: () => import('@/views/consultancy/AgreementDetailView.vue'),
        meta: { permission: 'consultancy_agreement:read' },
      },
      {
        path: 'consultancy/agreements/:id/edit',
        name: 'agreement-edit',
        component: () => import('@/views/consultancy/AgreementFormView.vue'),
        meta: { permission: 'consultancy_agreement:update' },
      },
      {
        path: 'consultancy/follow-ups',
        name: 'consultancy-follow-ups',
        component: () => import('@/views/consultancy/FollowUpListView.vue'),
        meta: { permission: 'follow_up:read' },
      },
      {
        path: 'consultancy/follow-ups/open',
        name: 'follow-ups-open-report',
        component: () => import('@/views/consultancy/OpenFollowUpReportView.vue'),
        meta: { permission: 'follow_up:read' },
      },
      {
        path: 'consultancy/follow-ups/new',
        name: 'follow-up-new',
        component: () => import('@/views/consultancy/FollowUpFormView.vue'),
        meta: { permission: 'follow_up:create' },
      },
      {
        path: 'consultancy/follow-ups/:id',
        name: 'follow-up-detail',
        component: () => import('@/views/consultancy/FollowUpDetailView.vue'),
        meta: { permission: 'follow_up:read' },
      },
      {
        path: 'consultancy/follow-ups/:id/edit',
        name: 'follow-up-edit',
        component: () => import('@/views/consultancy/FollowUpFormView.vue'),
        meta: { permission: 'follow_up:update' },
      },

      // ── KPI & Performance Management ───────────────────────────────────────────
      {
        path: 'kpi',
        name: 'kpi-dashboard',
        component: () => import('@/views/kpi/KpiDashboardView.vue'),
        meta: { permission: 'kpi_dashboard:read' },
      },
      {
        path: 'kpi/definitions',
        name: 'kpi-definitions',
        component: () => import('@/views/kpi/KpiDefinitionListView.vue'),
        meta: { permission: 'kpi:read' },
      },
      {
        path: 'kpi/definitions/:id',
        name: 'kpi-definition-detail',
        component: () => import('@/views/kpi/KpiDefinitionListView.vue'),
        meta: { permission: 'kpi:read' },
      },
      {
        path: 'kpi/snapshots',
        name: 'kpi-snapshots',
        component: () => import('@/views/kpi/PerformanceSnapshotListView.vue'),
        meta: { permission: 'kpi_snapshot:read' },
      },

      // ── Legal (Contract & Consent) Management ──────────────────────────────────
      {
        path: 'legal',
        name: 'legal-dashboard',
        component: () => import('@/views/legal/LegalDashboardView.vue'),
        meta: { permission: 'student_contract:read' },
      },
      {
        path: 'legal/contracts',
        name: 'contracts',
        component: () => import('@/views/legal/ContractListView.vue'),
        meta: { permission: 'student_contract:read' },
      },
      {
        path: 'legal/contracts/new',
        name: 'contract-new',
        component: () => import('@/views/legal/ContractListView.vue'),
        meta: { permission: 'student_contract:generate' },
      },
      {
        path: 'legal/contracts/:id',
        name: 'contract-detail',
        component: () => import('@/views/legal/ContractDetailView.vue'),
        meta: { permission: 'student_contract:read' },
      },
      {
        path: 'legal/contract-templates',
        name: 'contract-templates',
        component: () => import('@/views/legal/ContractTemplateListView.vue'),
        meta: { permission: 'contract_template:read' },
      },
      {
        path: 'legal/consents',
        name: 'consents',
        component: () => import('@/views/legal/ConsentListView.vue'),
        meta: { permission: 'student_consent:read' },
      },
      {
        path: 'legal/consents/:id',
        name: 'consent-detail',
        component: () => import('@/views/legal/ConsentListView.vue'),
        meta: { permission: 'student_consent:read' },
      },
      {
        path: 'legal/reports',
        name: 'legal-reports',
        component: () => import('@/views/legal/LegalReportsView.vue'),
        meta: { permission: 'legal_report:read' },
      },

      // ── Goal Management ────────────────────────────────────────────────────────
      {
        path: 'goals',
        name: 'goal-dashboard',
        component: () => import('@/views/goals/GoalDashboardView.vue'),
        meta: { permission: 'student_goal:read' },
      },
      {
        path: 'goals/libraries',
        name: 'goal-libraries',
        component: () => import('@/views/goals/GoalLibraryListView.vue'),
        meta: { permission: 'goal_library:read' },
      },
      {
        path: 'goals/templates',
        name: 'goal-template-list',
        component: () => import('@/views/goals/GoalTemplateListView.vue'),
        meta: { permission: 'goal_template:read' },
      },
      {
        path: 'goals/templates/new',
        name: 'goal-template-new',
        component: () => import('@/views/goals/GoalTemplateFormView.vue'),
        meta: { permission: 'goal_template:create' },
      },
      {
        path: 'goals/templates/:id',
        name: 'goal-template-detail',
        component: () => import('@/views/goals/GoalTemplateDetailView.vue'),
        meta: { permission: 'goal_template:read' },
      },
      {
        path: 'goals/templates/:id/edit',
        name: 'goal-template-edit',
        component: () => import('@/views/goals/GoalTemplateFormView.vue'),
        meta: { permission: 'goal_template:update' },
      },
      {
        path: 'goals/student-goals',
        name: 'student-goal-list',
        component: () => import('@/views/goals/StudentGoalListView.vue'),
        meta: { permission: 'student_goal:read' },
      },
      {
        path: 'goals/student-goals/new',
        name: 'student-goal-new',
        component: () => import('@/views/goals/StudentGoalFormView.vue'),
        meta: { permission: 'student_goal:create' },
      },
      {
        path: 'goals/student-goals/:id',
        name: 'student-goal-detail',
        component: () => import('@/views/goals/StudentGoalDetailView.vue'),
        meta: { permission: 'student_goal:read' },
      },
      {
        path: 'goals/student-goals/:id/edit',
        name: 'student-goal-edit',
        component: () => import('@/views/goals/StudentGoalFormView.vue'),
        meta: { permission: 'student_goal:update' },
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

  if (to.meta.requiresGuest && auth.isAuthenticated) {
    return { path: '/' }
  }

  if (to.meta.permission && !auth.hasPermission(to.meta.permission as string)) {
    return { name: 'forbidden' }
  }
})

export default router
