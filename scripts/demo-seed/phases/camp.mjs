// Kamp: 1 kamp, 2 dönem, kayıtlar (biri bekleme listesinde), etkinlikler, eğitmen ataması, yoklama.
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
  // dönem 1 yoklaması + tamamlama
  for (const [k, eid] of en1.entries()) {
    for (let day = 0; day < 3; day++) await step('kamp yoklaması', () => c.post('/camps/attendance', { campEnrollmentId: eid, attendanceDate: dayStr(-119 + day), status: (k + day) % 5 === 4 ? 'absent' : 'present', reasonId: null }))
    await step('kamp kaydı tamamla', () => c.post(`/camps/enrollments/${eid}/complete`, {}))
  }
  info('kamp: 1 kamp, 2 dönem, 4 etkinlik, 8 kayıt (1 bekleme listesi), 2 eğitmen')
}
