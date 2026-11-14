// Programlar, kayıtlar, seanslar (son ~8 hafta + 2 hafta ileri), yoklama, notlar, ertelenen/iptal/gelmedi, tekrarlayan program, telafi.
import { Client, fx, step, info, idOf, at, weekdayOf, dayStr, rng, count } from '../lib.mjs'

const PROGRAMS = [
  ['ABA', 'Bireysel ABA Terapisi', 'therapy', 'Birebir davranış analizi temelli terapi.'],
  ['DIL', 'Dil ve Konuşma Terapisi', 'therapy', 'Alıcı ve ifade edici dil becerileri.'],
  ['SOS', 'Grup Sosyal Beceri Atölyesi', 'group_education', 'Küçük grup sosyal beceri çalışması.'],
]

export async function setupSessions(ctx) {
  const { c, code, ids } = ctx
  // ── Programlar + hizmetler ──
  const progs = await c.list('/programs')
  ids.programs = []
  for (const [pc, name, type, desc] of PROGRAMS) {
    const f = progs.find((p) => p.code === pc)
    const id = f?.id ?? await step(`program ${pc}`, async () => {
      const pid = idOf(await c.post('/programs', { code: pc, name, programTypeId: await ctx.ref('program_type', type), description: desc }))
      await c.post(`/programs/${pid}/services`, { name: `${name} — seans`, serviceTypeId: await ctx.ref('service_type', 'therapy'), defaultDurationMinutes: 45, defaultSessionsPerWeek: 2, sortOrder: 1 })
      return pid
    })
    if (id) ids.programs.push({ id, code: pc })
  }
  // ── Kayıt (enrollment) + öğrenci programı ──
  ids.studentPrograms = {}
  const enrolled = await c.list('/enrollments').catch(() => [])
  for (const s of ids.students) await step(`kayıt ${s.no}`, async () => {
    let en = enrolled.find((e) => e.studentId === s.id)
    if (!en) en = { id: idOf(await c.post('/enrollments', { studentId: s.id, campusId: s.campusId, statusId: await ctx.ref('enrollment_status', 'active'), enrolledOn: '2026-01-12' })) }
    const prog = ids.programs[s.idx % 2]   // ABA veya Dil
    const sp = idOf(await c.post('/student-programs', { studentId: s.id, programId: prog.id, enrollmentId: en.id, campusId: s.campusId, startDate: '2026-01-12', endDate: null }).catch(async (e) => {
      if (/exist|already|zaten|duplicate|unique/i.test(e.message)) return null; throw e }))
    ids.studentPrograms[s.id] = sp
  })

  if ((await count(c, '/scheduling/sessions')) > 0) { info('seanslar zaten var — atlandı'); return }

  const r = rng(code === 'demo_a' ? 5 : 9)
  const sessionType = await ctx.ref('session_type', 'individual')
  const groupType = await ctx.ref('session_type', 'group')
  const rooms = ctx.ids.rooms
  const roomsOf = (campusId) => rooms.filter((x) => !x.virtual && x.homeCampusId === campusId)
  const reasons = ['illness', 'family', 'transport', 'weather']
  const made = { total: 0, past: 0, future: 0, cancelled: 0, resched: 0, noshow: 0, att: { present: 0, absent: 0, late: 0, excused: 0 }, notes: 0 }
  const missed = []   // telafi için
  const pastSessions = []

  for (const s of ids.students) {
    const ed = ids.educators[s.idx % 6]
    const g = (s.idx % 6) >> 1
    const hour = 9 + Math.floor(s.idx / 6) + (g === 2 ? 3 : 0)
    const room = roomsOf(s.campusId)[g % 2]
    const days = s.idx < 8 ? [s.idx % 5, (s.idx + 2) % 5] : [s.idx % 5]
    for (let d = -56; d <= 13; d++) {
      const wd = weekdayOf(d)
      if (wd === 0 || wd === 6 || !days.includes(wd - 1)) continue
      if (d === 0) continue   // bugünü boş bırak
      const past = d < 0
      const body = { sessionTypeId: sessionType, startsAt: at(d, hour), endsAt: at(d, hour, 45), campusId: s.campusId, roomId: room?.id ?? null,
        title: `Demo seans — ${s.name}`, isMakeup: false, participantStudentIds: [s.id], educatorAssignments: [{ educatorId: ed.id, role: 'lead' }] }
      const dto = await step(`seans ${s.no} d${d}`, () => c.post('/scheduling/sessions', body))
      if (!dto?.id) continue
      made.total++; past ? made.past++ : made.future++
      if (!past) continue
      let rv = dto.rowVersion
      const roll = r()
      // yaklaşık 40. seansı iptal, 25.'yi gelmedi (no-show) yap
      if (made.past % 31 === 0) {
        await step('iptal', () => c.post(`/scheduling/sessions/${dto.id}/cancel`, { reason: 'Eğitmen raporlu', rowVersion: rv })); made.cancelled++; continue }
      if (made.past % 23 === 0) {
        await step('gelmedi', () => c.post(`/scheduling/sessions/${dto.id}/no-show`, { rowVersion: rv })); made.noshow++; missed.push({ sid: dto.id, studentId: s.id }); continue }
      const status = roll < 0.68 ? 'present' : roll < 0.8 ? 'late' : roll < 0.94 ? 'absent' : 'excused'
      const reason = status === 'absent' || status === 'excused' ? await ctx.ref('attendance_reason', reasons[Math.floor(r() * reasons.length)]) : null
      await step('yoklama', () => c.post(`/scheduling/sessions/${dto.id}/attendance`, { studentId: s.id, status, reasonId: reason, minutesAttended: status === 'present' ? 45 : status === 'late' ? 30 : 0, note: status === 'late' ? 'Ulaşım nedeniyle 15 dk geç geldi.' : null }))
      made.att[status]++
      if (status === 'absent') missed.push({ sid: dto.id, studentId: s.id })
      const done = await step('tamamla', () => c.post(`/scheduling/sessions/${dto.id}/complete`, { rowVersion: rv }))
      pastSessions.push({ id: dto.id, studentId: s.id, status, educatorId: ed.id, d })
    }
  }
  // ── Seans notları (bir kısmı veliye görünür) ──
  const NOTES = ['Bugün göz teması süresi belirgin arttı; pekiştireç olarak oyun kullanıldı.', 'İki sözcüklü isteklerde yardımsız 6/10 deneme başarı.',
    'Geçişlerde zorlandı; görsel program kartı ile desteklendi.', 'Akranıyla sıra bekleme çalışması olumlu geçti.', 'Ev ödevi: günde 10 dk kartlarla adlandırma.',
    'Seans başında huzursuzdu, 5 dk duyusal mola sonrası uyum sağladı.', 'Kalem tutuşu çalışmasında ilerleme var; çizgi çizme tekrarı yapıldı.', 'Velisiyle görüşüldü; evde rutin önerileri paylaşıldı.']
  // Not yazarı = oturumdaki kullanıcıya bağlı eğitmen kaydı (authored_by → educators.educator); bu yüzden notlar
  // eğitmen kullanıcısı (educators[0] ile bağlı) olarak yazılır.
  const attended = pastSessions.filter((p) => p.status !== 'absent' && p.status !== 'excused' && p.educatorId === ids.educators[0].id)
  const ec = new Client(`${code}-egitmen`)
  await ec.login(`${fx.users.educator.local}@${ctx.t.domain}`, fx.password)
  for (let i = 0; i < 8 && i < attended.length; i++) {
    const p = attended[Math.floor((i + 0.5) * attended.length / 8)]
    const note = await step('seans notu', () => ec.post(`/scheduling/sessions/${p.id}/notes`, { body: NOTES[i], parentVisible: i % 2 === 0 }))
    if (note?.id) made.notes++
  }
  // ── veli1'in çocukları (portal): her birine bir veliye görünür + bir dahili (veliye kapalı) not; ilk çocuğun görünür notu düzenlenir ──
  // Portal seans geçmişi yalnızca parent_visible notları gösterir; düzenlenen notta "düzenlendi" çıkar (V55 edited_at).
  const p1 = ids.portal?.guardian1
  for (const [k, sid] of (p1?.studentIds ?? []).entries()) {
    const ps = pastSessions.find((x) => x.studentId === sid && x.status === 'present') ?? pastSessions.find((x) => x.studentId === sid)
    if (!ps) continue
    const vis = await step('veli notu (görünür)', () => ec.post(`/scheduling/sessions/${ps.id}/notes`, { body: 'Veliye açık not: bugünkü çalışmada hedef davranış desteksiz sürdürüldü; evde kısa tekrar önerilir.', parentVisible: true }))
    await step('dahili not (veliye kapalı)', () => ec.post(`/scheduling/sessions/${ps.id}/notes`, { body: 'Dahili not (veliye kapalı): ekip içi değerlendirme, velilere gösterilmez.', parentVisible: false }))
    if (vis?.id) made.notes += 2
    if (k === 0 && vis?.id) await step('veli notu düzenleme', () => ec.put(`/scheduling/sessions/${ps.id}/notes/${vis.id}`, { body: 'Veliye açık not (güncellendi): hedef davranış desteksiz sürdürüldü; evde 5 dk tekrar önerilir.', parentVisible: true, rowVersion: vis.rowVersion }))
  }
  // ── Grup seansı notu sızıntı örneği (V56): veli1'in çocuğu (A) + veli2'nin çocuğu (B) aynı seansta ──
  // Veliye görünür her not bir öğrenciye bağlıdır; A'nın velisi B'ye yazılan notu görmez. Öğrencisiz not API'den
  // veliye görünür yapılamaz (grup seansında 422), bu yüzden öğrencisiz dahili not da eklenir (kimseye gösterilmez).
  const gA = ids.students[0], gB = ids.students[2]
  const grp = await step('grup seansı', () => c.post('/scheduling/sessions', { sessionTypeId: groupType, startsAt: at(-3, 20), endsAt: at(-3, 20, 45), campusId: gA.campusId, roomId: null,
    title: 'Demo grup seansı (sosyal beceri)', isMakeup: false, participantStudentIds: [gA.id, gB.id], educatorAssignments: [{ educatorId: ids.educators[0].id, role: 'lead' }] }))
  if (grp?.id) {
    const gn = (body, parentVisible, studentId) => step('grup notu', () => ec.post(`/scheduling/sessions/${grp.id}/notes`, { body, parentVisible, studentId }))
    await gn('Grup-A: sıra bekleme çalışmasında akranına yardım etti (yalnızca bu öğrencinin velisine açık).', true, gA.id)
    await gn('Grup-B-ÖZEL: bu not yalnızca B öğrencisinin velisine açıktır; A velisi görmemelidir.', true, gB.id)
    await gn('Grup-DAHİLİ: öğrencisiz iç not, hiçbir veliye gösterilmez.', false, null)
    made.notes += 3
  }
  // ── Ertelenen seans: gelecekteki iki seansı ertele ──
  const future = (await c.list('/scheduling/sessions?from=' + dayStr(1) + '&to=' + dayStr(13))).filter((x) => x.status === 'scheduled' || !x.status).slice(0, 2)
  for (const f of future) await step('erteleme', async () => {
    const d = await c.get(`/scheduling/sessions/${f.id}`)
    const ns = new Date(new Date(d.startsAt).getTime() + 7 * 36e5), ne = new Date(new Date(d.endsAt).getTime() + 7 * 36e5)
    await c.put(`/scheduling/sessions/${f.id}/reschedule`, { newStartsAt: ns.toISOString(), newEndsAt: ne.toISOString(), roomId: d.roomId, rowVersion: d.rowVersion }); made.resched++
  })
  // ── Telafi talebi (kaçırılan seans için) ──
  for (const m of missed.slice(0, 3)) await step('telafi talebi', async () => {
    const mr = await c.post('/scheduling/makeup-requests', { studentId: m.studentId, missedSessionId: m.sid, missedReasonId: await ctx.ref('missed_reason', 'illness'), note: 'Hastalık nedeniyle telafi isteniyor.', expiresOn: dayStr(30) })
    if (missed.indexOf(m) === 0) await c.post(`/scheduling/makeup-requests/${mr.id}/approve`, { rowVersion: mr.rowVersion })
  })
  // ── Tekrarlayan program: ilk öğrenci, Çarşamba 15:00, 2 hafta ileri üret ──
  const s0 = ids.students[0], sp0 = ids.studentPrograms[s0.id]
  const rr = await step('tekrarlayan program', async () => {
    const sched = await c.post('/scheduling/recurring-schedules', { frequency: 'weekly', startTime: '15:00:00', durationMinutes: 45, rangeStart: dayStr(1), rangeEnd: dayStr(60),
      campusId: s0.campusId, studentProgramId: sp0 ?? null, sessionTypeId: sessionType, roomId: roomsOf(s0.campusId)[0]?.id ?? null, intervalCount: 1, byWeekday: [3], byMonthday: null,
      maxOccurrences: null, programServiceId: null, educators: [{ educatorId: ids.educators[0].id, role: 'lead' }] })
    await c.post(`/scheduling/recurring-schedules/${sched.id}/generate`, { windowStart: dayStr(1), windowEnd: dayStr(14) })
    return sched
  })
  // ── Takvim kayıtları ──
  await step('takvim kaydı', () => c.post('/scheduling/calendar-entries', { title: 'Demo — Cumhuriyet Bayramı (kapalı)', entryType: 'holiday', startsAt: at(-14, 0), endsAt: at(-14, 23), isAllDay: true, campusId: null }))
  await step('takvim kaydı', () => c.post('/scheduling/calendar-entries', { title: 'Demo — Veli Toplantısı Haftası', entryType: 'event', startsAt: at(9, 9), endsAt: at(9, 17), isAllDay: false, campusId: ctx.campuses[0].id }))
  ids.pastSessions = pastSessions
  info(`seans: ${made.total} (geçmiş ${made.past}, gelecek ${made.future}) · iptal ${made.cancelled} · gelmedi ${made.noshow} · ertelenen ${made.resched} · yoklama ${JSON.stringify(made.att)} · not ${made.notes}`)
}
