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

/** Kayıtlı/bekleyen öğrencisi olan dönem silinemez (sunucu da 409 döner). */
export function periodHasStudents(p: Pick<CampPeriodListItemDto, 'enrolledCount' | 'waitlistCount'>): boolean {
  return p.enrolledCount > 0 || p.waitlistCount > 0
}

/** Pasifleştirme: bitmemiş dönemlerinde kayıtlı/bekleyen öğrenci olan kamp pasifleştirilemez. */
export function blocksDeactivation(periods: CampPeriodListItemDto[], today: string): boolean {
  return periods.some((p) => p.endDate >= today && periodHasStudents(p))
}
