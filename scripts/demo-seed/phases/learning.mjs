// BEP: dönem, hedefler, ilerleme kayıtları, planlar (taslak→onaylı→aktif), gözden geçirme; değerlendirme şablonu + oturumlar + raporlar.
import { step, info, idOf, dayStr, count, rng, Client, fx } from '../lib.mjs'
import { BEP_GOALS } from '../data.mjs'

export async function setupLearning(ctx) {
  const { c, code, ids } = ctx
  if ((await count(c, '/education-plans')) > 0) { info('BEP planları zaten var — atlandı'); }
  else await bep(ctx)
  if ((await count(c, '/assessment-sessions')) > 0) { info('değerlendirmeler zaten var — atlandı'); }
  else await assessments(ctx)
}

async function bep(ctx) {
  const { c, ids } = ctx
  const r = rng(77)
  const period = await step('akademik dönem', async () => {
    const list = await c.list('/education-plans/academic-periods')
    return list.find((p) => p.name === '2026-2027 Güz') ?? await c.post('/education-plans/academic-periods', { name: '2026-2027 Güz', startDate: '2026-09-07', endDate: '2027-01-22', termId: await ctx.ref('academic_term', 'fall'), isCurrent: true })
  })
  const approver = ctx.c.me.userId
  let ec = null
  const educatorClient = async () => {
    if (!ec) { ec = new Client(`${ctx.code}-egitmen`); await ec.login(`${fx.users.educator.local}@${ctx.t.domain}`, fx.password) }
    return ec
  }
  const made = { plans: 0, goals: 0, progress: 0 }
  for (const s of ids.students.slice(0, 8)) {
    const goalIds = []
    for (let g = 0; g < 3; g++) {
      const [stmt, cat, area] = BEP_GOALS[(s.idx + g) % BEP_GOALS.length]
      const horizon = g === 2 ? 'long_term' : 'short_term'
      const gid = await step(`hedef ${s.no}/${g}`, async () => idOf(await c.post('/goals/student-goals', {
        studentId: s.id, statement: stmt, horizon, templateId: null, categoryId: await ctx.ref('goal_category', cat), developmentAreaId: await ctx.ref('development_area', area),
        parentGoalId: null, masteryCriteria: '3 ardışık seansta %80 başarı', baseline: '%20', targetValue: 80, startDate: '2026-09-14', targetDate: horizon === 'long_term' ? '2027-06-30' : '2026-12-31' })))
      if (!gid) continue
      goalIds.push({ id: gid, horizon }); made.goals++
      // ilerleme: 4 ölçüm, artan eğilim
      let v = 20 + Math.floor(r() * 10)
      for (let k = 0; k < 4; k++) {
        v += Math.floor(r() * 14) - 2
        await step('ilerleme', () => c.post(`/goals/student-goals/${gid}/progress`, { measuredOn: dayStr(-49 + k * 14), measuredValue: v, percentComplete: Math.min(100, v), trend: k === 0 ? 'stable' : 'improving', note: `${k + 1}. ölçüm`, sessionId: null }))
        made.progress++
      }
    }
    // plan
    const plan = await step(`plan ${s.no}`, async () => {
      const p = await c.post('/education-plans', { studentId: s.id, title: `${s.name} — 2026 Güz BEP`, academicPeriodId: period?.id ?? null, campusId: s.campusId, preparedBy: ids.educators[s.idx % 6].id, effectiveFrom: '2026-09-14', effectiveTo: '2027-01-22' })
      return p
    })
    if (!plan?.id) continue
    made.plans++
    for (let i = 0; i < goalIds.length; i++) await step('plana hedef', () => c.post(`/education-plans/${plan.id}/goals`, { studentGoalId: goalIds[i].id, horizon: goalIds[i].horizon, sortOrder: i + 1 }))
    // durum: 0-1 taslak; 2-3 incelemede; 4-5 onaylı; 6-7 aktif (+ gözden geçirme)
    if (s.idx >= 2) await step('plan gönder', () => c.post(`/education-plans/${plan.id}/submit`, {}))
    // Onaylayan = oturumdaki kullanıcıya bağlı eğitmen kaydı (ActingEducator); yönetici değil eğitmen kullanıcısı onaylar.
    if (s.idx >= 4) await step('plan onayla', async () => (await educatorClient()).post(`/education-plans/${plan.id}/approve`, { approverId: ids.educators[0].id, comment: 'Hedefler yaş düzeyine uygun.' }))
    if (s.idx >= 6) {
      await step('plan aktif', () => c.post(`/education-plans/${plan.id}/activate`, {}))
      await step('plan veliye aç', () => c.patch(`/education-plans/${plan.id}/guardian-visibility`, { visible: true }))
      await step('gözden geçirme', () => c.post(`/education-plans/${plan.id}/reviews`, { reviewedOn: dayStr(-10), reviewerId: ids.educators[s.idx % 6].id, summary: 'Ara değerlendirme: hedeflerde olumlu ilerleme.', outcome: 'on_track' }))
    }
  }
  // Başarıya ulaşmış bir hedef
  info(`BEP: ${made.plans} plan, ${made.goals} hedef, ${made.progress} ilerleme kaydı`)
}

async function assessments(ctx) {
  const { c, ids } = ctx
  const tcode = `DEMO-GELISIM-${ctx.code.slice(-1).toUpperCase()}`
  const tpl = await step('değerlendirme şablonu', async () => {
    let t = (await c.list('/assessment-templates')).find((x) => x.code === tcode)
    if (!t) t = await c.post('/assessment-templates', { code: tcode, name: 'Gelişimsel Tarama Formu (Demo)', typeId: await ctx.ref('assessment_type', 'developmental'),
      categoryId: await ctx.ref('assessment_category', 'language'), scoringModel: 'sum', translations: [{ locale: 'tr', name: 'Gelişimsel Tarama Formu (Demo)', description: 'Demo veri' }] })
    let full = await c.get(`/assessment-templates/${t.id}`)
    for (const [si, [sc, area]] of [['DIL', 'language'], ['SOSYAL', 'social_emotional']].entries()) {
      let sec = full.sections.find((x) => x.code === sc)
      if (!sec) { full = await c.post(`/assessment-templates/${t.id}/sections`, { code: sc, sortOrder: si + 1, developmentAreaId: await ctx.ref('development_area', area) }); sec = full.sections.find((x) => x.code === sc) }
      for (let k = 1; k <= 3; k++) if (!(sec.items ?? []).some((i) => i.code === `${sc}-${k}`))
        await c.post(`/assessment-templates/sections/${sec.id}/items`, { code: `${sc}-${k}`, prompt: `${sc === 'DIL' ? 'Dil' : 'Sosyal'} becerisi maddesi ${k}`, responseType: 'numeric', choices: null, weight: 1, sortOrder: k })
    }
    full = await c.get(`/assessment-templates/${t.id}`)
    full.itemIds = full.sections.flatMap((x) => x.items.map((i) => i.id))
    return full
  })
  if (!tpl?.id) return
  ids.programs ||= (await c.list('/programs')).map((p) => ({ id: p.id, code: p.code })).sort((a, b) => ['ABA', 'DIL', 'SOS'].indexOf(a.code) - ['ABA', 'DIL', 'SOS'].indexOf(b.code))
  const assessor = ids.educators[3].id    // psikolog
  const mk = async (student, days, flow) => step(`değerlendirme ${student.no}`, async () => {
    let sdto = await c.post('/assessment-sessions', { templateId: tpl.id, leadId: null, studentId: student.id, campusId: student.campusId, assessorId: assessor, scheduledAt: new Date(Date.now() + days * 864e5).toISOString() })
    const id = sdto.id; let rv = sdto.rowVersion
    if (flow >= 1) { sdto = await c.post(`/assessment-sessions/${id}/start`, { rowVersion: rv }); rv = sdto.rowVersion }
    if (flow >= 2) {
      await c.post(`/assessment-sessions/${id}/responses`, { responses: tpl.itemIds.map((itemId, i) => ({ itemId, numericValue: 2 + (i % 3), textValue: null, choiceValue: null, note: null })) })
      sdto = await c.post(`/assessment-sessions/${id}/complete`, { rowVersion: rv }); rv = sdto.rowVersion
      const rep = await c.post(`/assessment-sessions/${id}/report`, { summary: 'Dil alanında yaş düzeyinin altında, sosyal alanda yaşıtlarına yakın performans.', findings: 'İfade edici dilde 2 sözcüklü cümle; yönerge alma 1 basamak.', fileId: null })
      if (flow >= 3) await step('rapor kesinleştir', () => c.post(`/assessment-sessions/${id}/report/finalize`, { reportId: rep.id, rowVersion: rep.rowVersion }))
      await c.post(`/assessment-sessions/${id}/recommendations`, { leadId: null, studentId: student.id, recommendedProgramId: ids.programs[1].id, recommendedIntensity: 'haftada 2 seans', rationale: 'Dil ve konuşma terapisi önerilir.', recommendedBy: assessor })
    }
    return id
  })
  await mk(ids.students[0], -20, 3)   // tamamlanmış + kesinleşmiş rapor
  await mk(ids.students[3], -6, 2)    // tamamlanmış, rapor taslak
  await mk(ids.students[5], 0, 1)     // başlamış
  await mk(ids.students[7], 5, 0)     // planlanmış
  info('değerlendirme: 1 şablon, 4 oturum')
}
