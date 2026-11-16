// Kamp: bir kampın başından sonuna akışı — kayıt → etkinlikler → katılım (toplu/tekil/düzeltme) → günlük yoklama (toplu/tekil/düzeltme)
// → tamamlama → kamp sonu raporu; ayrıca bekleme listesi, toplu tamamlamaya hazır dönem, boş dönem.
import { step, info, idOf, at, dayStr, count } from '../lib.mjs'
import { POOLS } from '../data.mjs'

export async function setupCamp(ctx) {
  const { c, code, ids } = ctx
  if ((await count(c, '/camps')) > 0) { info('kamp zaten var — atlandı'); return }
  const P = POOLS[code], pk = code.slice(-1).toUpperCase()
  const camp = await step('kamp', async () => c.post('/camps', { code: `YAZ26-${pk}`, name: P.campName, campTypeId: await ctx.ref('camp_type', 'summer'), campusId: ctx.campuses[0].id, description: 'Yaz dönemi yoğun eğitim ve sosyal beceri kampı (demo).', location: ctx.campuses[0].name, capacity: 12 }))
  const campId = idOf(camp); if (!campId) return
  const p1 = idOf(await step('dönem 1', () => c.post(`/camps/${campId}/periods`, { name: 'Temmuz Dönemi', startDate: dayStr(-120), endDate: dayStr(-106), capacity: 6 })))
  const p2 = idOf(await step('dönem 2', () => c.post(`/camps/${campId}/periods`, { name: 'Aralık Ara Tatil Dönemi', startDate: dayStr(40), endDate: dayStr(54), capacity: 3 })))
  // dönem 3: bitmiş ama kayıtlıları tamamlanmamış → "dönemi tamamla" (toplu tamamlama) örneği; dönem 4: boş → düzenle/sil örneği
  const p3 = idOf(await step('dönem 3', () => c.post(`/camps/${campId}/periods`, { name: 'Haziran Dönemi (tamamlanacak)', startDate: dayStr(-20), endDate: dayStr(-6), capacity: 4 })))
  await step('dönem 4', () => c.post(`/camps/${campId}/periods`, { name: 'Eylül Hafta Sonu (taslak)', startDate: dayStr(100), endDate: dayStr(102), capacity: 6 }))
  if (p3) for (const s of ids.students.slice(0, 2)) await step(`kamp kaydı (dönem 3) ${s.no}`, () => c.post(`/camps/periods/${p3}/enrollments`, { campPeriodId: p3, studentId: s.id, status: 'enrolled', studentPackageId: null }))
  for (const e of ids.educators.slice(0, 2)) await step('kamp eğitmeni', () => c.post(`/camps/${campId}/educators`, { campId, educatorId: e.id, role: e === ids.educators[0] ? 'lead' : 'assistant', campPeriodId: null, campActivityId: null }))
  const acts = []
  for (const [pid, d, list] of [[p1, -119, ['Duyusal Oyun Atölyesi', 'Hidroterapi Havuz Günü']], [p2, 41, ['Sanat ve Müzik Atölyesi', 'Doğa Yürüyüşü']]]) {
    if (!pid) continue
    for (const [i, n] of list.entries()) {
      const a = await step(`etkinlik ${n}`, async () => c.post(`/camps/periods/${pid}/activities`, { campPeriodId: pid, name: n, activityTypeId: await ctx.ref('camp_activity_type', i ? 'sports' : 'therapy'), description: 'Demo etkinlik', startsAt: at(d + i, 10), endsAt: at(d + i, 12), location: 'Kamp alanı', capacity: 8, sessionId: null }))
      if (idOf(a)) acts.push({ id: idOf(a), pid })
    }
  }
  // kayıtlar: dönem 1 → 4 öğrenci (tamamlanmış); dönem 2 → 3 kontenjan dolu + 1 bekleme listesi (kontenjan 3)
  const en1 = []
  if (p1) for (const s of ids.students.slice(0, 4)) { const e = await step(`kamp kaydı ${s.no}`, () => c.post(`/camps/periods/${p1}/enrollments`, { campPeriodId: p1, studentId: s.id, status: 'enrolled', studentPackageId: null })); if (idOf(e)) en1.push(idOf(e)) }
  if (p2) {
    for (const s of ids.students.slice(4, 7)) await step(`kamp kaydı ${s.no}`, () => c.post(`/camps/periods/${p2}/enrollments`, { campPeriodId: p2, studentId: s.id, status: 'enrolled', studentPackageId: null }))
    await step('bekleme listesi', () => c.post(`/camps/periods/${p2}/enrollments`, { campPeriodId: p2, studentId: ids.students[7].id, status: 'waitlist', studentPackageId: null }))
  }
  // ── Dönem 1: tam akış ──
  // 1) Etkinlik katılımı: ilk etkinlik toplu (4 öğrenci), ikinci etkinlik tekil kayıtlarla; bir katılım düzeltilir.
  const [act1, act2] = acts.filter((a) => a.pid === p1)
  if (act1 && en1.length) await step('kamp katılımı (toplu)', () => c.post(`/camps/activities/${act1.id}/participations/bulk`, { campActivityId: act1.id, records: en1.map((eid) => ({ enrollmentId: eid, status: 'attended', notes: null })) }))
  if (act2) {
    const parts = []
    for (const [k, eid] of en1.slice(0, 3).entries()) parts.push(idOf(await step('kamp katılımı (tekil)', () => c.post(`/camps/activities/${act2.id}/participations`, { campActivityId: act2.id, campEnrollmentId: eid, status: k === 2 ? 'registered' : 'attended', notes: null }))))
    if (parts[2]) await step('kamp katılımı düzeltme', () => c.patch(`/camps/participations/${parts[2]}`, { id: parts[2], status: 'absent', notes: 'Rahatsızlandı, katılamadı.' }))
  }
  // 2) Günlük yoklama: ilk gün toplu; sonraki iki gün tekil; bir yoklama düzeltilir.
  if (p1 && en1.length) await step('kamp yoklaması (toplu)', () => c.post(`/camps/periods/${p1}/attendance/bulk`, { campPeriodId: p1, attendanceDate: dayStr(-119), records: en1.map((eid, k) => ({ enrollmentId: eid, status: k === 3 ? 'late' : 'present', reasonId: null })) }))
  const att = []
  for (const [k, eid] of en1.entries())
    for (let day = 1; day < 3; day++) att.push(idOf(await step('kamp yoklaması', () => c.post('/camps/attendance', { campEnrollmentId: eid, attendanceDate: dayStr(-119 + day), status: (k + day) % 5 === 4 ? 'absent' : 'present', reasonId: null }))))
  if (att[0]) await step('kamp yoklaması düzeltme', () => c.patch(`/camps/attendance/${att[0]}`, { attendanceId: att[0], status: 'excused', reasonId: null }))
  // 3) Tamamlama, ardından kamp sonu raporu (yalnızca tamamlanmış kayıt için; biri bilerek raporsuz — "Rapor Yaz" örneği).
  for (const [k, eid] of en1.entries()) {
    await step('kamp kaydı tamamla', () => c.post(`/camps/enrollments/${eid}/complete`, {}))
    if (k < 3) await step('kamp sonu raporu', () => c.post(`/camps/enrollments/${eid}/reports`, { campEnrollmentId: eid, summary: ['Kamp boyunca grup etkinliklerine istekle katıldı; havuz gününde su korkusunu aştı.', 'Sosyal beceri çalışmalarında sıra bekleme ve paylaşma davranışlarında belirgin ilerleme gösterdi.', 'İlk günlerde uyum güçlüğü yaşadı, son hafta bağımsız katılım sağladı. Evde duyusal oyunlara devam önerilir.'][k], fileId: null }))
  }
  // Dönem 3 (bitmiş, tamamlanmamış): ilk iki gün toplu yoklama; kayıtlar "Dönemi Tamamla" ile kapatılmaya hazır kalır.
  if (p3) {
    const en3 = (await c.get(`/camps/enrollments?campPeriodId=${p3}&pageSize=50`))?.items ?? []
    for (let day = 0; day < 2; day++) await step('kamp yoklaması (dönem 3, toplu)', () => c.post(`/camps/periods/${p3}/attendance/bulk`, { campPeriodId: p3, attendanceDate: dayStr(-20 + day), records: en3.map((e, k) => ({ enrollmentId: e.id, status: (k + day) % 2 ? 'absent' : 'present', reasonId: null })) }))
  }
  info('kamp: 1 kamp, 4 dönem (dönem 1 tam akış: katılım+yoklama+tamamlama+3 rapor; dönem 3 toplu tamamlanmaya hazır, yoklamalı; dönem 4 boş), 4 etkinlik, 10 kayıt (1 bekleme listesi), 2 eğitmen')
}
