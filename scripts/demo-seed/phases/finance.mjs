// Finans: paket tanımları, öğrenci paketleri, kredi hareketleri, faturalar, ödemeler (farklı yöntem, biri bekleyen), burs, promosyon, indirim, iade.
import { step, knownBug, info, dayStr, count } from '../lib.mjs'

export async function setupFinance(ctx) {
  const { c, code, ids } = ctx
  const pk = code.slice(-1).toUpperCase()
  ids.programs ||= (await c.list('/programs')).map((p) => ({ id: p.id, code: p.code }))
  if ((await count(c, '/payments/invoices')) > 0) { info('faturalar zaten var — atlandı'); return }
  const made = { defs: 0, pkgs: 0, inv: 0, pay: 0, ledger: 0 }
  // ── paket tanımları ──
  const defs = []
  for (const [dc, name, price, credits, days, type] of [[`P8-${pk}`, '8 Seanslık Paket', 6400, 8, 90, 'session_package'], [`P16-${pk}`, '16 Seanslık Paket', 11200, 16, 150, 'session_package'], [`P24-${pk}`, '24 Seanslık Paket', 15600, 24, 210, 'credit_package']]) {
    const d = await step(`paket tanımı ${dc}`, async () => c.post('/payments/package-definitions', { code: dc, name, listPrice: price, packageTypeId: await ctx.ref('package_type', type), programId: ids.programs[0]?.id ?? null, totalCredits: credits, validityDays: days, currency: 'TRY' }))
    if (d?.id) { defs.push({ ...d, price, credits, days }); made.defs++ }
  }
  if (!defs.length) return
  // ── öğrenci paketleri (ilk 8 öğrenci) + faturalar + ödemeler ──
  const methods = ['cash', 'credit_card', 'bank_transfer', 'installment']
  let n = 0
  const payments = []
  for (const s of ids.students.slice(0, 8)) {
    const def = defs[s.idx % defs.length]
    const sp = await step(`öğrenci paketi ${s.no}`, () => c.post('/payments/packages', { studentId: s.id, totalCredits: def.credits, price: def.price, packageDefinitionId: def.id, expiresOn: dayStr(def.days - 30), currency: 'TRY' }))
    if (!sp?.id) continue
    made.pkgs++
    // kredi hareketleri: tüketim (geçmiş seanslar için), bonus, düzeltme
    const used = 2 + (s.idx % 4)
    for (let k = 0; k < used; k++) { await step('kredi tüketimi', () => c.post('/payments/credits/consume', { studentPackageId: sp.id, amount: 1, sessionId: ids.pastSessions?.find((p) => p.studentId === s.id)?.id ?? null, reason: 'Seans katılımı' })); made.ledger++ }
    if (s.idx === 1) { await step('bonus kredi', () => c.post('/payments/credits/grant', { studentPackageId: sp.id, amount: 2, reason: 'Sadakat bonusu (demo)' })); made.ledger++ }
    if (s.idx === 2) { await step('kredi düzeltme', () => c.post('/payments/credits/adjust', { studentPackageId: sp.id, delta: -1, reason: 'Yanlış düşüm düzeltmesi (demo)' })); made.ledger++ }
    if (s.idx === 3) { await step('kredi iadesi', () => c.post('/payments/credits/refund', { studentPackageId: sp.id, amount: 1, reason: 'İptal edilen seans için iade (demo)' })); made.ledger++ }
    // fatura
    const guardian = s.guardianIds[0]
    const inv = await step(`fatura ${s.no}`, () => c.post('/payments/invoices', { issueDate: dayStr(-40 + s.idx * 3), currency: 'TRY', studentId: s.id, guardianId: guardian, dueDate: dayStr(-25 + s.idx * 3) }))
    if (!inv?.id) continue
    made.inv++
    // Satır → (taslak değilse) kes → (iptal ise) iptal; tümü API ile. Toplam/durum/numara backend'de hesaplanır.
    const invNo = s.idx === 7 ? null : `${pk}F-2026-${String(++n).padStart(4, '0')}`
    let invDto = await step(`fatura satırı ${s.no}`, () => c.post(`/payments/invoices/${inv.id}/lines`, { description: def.name, unitPrice: def.price, quantity: 1, studentPackageId: sp.id, sortOrder: 1 }))
    if (!invDto) continue
    if (s.idx !== 7) invDto = await step(`fatura kes ${s.no}`, () => c.post(`/payments/invoices/${inv.id}/issue`, { invoiceNo: invNo, rowVersion: invDto.rowVersion }))
    if (s.idx === 6 && invDto) await step(`fatura iptal ${s.no}`, () => c.post(`/payments/invoices/${inv.id}/void`, { reason: "Demo: hatalı düzenlenen fatura iptal edildi.", rowVersion: invDto.rowVersion }))
    if (s.idx === 7 || s.idx === 6) continue
    // ödemeler: idx 0-2 tam, 3-4 kısmi, 5 bekleyen (pending kalır)
    const amount = s.idx <= 2 ? def.price : s.idx <= 4 ? Math.round(def.price / 2) : def.price
    const pay = await step(`ödeme ${s.no}`, async () => c.post('/payments/transactions', { amount, currency: 'TRY', invoiceId: inv.id, studentId: s.id, paymentMethodId: await ctx.ref('payment_method', methods[s.idx % 4]),
      gatewayProviderId: null, gatewayReference: `DEMO-${pk}-${s.idx}`, idempotencyKey: `demo-${code}-pay-${s.idx}`, paidAt: new Date(Date.now() - (30 - s.idx * 2) * 864e5).toISOString() }))
    if (pay?.id) { made.pay++; payments.push({ ...pay, idx: s.idx }) }
  }
  // ödeme onayı (capture): bekleyen (idx 5) hariç hepsi; fatura durumu (kısmi/ödenmiş) capture ile backend'de güncellenir
  for (const p of payments) if (p.idx !== 5) await step(`ödeme onayı #${p.idx}`, () => c.post(`/payments/transactions/${p.id}/capture`, { gatewayReference: null, paidAt: null, rowVersion: p.rowVersion }))
  const p1 = payments.find((p) => p.idx === 1)
  if (p1) await knownBug('iade talebi/işleme', async () => { const r = await c.post('/payments/refunds', { paymentId: p1.id, amount: 500, reason: 'Hizmet bedeli düzeltmesi (demo)' }); await c.post(`/payments/refunds/${r.id}/process`, {}) })
  // ── burs, promosyon, indirim ──
  await step('burs', async () => c.post('/payments/scholarships', { studentId: ids.students[9].id, scholarshipTypeId: await ctx.ref('scholarship_type', 'need_based'), percentage: 25, amount: null, validFrom: '2026-09-01', validTo: '2027-06-30', note: 'İhtiyaç bursu (demo)' }))
  const promo = await step('promosyon', () => c.post('/payments/promotions', { code: `DEMO-YAZ-${pk}`, name: 'Demo Erken Kayıt İndirimi', value: 10, isPercentage: true, validFrom: dayStr(-30), validTo: dayStr(60), maxRedemptions: 50 }))
  if (promo?.id) await step('promosyon aktif', () => c.post(`/payments/promotions/${promo.id}/activate`, {}))
  const draftInv = (await c.list('/payments/invoices')).find((i) => i.status === 'draft')
  if (draftInv) await step('indirim (taslak faturaya)', async () => c.post('/payments/discounts', { value: 10, isPercentage: true, invoiceId: draftInv.id, studentPackageId: null, discountTypeId: await ctx.ref('discount_type', 'early_bird'), reason: 'Erken kayıt indirimi (demo)' }))
  info(`finans: ${made.defs} paket tanımı, ${made.pkgs} öğrenci paketi, ${made.inv} fatura, ${made.pay} ödeme, ${made.ledger} kredi hareketi`)
}
