<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { StatusEntities } from '@/constants/statuses'
import type { StatusEntity, StatusOf } from '@/constants/statuses'

type Tone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

const props = defineProps<{
  /** Varlık tipi (statuses.ts). `status` ile birlikte kullanılır. */
  entity?: StatusEntity
  /** DB'deki küçük harfli durum kodu. */
  status?: string | null
  /** Boolean aktif/pasif rozeti için (entity/status kullanılmaz). */
  value?: boolean
  trueLabel?: string
  falseLabel?: string
}>()

const { t } = useI18n()

const toneClasses: Record<Tone, string> = {
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  warning: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  danger: 'bg-red-50 text-red-700 ring-red-600/20',
  info: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  neutral: 'bg-gray-100 text-gray-600 ring-gray-500/20',
}

// Her varlık için tüm durumlar tanımlı olmak zorunda (Record → eksik durum derleme hatası verir).
const tones: { [E in StatusEntity]: Record<StatusOf<E>, Tone> } = {
  user: { invited: 'info', active: 'success', suspended: 'warning', disabled: 'neutral' },
  corporation: { active: 'success', suspended: 'warning', closed: 'danger' },
  contract: {
    draft: 'neutral', sent: 'info', signed: 'info', active: 'success', expired: 'neutral', terminated: 'danger',
  },
  consent: { granted: 'success', withdrawn: 'danger', expired: 'neutral' },
  campEnrollment: { enrolled: 'success', waitlist: 'warning', withdrawn: 'danger', completed: 'info' },
  meeting: { scheduled: 'info', completed: 'success', cancelled: 'danger' },
  meetingFollowUp: { open: 'warning', in_progress: 'info', done: 'success', cancelled: 'neutral' },
  consultancyFollowUp: { pending: 'warning', in_progress: 'info', completed: 'success', cancelled: 'neutral' },
  consultancyPlan: { draft: 'neutral', active: 'success', completed: 'info', cancelled: 'danger' },
  schoolVisit: { planned: 'info', completed: 'success', cancelled: 'danger' },
  session: {
    scheduled: 'info', rescheduled: 'warning', completed: 'success', cancelled: 'danger', no_show: 'neutral',
  },
  leave: { pending: 'warning', approved: 'success', rejected: 'danger', cancelled: 'neutral' },
  creditEntry: { grant: 'success', consume: 'danger', refund: 'info', adjustment: 'warning', expire: 'neutral' },
  studentProgram: { active: 'success', paused: 'warning', completed: 'info', cancelled: 'neutral' },
  studentGoal: { active: 'success', achieved: 'info', discontinued: 'danger', on_hold: 'warning' },
  educationPlan: { draft: 'neutral', in_review: 'warning', approved: 'success', active: 'info', revised: 'warning', closed: 'neutral' },
  assessmentSession: { planned: 'info', in_progress: 'warning', completed: 'success', cancelled: 'danger' },
  attendance: { present: 'success', absent: 'danger', late: 'warning', excused: 'info', left_early: 'warning' },
  makeupRequest: { requested: 'warning', approved: 'info', scheduled: 'info', completed: 'success', rejected: 'danger', expired: 'neutral' },
  consultancyAgreement: { draft: 'neutral', sent: 'info', signed: 'success', expired: 'neutral', cancelled: 'danger' },
  studentPackage: { active: 'success', exhausted: 'warning', expired: 'neutral', cancelled: 'danger' },
  invoice: { draft: 'neutral', issued: 'info', paid: 'success', partial: 'warning', overdue: 'danger', void: 'neutral' },
  payment: { pending: 'warning', authorized: 'info', captured: 'success', failed: 'danger', refunded: 'neutral' },
  refund: { pending: 'warning', processed: 'success', failed: 'danger' },
  leadInterview: { scheduled: 'info', completed: 'success', no_show: 'warning', cancelled: 'danger' },
  viewingAuthorization: { active: 'success', expired: 'warning', revoked: 'danger' },
  meetingAttendance: { invited: 'neutral', attended: 'success', absent: 'danger', tentative: 'warning' },
  educationPlanDecision: { approved: 'success', rejected: 'danger', changes_requested: 'warning' },
}

const warned = new Set<string>()

const resolved = computed<{ label: string; classes: string }>(() => {
  if (typeof props.value === 'boolean') {
    return props.value
      ? { label: props.trueLabel ?? t('common.active'), classes: toneClasses.success }
      : { label: props.falseLabel ?? t('common.passive'), classes: toneClasses.neutral }
  }
  const { entity, status } = props
  const known =
    entity && status
      ? (Object.values(StatusEntities[entity]) as string[]).includes(status)
      : false
  if (entity && status && known) {
    const tone = (tones[entity] as Record<string, Tone>)[status]
    return { label: t(`statusBadge.${entity}.${status}`), classes: toneClasses[tone] }
  }
  const key = `${entity ?? '?'}:${status ?? ''}`
  if (!warned.has(key)) {
    warned.add(key)
    console.warn(`[StatusBadge] Bilinmeyen durum: entity="${entity}", status="${status}"`)
  }
  return { label: t('statusBadge.unknown'), classes: toneClasses.neutral }
})
</script>

<template>
  <span
    :class="[
      'inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset',
      resolved.classes,
    ]"
  >
    {{ resolved.label }}
  </span>
</template>
