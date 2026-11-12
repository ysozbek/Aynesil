// Hukuk: sözleşme + rıza şablonları, öğrenci sözleşmeleri (taslak/gönderilmiş/imzalı/aktif), rızalar (kamera dahil).
import { step, info, dayStr, count } from '../lib.mjs'

export async function setupLegal(ctx) {
  const { c, code, t, ids } = ctx
  const pk = code.slice(-1).toUpperCase()
  if ((await count(c, '/legal/contracts')) > 0) { info('sözleşmeler zaten var — atlandı'); return }
  const BODY = `Bu belge ${t.name} ile veli arasında, özel eğitim hizmetinin kapsamını ve karşılıklı yükümlülükleri düzenleyen DEMO metnidir. Gerçek bir hukuki belge değildir.`
  const tpl = async (path, mk) => (await c.list(path)).find((x) => x.code === mk.code) ?? await c.post(path, mk)
  const ct = await step('sözleşme şablonu', async () => tpl('/legal/contract-templates', { code: `kayit_${pk.toLowerCase()}`, contractTypeId: await ctx.ref('contract_type', 'enrollment_contract'), effectiveFrom: '2026-01-01',
    translations: [{ locale: 'tr', title: 'Eğitim Hizmeti Kayıt Sözleşmesi (Demo)', body: BODY }] }))
  await step('sözleşme şablonu 2', async () => tpl('/legal/contract-templates', { code: `kvkk_${pk.toLowerCase()}`, contractTypeId: await ctx.ref('contract_type', 'kvkk_disclosure'), effectiveFrom: '2026-01-01',
    translations: [{ locale: 'tr', title: 'KVKK Aydınlatma Metni (Demo)', body: 'Kişisel verileriniz yalnızca hizmet sunumu amacıyla işlenir. (Demo metin)' }] }))
  const consents = {}
  for (const [ctype, mand] of [['data_processing', true], ['camera_viewing', false], ['media_release', false]]) {
    consents[ctype] = await step(`rıza şablonu ${ctype}`, async () => tpl('/legal/consent-templates', { code: `${ctype}_${pk.toLowerCase()}`, consentTypeId: await ctx.ref('consent_type', ctype), isMandatory: mand, effectiveFrom: '2026-01-01',
      translations: [{ locale: 'tr', title: { data_processing: 'Veri İşleme Rızası', camera_viewing: 'Kamera İzleme Rızası', media_release: 'Görsel Paylaşım Rızası' }[ctype] + ' (Demo)', body: 'Demo rıza metni.' }] }))
  }
  // ── öğrenci sözleşmeleri ──
  if (ct?.id) for (const s of ids.students.slice(0, 8)) await step(`sözleşme ${s.no}`, async () => {
    const k = await c.post('/legal/contracts', { studentId: s.id, templateId: ct.id, guardianId: s.guardianIds[0], startsOn: '2026-09-14', endsOn: '2027-06-30' })
    if (s.idx <= 1) return                                   // taslak
    await c.post(`/legal/contracts/${k.id}/send`, {})
    if (s.idx <= 3) return                                   // gönderilmiş
    const g = ids.guardians.find((x) => x.studentId === s.id)
    await c.post(`/legal/contracts/${k.id}/sign`, { signedByName: g?.name ?? 'Veli', signatureMethod: s.idx % 2 ? 'wet' : 'e_sign', signatureRef: `DEMO-SIG-${s.idx}`, signedFileId: null })
    if (s.idx <= 5) return                                   // imzalı
    await c.post(`/legal/contracts/${k.id}/activate`, {})    // aktif
    if (s.idx === 7) await c.post(`/legal/contracts/${k.id}/terminate`, {})   // feshedilmiş
  })
  // ── rızalar: tüm ilk 10 öğrenci için veri işleme; kamera rızası veli1'in çocukları + 2 öğrenci; biri geri çekilmiş ──
  const grant = (s, type, until) => step(`rıza ${type} ${s.no}`, async () => c.post('/legal/consents/grant', { studentId: s.id, guardianId: (type === 'camera_viewing' && ids.portal?.guardian1?.studentIds.includes(s.id)) ? ids.portal.guardian1.guardianId : s.guardianIds[0], templateId: consents[type]?.id ?? null, consentTypeId: await ctx.ref('consent_type', type), validUntil: until, evidenceFileId: null }))
  for (const s of ids.students.slice(0, 10)) await grant(s, 'data_processing', null)
  for (const i of [0, 1, 2, 4]) await grant(ids.students[i], 'camera_viewing', dayStr(365))
  const w = await grant(ids.students[6], 'media_release', dayStr(365))
  if (w?.id) await step('rıza geri çek', () => c.post(`/legal/consents/${w.id}/withdraw`, {}))
  info('hukuk: 2 sözleşme şablonu, 3 rıza şablonu, 8 sözleşme, rızalar')
}
