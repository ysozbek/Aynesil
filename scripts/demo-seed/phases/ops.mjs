// Toplantılar + takip görevleri, izinler, danışmanlık, kamera, KPI.
import { step, info, idOf, at, dayStr, count } from '../lib.mjs'
import { POOLS } from '../data.mjs'

export async function setupOps(ctx) {
  await meetings(ctx); await leaves(ctx); await consultancy(ctx); await cameras(ctx); await kpi(ctx)
}

async function meetings(ctx) {
  const { c, ids } = ctx
  if ((await count(c, '/meetings')) > 0) { info('toplantılar zaten var — atlandı'); return }
  const org = ids.users.coordinator
  const mk = (title, typeCode, d, h, extra = {}) => step(`toplantı ${title}`, async () => c.post('/meetings', {
    title, meetingTypeId: await ctx.ref('meeting_type', typeCode), scheduledAt: at(d, h), endsAt: at(d, h + 1), location: 'Toplantı odası', roomId: null, campusId: ctx.campuses[0].id, organizerId: org,
    participants: [{ participantType: 'user', userId: ids.users.educator, guardianId: null, leadId: null, externalName: null, attendance: 'invited' },
                   { participantType: 'guardian', userId: null, guardianId: ids.guardians[0]?.id ?? null, leadId: null, externalName: null, attendance: 'invited' }], ...extra }))
  // geçmişe toplantı API'de kabul ediliyor mu? (deneriz; kabul etmezse ileri tarihli planlanır)
  const done = await mk('Aylık vaka değerlendirme toplantısı', 'internal', -12, 10)
  if (done?.id) {
    await step('toplantı tamamla', () => c.post(`/meetings/${done.id}/complete`, {}))
    await step('toplantı sonucu', () => c.post(`/meetings/${done.id}/outcomes`, { meetingId: done.id, summary: 'Üç öğrencinin BEP hedefleri gözden geçirildi.', decisions: 'Dil terapisi sıklığı artırılacak; veli bilgilendirilecek.' }))
    await step('takip: tamamlanmış', async () => { const f = await c.post(`/meetings/${done.id}/follow-ups`, { meetingId: done.id, action: 'Veliye bilgilendirme e-postası gönder', assigneeId: org, dueDate: dayStr(-8) }); await c.patch(`/meetings/follow-ups/${f.id}/status`, { status: 'done' }) })
    await step('takip: gecikmiş', () => c.post(`/meetings/${done.id}/follow-ups`, { meetingId: done.id, action: 'Dil terapisi seans planını güncelle', assigneeId: org, dueDate: dayStr(-3) }))
    await step('takip: açık', () => c.post(`/meetings/${done.id}/follow-ups`, { meetingId: done.id, action: 'Hedef ilerleme raporlarını hazırla', assigneeId: ids.users.admin, dueDate: dayStr(6) }))
  }
  await mk('Veli görüşmesi — dönem ortası', 'parent', 2, 14)
  await mk('Kurum dışı uzman görüşmesi', 'external', 7, 11)
  const cancelled = await mk('İptal edilen ekip toplantısı', 'internal', 4, 9)
  if (cancelled?.id) await step('toplantı iptal', () => c.post(`/meetings/${cancelled.id}/cancel`, {}))
  info('toplantı: 4 toplantı, takip görevleri (tamamlanmış/gecikmiş/açık)')
}

async function leaves(ctx) {
  const { c, ids } = ctx
  if ((await count(c, '/leave/requests')) > 0) { info('izin talepleri zaten var — atlandı'); return }
  const year = new Date().getUTCFullYear()
  const annual = await ctx.ref('leave_type', 'annual'), sick = await ctx.ref('leave_type', 'sick')
  for (const e of ids.educators.slice(0, 4)) {
    await step('izin bakiyesi yıllık', () => c.post('/leave/balances', { educatorId: e.id, leaveTypeId: annual, periodYear: year, entitled: 14, unit: 'day' }))
    await step('izin bakiyesi hastalık', () => c.post('/leave/balances', { educatorId: e.id, leaveTypeId: sick, periodYear: year, entitled: 10, unit: 'day' }))
  }
  const rq = (e, t, d1, d2, qty, reason) => step('izin talebi', () => c.post('/leave/requests', { educatorId: e.id, leaveTypeId: t, unit: 'day', startsAt: at(d1, 9), endsAt: at(d2, 18), quantity: qty, reason }))
  await rq(ids.educators[0], annual, 20, 24, 5, 'Aile tatili')                       // bekleyen
  const ap = await rq(ids.educators[1], annual, -30, -28, 3, 'Kişisel işler')        // onaylanacak
  const rj = await rq(ids.educators[2], annual, 10, 14, 5, 'Uzun hafta sonu')        // reddedilecek
  const cn = await rq(ids.educators[3], sick, 3, 3, 1, 'Doktor randevusu')           // iptal edilecek
  const approver = ctx.c.me.userId
  if (ap?.id) await step('izin onay', () => c.post(`/leave/requests/${ap.id}/approve`, { approverId: approver, comment: 'Uygun.', stepNo: 1 }))
  if (rj?.id) await step('izin ret', () => c.post(`/leave/requests/${rj.id}/reject`, { approverId: approver, comment: 'Aynı hafta eğitmen yetersizliği.', stepNo: 1 }))
  if (cn?.id) await step('izin iptal', () => c.post(`/leave/requests/${cn.id}/cancel`, {}))
  info('izin: 8 bakiye, 4 talep (bekleyen/onaylı/reddedilmiş/iptal)')
}

async function consultancy(ctx) {
  const { c, code, ids } = ctx
  if ((await count(c, '/consultancy/institutions')) > 0) { info('danışmanlık zaten var — atlandı'); return }
  const P = POOLS[code]
  const inst = await step('okul', async () => c.post('/consultancy/institutions', { name: P.school, institutionTypeId: await ctx.ref('institution_type', 'primary_school'), city: ctx.t.city, district: 'Merkez', contactName: 'Okul Rehber Öğretmeni', contactPhone: null, contactEmail: `rehberlik@okul.${ctx.t.domain}` }))
  if (!inst?.id) return
  const plan = await step('danışmanlık planı', async () => c.post('/consultancy/plans', { institutionId: inst.id, name: 'Kaynaştırma Sınıfı Destek Programı (Demo)', consultancyTypeId: await ctx.ref('consultancy_type', 'observation'), periodStart: dayStr(-30), periodEnd: dayStr(90), scope: 'Sınıf içi gözlem, öğretmen eğitimi ve takip.', leadEducatorId: ids.educators[3].id }))
  if (plan?.id) await step('danışmanlık planı aktifleştir', () => c.post(`/consultancy/plans/${plan.id}/activate`, {}))
  const visit = await step('ziyaret', () => c.post('/consultancy/visits', { institutionId: inst.id, visitDate: dayStr(-5), consultancyPlanId: plan?.id ?? null, visitorId: ids.educators[3].id, purpose: 'İlk sınıf gözlemi' }))
  let obs
  if (visit?.id) {
    obs = await step('gözlem', async () => c.post(`/consultancy/visits/${visit.id}/observations`, { schoolVisitId: visit.id, observation: 'Öğrenci grup etkinliklerinde yönergeyi görsel destekle takip etti.', observationTypeId: await ctx.ref('observation_type', 'classroom'), subject: 'Sınıf içi katılım', recommendations: 'Görsel program kartı sınıfta kalıcı kullanılsın.' }))
    await step('ziyaret tamamla', () => c.post(`/consultancy/visits/${visit.id}/complete`, {}))
  }
  await step('ikinci ziyaret', () => c.post('/consultancy/visits', { institutionId: inst.id, visitDate: dayStr(9), consultancyPlanId: plan?.id ?? null, visitorId: ids.educators[3].id, purpose: 'Takip ziyareti' }))
  await step('takip görevi', () => c.post('/consultancy/follow-ups', { title: 'Öğretmene görsel program kartı eğitimi ver', consultancyPlanId: plan?.id ?? null, schoolVisitId: visit?.id ?? null, observationRecordId: obs?.id ?? null, description: 'Demo takip görevi', dueDate: dayStr(12), assignedTo: ids.educators[3].id }))
  if (plan?.id) await step('danışmanlık sözleşmesi', async () => c.post('/consultancy/agreements', { consultancyPlanId: plan.id, title: 'Danışmanlık Hizmet Sözleşmesi (Demo)', agreementTypeId: await ctx.ref('agreement_type', 'consultancy_contract'), description: 'Demo', startDate: dayStr(-30), endDate: dayStr(90) }))
  info('danışmanlık: 1 okul, plan, 2 ziyaret, gözlem, takip görevi, sözleşme')
}

async function cameras(ctx) {
  const { c, code, ids } = ctx
  if ((await count(c, '/cameras')) > 0) { info('kameralar zaten var — atlandı'); return }
  const pk = code.slice(-1).toUpperCase()
  const cams = []
  for (const [i, camp] of ctx.campuses.entries()) {
    const cam = await step(`kamera ${i}`, async () => c.post('/cameras', { code: `CAM-${pk}${i + 1}`, name: `${camp.name} — ${i ? 'Koridor' : 'Terapi odası'} kamerası`, campusId: camp.id, cameraTypeId: await ctx.ref('camera_type', i ? 'ptz' : 'fixed'), streamProviderId: null, streamRef: `rtsp://demo.test/${code}/cam${i + 1}` }))
    if (idOf(cam)) cams.push({ id: idOf(cam) })
  }
  const room = ids.rooms.find((r) => !r.virtual && r.homeCampusId === ctx.campuses[0].id)
  if (cams[0] && room) await step('kamera-derslik', () => c.post(`/cameras/${cams[0].id}/rooms`, { roomId: room.id }))
  const room2 = ids.rooms.find((r) => !r.virtual && r.homeCampusId === ctx.campuses[1].id)
  if (cams[1] && room2) await step('kamera-derslik 2', () => c.post(`/cameras/${cams[1].id}/rooms`, { roomId: room2.id }))
  // veli1 → öğrenci 0 ve 1 için izleme yetkisi (kamera rızası legal fazında verildi)
  const p1 = ids.portal?.guardian1
  if (p1) for (const sid of p1.studentIds) await step('izleme yetkisi', async () => c.post('/cameras/authorizations', { guardianId: p1.guardianId, studentId: sid, validFrom: new Date(Date.now() - 864e5).toISOString(), validTo: new Date(Date.now() + 180 * 864e5).toISOString(), sessionId: null, consentId: null, accessTypeId: await ctx.ref('access_type', 'live_only') }))
  info(`kamera: ${cams.length} kamera, derslik ataması, veli1 izleme yetkisi`)
}

async function kpi(ctx) {
  const { c } = ctx
  const today = new Date()
  const pm = (offset) => { const s = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() + offset, 1)); const e = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() + offset + 1, 0)); return [s.toISOString().slice(0, 10), e.toISOString().slice(0, 10)] }
  for (const off of [-2, -1]) { const [s, e] = pm(off); await step(`KPI hesapla ${s}`, () => c.post('/performance-kpi/compute/bulk', { periodStart: s, periodEnd: e })) }
  info('KPI: geçen ay ve önceki ay için toplu hesaplama (API)')
}
