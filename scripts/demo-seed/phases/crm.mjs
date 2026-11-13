// CRM: ~20 aday, aşamalar, aktiviteler, görüşmeler, 4 dönüşmüş aday.
import { step, knownBug, info, idOf, at, dayStr, count, trAscii, sqlStep, sql, q } from '../lib.mjs'
import { POOLS } from '../data.mjs'

export async function setupCrm(ctx) {
  const { c, code, t, ids } = ctx
  if ((await count(c, '/leads')) > 0) { info('adaylar zaten var — atlandı'); return }
  const P = POOLS[code]
  const sources = ['website', 'phone', 'social_media', 'referral', 'walk_in']
  const PLAN = ['new', 'new', 'new', 'new', 'contacted', 'contacted', 'contacted', 'contacted', 'qualified', 'qualified', 'qualified',
                'interview', 'interview', 'interview', 'lost', 'lost', 'converted', 'converted', 'converted', 'converted']
  const STAGE = { new: 'prospect', contacted: 'contacted', qualified: 'assessment', interview: 'recommendation', lost: 'contacted', converted: 'enrollment' }
  const NEED = ['Konuşma gecikmesi', 'Dikkat ve odaklanma güçlüğü', 'Sosyal etkileşimde zorlanma', 'Otizm spektrum tanısı sonrası destek', 'Okula uyum hazırlığı', 'İnce motor gelişim desteği']
  const made = { leads: 0, acts: 0, itv: 0, conv: 0 }
  const convStudents = ids.students.slice(11, 15)
  let ci = 0
  for (let i = 0; i < PLAN.length; i++) {
    const target = PLAN[i]
    const converted = target === 'converted'
    const stu = converted ? convStudents[ci++] : null
    const contact = converted ? ids.guardians.find((g) => g.studentId === stu.id)?.name ?? P.lead[i] : P.lead[i]
    const campus = ctx.campuses[i % 2]
    const lead = await step(`aday ${i}`, async () => c.post('/leads', {
      contactName: contact, campusId: campus.id, sourceId: await ctx.ref('lead_source', sources[i % 5]), statusId: await ctx.ref('lead_status', 'new'), pipelineStageId: await ctx.ref('pipeline_stage', 'prospect'),
      childName: converted ? stu.name : `${P.leadChild[i]} ${contact.split(' ').pop()}`, childBirthDate: `20${16 + (i % 5)}-0${1 + (i % 9)}-1${i % 9}`,
      contactPhone: null, contactEmail: `${trAscii(contact)}@aday.${t.domain}`, presentingNeed: NEED[i % NEED.length], referralDetail: sources[i % 5] === 'referral' ? 'Eski veli tavsiyesi' : null, assignedToId: null, score: 40 + ((i * 7) % 55) }))
    if (!lead?.id) continue
    made.leads++
    let rv = lead.rowVersion
    const move = async (status) => {
      const d = await c.post(`/leads/${lead.id}/status`, { newStatusId: await ctx.ref('lead_status', status), newPipelineStageId: await ctx.ref('pipeline_stage', STAGE[status] ?? 'prospect'), rowVersion: rv })
      rv = d?.rowVersion ?? rv + 1
    }
    const path = { new: [], contacted: ['contacted'], qualified: ['contacted', 'qualified'], interview: ['contacted', 'qualified', 'interview'],
      lost: ['contacted', 'lost'], converted: ['contacted', 'qualified', 'interview'] }[target]
    for (const st of path) await step(`aday durum ${st}`, () => move(st))
    if (i % 3 === 0 && ids.users.coordinator) await step('aday ata', async () => { const d = await c.post(`/leads/${lead.id}/assign`, { userId: ids.users.coordinator, rowVersion: rv }); rv = d?.rowVersion ?? rv + 1 })
    // aktiviteler
    if (target !== 'new' || i < 2) {
      const n = target === 'new' ? 1 : 2 + (i % 2)
      for (let k = 0; k < n; k++) {
        await step('aktivite', () => c.post(`/leads/${lead.id}/activities`, { activityTypeId: null, subject: ['İlk telefon görüşmesi', 'Bilgilendirme e-postası', 'Kurum ziyareti', 'Takip araması'][k % 4],
          body: 'Demo aktivite kaydı.', direction: k % 2 ? 'inbound' : 'outbound', occurredAt: at(-(20 - i - k * 3), 11), followUpAt: k === n - 1 && target !== 'lost' ? at(3 + i % 5, 10) : null, performedBy: null }))
        made.acts++
      }
    }
    // görüşmeler
    if (['interview', 'converted', 'qualified'].includes(target)) {
      const itvDays = target === 'interview' ? (i % 2 ? 3 : -4) : -9
      const itv = await step('görüşme', () => c.post(`/leads/${lead.id}/interviews`, { campusId: campus.id, scheduledAt: at(itvDays < 0 ? 1 : itvDays, 14) }))
      if (itv?.id) {
        made.itv++
        // Yeni akış örneği: gelecekteki görüşmelerden birini ertele (POST /interviews/{id}/reschedule)
        if (itvDays > 0 && i % 4 === 1) await step('görüşme ertele', () => c.post(`/interviews/${itv.id}/reschedule`, { newScheduledAt: at(itvDays + 2, 15), rowVersion: itv.rowVersion }))
        if (itvDays < 0) {
          // API geçmiş tarihli görüşme planlamayı reddediyor → planlandıktan sonra tarih SQL ile geçmişe çekilir
          sqlStep('crm.interview.scheduled_at (geçmiş tarihli görüşmeler; API gelecek tarih şartı koyuyor)', `update crm.interview set scheduled_at=${q(at(itvDays, 14))} where id=${q(itv.id)}`)
          itv.rowVersion = Number(sql(`select row_version from crm.interview where id=${q(itv.id)}`))
          if (i % 5 === 0 && target !== 'converted') await step('görüşme gelmedi', () => c.post(`/interviews/${itv.id}/no-show`, { rowVersion: itv.rowVersion }))
          else await step('görüşme tamamla', () => c.post(`/interviews/${itv.id}/complete`, { outcome: 'positive', recommendation: 'Bireysel değerlendirme ve program planı önerildi.', conductedBy: ids.users.coordinator, rowVersion: itv.rowVersion }))
        }
      }
    }
    if (converted) { await step('aday dönüştür', async () => { await c.post(`/leads/${lead.id}/convert`, { studentId: stu.id, rowVersion: rv }); made.conv++ }) }
  }
  info(`CRM: ${made.leads} aday, ${made.acts} aktivite, ${made.itv} görüşme, ${made.conv} dönüşüm`)
}
