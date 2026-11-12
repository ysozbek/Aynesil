/**
 * Tarih/saat yardımcıları.
 *
 * Backend `DateTimeOffset` alanları offset'li ISO ister. `<input type="date">` ve
 * `<input type="datetime-local">` değerleri ise saat dilimsizdir; bu yüzden tarayıcının
 * yerel saatine göre yorumlanıp ISO (UTC) olarak gönderilmeli, forma yüklenirken de
 * yerel saate çevrilmelidir.
 */

function pad(n: number): string {
  return String(n).padStart(2, '0')
}

/** Date → yerel `YYYY-MM-DD` (toISOString UTC'ye çevirdiği için gün sınırında kayar). */
export function toLocalDateString(d: Date = new Date()): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** `YYYY-MM-DD` → o günün yerel 00:00'ının ISO karşılığı (aralık başlangıcı). */
export function startOfDayIso(dateStr: string): string {
  return new Date(`${dateStr}T00:00:00`).toISOString()
}

/** `YYYY-MM-DD` → o günün yerel 23:59:59.999'unun ISO karşılığı (aralık bitişi, gün dahil). */
export function endOfDayIso(dateStr: string): string {
  return new Date(`${dateStr}T23:59:59.999`).toISOString()
}

/** Tarih aralığı filtresi → `to` günün sonunu kapsar. Boş değer `undefined` olur. */
export function dateRangeToIso(from?: string | null, to?: string | null) {
  return {
    from: from ? startOfDayIso(from) : undefined,
    to: to ? endOfDayIso(to) : undefined,
  }
}

/** `datetime-local` değeri (yerel) → ISO (UTC) gönderim değeri. */
export function localInputToIso(value: string): string {
  return new Date(value).toISOString()
}

/** Backend ISO değeri → `datetime-local` input değeri (yerel saat, `YYYY-MM-DDTHH:mm`). */
export function isoToLocalInput(iso?: string | null): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  return `${toLocalDateString(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export type KpiPeriodType = 'Monthly' | 'Quarterly' | 'Annual'

/** `anchor`'ı içeren dönemden `delta` dönem sonrasının (negatifse öncesinin) ilk gününü döndürür. */
export function shiftPeriod(type: KpiPeriodType, anchor: Date, delta: number): Date {
  const y = anchor.getFullYear()
  const m = anchor.getMonth()
  if (type === 'Monthly') return new Date(y, m + delta, 1)
  if (type === 'Quarterly') return new Date(y, Math.floor(m / 3) * 3 + 3 * delta, 1)
  return new Date(y + delta, 0, 1)
}

/** Dönemin henüz başlamamış (gelecek) olup olmadığı. */
export function isFuturePeriod(type: KpiPeriodType, anchor: Date, now: Date = new Date()): boolean {
  return periodRange(type, anchor).periodStart > toLocalDateString(now)
}

/** Dönem etiketi: Aylık → "Ekim 2026", Çeyrek → "Q3 2026", Yıllık → "2026". */
export function periodLabel(type: KpiPeriodType, anchor: Date): string {
  if (type === 'Monthly') return anchor.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' })
  if (type === 'Quarterly') return `Q${Math.floor(anchor.getMonth() / 3) + 1} ${anchor.getFullYear()}`
  return String(anchor.getFullYear())
}

/** Dönem tipinden, `ref` gününü içeren takvim döneminin `DateOnly` sınırlarını hesaplar. */
export function periodRange(type: KpiPeriodType, ref: Date = new Date()) {
  const y = ref.getFullYear()
  const m = ref.getMonth()
  let start: Date
  let end: Date
  if (type === 'Monthly') {
    start = new Date(y, m, 1)
    end = new Date(y, m + 1, 0)
  } else if (type === 'Quarterly') {
    const qStart = Math.floor(m / 3) * 3
    start = new Date(y, qStart, 1)
    end = new Date(y, qStart + 3, 0)
  } else {
    start = new Date(y, 0, 1)
    end = new Date(y, 11, 31)
  }
  return { periodStart: toLocalDateString(start), periodEnd: toLocalDateString(end) }
}
