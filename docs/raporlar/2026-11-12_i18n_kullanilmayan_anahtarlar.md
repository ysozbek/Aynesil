# Kullanılmayan i18n anahtarları (silindi)

Tarih: 2026-11-12 · Kaynak: `frontend/aynesil-web/src/i18n/locales/tr.ts` · Toplam anahtar: 2142 · Silinen: 368

Yöntem: kodda (`src/`, i18n hariç) anahtarın tam yazımı, `t(`...${x}`)` kalıpları ve dinamik önekler (`prefix.${...}`, `prefix.' +`) aranır; hiçbiriyle eşleşmeyen anahtar silinir. Menü etiketleri DB'den geldiği için `navigation.*` menü anahtarları kullanılmıyordu. Durum etiketleri `statusBadge.*` altında tek yerde toplandı; eski kopyalar (`legal.contract.status.*`, `scheduling.session.status.*`, `bep.status.*` vb.) bu yüzden silindi.

## common (3)

- `common.confirm`
- `common.submit`
- `common.close`

## pickers (1)

- `pickers.user`

## auth (2)

- `auth.forgotPassword`
- `auth.lockedOut`

## navigation (48)

- `navigation.profile`
- `navigation.corporations`
- `navigation.campuses`
- `navigation.users`
- `navigation.roles`
- `navigation.permissions`
- `navigation.menus`
- `navigation.crm`
- `navigation.leads`
- `navigation.pipeline`
- `navigation.activities`
- `navigation.assessment`
- `navigation.assessmentTemplates`
- `navigation.assessmentSessions`
- `navigation.students`
- `navigation.guardians`
- `navigation.educators`
- `navigation.programs`
- `navigation.bep`
- `navigation.goals`
- `navigation.goalLibraries`
- `navigation.goalTemplates`
- `navigation.studentGoals`
- `navigation.scheduling`
- `navigation.sessions`
- `navigation.calendar`
- `navigation.rooms`
- `navigation.recurringSchedules`
- `navigation.attendance`
- `navigation.makeupRequests`
- `navigation.finance`
- `navigation.packages`
- `navigation.studentPackages`
- `navigation.credits`
- `navigation.invoices`
- `navigation.payments`
- `navigation.scholarships`
- `navigation.promotions`
- `navigation.referenceData`
- `navigation.notifications`
- `navigation.notificationInbox`
- `navigation.notificationTemplates`
- `navigation.notificationPreferences`
- `navigation.meetings`
- `navigation.meetingCalendar`
- `navigation.followUps`
- `navigation.portal`
- `navigation.myChildren`

## validation (3)

- `validation.minLength`
- `validation.maxLength`
- `validation.email`

## errors (1)

- `errors.networkError`

## pagination (1)

- `pagination.perPage`

## corporation (3)

- `corporation.all`
- `corporation.deleteTitle`
- `corporation.deleteMessage`

## crm (10)

- `crm.nav.interviews`
- `crm.lead.corporation`
- `crm.lead.studentId`
- `crm.lead.studentIdPlaceholder`
- `crm.lead.studentIdHint`
- `crm.lead.convertCreateStudent`
- `crm.interview.status.scheduled`
- `crm.interview.status.completed`
- `crm.interview.status.cancelled`
- `crm.interview.status.no_show`

## assessment (9)

- `assessment.session.startedAt`
- `assessment.session.leadId`
- `assessment.session.leadIdPlaceholder`
- `assessment.session.studentId`
- `assessment.session.studentIdPlaceholder`
- `assessment.session.status.planned`
- `assessment.session.status.in_progress`
- `assessment.session.status.completed`
- `assessment.session.status.cancelled`

## student (45)

- `student.dashboard`
- `student.guardian.unlinkTitle`
- `student.guardian.unlinkMessage`
- `student.guardian.guardianId`
- `student.guardian.noGuardians`
- `student.caseNote.edit`
- `student.caseNote.authoredBy`
- `student.caseNote.noNotes`
- `student.caseNote.confidential`
- `student.medicalReport.edit`
- `student.medicalReport.deleteTitle`
- `student.medicalReport.deleteMessage`
- `student.medicalReport.noReports`
- `student.developmentReport.edit`
- `student.developmentReport.authoredBy`
- `student.developmentReport.deleteTitle`
- `student.developmentReport.deleteMessage`
- `student.developmentReport.noReports`
- `student.externalReport.reportDate`
- `student.externalReport.summary`
- `student.externalReport.deleteTitle`
- `student.externalReport.deleteMessage`
- `student.externalReport.noReports`
- `student.diagnosis.edit`
- `student.diagnosis.deleteTitle`
- `student.diagnosis.deleteMessage`
- `student.diagnosis.noDiagnoses`
- `student.campus.endTitle`
- `student.campus.endMessage`
- `student.campus.noCampuses`
- `student.emergencyContact.fullName`
- `student.emergencyContact.relationship`
- `student.emergencyContact.phone`
- `student.emergencyContact.priority`
- `student.emergencyContact.add`
- `student.emergencyContact.noContacts`
- `student.developmentalProfile.edit`
- `student.developmentalProfile.noProfiles`
- `student.activePrograms`
- `student.assignedEducators`
- `student.currentGoals`
- `student.upcomingSessions`
- `student.recentAssessments`
- `student.noActivePrograms`
- `student.noGoals`

## guardian (4)

- `guardian.searchPlaceholder`
- `guardian.deleteTitle`
- `guardian.deleteMessage`
- `guardian.noStudents`

## educator (33)

- `educator.searchPlaceholder`
- `educator.allTitles`
- `educator.allTypes`
- `educator.deleteTitle`
- `educator.deleteMessage`
- `educator.activate`
- `educator.deactivate`
- `educator.activateTitle`
- `educator.deactivateTitle`
- `educator.availability`
- `educator.utilization`
- `educator.activeStudentPrograms`
- `educator.specialty.removeTitle`
- `educator.specialty.removeMessage`
- `educator.specialty.specialty`
- `educator.specialty.noSpecialties`
- `educator.certification.type`
- `educator.certification.deleteTitle`
- `educator.certification.deleteMessage`
- `educator.certification.noCertifications`
- `educator.campus.endTitle`
- `educator.campus.noCampuses`
- `educator.campus.primary`
- `educator.campus.activeFrom`
- `educator.campus.activeTo`
- `educator.hierarchy.unlink`
- `educator.hierarchy.unlinkTitle`
- `educator.hierarchy.supervisors`
- `educator.hierarchy.relationship`
- `educator.hierarchy.activeFrom`
- `educator.hierarchy.activeTo`
- `educator.hierarchy.noSupervisors`
- `educator.hierarchy.noSubordinates`

## program (11)

- `program.searchPlaceholder`
- `program.allTypes`
- `program.deleteTitle`
- `program.deleteMessage`
- `program.service.deleteTitle`
- `program.service.deleteMessage`
- `program.service.noServices`
- `program.translation.edit`
- `program.translation.name`
- `program.translation.description`
- `program.translation.noTranslations`

## enrollment (5)

- `enrollment.programChangeStatus`
- `enrollment.spStatus.active`
- `enrollment.spStatus.paused`
- `enrollment.spStatus.completed`
- `enrollment.spStatus.cancelled`

## bep (37)

- `bep.approvedAt`
- `bep.deleteTitle`
- `bep.deleteMessage`
- `bep.submitConfirm`
- `bep.closeConfirm`
- `bep.reviseDescription`
- `bep.changeSummary`
- `bep.approverComment`
- `bep.approverId`
- `bep.approver`
- `bep.setGuardianVisible`
- `bep.setGuardianHidden`
- `bep.removeGoalTitle`
- `bep.removeGoalMessage`
- `bep.goalHorizon`
- `bep.goalSortOrder`
- `bep.noLongTermGoals`
- `bep.noShortTermGoals`
- `bep.review.reviewer`
- `bep.review.noReviews`
- `bep.approval.approved`
- `bep.approval.rejected`
- `bep.approval.decision`
- `bep.approval.comment`
- `bep.approval.decidedAt`
- `bep.approval.noApprovals`
- `bep.revision.from`
- `bep.revision.to`
- `bep.revision.revisedAt`
- `bep.revision.revisedBy`
- `bep.revision.noRevisions`
- `bep.status.draft`
- `bep.status.in_review`
- `bep.status.approved`
- `bep.status.active`
- `bep.status.revised`
- `bep.status.closed`

## scheduling (28)

- `scheduling.nav.recurring`
- `scheduling.nav.attendance`
- `scheduling.session.backToList`
- `scheduling.session.addParticipantPlaceholder`
- `scheduling.session.addEducatorPlaceholder`
- `scheduling.session.notes`
- `scheduling.session.roomIdPlaceholder`
- `scheduling.session.status.scheduled`
- `scheduling.session.status.rescheduled`
- `scheduling.session.status.completed`
- `scheduling.session.status.cancelled`
- `scheduling.session.status.no_show`
- `scheduling.session.cancelMessage`
- `scheduling.attendance.enterStudentId`
- `scheduling.attendance.studentIdPlaceholder`
- `scheduling.attendance.status.present`
- `scheduling.attendance.status.absent`
- `scheduling.attendance.status.late`
- `scheduling.attendance.status.excused`
- `scheduling.attendance.status.left_early`
- `scheduling.makeup.missedDate`
- `scheduling.makeup.makeupDate`
- `scheduling.makeup.status.requested`
- `scheduling.makeup.status.approved`
- `scheduling.makeup.status.rejected`
- `scheduling.makeup.status.scheduled`
- `scheduling.makeup.status.completed`
- `scheduling.makeup.status.expired`

## finance (32)

- `finance.nav.studentPackages`
- `finance.nav.refunds`
- `finance.nav.scholarships`
- `finance.nav.promotions`
- `finance.dashboard.packagesSold`
- `finance.dashboard.packageRevenue`
- `finance.dashboard.creditsRemaining`
- `finance.package.descriptionField`
- `finance.studentPackage.status.active`
- `finance.studentPackage.status.expired`
- `finance.studentPackage.status.cancelled`
- `finance.studentPackage.status.exhausted`
- `finance.credit.enterStudentId`
- `finance.credit.studentIdPlaceholder`
- `finance.credit.type.grant`
- `finance.credit.type.consume`
- `finance.credit.type.refund`
- `finance.credit.type.adjustment`
- `finance.credit.type.expire`
- `finance.invoice.discount`
- `finance.invoice.status.draft`
- `finance.invoice.status.issued`
- `finance.invoice.status.paid`
- `finance.invoice.status.partial`
- `finance.invoice.status.overdue`
- `finance.invoice.status.void`
- `finance.payment.notes`
- `finance.payment.status.pending`
- `finance.payment.status.captured`
- `finance.payment.status.failed`
- `finance.payment.status.refunded`
- `finance.promotion.redemptions`

## goal (45)

- `goal.title`
- `goal.library.description`
- `goal.library.edit`
- `goal.library.backToList`
- `goal.library.deleteTitle`
- `goal.library.deleteMessage`
- `goal.library.noLibraries`
- `goal.template.description`
- `goal.template.edit`
- `goal.template.backToList`
- `goal.template.allLibraries`
- `goal.template.allCategories`
- `goal.template.allAreas`
- `goal.template.deleteTitle`
- `goal.template.deleteMessage`
- `goal.template.noTemplates`
- `goal.template.tab.overview`
- `goal.template.tab.translations`
- `goal.template.translation.add`
- `goal.template.translation.locale`
- `goal.template.translation.statement`
- `goal.template.translation.criteria`
- `goal.template.translation.noTranslations`
- `goal.studentGoal.description`
- `goal.studentGoal.edit`
- `goal.studentGoal.backToList`
- `goal.studentGoal.statusOptions.active`
- `goal.studentGoal.statusOptions.achieved`
- `goal.studentGoal.statusOptions.discontinued`
- `goal.studentGoal.statusOptions.on_hold`
- `goal.studentGoal.templateId`
- `goal.studentGoal.parentGoalId`
- `goal.studentGoal.studentId`
- `goal.studentGoal.deleteTitle`
- `goal.studentGoal.deleteMessage`
- `goal.studentGoal.noGoals`
- `goal.studentGoal.allHorizons`
- `goal.studentGoal.allStatuses`
- `goal.studentGoal.tab.overview`
- `goal.studentGoal.tab.progress`
- `goal.studentGoal.tab.trend`
- `goal.progress.noProgress`
- `goal.dashboard.enterStudentId`
- `goal.dashboard.load`
- `goal.dashboard.noData`

## notification (2)

- `notification.nav.inbox`
- `notification.nav.triggers`

## meeting (4)

- `meeting.status.scheduled`
- `meeting.status.completed`
- `meeting.status.cancelled`
- `meeting.nav.dashboard`

## portal (2)

- `portal.nav.dashboard`
- `portal.nav.children`

## leave (7)

- `leave.detail.sessionTitle`
- `leave.detail.sessionStart`
- `leave.detail.sessionEnd`
- `leave.status.pending`
- `leave.status.approved`
- `leave.status.rejected`
- `leave.status.cancelled`

## camera (6)

- `camera.detail.roomIdPlaceholder`
- `camera.auth.guardianId`
- `camera.auth.studentId`
- `camera.auth.active`
- `camera.auth.revoked`
- `camera.auth.expired`

## camp (4)

- `camp.enrollment.enrolled`
- `camp.enrollment.waitlist`
- `camp.enrollment.completed`
- `camp.enrollment.withdrawn`

## consultancy (7)

- `consultancy.visit.status.planned`
- `consultancy.visit.status.completed`
- `consultancy.visit.status.cancelled`
- `consultancy.plan.status.draft`
- `consultancy.plan.status.active`
- `consultancy.plan.status.completed`
- `consultancy.plan.status.cancelled`

## consultancyContract (5)

- `consultancyContract.draft`
- `consultancyContract.sent`
- `consultancyContract.signed`
- `consultancyContract.expired`
- `consultancyContract.cancelled`

## legal (10)

- `legal.contract.status.draft`
- `legal.contract.status.sent`
- `legal.contract.status.signed`
- `legal.contract.status.active`
- `legal.contract.status.expired`
- `legal.contract.status.terminated`
- `legal.contract.actions.activate`
- `legal.consent.state.granted`
- `legal.consent.state.withdrawn`
- `legal.consent.state.expired`

