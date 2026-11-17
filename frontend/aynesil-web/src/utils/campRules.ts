/**
 * Kamp yönetim ekranlarının durum kuralları (sunucu kurallarının arayüz yansıması).
 * Asıl denetim sunucudadır (409); burada yalnızca düğmeleri doğru göstermek için kullanılır.
 */
import type { CampPeriodListItemDto } from '@/types/camp.types'

export type PeriodPhase = 'upcoming' | 'ongoing' | 'ended'

export const CampEducatorRoles = ['lead', 'assistant', 'observer', 'supervisor'] as const
export type CampEducatorRole = (typeof CampEducatorRoles)[number]

/** `YYYY-MM-DD` karşılaştırması yerel güne göre yapılır. */
export function periodPhase(p: Pick<CampPeriodListItemDto, 'startDate' | 'endDate'>, today: string): PeriodPhase {
  if (p.endDate < today) return 'ended'
  if (p.startDate > today) return 'upcoming'
  return 'ongoing'
}

/** Toplu tamamlama yalnızca başlamış dönemde ve kayıtlı öğrenci varken anlamlıdır. */
export function canBulkComplete(p: Pick<CampPeriodListItemDto, 'startDate' | 'enrolledCount'>, today: string): boolean {
  return p.startDate <= today && p.enrolledCount > 0
}

/** Tekil kayıt tamamlama yalnızca başlamış dönemde (sunucu başlamamış dönemde 409 CampEnrollmentCompleteNotStarted). */
export function canCompleteEnrollment(p: Pick<CampPeriodListItemDto, 'startDate'> | null | undefined, today: string): boolean {
  return !!p && p.startDate <= today
}

/**
 * Kayıt sonucu: çekilmiş öğrenci dolu döneme yeniden kaydedilince sunucu kaydı bekleme listesine düşürür.
 * İstenen 'enrolled' ama dönen 'waitlist' ise kullanıcıya bildirilir.
 */
export function fellToWaitlist(requested: string, result: { status: string } | null | undefined): boolean {
  return requested === 'enrolled' && result?.status === 'waitlist'
}

/** Kayıtlı/bekleyen öğrencisi olan dönem silinemez (sunucu da 409 döner). */
export function periodHasStudents(p: Pick<CampPeriodListItemDto, 'enrolledCount' | 'waitlistCount'>): boolean {
  return p.enrolledCount > 0 || p.waitlistCount > 0
}

/** Pasifleştirme: bitmemiş dönemlerinde kayıtlı/bekleyen öğrenci olan kamp pasifleştirilemez. */
export function blocksDeactivation(periods: CampPeriodListItemDto[], today: string): boolean {
  return periods.some((p) => p.endDate >= today && periodHasStudents(p))
}

export const CampAttendanceStatuses = ['present', 'absent', 'late', 'excused'] as const
export const CampParticipationStatuses = ['registered', 'attended', 'absent', 'excused'] as const

/** Yoklama ve katılım yalnızca dönemin `enrolled` öğrencileri için girilir (tamamlanmış/iptal/bekleyen 409). */
export function canRecordForEnrollment(status: string): boolean {
  return status === 'enrolled'
}

/** Kamp sonu raporu yalnızca tamamlanmış kayıt için yazılır (aksi halde 409). */
export function canWriteReport(status: string): boolean {
  return status === 'completed'
}

export interface DraftRow { enrollmentId: string; status: string; reasonId?: string }
export interface ExistingRecord { id: string; status: string; reasonId?: string }

/** Devamsızlık nedeni yalnızca `absent`/`excused` yoklamasında anlamlıdır. */
export function attendanceNeedsReason(status: string): boolean {
  return status === 'absent' || status === 'excused'
}

/**
 * Toplu kayıt planı: satır taslağını mevcut kayıtlarla karşılaştırır.
 * `create` → henüz kaydı olmayıp taslağı dolu olanlar (toplu uca gider),
 * `update` → kaydı olup durumu değişenler (tekil düzeltme ucuna gider).
 * Boş taslak "kaydedilmedi" demektir ve atlanır.
 */
export function planBulkSave(rows: DraftRow[], existing: Map<string, ExistingRecord>) {
  const create: DraftRow[] = []
  const update: { id: string; status: string; reasonId?: string }[] = []
  for (const r of rows) {
    if (!r.status) continue
    const e = existing.get(r.enrollmentId)
    const reason = attendanceNeedsReason(r.status) ? (r.reasonId || undefined) : undefined
    if (!e) create.push({ ...r, reasonId: reason })
    else if (e.status !== r.status || (e.reasonId ?? undefined) !== reason) update.push({ id: e.id, status: r.status, reasonId: reason })
  }
  return { create, update }
}

/** Devam oranı (yüzde, 1 ondalık); gün yoksa 0. */
export function attendanceRate(present: number, total: number): number {
  return total > 0 ? Math.round((present / total) * 1000) / 10 : 0
}
