#!/usr/bin/env node
/**
 * AyNesil API duman testi.
 *   1) fixtures.json'daki yönetici ile giriş yapar
 *   2) hazırlık: kampüs, referans değer, öğrenci, eğitmen, derslik oluşturur
 *   3) yazma akışları: kamp + dönem, toplantı oluştur + güncelle, seans oluştur
 *   4) endpoints.json'daki (extract-endpoints.mjs) tüm GET çağrılarını gerçek API'ye gönderir,
 *      HTTP durumunu ve yanıtın frontend TS tipindeki alanlarına uyumunu denetler
 * Çıktı: konsol özeti + out/report.json + out/report.md.   Hata düzeltmez, yalnızca raporlar.
 * Kullanım: node scripts/smoke-api/run.mjs [--only <ön ek>] [--strict]
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { execFileSync } from 'node:child_process'

const here = path.dirname(fileURLToPath(import.meta.url))
const fx = JSON.parse(fs.readFileSync(path.join(here, 'fixtures.json'), 'utf8'))
const endpoints = JSON.parse(fs.readFileSync(path.join(here, 'endpoints.json'), 'utf8'))
const API = (process.env.SMOKE_API || fx.apiBase).replace(/\/$/, '')
const args = process.argv.slice(2)
const only = args.includes('--only') ? args[args.indexOf('--only') + 1] : null
const strict = args.includes('--strict')
const byId = Object.fromEntries(endpoints.map(e => [`${e.verb}:${e.id}`, e]))
const RUN = Date.now().toString(36).toUpperCase()

let token = null
const results = []   // { phase, id, verb, url, status, outcome, note, mismatches }

// ── HTTP ────────────────────────────────────────────────────────────────────
async function http(method, p, body) {
  const t0 = Date.now()
  try {
    const res = await fetch(API + p, {
      method,
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(30_000),
    })
    const text = await res.text()
    let json = null
    try { json = text ? JSON.parse(text) : null } catch { /* gövde JSON değil */ }
    return { status: res.status, json, text, ms: Date.now() - t0, retryAfter: res.headers.get('retry-after') }
  } catch (e) {
    return { status: 0, json: null, text: String(e?.message || e), ms: Date.now() - t0, netErr: true }
  }
}

// ── Şema doğrulama (extract-endpoints.mjs şemasına karşı) ──────────────────
const typeOf = (v) => (v === null ? 'null' : Array.isArray(v) ? 'array' : typeof v)
function validate(schema, value, p, errs, stats) {
  if (!schema || schema.kind === 'any') return
  if (value === undefined) { if (!schema.optional) errs.add(`${p}: alan yok (zorunlu)`); return }
  if (value === null) {
    if (!schema.nullable && !schema.optional) errs.add(`${p}: null geldi, tip null kabul etmiyor`)
    return
  }
  switch (schema.kind) {
    case 'string': case 'number': case 'boolean':
      if (typeof value !== schema.kind) errs.add(`${p}: ${schema.kind} bekleniyor, ${typeOf(value)} geldi`)
      return
    case 'array':
      if (!Array.isArray(value)) { errs.add(`${p}: dizi bekleniyor, ${typeOf(value)} geldi`); return }
      if (!value.length) stats.emptyArrays++
      value.slice(0, 3).forEach(it => validate(schema.item, it, `${p}[]`, errs, stats))
      return
    case 'map':
      if (typeOf(value) !== 'object') errs.add(`${p}: nesne bekleniyor, ${typeOf(value)} geldi`)
      return
    case 'object':
      if (typeOf(value) !== 'object') { errs.add(`${p}: nesne bekleniyor, ${typeOf(value)} geldi`); return }
      for (const [k, s] of Object.entries(schema.props || {})) validate(s, value[k], p ? `${p}.${k}` : k, errs, stats)
  }
}
function checkAgainst(ep, res) {
  // zarf + veri tipi. Dönüş: { mismatches, emptyNote }
  const mism = []
  const b = res.json
  if (res.status === 204) return { mismatches: mism }
  if (!b || typeof b !== 'object' || typeof b.success !== 'boolean') {
    mism.push('yanıt ApiResponse zarfı değil (success alanı yok)')
    return { mismatches: mism }
  }
  if (b.success !== true) mism.push(`success=false (${b.message ?? ''})`)
  if (ep && ep.typeText && ep.typeText !== 'void' && ep.schema.kind !== 'any') {
    const errs = new Set(); const stats = { emptyArrays: 0 }
    validate(ep.schema, b.data, '', errs, stats)
    mism.push(...[...errs].map(e => e.replace(/^: /, 'data: ').replace(/^(\[\]|\.)/, 'data$1')))
    return { mismatches: mism, emptyNote: stats.emptyArrays ? `${stats.emptyArrays} boş dizi, öğeler doğrulanamadı` : undefined }
  }
  return { mismatches: mism }
}

function record(r) { results.push(r); return r }
function classify(status) { return status === 0 ? 'neterr' : status >= 500 ? '5xx' : status >= 400 ? '4xx' : 'http-ok' }
function errNote(res) {
  if ((res.status === 404 && !res.json) || res.status === 405) return `ROTA YOK: backend bu yolu/fiili tanımıyor (HTTP ${res.status}, gövdesiz yanıt)`
  const j = res.json
  const msg = j?.message || j?.title || (j?.errors ? JSON.stringify(j.errors) : '') || res.text.slice(0, 200)
  return `${msg}`.slice(0, 300)
}
const idOf = (data) => (typeof data === 'string' ? data : data?.id)

// ── GET taraması: yol parametresi çözümü ───────────────────────────────────
class Skip extends Error {}
const listCache = new Map()
const known = {}  // yazma akışlarında oluşan kimlikler: campId, periodId ...
async function firstId(listPath) {
  if (!listCache.has(listPath)) {
    const sep = listPath.includes('?') ? '&' : '?'
    const res = await http('GET', `${listPath}${sep}page=1&pageSize=20`)
    const d = res.json?.data
    const arr = Array.isArray(d) ? d : Array.isArray(d?.items) ? d.items : []
    listCache.set(listPath, { status: res.status, id: arr.map(x => x?.id ?? x?.studentId).find(Boolean), empty: !arr.length })
  }
  const c = listCache.get(listPath)
  if (!c.id) throw new Skip(c.status >= 400 ? `kaynak liste ${listPath} → HTTP ${c.status}` : `kaynak liste ${listPath} boş (veri yok)`)
  return c.id
}
let paramOverride = {}
async function resolveParam(ep, expr) {
  const name = expr.split('.').pop()
  if (paramOverride[name]) return firstId(paramOverride[name])
  if (fx.pathParamDefaults[name] !== undefined) return fx.pathParamDefaults[name]
  if (known[name]) return known[name]
  const fileBase = ep.file.split('/').pop().split(':')[0].replace(/\.ts$/, '')
  const bySvc = fx.paramSourcesByFile?.[name]?.[fileBase]
  if (bySvc) return firstId(bySvc)
  if (fx.paramSources[name]) return firstId(fx.paramSources[name])
  if (name === 'id') {
    const prefix = ep.path.slice(0, ep.path.indexOf('{{')).replace(/\/$/, '')
    return firstId(prefix)
  }
  throw new Skip(`yol parametresi '${name}' için kaynak tanımlı değil`)
}
async function buildUrl(ep) {
  let u = ep.path
  const override = fx.queryOverrides?.[ep.id]
  const isPaged = ep.schema?.props && 'totalCount' in ep.schema.props
  u = u.replace('{{QS}}', override ? `?${override}` : isPaged ? `?${fx.queryDefaults._paged}` : '')
  if (override && !ep.hasQs) u += (u.includes('?') ? '&' : '?') + override
  u = u.replace(/\{\{today(?:-(\d+))?\}\}/g, (_, n) => day(-(Number(n) || 0)))
  const params = [...u.matchAll(/\{\{(.+?)\}\}/g)]
  for (const m of params) u = u.replace(m[0], encodeURIComponent(await resolveParam(ep, m[1])))
  return u
}

// ── Fazlar ──────────────────────────────────────────────────────────────────
async function login(username = fx.admin.username, password = fx.admin.password, phase = 'giriş') {
  let res = await http('POST', '/auth/login', { username, password })
  // Giriş IP + kullanıcı başına hız sınırlı (10/dk); demo-seed hemen önce aynı IP'den giriş yapmış olabilir.
  for (let i = 0; i < 3 && res.status === 429; i++) {
    console.log(`  (giriş hız sınırı: ${res.retryAfter || 60} sn bekleniyor)`)
    await new Promise((ok) => setTimeout(ok, (Number(res.retryAfter) || 60) * 1000 + 250))
    res = await http('POST', '/auth/login', { username, password })
  }
  const ep = byId['post:auth.login'] // auth.service yoksa frontend tipi store'dadır; zarf kontrolü yeterli
  const { mismatches } = checkAgainst(ep, res)
  const d = res.json?.data
  for (const k of ['accessToken', 'refreshToken', 'expiresAt', 'fullName', 'userId', 'corporationId'])
    if (typeof d?.[k] !== 'string') mismatches.push(`data.${k}: string bekleniyor (LoginResult)`)
  record({ phase, id: `auth.login (${username})`, verb: 'POST', url: '/auth/login', status: res.status,
    outcome: res.status >= 400 || res.status === 0 ? classify(res.status) : mismatches.length ? 'type' : 'ok',
    note: res.status >= 400 || res.status === 0 ? errNote(res) : '', mismatches })
  if (!d?.accessToken) { console.error('Giriş başarısız:', res.status, errNote(res)); await finish(true) }
  token = d.accessToken
  return d
}

/** Yazma çağrısı: durum + frontend'in tanımladığı yanıt tipi. */
async function write(phase, id, verb, urlPath, body, epKey) {
  const res = await http(verb, urlPath, body)
  const ep = epKey ? byId[epKey] : null
  let outcome = classify(res.status), note = '', mismatches = []
  if (outcome === 'http-ok') {
    const c = checkAgainst(ep, res)
    mismatches = c.mismatches
    outcome = mismatches.length ? 'type' : 'ok'
    if (outcome === 'type' && ep) note = `frontend tipi: ${ep.typeText}; gerçek yanıt: ${typeOf(res.json?.data)}`
  } else note = errNote(res)
  record({ phase, id, verb, url: urlPath, status: res.status, outcome, note, mismatches })
  return res
}

const iso = (d) => d.toISOString()
const day = (n) => { const d = new Date(Date.now() + n * 864e5); return d.toISOString().slice(0, 10) }
const at = (n, h) => { const d = new Date(Date.now() + n * 864e5); d.setUTCHours(h, 0, 0, 0); return iso(d) }
const ctx = {}

async function setup() {
  const camp = await http('GET', '/campuses?page=1&pageSize=5')
  ctx.campusId = camp.json?.data?.items?.[0]?.id
  const st = await http('GET', '/reference-data/session_type?activeOnly=true')
  ctx.sessionTypeId = st.json?.data?.[0]?.id
  if (!ctx.campusId) console.warn('! kampüs bulunamadı')
  if (!ctx.sessionTypeId) console.warn('! session_type referans değeri bulunamadı')

  let r = await write('hazırlık', 'student.createStudent', 'POST', '/students', {
    firstName: 'Smoke', lastName: `Öğrenci${RUN}`, studentNo: `SM-${RUN}`, nationalId: null, birthDate: '2016-05-01',
    gender: null, primaryCampusId: ctx.campusId ?? null, statusId: null, leadId: null, notes: 'smoke-api' }, 'post:student.createStudent')
  ctx.studentId = idOf(r.json?.data)
  r = await write('hazırlık', 'educator.createEducator', 'POST', '/educators', {
    firstName: 'Smoke', lastName: `Eğitmen${RUN}`, titleId: null, email: null, phone: null, employmentType: null,
    hireDate: null, primaryCampusId: ctx.campusId ?? null }, 'post:educator.createEducator')
  ctx.educatorId = idOf(r.json?.data)
  r = await write('hazırlık', 'scheduling.createRoom', 'POST', '/scheduling/rooms', {
    campusId: ctx.campusId, code: `R-${RUN}`, name: `Smoke Derslik ${RUN}`, capacity: 4, isVirtual: false }, 'post:scheduling.createRoom')
  ctx.roomId = idOf(r.json?.data)
  r = await write('hazırlık', 'program.createProgram', 'POST', '/programs', {
    code: `PRG-${RUN}`, name: `Smoke Program ${RUN}`, programTypeId: null, description: 'smoke-api' }, 'post:program.createProgram')
  ctx.programId = idOf(r.json?.data)
  r = await write('hazırlık', 'lead.create', 'POST', '/leads', {
    contactName: `Smoke Aday ${RUN}`, campusId: ctx.campusId, childName: 'Smoke Çocuk', contactPhone: '5550000000',
    contactEmail: `smoke-${RUN.toLowerCase()}@aynesil.test`, presentingNeed: 'smoke-api' }, 'post:lead.create')
  ctx.leadId = idOf(r.json?.data)
}

// ── Ekransız uçları kapatan akışlar (yazma): must_change_password, şablon sürümleri, rıza, telafi, iade, izin bakiyesi, KPI tanımı, sözleşme ──
/** Beklenen durum/kod denetimi: sapma `leak` (yetki/koruma delinmesi) ya da 4xx/5xx olarak raporlanır. */
function expectStatus(phase, id, verb, urlPath, res, statuses, code) {
  const okStatus = statuses.includes(res.status)
  const okCode = !code || res.json?.code === code
  record({ phase, id, verb, url: urlPath, status: res.status,
    outcome: okStatus && okCode ? 'ok' : res.status < 300 ? 'leak' : res.status >= 500 || res.status === 0 ? '5xx' : '4xx',
    note: okStatus && okCode ? `beklenen ${statuses.join('/')}${code ? ' ' + code : ''}` : `beklenen ${statuses.join('/')}${code ? ' ' + code : ''}; gelen ${res.status} ${res.json?.code ?? ''}`, mismatches: [] })
}

async function newFlows() {
  const tag = RUN.toLowerCase()
  const adminToken = token
  const ref = async (type) => (await http('GET', `/reference-data/${type}?activeOnly=true`)).json?.data ?? []

  // 1) must_change_password: yönetici açtığı hesap parola değişene kadar yalnızca parola değiştirme/çıkış uçlarını kullanabilir
  const mustUser = `smoke.mcp.${tag}@demo-a.test`, tempPw = 'GeciciSifre!2026', newPw = 'YeniSifre!2026x'
  let r = await write('yazma', 'user.create (must_change_password)', 'POST', '/users', {
    username: mustUser, fullName: `Smoke İlk Giriş ${RUN}`, email: mustUser, phone: null, password: tempPw, preferredLocale: 'tr',
    primaryCampusId: ctx.campusId ?? null, activateImmediately: true })
  if (r.status && r.status < 300) {
    const login1 = await http('POST', '/auth/login', { username: mustUser, password: tempPw })
    const flagged = login1.json?.data?.mustChangePassword === true
    record({ phase: 'yazma', id: 'auth.login (must_change_password bayrağı açık)', verb: 'POST', url: '/auth/login', status: login1.status,
      outcome: login1.status === 200 && flagged ? 'ok' : 'type', note: flagged ? 'beklenen: mustChangePassword=true' : 'mustChangePassword=true bekleniyordu', mismatches: [] })
    token = login1.json?.data?.accessToken
    for (const [verb, p] of [['GET', '/students?page=1&pageSize=1'], ['GET', '/menus/me'], ['GET', '/campuses?page=1&pageSize=1'], ['GET', '/settings']])
      expectStatus('yazma', `must_change_password: ${p.split('?')[0]} reddedilir`, verb, p, await http(verb, p), [403], 'Auth.PasswordChangeRequired')
    expectStatus('yazma', 'auth.changePassword (must_change_password açıkken izinli)', 'POST', '/auth/change-password',
      await http('POST', '/auth/change-password', { currentPassword: tempPw, newPassword: newPw }), [200, 204])
    const login2 = await http('POST', '/auth/login', { username: mustUser, password: newPw })
    token = login2.json?.data?.accessToken
    record({ phase: 'yazma', id: 'auth.login (parola değişti, bayrak kapandı)', verb: 'POST', url: '/auth/login', status: login2.status,
      outcome: login2.status === 200 && login2.json?.data?.mustChangePassword === false ? 'ok' : 'type', note: 'beklenen: mustChangePassword=false', mismatches: [] })
    expectStatus('yazma', 'must_change_password kapandıktan sonra /menus/me erişilir', 'GET', '/menus/me', await http('GET', '/menus/me'), [200])
    token = adminToken
  } else record({ phase: 'yazma', id: 'must_change_password akışı', verb: 'POST', url: '/users', status: null, outcome: 'skip', note: 'kullanıcı oluşmadı', mismatches: [] })
  token = adminToken
  // şifremi unuttum: geçersiz token → 422 Auth.InvalidResetToken
  expectStatus('yazma', 'auth.confirmPasswordReset (geçersiz token)', 'POST', '/auth/confirm-password-reset',
    await http('POST', '/auth/confirm-password-reset', { token: `gecersiz-${tag}`, newPassword: 'YeniSifre!2026x' }), [422], 'Auth.InvalidResetToken')

  // 2) sözleşme şablonu: oluştur → güncelle → yeni sürüm → sürümler
  const consentTypes = await ref('consent_type'), contractTypes = await ref('contract_type')
  const tplCode = `smoke_tpl_${tag}`
  r = await write('yazma', 'legal.createContractTemplate', 'POST', '/legal/contract-templates', {
    code: tplCode, contractTypeId: contractTypes[0]?.id ?? null, effectiveFrom: day(0),
    translations: [{ locale: 'tr', title: `Smoke Şablon ${RUN}`, body: 'Smoke şablon metni.' }] })
  const tpl = r.json?.data
  if (tpl?.id) {
    await write('yazma', 'legal.updateContractTemplate', 'PUT', `/legal/contract-templates/${tpl.id}`, {
      contractTypeId: contractTypes[0]?.id ?? null, effectiveFrom: day(0),
      translations: [{ locale: 'tr', title: `Smoke Şablon ${RUN} (güncel)`, body: 'Smoke şablon metni, güncellendi.' }], rowVersion: tpl.rowVersion })
    r = await write('yazma', 'legal.newContractTemplateVersion', 'POST', `/legal/contract-templates/${tpl.id}/new-version`, {
      effectiveFrom: day(1), translations: [{ locale: 'tr', title: `Smoke Şablon ${RUN} v2`, body: 'Smoke şablon metni v2.' }] })
    const v = await http('GET', `/legal/contract-templates/${tplCode}/versions`)
    const versions = v.json?.data ?? []
    record({ phase: 'yazma', id: 'legal.listContractTemplateVersions (v1 arşiv + v2 güncel)', verb: 'GET', url: `/legal/contract-templates/${tplCode}/versions`, status: v.status,
      outcome: v.status === 200 && versions.length === 2 && versions.filter(x => x.isCurrent).length === 1 ? 'ok' : 'type', note: `sürüm sayısı ${versions.length}`, mismatches: [] })
    // 2b) sözleşme: oluştur → gönder → imzala → etkinleştir → süresini bitir (güncel şablonla)
    const cur = r.json?.data
    if (ctx.studentId && cur?.id) {
      const c = await write('yazma', 'legal.createContract', 'POST', '/legal/contracts', { studentId: ctx.studentId, templateId: cur.id, guardianId: null, startsOn: day(0), endsOn: day(200) })
      const cid = idOf(c.json?.data)
      if (cid) {
        await write('yazma', 'legal.sendContract', 'POST', `/legal/contracts/${cid}/send`, {})
        const g = await http('GET', `/legal/contracts/${cid}`)
        await write('yazma', 'legal.signContract', 'POST', `/legal/contracts/${cid}/sign`, { signedByName: 'Smoke Veli', signatureMethod: 'wet', signatureRef: `SMOKE-${RUN}`, signedFileId: null })
        await write('yazma', 'legal.activateContract', 'POST', `/legal/contracts/${cid}/activate`, {})
        await write('yazma', 'legal.expireContract', 'POST', `/legal/contracts/${cid}/expire`, {})
        void g
      }
    }
  } else record({ phase: 'yazma', id: 'legal.updateContractTemplate', verb: 'PUT', url: '/legal/contract-templates/{id}', status: null, outcome: 'skip', note: 'şablon oluşmadı', mismatches: [] })
  // onam şablonu + rıza ver / geri al
  const ctpl = await write('yazma', 'legal.createConsentTemplate', 'POST', '/legal/consent-templates', {
    code: `smoke_consent_${tag}`, consentTypeId: consentTypes.find(x => x.code === 'data_processing')?.id ?? consentTypes[0]?.id ?? null, isMandatory: false, effectiveFrom: day(0),
    translations: [{ locale: 'tr', title: `Smoke Onam ${RUN}`, body: 'Smoke onam metni.' }] })
  const ctplDto = ctpl.json?.data
  if (ctplDto?.id) {
    await write('yazma', 'legal.updateConsentTemplate', 'PUT', `/legal/consent-templates/${ctplDto.id}`, {
      consentTypeId: ctplDto.consentTypeId ?? null, isMandatory: true, effectiveFrom: day(0),
      translations: [{ locale: 'tr', title: `Smoke Onam ${RUN} (güncel)`, body: 'Güncellendi.' }], rowVersion: ctplDto.rowVersion })
    await write('yazma', 'legal.newConsentTemplateVersion', 'POST', `/legal/consent-templates/${ctplDto.id}/new-version`, {
      effectiveFrom: day(1), translations: [{ locale: 'tr', title: `Smoke Onam ${RUN} v2`, body: 'v2.' }] })
  }
  if (ctx.studentId) {
    const g = await write('yazma', 'legal.grantConsent', 'POST', '/legal/consents/grant', {
      studentId: ctx.studentId, guardianId: null, templateId: null, consentTypeId: consentTypes.find(x => x.code === 'media_release')?.id ?? consentTypes[0]?.id, validUntil: day(30), evidenceFileId: null })
    const gid = idOf(g.json?.data)
    if (gid) {
      await write('yazma', 'legal.withdrawConsent', 'POST', `/legal/consents/${gid}/withdraw`, {})
      // geri alınan rıza değişmez: ikinci geri alma reddedilir
      expectStatus('yazma', 'legal.withdrawConsent (zaten geri alınmış → reddedilir)', 'POST', `/legal/consents/${gid}/withdraw`, await http('POST', `/legal/consents/${gid}/withdraw`, {}), [400, 409, 422], 'Rule.ConsentWithdrawNeedsGranted')
    }
  }

  // 3) telafi: talep → onay → seans ata → tamamla
  if (ctx.studentId && ctx.sessionTypeId && ctx.educatorId) {
    const mr = await write('yazma', 'scheduling.createMakeupRequest', 'POST', '/scheduling/makeup-requests', { studentId: ctx.studentId, missedSessionId: null, missedReasonId: null, note: `Smoke telafi ${RUN}`, expiresOn: day(30) })
    const mrDto = mr.json?.data
    if (mrDto?.id) {
      const ap = await write('yazma', 'scheduling.approveMakeupRequest', 'POST', `/scheduling/makeup-requests/${mrDto.id}/approve`, { rowVersion: mrDto.rowVersion })
      const sess = await write('yazma', 'scheduling.createSession (telafi seansı)', 'POST', '/scheduling/sessions', {
        sessionTypeId: ctx.sessionTypeId, startsAt: at(12, 9), endsAt: at(12, 10), campusId: ctx.campusId, roomId: ctx.roomId,
        title: `Smoke Telafi ${RUN}`, isMakeup: true, participantStudentIds: [ctx.studentId], educatorAssignments: [{ educatorId: ctx.educatorId, role: 'lead' }] })
      const sid = idOf(sess.json?.data)
      if (sid && ap.json?.data) {
        const as = await write('yazma', 'scheduling.assignMakeupSession', 'POST', `/scheduling/makeup-requests/${mrDto.id}/assign-session`, { makeupSessionId: sid, rowVersion: ap.json.data.rowVersion })
        if (as.json?.data) await write('yazma', 'scheduling.completeMakeupRequest', 'POST', `/scheduling/makeup-requests/${mrDto.id}/complete`, { rowVersion: as.json.data.rowVersion })
      }
      // başka öğrencinin seansı atanamaz: ikinci bir talep → öğrencisi olmayan seans → reddedilir
      const mr2 = await http('POST', '/scheduling/makeup-requests', { studentId: ctx.studentId, missedSessionId: null, missedReasonId: null, note: `Smoke telafi 2 ${RUN}`, expiresOn: day(30) })
      const mr2Dto = mr2.json?.data
      const foreignSession = (await http('GET', '/scheduling/sessions?page=1&pageSize=50')).json?.data?.items?.find(x => x.participantCount > 0 && x.id !== sid)
      if (mr2Dto?.id && foreignSession) {
        const ap2 = await http('POST', `/scheduling/makeup-requests/${mr2Dto.id}/approve`, { rowVersion: mr2Dto.rowVersion })
        const mine = await http('GET', `/scheduling/calendar/student/${ctx.studentId}?from=${encodeURIComponent(at(-1, 0))}&to=${encodeURIComponent(at(60, 0))}`)
        const notMine = !((mine.json?.data ?? []).some(x => x.id === foreignSession.id))
        if (notMine && ap2.json?.data) expectStatus('yazma', 'scheduling.assignMakeupSession (başka öğrencinin seansı → reddedilir)', 'POST', `/scheduling/makeup-requests/${mr2Dto.id}/assign-session`,
          await http('POST', `/scheduling/makeup-requests/${mr2Dto.id}/assign-session`, { makeupSessionId: foreignSession.id, rowVersion: ap2.json.data.rowVersion }), [400, 409, 422], 'Rule.MakeupSessionStudentMismatch')
      }
    }
  } else record({ phase: 'yazma', id: 'scheduling.assignMakeupSession', verb: 'POST', url: '/scheduling/makeup-requests/{id}/assign-session', status: null, outcome: 'skip', note: 'ön koşul eksik', mismatches: [] })

  // 4) iade: fatura → satır → kes → ödeme → onay → iade talebi → işleme al; ikinci talep → başarısız
  if (ctx.studentId) {
    const inv = await write('yazma', 'finance.createInvoice', 'POST', '/payments/invoices', { issueDate: day(0), currency: 'TRY', studentId: ctx.studentId, guardianId: null, dueDate: day(14) })
    const invDto = inv.json?.data
    if (invDto?.id) {
      const line = await write('yazma', 'finance.addInvoiceLine', 'POST', `/payments/invoices/${invDto.id}/lines`, { description: 'Smoke paket', unitPrice: 1000, quantity: 1, studentPackageId: null, sortOrder: 1 })
      const issued = line.json?.data ? await write('yazma', 'finance.issueInvoice', 'POST', `/payments/invoices/${invDto.id}/issue`, { invoiceNo: `SMK-${RUN}`, rowVersion: line.json.data.rowVersion }) : null
      const method = (await ref('payment_method'))[0]?.id
      if (issued?.json?.data && method) {
        const pay = await write('yazma', 'finance.createTransaction', 'POST', '/payments/transactions', { amount: 1000, currency: 'TRY', invoiceId: invDto.id, studentId: ctx.studentId,
          paymentMethodId: method, gatewayProviderId: null, gatewayReference: `SMK-${RUN}`, idempotencyKey: `smoke-${RUN}`, paidAt: iso(new Date()) })
        const payDto = pay.json?.data
        if (payDto?.id) {
          await write('yazma', 'finance.captureTransaction', 'POST', `/payments/transactions/${payDto.id}/capture`, { gatewayReference: null, paidAt: null, rowVersion: payDto.rowVersion })
          const rf = await write('yazma', 'finance.requestRefund', 'POST', '/payments/refunds', { paymentId: payDto.id, amount: 200, reason: 'Smoke iade (işlenecek)' })
          if (rf.json?.data?.id) await write('yazma', 'finance.processRefund', 'POST', `/payments/refunds/${rf.json.data.id}/process`, {})
          const rf2 = await write('yazma', 'finance.requestRefund (ikinci)', 'POST', '/payments/refunds', { paymentId: payDto.id, amount: 100, reason: 'Smoke iade (başarısız)' })
          if (rf2.json?.data?.id) {
            await write('yazma', 'finance.failRefund', 'POST', `/payments/refunds/${rf2.json.data.id}/fail`, {})
            expectStatus('yazma', 'finance.processRefund (başarısız iade tekrar işlenemez)', 'POST', `/payments/refunds/${rf2.json.data.id}/process`, await http('POST', `/payments/refunds/${rf2.json.data.id}/process`, {}), [400, 409, 422], 'Rule.OnlyPendingRefundProcess')
          }
        }
      }
    }
  }

  // 5) izin bakiyesi tanımla (+ aynı kombinasyon ikinci kez reddedilir)
  const annual = (await ref('leave_type')).find(x => x.code === 'annual') ?? (await ref('leave_type'))[0]
  if (ctx.educatorId && annual) {
    const body = { educatorId: ctx.educatorId, leaveTypeId: annual.id, periodYear: new Date().getFullYear(), entitled: 14, unit: 'day' }
    await write('yazma', 'leave.initializeBalance', 'POST', '/leave/balances', body)
    expectStatus('yazma', 'leave.initializeBalance (aynı eğitmen/tür/yıl → reddedilir)', 'POST', '/leave/balances', await http('POST', '/leave/balances', body), [400, 409, 422], 'Rule.LeaveBalanceExists')
  } else record({ phase: 'yazma', id: 'leave.initializeBalance', verb: 'POST', url: '/leave/balances', status: null, outcome: 'skip', note: 'eğitmen/izin türü yok', mismatches: [] })

  // 6) KPI tanımı: oluştur → güncelle; geçersiz spec reddedilir
  const kc = `smoke.kpi_${tag}`
  const kd = await write('yazma', 'kpi.createDefinition', 'POST', '/performance-kpi/definitions', { code: kc, name: `Smoke KPI ${RUN}`, categoryId: null, unit: '%', spec: '{"source":"smoke"}' })
  if (kd.json?.data?.id) {
    await write('yazma', 'kpi.updateDefinition', 'PUT', `/performance-kpi/definitions/${kd.json.data.id}`, { name: `Smoke KPI ${RUN} (güncel)`, categoryId: null, unit: 'adet', spec: '{"source":"smoke","v":2}', rowVersion: kd.json.data.rowVersion })
    expectStatus('yazma', 'kpi.createDefinition (geçersiz spec → reddedilir)', 'POST', '/performance-kpi/definitions',
      await http('POST', '/performance-kpi/definitions', { code: `${kc}_x`, name: 'x', categoryId: null, unit: null, spec: '[1,2]' }), [400, 409, 422])
  }
  token = adminToken
}

// ── B→A kapanışı: seans notu/hedefi, akademik dönem, taslak sözleşme, modül görünürlüğü ──
async function closureFlows() {
  const tag = RUN.toLowerCase()
  const adminToken = token
  const skip = (id, verb, url, note) => record({ phase: 'yazma', id, verb, url, status: null, outcome: 'skip', note, mismatches: [] })

  // 1) seans: hedef bağla → çıkar; not yaz → düzenle (düzenlendi bilgisi updated_at ile görünür)
  if (ctx.studentId && ctx.sessionTypeId && ctx.educatorId && ctx.roomId) {
    let r = await write('yazma', 'scheduling.createSession (not/hedef)', 'POST', '/scheduling/sessions', {
      sessionTypeId: ctx.sessionTypeId, startsAt: at(20, 9), endsAt: at(20, 10), campusId: ctx.campusId, roomId: ctx.roomId,
      title: `Smoke Not ${RUN}`, isMakeup: false, participantStudentIds: [ctx.studentId],
      educatorAssignments: [{ educatorId: ctx.educatorId, role: 'lead' }] })
    const sid = idOf(r.json?.data)
    r = await write('yazma', 'goals.createStudentGoal (seans hedefi için)', 'POST', '/goals/student-goals', {
      studentId: ctx.studentId, statement: `Smoke hedef ${RUN}`, horizon: 'short_term', templateId: null, categoryId: null,
      developmentAreaId: null, parentGoalId: null, masteryCriteria: null, baseline: null, targetValue: null, startDate: day(0), targetDate: day(60) })
    const gid = idOf(r.json?.data)
    if (sid && gid) {
      await write('yazma', 'scheduling.upsertSessionGoal', 'PUT', `/scheduling/sessions/${sid}/goals/${gid}`, { workedOn: true, progressNote: 'smoke', measuredValue: null })
      await write('yazma', 'scheduling.removeSessionGoal', 'DELETE', `/scheduling/sessions/${sid}/goals/${gid}`)
      const after = await http('GET', `/scheduling/sessions/${sid}`)
      const still = (after.json?.data?.goals ?? []).some(g => g.studentGoalId === gid)
      record({ phase: 'yazma', id: 'scheduling.removeSessionGoal (seanstan çıktı)', verb: 'GET', url: `/scheduling/sessions/${sid}`, status: after.status,
        outcome: !still && after.status === 200 ? 'ok' : 'type', note: still ? 'hedef hâlâ seansta' : 'hedef çıkarıldı', mismatches: [] })
      expectStatus('yazma', 'scheduling.removeSessionGoal (bağlı olmayan hedef → 404)', 'DELETE', `/scheduling/sessions/${sid}/goals/${gid}`,
        await http('DELETE', `/scheduling/sessions/${sid}/goals/${gid}`), [404], 'Rule.GoalNotInSession')
    }
    // not yazmak için oturum açan kullanıcı bir eğitmen kaydına bağlı olmalı (ActingEducator): yönetici geçici bağlanır
    if (sid && ctx.adminUserId) {
      const link = await write('yazma', 'educators.linkUserAccount (not yazarı)', 'PUT', `/educators/${ctx.educatorId}/user-account`, { userId: ctx.adminUserId })
      if (link.status < 300) {
        const n = await write('yazma', 'scheduling.writeNote', 'POST', `/scheduling/sessions/${sid}/notes`, { body: 'Smoke not', parentVisible: true })
        const note = n.json?.data
        if (note?.id) {
          const e = await write('yazma', 'scheduling.editNote (yazan eğitmen)', 'PUT', `/scheduling/sessions/${sid}/notes/${note.id}`, { body: 'Smoke not (düzenlendi)', parentVisible: false, rowVersion: note.rowVersion })
          const list = (await http('GET', `/scheduling/sessions/${sid}`)).json?.data?.notes ?? []
          const got = list.find(x => x.id === note.id)
          const edited = got && got.body === 'Smoke not (düzenlendi)' && got.parentVisible === false && !!got.editedAt
          record({ phase: 'yazma', id: 'scheduling.editNote (kalıcılık + düzenlendi bilgisi)', verb: 'GET', url: `/scheduling/sessions/${sid}`, status: 200,
            outcome: edited ? 'ok' : 'type', note: edited ? 'gövde güncel, editedAt dolu' : 'düzenleme okunamadı', mismatches: [] })
          // eski rowVersion ile ikinci düzenleme reddedilir
          expectStatus('yazma', 'scheduling.editNote (eski rowVersion → reddedilir)', 'PUT', `/scheduling/sessions/${sid}/notes/${note.id}`,
            await http('PUT', `/scheduling/sessions/${sid}/notes/${note.id}`, { body: 'x', parentVisible: false, rowVersion: note.rowVersion }), [400, 409, 422])
          void e
        }
        await http('DELETE', `/educators/${ctx.educatorId}/user-account`)
      }
    } else if (sid) skip('scheduling.editNote', 'PUT', '/scheduling/sessions/{id}/notes/{noteId}', 'yönetici kullanıcı kimliği yok')
  } else skip('scheduling.removeSessionGoal / editNote', 'DELETE', '/scheduling/sessions/{id}/goals/{goalId}', 'ön koşul eksik')

  // 2) akademik dönem: oluştur → düzenle → güncel yap → güncel dönem silinemez → sil
  const terms = (await http('GET', '/reference-data/ACADEMIC_TERM?activeOnly=true')).json?.data ?? []
  const periods = (await http('GET', '/education-plans/academic-periods?page=1&pageSize=200')).json?.data?.items ?? []
  const originalCurrent = periods.find(x => x.isCurrent)
  const p1 = await write('yazma', 'plans.createAcademicPeriod', 'POST', '/education-plans/academic-periods', {
    name: `Smoke Dönem ${RUN}`, startDate: day(400), endDate: day(500), termId: terms[0]?.id ?? null, isCurrent: false })
  const p2 = await write('yazma', 'plans.createAcademicPeriod (2)', 'POST', '/education-plans/academic-periods', {
    name: `Smoke Dönem ${RUN} B`, startDate: day(510), endDate: day(600), termId: null, isCurrent: false })
  const d1 = p1.json?.data, d2 = p2.json?.data
  if (d1?.id) {
    await write('yazma', 'plans.updateAcademicPeriod', 'PUT', `/education-plans/academic-periods/${d1.id}`, {
      name: `Smoke Dönem ${RUN} (güncel)`, startDate: day(400), endDate: day(520), termId: null, rowVersion: d1.rowVersion })
    expectStatus('yazma', 'plans.updateAcademicPeriod (eski rowVersion → reddedilir)', 'PUT', `/education-plans/academic-periods/${d1.id}`,
      await http('PUT', `/education-plans/academic-periods/${d1.id}`, { name: 'x', startDate: day(400), endDate: day(520), termId: null, rowVersion: d1.rowVersion }), [400, 409, 422])
    if (originalCurrent) {
      await write('yazma', 'plans.setCurrentAcademicPeriod', 'POST', `/education-plans/academic-periods/${d1.id}/set-current`)
      expectStatus('yazma', 'plans.deleteAcademicPeriod (güncel dönem → reddedilir)', 'DELETE', `/education-plans/academic-periods/${d1.id}`,
        await http('DELETE', `/education-plans/academic-periods/${d1.id}`), [400, 409, 422], 'Rule.AcademicPeriodCurrentDelete')
      await write('yazma', 'plans.setCurrentAcademicPeriod (eski döneme dön)', 'POST', `/education-plans/academic-periods/${originalCurrent.id}/set-current`)
    } else skip('plans.setCurrentAcademicPeriod', 'POST', '/education-plans/academic-periods/{id}/set-current', 'kurumda güncel dönem yok; demo verisi değiştirilmedi')
    await write('yazma', 'plans.deleteAcademicPeriod', 'DELETE', `/education-plans/academic-periods/${d1.id}`)
  }
  if (d2?.id) await write('yazma', 'plans.deleteAcademicPeriod (2)', 'DELETE', `/education-plans/academic-periods/${d2.id}`)

  // 3) taslak sözleşme: düzenle → sil; gönderilmiş sözleşme düzenlenemez/silinemez
  if (ctx.studentId) {
    const c1 = await write('yazma', 'legal.createContract (taslak düzenleme)', 'POST', '/legal/contracts', { studentId: ctx.studentId, templateId: null, guardianId: null, startsOn: day(0), endsOn: day(100) })
    const k1 = c1.json?.data
    if (k1?.id) {
      await write('yazma', 'legal.updateContract (taslak)', 'PUT', `/legal/contracts/${k1.id}`, { guardianId: null, startsOn: day(1), endsOn: day(120), rowVersion: k1.rowVersion })
      const got = (await http('GET', `/legal/contracts/${k1.id}`)).json?.data
      record({ phase: 'yazma', id: 'legal.updateContract (kalıcılık)', verb: 'GET', url: `/legal/contracts/${k1.id}`, status: 200,
        outcome: got?.startsOn === day(1) && got?.endsOn === day(120) ? 'ok' : 'type', note: 'tarihler güncellendi', mismatches: [] })
      await write('yazma', 'legal.deleteContract (taslak)', 'DELETE', `/legal/contracts/${k1.id}`)
      expectStatus('yazma', 'legal.getContract (silinen taslak → 404)', 'GET', `/legal/contracts/${k1.id}`, await http('GET', `/legal/contracts/${k1.id}`), [404])
    }
    const c2 = await write('yazma', 'legal.createContract (gönderilecek)', 'POST', '/legal/contracts', { studentId: ctx.studentId, templateId: null, guardianId: null, startsOn: day(0), endsOn: day(100) })
    const k2 = c2.json?.data
    if (k2?.id) {
      await write('yazma', 'legal.sendContract (kilit için)', 'POST', `/legal/contracts/${k2.id}/send`, {})
      const sent = (await http('GET', `/legal/contracts/${k2.id}`)).json?.data
      expectStatus('yazma', 'legal.updateContract (gönderilmiş → reddedilir)', 'PUT', `/legal/contracts/${k2.id}`,
        await http('PUT', `/legal/contracts/${k2.id}`, { guardianId: null, startsOn: day(2), endsOn: day(90), rowVersion: sent?.rowVersion ?? k2.rowVersion }), [400, 409, 422], 'Rule.ContractModifyStatus')
      expectStatus('yazma', 'legal.deleteContract (gönderilmiş → reddedilir)', 'DELETE', `/legal/contracts/${k2.id}`,
        await http('DELETE', `/legal/contracts/${k2.id}`), [400, 409, 422], 'Rule.OnlyDraftContractDelete')
    }
  } else skip('legal.updateContract / deleteContract', 'PUT', '/legal/contracts/{id}', 'öğrenci yok')

  // 4) modül görünürlüğü: kapat → menü gizlenir, uçlar 403; aç → geri gelir (demo_a, işlem sonunda hepsi açık bırakılır)
  const setModules = (...a) => execFileSync('python3', ['devops/set_modules.py', '--code', 'demo_a', ...a], { cwd: path.resolve(here, '../..'), stdio: 'pipe' })
  const menuCodes = async () => {
    const out = new Set(), walk = (ns) => (ns ?? []).forEach(n => { out.add(n.code); walk(n.children) })
    walk((await http('GET', '/menus/me')).json?.data)
    return out
  }
  const gated = [['camps', '/camps', ['camps']], ['surveys', '/surveys', ['students-surveys']], ['consultancy', '/consultancy/institutions', ['consultancy']],
    ['camera', '/cameras', ['cameras']], ['kpi', '/performance-kpi/definitions', ['performance']], ['care_team', '/care-team/my-students', ['students-care-team']]]
  try {
    setModules('--enable', 'all')
    const openMenu = await menuCodes()
    for (const [m, url] of gated) {
      const res = await http('GET', url)
      record({ phase: 'yazma', id: `modül açık: ${m}`, verb: 'GET', url, status: res.status, outcome: res.status !== 403 || res.json?.code !== 'Error.ModuleDisabled' ? 'ok' : '4xx',
        note: res.status !== 403 || res.json?.code !== 'Error.ModuleDisabled' ? 'modül engeli yok' : 'açık modül engellendi', mismatches: [] })
    }
    setModules('--disable', gated.map(g => g[0]).join(','))
    const closedMenu = await menuCodes()
    for (const [m, url, menus] of gated) {
      expectStatus('yazma', `modül kapalı: ${m} (uç → 403)`, 'GET', url, await http('GET', url), [403], 'Error.ModuleDisabled')
      for (const code of menus) {
        const hidden = !closedMenu.has(code)
        record({ phase: 'yazma', id: `modül kapalı: ${m} (menü '${code}' gizli)`, verb: 'GET', url: '/menus/me', status: 200,
          outcome: hidden ? 'ok' : 'leak', note: openMenu.has(code) ? (hidden ? 'açıkken vardı, kapalıyken yok' : 'kapalıyken hâlâ görünüyor') : 'menü bu rol/kurumda zaten yok (bayrak testi API ucuyla)', mismatches: [] })
      }
    }
    // kurum yöneticisi settings ile modülü açamaz (gönderilen modules yok sayılır)
    const corpId = (await http('GET', '/corporations?page=1&pageSize=5')).json?.data?.items?.[0]?.id
    const cs = corpId ? (await http('GET', `/corporations/${corpId}/settings`)).json?.data : null
    if (cs) {
      const sent = JSON.stringify({ ...JSON.parse(cs.settings || '{}'), modules: { camps: true, surveys: true } })
      await write('yazma', 'corporations.updateSettings (modules gönderilir → yok sayılır)', 'PUT', `/corporations/${corpId}/settings`, {
        defaultLocale: cs.defaultLocale, defaultCurrency: cs.defaultCurrency, timezone: cs.timezone, taxOffice: cs.taxOffice ?? null, taxNumber: cs.taxNumber ?? null,
        settings: sent, rowVersion: (await http('GET', `/corporations/${corpId}`)).json?.data?.rowVersion })
      const again = (await http('GET', `/corporations/${corpId}/settings`)).json?.data
      record({ phase: 'yazma', id: 'corporations.updateSettings (modules korunur)', verb: 'GET', url: `/corporations/${corpId}/settings`, status: 200,
        outcome: again?.modules?.camps === false && again?.modules?.surveys === false ? 'ok' : 'leak', note: JSON.stringify(again?.modules ?? null), mismatches: [] })
    }
  } catch (e) {
    skip('modül görünürlüğü', 'GET', '/menus/me', `set_modules.py çalıştırılamadı: ${String(e.message).split('\n')[0]}`)
  } finally {
    try { setModules('--enable', 'all') } catch { /* docker yoksa sweep zaten uyarır */ }
  }
  token = adminToken
}

async function writeFlows() {
  // 0) kullanıcı kaydı (davet) + parola sıfırlama isteği — INotificationService kaydı eksikse 500 verir
  const regUser = `smoke.reg.${RUN.toLowerCase()}@demo-a.test`
  let r = await write('yazma', 'auth.register', 'POST', '/auth/register', {
    username: regUser, fullName: `Smoke Kullanıcı ${RUN}`, email: regUser, phone: null, password: null,
    preferredLocale: 'tr', primaryCampusId: ctx.campusId ?? null, sendVerificationEmail: true })
  if (r.status && r.status < 300) {
    // anonim uç: kayıtlı e-posta için de olmayan e-posta için de aynı 2xx döner (e-posta numaralandırma yok)
    await write('yazma', 'auth.requestPasswordReset', 'POST', '/auth/request-password-reset', { email: regUser })
    await write('yazma', 'auth.requestPasswordReset (kayıtsız e-posta)', 'POST', '/auth/request-password-reset', { email: `nobody.${RUN.toLowerCase()}@demo-a.test` })
  } else record({ phase: 'yazma', id: 'auth.requestPasswordReset', verb: 'POST', url: '/auth/request-password-reset', status: null, outcome: 'skip', note: 'kayıt oluşmadı', mismatches: [] })
  // A) kamp + dönem
  r = await write('yazma', 'camp.create', 'POST', '/camps', {
    campusId: ctx.campusId, code: `SMK-${RUN}`, name: `Smoke Kamp ${RUN}`, description: 'smoke-api', location: 'Test', capacity: 20 }, 'post:camp.create')
  const campId = idOf(r.json?.data)
  if (campId) {
    known.campId = campId
    r = await write('yazma', 'camp.createPeriod', 'POST', `/camps/${campId}/periods`, {
      name: `Yaz ${RUN}`, startDate: day(30), endDate: day(44), capacity: 10 }, 'post:camp.createPeriod')
    const periodId = idOf(r.json?.data); if (periodId) known.periodId = periodId
    if (periodId) {
      const a = await write('yazma', 'camp.createActivity', 'POST', `/camps/periods/${periodId}/activities`, {
        name: `Etkinlik ${RUN}`, startsAt: at(31, 9), endsAt: at(31, 10) }, 'post:camp.createActivity')
      const aid = idOf(a.json?.data); if (aid) known.activityId = aid
    }
    await verifyGet('yazma', 'camp.get (doğrulama)', `/camps/${campId}`, 'get:camp.get')
    if (periodId) await verifyGet('yazma', 'camp.getPeriod (doğrulama)', `/camps/periods/${periodId}`, 'get:camp.getPeriod')
  } else record({ phase: 'yazma', id: 'camp.createPeriod', verb: 'POST', url: '/camps/{id}/periods', status: null, outcome: 'skip', note: 'kamp oluşmadı', mismatches: [] })

  // B) toplantı oluştur + güncelle
  const startsAt = at(3, 10), endsAt = at(3, 11)
  r = await write('yazma', 'meeting.schedule', 'POST', '/meetings', {
    campusId: ctx.campusId, title: `Smoke Toplantı ${RUN}`, location: 'Toplantı odası', scheduledAt: startsAt, endsAt }, 'post:meeting.schedule')
  const mid = idOf(r.json?.data)
  if (mid) {
    const g = await verifyGet('yazma', 'meeting.get (güncelleme öncesi)', `/meetings/${mid}`, 'get:meeting.get')
    const rv = g.json?.data?.rowVersion
    r = await write('yazma', 'meeting.update', 'PUT', `/meetings/${mid}`, {
      title: `Smoke Toplantı ${RUN} (güncel)`, location: 'Yeni oda', scheduledAt: at(4, 10), endsAt: at(4, 11), rowVersion: rv }, 'put:meeting.update')
    const g2 = await verifyGet('yazma', 'meeting.get (güncelleme sonrası)', `/meetings/${mid}`, 'get:meeting.get')
    const d2 = g2.json?.data
    const bad = []
    if (d2?.title !== `Smoke Toplantı ${RUN} (güncel)`) bad.push(`başlık güncellenmedi (gelen: ${d2?.title})`)
    if (rv !== undefined && d2?.rowVersion === rv) bad.push('rowVersion artmadı')
    if (bad.length) record({ phase: 'yazma', id: 'meeting.update (kalıcılık)', verb: 'GET', url: `/meetings/${mid}`, status: g2.status, outcome: 'type', note: 'güncelleme sonrası okuma tutarsız', mismatches: bad })
  } else record({ phase: 'yazma', id: 'meeting.update', verb: 'PUT', url: '/meetings/{id}', status: null, outcome: 'skip', note: 'toplantı oluşmadı', mismatches: [] })

  // C) seans oluştur
  if (ctx.sessionTypeId && ctx.studentId && ctx.educatorId) {
    r = await write('yazma', 'scheduling.createSession', 'POST', '/scheduling/sessions', {
      sessionTypeId: ctx.sessionTypeId, startsAt: at(5, 9), endsAt: at(5, 10), campusId: ctx.campusId, roomId: ctx.roomId,
      title: `Smoke Seans ${RUN}`, isMakeup: false, participantStudentIds: [ctx.studentId],
      educatorAssignments: [{ educatorId: ctx.educatorId, role: 'lead' }] }, 'post:scheduling.createSession')
    const sid = idOf(r.json?.data)
    if (sid) await verifyGet('yazma', 'scheduling.getSession (doğrulama)', `/scheduling/sessions/${sid}`, 'get:scheduling.getSession')
  } else record({ phase: 'yazma', id: 'scheduling.createSession', verb: 'POST', url: '/scheduling/sessions', status: null, outcome: 'skip',
    note: `ön koşul eksik (sessionType=${!!ctx.sessionTypeId}, öğrenci=${!!ctx.studentId}, eğitmen=${!!ctx.educatorId})`, mismatches: [] })
}

async function verifyGet(phase, id, urlPath, epKey) {
  const res = await http('GET', urlPath)
  let outcome = classify(res.status), note = '', mismatches = []
  if (outcome === 'http-ok') { const c = checkAgainst(byId[epKey], res); mismatches = c.mismatches; outcome = mismatches.length ? 'type' : 'ok'; note = c.emptyNote ?? '' }
  else note = errNote(res)
  record({ phase, id, verb: 'GET', url: urlPath, status: res.status, outcome, note, mismatches })
  return res
}

/** Veri/kimlik bulunamadığı için atlanan uç noktada rotanın varlığını yoklar: sahte GUID ile GET.
 *  Rota yoksa ASP.NET gövdesiz 404/405 döner; kayıt yoksa ApiResponse zarflı 404 döner. */
async function probeRoute(ep, why) {
  const ZERO = '00000000-0000-7000-8000-000000000000'
  let u = ep.path.replace('{{QS}}', '').replace(/\{\{today(?:-(\d+))?\}\}/g, () => day(0))
  u = u.replace(/\{\{(.+?)\}\}/g, (_, x) => { const n = x.split('.').pop(); return encodeURIComponent(fx.pathParamDefaults[n] ?? known[n] ?? ZERO) })
  const res = await http('GET', u)
  const noRoute = res.status === 405 || (res.status === 404 && !res.json)
  const base = { phase: 'GET', id: ep.id, verb: 'GET', url: ep.path, file: ep.file, mismatches: [] }
  if (noRoute) return { ...base, status: res.status, outcome: '4xx', note: `ROTA YOK: backend bu yolu/fiili tanımıyor (yoklama HTTP ${res.status}); atlanma nedeni: ${why}` }
  if (res.status >= 500) return { ...base, status: res.status, outcome: '5xx', note: `yoklama (sahte id) sunucu hatası verdi: ${errNote(res)}; atlanma nedeni: ${why}` }
  return { ...base, status: null, outcome: 'skip', note: `${why} — rota mevcut (yoklama HTTP ${res.status})` }
}
function expected(ep, res) {
  for (const [prefix, e] of Object.entries(fx.expectedStatus || {}))
    if (ep.id.startsWith(prefix) && res.status === e.status) return e.reason
  return null
}

async function sweep(filter = (e) => !e.id.startsWith('portal.'), phase = 'GET') {
  const gets = endpoints.filter(e => e.verb === 'get' && filter(e) && (!only || e.id.startsWith(only)))
  for (const ep of gets) {
    let url
    try { url = await buildUrl(ep) } catch (e) {
      if (e instanceof Skip) { record({ ...(await probeRoute(ep, e.message)), phase }); continue }
      throw e
    }
    const res = await http('GET', url)
    let outcome = classify(res.status), note = '', mismatches = []
    if (outcome === 'http-ok') { const c = checkAgainst(ep, res); mismatches = c.mismatches; outcome = mismatches.length ? 'type' : 'ok'; note = c.emptyNote ?? '' }
    else {
      note = errNote(res)
      const exp = expected(ep, res)
      if (exp) { outcome = 'skip'; note = `beklenen HTTP ${res.status}: ${exp}` }
    }
    record({ phase, id: ep.id, verb: 'GET', url, status: res.status, outcome, note, mismatches, file: ep.file, ms: res.ms })
  }
}


// ── İzolasyon kontrolleri ───────────────────────────────────────────────────
const isoRec = (id, url, res, okStatuses, what) => {
  const ok = okStatuses.includes(res.status)
  record({ phase: 'izolasyon', id, verb: 'GET', url, status: res.status, outcome: ok ? 'ok' : (res.status >= 200 && res.status < 300 ? 'leak' : res.status >= 500 ? '5xx' : '4xx'),
    note: ok ? `beklenen ${okStatuses.join('/')} — ${what}` : `${what}: beklenen ${okStatuses.join('/')}, gelen ${res.status}${res.status < 300 ? ' (VERİ SIZDI)' : res.status === 401 ? ' (veri sızmadı; ancak 401 dönüyor — 403/404 olmalı, frontend 401\'de oturumu düşürebilir)' : ''}`, mismatches: [] })
}
async function isolation() {
  const iso = fx.isolation, mainToken = token
  // diğer kurumun kayıt id'lerini kendi yöneticisiyle topla
  const otherLogin = await login(iso.otherTenantAdmin.username, iso.otherTenantAdmin.password, 'izolasyon-hazırlık')
  const otherCorporationId = otherLogin?.corporationId
  const first = async (p) => { const r = await http('GET', p + '?page=1&pageSize=5'); const d = r.json?.data; return (Array.isArray(d) ? d : d?.items ?? [])[0]?.id }
  const foreign = { student: await first('/students'), session: await first('/scheduling/sessions'), invoice: await first('/payments/invoices'), meeting: await first('/meetings') }
  token = mainToken
  const probes = [['student', '/students'], ['session', '/scheduling/sessions'], ['invoice', '/payments/invoices'], ['meeting', '/meetings']]
  for (const [k, base] of probes) {
    if (!foreign[k]) { record({ phase: 'izolasyon', id: `isolation.${k}`, verb: 'GET', url: base, status: null, outcome: 'skip', note: `${iso.otherTenantCode} kurumunda ${k} bulunamadı (demo-seed çalıştırılmamış olabilir)`, mismatches: [] }); continue }
    const r = await http('GET', `${base}/${foreign[k]}`)
    isoRec(`isolation.${k} (demo_a yöneticisi → demo_b ${k})`, `${base}/{demo_b ${k}}`, r, [404], 'başka kurumun kaydı')
  }
  // Kurumlar (Y1): liste yalnızca kendi kurumunu içerir; diğer kuruma GET/ayar/PUT → 404
  const corpList = await http('GET', '/corporations?page=1&pageSize=50')
  const corpItems = corpList.json?.data?.items ?? []
  const onlyOwn = corpList.status === 200 && corpItems.length === 1 && corpList.json?.data?.totalCount === 1
    && !corpItems.some(c => c.id === otherCorporationId)
  record({ phase: 'izolasyon', id: 'isolation.corporations (demo_a yöneticisi → kurum listesi)', verb: 'GET', url: '/corporations', status: corpList.status,
    outcome: onlyOwn ? 'ok' : corpList.status < 300 ? 'leak' : corpList.status >= 500 ? '5xx' : '4xx',
    note: onlyOwn ? 'beklenen: yalnızca kendi kurumu' : `beklenen: yalnızca kendi kurumu; gelen ${corpItems.length} kurum (${corpItems.map(c => c.code).join(', ')})`, mismatches: [] })
  if (!otherCorporationId) {
    record({ phase: 'izolasyon', id: 'isolation.corporation', verb: 'GET', url: '/corporations/{id}', status: null, outcome: 'skip', note: `${iso.otherTenantCode} kurum id'si alınamadı`, mismatches: [] })
  } else {
    for (const sub of ['', '/settings'])
      isoRec(`isolation.corporation${sub} (demo_a yöneticisi → demo_b kurumu)`, `/corporations/{demo_b}${sub}`,
        await http('GET', `/corporations/${otherCorporationId}${sub}`), [404], 'başka kurumun kurum kaydı')
    const r = await http('PUT', `/corporations/${otherCorporationId}`, { legalName: 'Smoke', displayName: 'Smoke', defaultLocale: 'tr',
      defaultCurrency: 'TRY', timezone: 'Europe/Istanbul', taxOffice: null, taxNumber: null, rowVersion: 1 })
    record({ phase: 'izolasyon', id: 'isolation.corporation.PUT (demo_a yöneticisi → demo_b kurumu)', verb: 'PUT', url: '/corporations/{demo_b}', status: r.status,
      outcome: r.status === 404 ? 'ok' : r.status < 300 ? 'leak' : r.status >= 500 ? '5xx' : '4xx', note: r.status === 404 ? 'beklenen 404' : `beklenen 404, gelen ${r.status}`, mismatches: [] })
  }
  // yazma yönü: başka kurumun öğrencisini güncellemeyi dene (PUT) → 404 beklenir
  if (foreign.student) {
    const r = await http('PUT', `/students/${foreign.student}`, { firstName: 'X', lastName: 'Y', rowVersion: 1 })
    record({ phase: 'izolasyon', id: 'isolation.student.PUT (demo_a yöneticisi → demo_b öğrenci)', verb: 'PUT', url: '/students/{demo_b student}', status: r.status,
      outcome: r.status === 404 ? 'ok' : r.status < 300 ? 'leak' : '4xx', note: r.status === 404 ? 'beklenen 404' : `beklenen 404, gelen ${r.status}`, mismatches: [] })
  }
}
async function guardianRound() {
  const adminToken = token
  // yabancı çocuklar: aynı kurumda başka velinin çocuğu + diğer kurumun öğrencisi
  const stu = await http('GET', '/students?page=1&pageSize=100')
  const own = (stu.json?.data?.items ?? []).find(s => s.studentNo === fx.isolation.foreignChildStudentNo)?.id
  await login(fx.isolation.otherTenantAdmin.username, fx.isolation.otherTenantAdmin.password, 'izolasyon-hazırlık')
  const otherStu = await http('GET', '/students?page=1&pageSize=5'); const other = otherStu.json?.data?.items?.[0]?.id
  const g = await login(fx.guardian.username, fx.guardian.password, 'giriş (veli)')
  listCache.clear(); paramOverride = { studentId: '/portal/my-students' }
  await sweep((e) => e.id.startsWith('portal.'), 'GET (veli)')
  paramOverride = {}
  // Y15: veli kendi çocuklarının yalnızca kesinleşmiş gelişim raporlarını görür (taslaklar gizli)
  const mine = (await http('GET', '/portal/my-students')).json?.data ?? []
  if (!mine.length) record({ phase: 'izolasyon', id: 'isolation.portal.development-reports (veli1 → kendi çocukları)', verb: 'GET', url: '/portal/students/{id}/development-reports', status: null, outcome: 'skip', note: 'veli1 çocuğu bulunamadı', mismatches: [] })
  for (const child of mine) {
    const r = await http('GET', `/portal/students/${child.id ?? child.studentId}/development-reports`)
    const items = r.json?.data ?? []
    const ok = r.status === 200 && items.length >= 1 && items.every(x => x.isFinalized === true) && !items.some(x => /TASLAK/.test(x.content ?? ''))
      && items.some(x => /KESINLESMIS/.test(x.content ?? ''))
    record({ phase: 'izolasyon', id: `isolation.portal.development-reports (veli1 → kendi çocuğu ${child.firstName ?? ''})`, verb: 'GET', url: '/portal/students/{id}/development-reports', status: r.status,
      outcome: ok ? 'ok' : items.some(x => x.isFinalized === false) ? 'leak' : r.status >= 500 ? '5xx' : '4xx',
      note: ok ? 'beklenen: yalnızca kesinleşmiş rapor(lar) görünür' : `beklenen: ≥1 kesinleşmiş, taslak yok; gelen ${r.status}, ${items.length} rapor (${items.map(x => x.isFinalized ? 'kesinleşmiş' : 'TASLAK').join(', ')})`, mismatches: [] })
  }
  // V55: portal seans geçmişinde yalnızca veliye görünür notlar gelir; düzenlenen notta editedAt dolu
  let sawEdited = false
  for (const child of mine) {
    const r = await http('GET', `/portal/students/${child.id ?? child.studentId}/sessions?page=1&pageSize=200`)
    const notes = (r.json?.data ?? []).flatMap(x => x.notes ?? [])
    const leak = notes.some(n => /Dahili not/.test(n.body))
    const seen = notes.some(n => /^Veliye açık not/.test(n.body))
    sawEdited ||= notes.some(n => n.editedAt)
    record({ phase: 'izolasyon', id: `isolation.portal.session-notes (veli1 → ${child.firstName ?? ''})`, verb: 'GET', url: '/portal/students/{id}/sessions', status: r.status,
      outcome: leak ? 'leak' : r.status === 200 && seen ? 'ok' : r.status >= 500 ? '5xx' : '4xx',
      note: leak ? 'dahili (parent_visible=false) not veliye göründü' : seen ? `beklenen: yalnızca görünür not(lar) (${notes.length})` : `görünür not gelmedi (${r.status}, ${notes.length} not)`, mismatches: [] })
  }
  // V56: grup seansında veli1 yalnızca kendi çocuğuna bağlı notu görür; B (veli2'nin çocuğu) ve öğrencisiz not sızmaz
  {
    let sawGroupA = false, leakedB = false, leakedInternal = false
    for (const child of mine) {
      const r = await http('GET', `/portal/students/${child.id ?? child.studentId}/sessions?page=1&pageSize=200`)
      const notes = (r.json?.data ?? []).flatMap(x => x.notes ?? [])
      sawGroupA ||= notes.some(n => /^Grup-A:/.test(n.body))
      leakedB ||= notes.some(n => /Grup-B-ÖZEL/.test(n.body))
      leakedInternal ||= notes.some(n => /Grup-DAHİLİ/.test(n.body))
    }
    if (mine.length) record({ phase: 'izolasyon', id: 'isolation.portal.group-session-notes (veli1 → B ve öğrencisiz not sızmaz)', verb: 'GET', url: '/portal/students/{id}/sessions', status: 200,
      outcome: leakedB || leakedInternal ? 'leak' : sawGroupA ? 'ok' : '4xx',
      note: leakedB ? 'B için yazılan not A velisine göründü' : leakedInternal ? 'öğrencisiz grup notu göründü' : sawGroupA ? 'beklenen: yalnızca Grup-A notu görünür' : 'Grup-A notu görünmedi', mismatches: [] })
  }
  if (mine.length) record({ phase: 'izolasyon', id: 'isolation.portal.session-notes (düzenlenen not editedAt)', verb: 'GET', url: '/portal/students/{id}/sessions', status: 200,
    outcome: sawEdited ? 'ok' : '4xx', note: sawEdited ? 'düzenlenmiş notta editedAt dolu' : 'düzenlenmiş not bulunamadı', mismatches: [] })
  const checks = [['aynı kurumda başka velinin çocuğu', own], ['başka kurumun öğrencisi', other]]
  for (const [what, sid] of checks) {
    if (!sid) { record({ phase: 'izolasyon', id: `isolation.portal (${what})`, verb: 'GET', url: '/portal/students/{id}', status: null, outcome: 'skip', note: 'kayıt bulunamadı', mismatches: [] }); continue }
    for (const sub of ['', '/dashboard', '/sessions', '/bep', '/packages', '/development-reports']) {
      const r = await http('GET', `/portal/students/${sid}${sub}`)
      isoRec(`isolation.portal${sub || '/student'} (veli1 → ${what})`, `/portal/students/{id}${sub}`, r, [403, 404], what)
    }
  }
  token = adminToken
}

// ── Rapor ───────────────────────────────────────────────────────────────────
async function finish(abort = false) {
  fs.mkdirSync(path.join(here, 'out'), { recursive: true })
  const cnt = (list, o) => list.filter(r => r.outcome === o).length
  const groups = {}
  for (const r of results) { const g = (r.phase === 'GET' || r.phase === 'GET (veli)') ? (r.phase === 'GET' ? '' : 'veli:') + r.id.split('.')[0] : `(${r.phase})`; (groups[g] ||= []).push(r) }
  const rows = Object.entries(groups).sort().map(([g, l]) => ({ grup: g, toplam: l.length, basarili: cnt(l, 'ok'), '4xx': cnt(l, '4xx'), '5xx': cnt(l, '5xx'), tipUyusmazligi: cnt(l, 'type'), sizinti: cnt(l, 'leak'), atlandi: cnt(l, 'skip') }))
  const total = { grup: 'TOPLAM', toplam: results.length, basarili: cnt(results, 'ok'), '4xx': cnt(results, '4xx'), '5xx': cnt(results, '5xx'), tipUyusmazligi: cnt(results, 'type'), sizinti: cnt(results, 'leak'), atlandi: cnt(results, 'skip') }
  console.table([...rows, total])
  const unvalidated = results.filter(r => r.outcome === 'ok' && /boş dizi/.test(r.note || '')).length
  console.log(`Not: 'başarılı' sayısının ${unvalidated} tanesinde liste boştu; öğe alanları doğrulanamadı.`)
  const bad = results.filter(r => r.outcome !== 'ok')
  let md = `# Duman testi raporu\n\n- Tarih: ${new Date().toISOString()}\n- API: ${API}\n- Kurum: ${fx.tenant.code} (yönetici turu) · veli turu: ${fx.guardian.username}\n\n`
  md += `| Grup | Toplam | Başarılı | 4xx | 5xx | Tip uyuşmazlığı | İzolasyon ihlali | Atlandı |\n|---|---|---|---|---|---|---|---|\n`
  for (const r of [...rows, total]) md += `| ${r.grup} | ${r.toplam} | ${r.basarili} | ${r['4xx']} | ${r['5xx']} | ${r.tipUyusmazligi} | ${r.sizinti} | ${r.atlandi} |\n`
  md += `\n> 'Başarılı' çağrıların ${unvalidated} tanesinde liste boş döndü; o çağrılarda öğe alanları doğrulanamadı.\n`
  md += `\n## Başarısız / uyuşmayan / atlanan çağrılar\n\n| Sonuç | HTTP | Çağrı | Ayrıntı |\n|---|---|---|---|\n`
  const order = { leak: -1, '5xx': 0, '4xx': 1, type: 2, neterr: 3, skip: 4 }
  for (const r of bad.sort((a, b) => (order[a.outcome] ?? 9) - (order[b.outcome] ?? 9))) {
    const detail = [r.note, ...(r.mismatches || [])].filter(Boolean).join(' · ').replace(/\|/g, '\\|')
    md += `| ${r.outcome} | ${r.status ?? '-'} | ${r.verb} ${r.id} \`${r.url}\` | ${detail} |\n`
  }
  fs.writeFileSync(path.join(here, 'out/report.json'), JSON.stringify({ at: new Date().toISOString(), api: API, results }, null, 2))
  fs.writeFileSync(path.join(here, 'out/report.md'), md)
  console.log(`\nRapor: scripts/smoke-api/out/report.md  (json: out/report.json)`)
  process.exit(abort || (strict && bad.some(r => r.outcome !== 'skip')) ? 1 : 0)
}

const me = await login()
ctx.adminUserId = me?.userId
await setup()
await writeFlows()
await newFlows()
await closureFlows()
await sweep()
await isolation()
await guardianRound()
await finish()
