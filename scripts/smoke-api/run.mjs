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

// ── B→A üçüncü tur: kurumların günlük işi (takvim kayıtları, tekrar eden plan toplu işlemleri, toplu yoklama, ...) ──
async function dailyOpsFlows() {
  const adminToken = token
  const ref = async (type) => (await http('GET', `/reference-data/${type}?activeOnly=true`)).json?.data ?? []
  const skip = (id, verb, url, note) => record({ phase: 'yazma', id, verb, url, status: null, outcome: 'skip', note, mismatches: [] })
  const nextDay = (d) => new Date(new Date(`${d}T00:00:00Z`).getTime() + 864e5).toISOString()

  // 1) takvim kaydı: oluştur → listede → tekrar eden plandan seans üretimi tatili atlar → sil → ikinci silme 404
  const holiday = day(300)
  const wd = new Date(`${holiday}T00:00:00Z`).getUTCDay()
  const ce = await write('yazma', 'scheduling.createCalendarEntry (resmi tatil)', 'POST', '/scheduling/calendar-entries', {
    title: `Smoke Tatil ${RUN}`, entryType: 'holiday', campusId: null, startsAt: `${holiday}T00:00:00Z`, endsAt: nextDay(holiday), isAllDay: true })
  const entry = ce.json?.data
  if (entry?.id) {
    const lst = await http('GET', `/scheduling/calendar-entries?from=${encodeURIComponent(`${holiday}T00:00:00Z`)}&to=${encodeURIComponent(nextDay(holiday))}`)
    record({ phase: 'yazma', id: 'scheduling.listCalendarEntries (kayıt listede)', verb: 'GET', url: '/scheduling/calendar-entries', status: lst.status,
      outcome: lst.status === 200 && (lst.json?.data ?? []).some(x => x.id === entry.id) ? 'ok' : 'type', note: 'oluşturulan tatil listelenmeli', mismatches: [] })
    if (ctx.sessionTypeId) {
      const sc = await write('yazma', 'scheduling.createRecurringSchedule (tatil sınaması)', 'POST', '/scheduling/recurring-schedules', {
        frequency: 'weekly', startTime: '10:00:00', durationMinutes: 40, rangeStart: holiday, rangeEnd: day(314), campusId: ctx.campusId, studentProgramId: null,
        sessionTypeId: ctx.sessionTypeId, roomId: null, intervalCount: 1, byWeekday: [wd], byMonthday: null, maxOccurrences: null })
      const schedId = idOf(sc.json?.data)
      if (schedId) {
        const gen = await write('yazma', 'scheduling.generateSessions (tatil günü atlanır)', 'POST', `/scheduling/recurring-schedules/${schedId}/generate`, { windowStart: holiday, windowEnd: day(313) })
        const g = gen.json?.data
        record({ phase: 'yazma', id: 'scheduling.generateSessions (closureSkipped = 1, 1 seans üretildi)', verb: 'POST', url: `/scheduling/recurring-schedules/${schedId}/generate`, status: gen.status,
          outcome: g?.closureSkipped === 1 && g?.affectedCount === 1 ? 'ok' : 'type', note: `closureSkipped=${g?.closureSkipped} affectedCount=${g?.affectedCount}`, mismatches: [] })
        ctx.recurringId = schedId
        // 2) tekrar eden plan toplu işlemleri (üretilen tek seans üzerinde): oda değişikliği → istisna → toplu iptal
        if (ctx.roomId) {
          const re = await write('yazma', 'scheduling.bulkReassignRoom', 'POST', `/scheduling/recurring-schedules/${schedId}/bulk-reassign-room`, { newRoomId: ctx.roomId, from: holiday })
          const sessList = await http('GET', `/scheduling/sessions?recurringScheduleId=${schedId}&page=1&pageSize=20`)
          const first = (sessList.json?.data?.items ?? [])[0]
          record({ phase: 'yazma', id: 'scheduling.bulkReassignRoom (oda değişti, durum "scheduled" kaldı)', verb: 'POST', url: `/scheduling/recurring-schedules/${schedId}/bulk-reassign-room`, status: re.status,
            outcome: re.json?.data?.affectedCount === 1 && (!first || first.status === 'scheduled') ? 'ok' : 'type', note: `affectedCount=${re.json?.data?.affectedCount} durum=${first?.status}`, mismatches: [] })
        } else skip('scheduling.bulkReassignRoom', 'POST', '/scheduling/recurring-schedules/{id}/bulk-reassign-room', 'oda yok')
        expectStatus('yazma', 'scheduling.bulkCancel (bitiş < başlangıç → reddedilir)', 'POST', `/scheduling/recurring-schedules/${schedId}/bulk-cancel`,
          await http('POST', `/scheduling/recurring-schedules/${schedId}/bulk-cancel`, { from: day(310), to: day(300), reason: 'x' }), [400, 422])
        const ex = await write('yazma', 'scheduling.addRecurrenceException (henüz seansı olmayan tarih)', 'POST', `/scheduling/recurring-schedules/${schedId}/exceptions`, { exceptionDate: day(321), action: 'skip', newStartAt: null, reason: 'Smoke' })
        record({ phase: 'yazma', id: 'scheduling.addRecurrenceException (affectedCount = 0)', verb: 'POST', url: `/scheduling/recurring-schedules/${schedId}/exceptions`, status: ex.status,
          outcome: ex.json?.data?.affectedCount === 0 ? 'ok' : 'type', note: `affectedCount=${ex.json?.data?.affectedCount}`, mismatches: [] })
        expectStatus('yazma', 'scheduling.addRecurrenceException (aynı tarih ikinci kez → reddedilir)', 'POST', `/scheduling/recurring-schedules/${schedId}/exceptions`,
          await http('POST', `/scheduling/recurring-schedules/${schedId}/exceptions`, { exceptionDate: day(321), action: 'cancel', newStartAt: null, reason: null }), [400, 409, 422], 'Rule.ExceptionExistsForDate')
        const ex2 = await write('yazma', 'scheduling.addRecurrenceException (üretilmiş seansı iptal eder)', 'POST', `/scheduling/recurring-schedules/${schedId}/exceptions`, { exceptionDate: day(307), action: 'cancel', newStartAt: null, reason: 'Smoke istisna' })
        const bc = await write('yazma', 'scheduling.bulkCancel (kalan seans yok → 0)', 'POST', `/scheduling/recurring-schedules/${schedId}/bulk-cancel`, { from: holiday, to: day(314), reason: 'Smoke toplu iptal' })
        record({ phase: 'yazma', id: 'scheduling.bulkCancel / istisna (seans tam bir kez iptal edildi)', verb: 'POST', url: `/scheduling/recurring-schedules/${schedId}/bulk-cancel`, status: bc.status,
          outcome: (ex2.json?.data?.affectedCount ?? 0) + (bc.json?.data?.affectedCount ?? 0) === 1 ? 'ok' : 'type', note: `istisna=${ex2.json?.data?.affectedCount} toplu=${bc.json?.data?.affectedCount}`, mismatches: [] })
      }
    } else skip('scheduling.generateSessions (tatil günü atlanır)', 'POST', '/scheduling/recurring-schedules/{id}/generate', 'seans türü yok')
    await write('yazma', 'scheduling.deleteCalendarEntry', 'DELETE', `/scheduling/calendar-entries/${entry.id}`)
    expectStatus('yazma', 'scheduling.deleteCalendarEntry (silinmiş → 404)', 'DELETE', `/scheduling/calendar-entries/${entry.id}`, await http('DELETE', `/scheduling/calendar-entries/${entry.id}`), [404])
  }

  // 3) toplu yoklama: tümü "present" → bir öğrenci "absent" düzeltmesi → katılımcı olmayan öğrenci reddedilir
  if (ctx.studentId && ctx.sessionTypeId && ctx.educatorId) {
    const sr = await write('yazma', 'scheduling.createSession (toplu yoklama)', 'POST', '/scheduling/sessions', {
      sessionTypeId: ctx.sessionTypeId, startsAt: at(22, 9), endsAt: at(22, 10), campusId: ctx.campusId, roomId: null,
      title: `Smoke Yoklama ${RUN}`, isMakeup: false, participantStudentIds: [ctx.studentId], educatorAssignments: [{ educatorId: ctx.educatorId, role: 'lead' }] })
    const sid = idOf(sr.json?.data)
    if (sid) {
      const bulk = await write('yazma', 'scheduling.bulkRecordAttendance (tümü geldi)', 'POST', `/scheduling/sessions/${sid}/attendance/bulk`, { entries: [{ studentId: ctx.studentId, status: 'present' }] })
      record({ phase: 'yazma', id: 'scheduling.bulkRecordAttendance (1 kayıt, present)', verb: 'POST', url: `/scheduling/sessions/${sid}/attendance/bulk`, status: bulk.status,
        outcome: bulk.status === 200 && bulk.json?.data?.length === 1 && bulk.json.data[0].status === 'present' ? 'ok' : 'type', note: `kayıt=${bulk.json?.data?.length}`, mismatches: [] })
      await write('yazma', 'scheduling.recordAttendance (tek tek düzeltme)', 'POST', `/scheduling/sessions/${sid}/attendance`, { studentId: ctx.studentId, status: 'absent' })
      const after = (await http('GET', `/scheduling/sessions/${sid}/attendance`)).json?.data ?? []
      record({ phase: 'yazma', id: 'scheduling.getSessionAttendance (düzeltme kalıcı, tek satır)', verb: 'GET', url: `/scheduling/sessions/${sid}/attendance`, status: 200,
        outcome: after.length === 1 && after[0].status === 'absent' ? 'ok' : 'type', note: `satır=${after.length} durum=${after[0]?.status}`, mismatches: [] })
      expectStatus('yazma', 'scheduling.bulkRecordAttendance (katılımcı olmayan öğrenci → reddedilir)', 'POST', `/scheduling/sessions/${sid}/attendance/bulk`,
        await http('POST', `/scheduling/sessions/${sid}/attendance/bulk`, { entries: [{ studentId: '00000000-0000-7000-8000-000000000001', status: 'present' }] }), [400, 409, 422], 'Rule.ParticipantNotInSession')
    }
  } else skip('scheduling.bulkRecordAttendance', 'POST', '/scheduling/sessions/{id}/attendance/bulk', 'ön koşul eksik')

  // 4) toplantı sonucu ekle → düzenle
  const mt = await write('yazma', 'meeting.schedule (sonuç düzenleme)', 'POST', '/meetings', {
    campusId: ctx.campusId, title: `Smoke Sonuç Toplantısı ${RUN}`, location: 'Oda', scheduledAt: at(5, 10), endsAt: at(5, 11) })
  const meetingId = idOf(mt.json?.data)
  if (meetingId) {
    const oc = await write('yazma', 'meeting.recordOutcome', 'POST', `/meetings/${meetingId}/outcomes`, { meetingId, summary: 'Smoke özet', decisions: 'Smoke karar' })
    const outcomeId = oc.json?.data?.id
    if (outcomeId) {
      await write('yazma', 'meeting.updateOutcome', 'PUT', `/meetings/outcomes/${outcomeId}`, { summary: 'Smoke özet (düzenlendi)', decisions: 'Smoke karar 2' })
      const got = (await http('GET', `/meetings/${meetingId}`)).json?.data?.outcomes?.find(o => o.id === outcomeId)
      record({ phase: 'yazma', id: 'meeting.updateOutcome (kalıcılık)', verb: 'GET', url: `/meetings/${meetingId}`, status: 200,
        outcome: got?.summary === 'Smoke özet (düzenlendi)' && got?.decisions === 'Smoke karar 2' ? 'ok' : 'type', note: got ? 'metin güncel' : 'sonuç okunamadı', mismatches: [] })
      expectStatus('yazma', 'meeting.updateOutcome (boş metin → reddedilir)', 'PUT', `/meetings/outcomes/${outcomeId}`, await http('PUT', `/meetings/outcomes/${outcomeId}`, { summary: ' ', decisions: null }), [400, 422])
      // tamamlanmış toplantıda sonuç ekleme/düzenleme serbest
      await write('yazma', 'meeting.complete (sonuç sonrası)', 'POST', `/meetings/${meetingId}/complete`)
      await write('yazma', 'meeting.updateOutcome (tamamlanmış toplantı)', 'PUT', `/meetings/outcomes/${outcomeId}`, { summary: 'Tamamlandıktan sonra düzenlendi', decisions: null })
      await write('yazma', 'meeting.recordOutcome (tamamlanmış toplantı)', 'POST', `/meetings/${meetingId}/outcomes`, { meetingId, summary: 'Sonradan eklenen', decisions: null })
    }
    // iptal edilmiş toplantıda kapalı
    const mc = await http('POST', '/meetings', { campusId: ctx.campusId, title: `Smoke İptal ${RUN}`, location: 'Oda', scheduledAt: at(6, 10), endsAt: at(6, 11) })
    const cid = idOf(mc.json?.data)
    if (cid) {
      const oc2 = await http('POST', `/meetings/${cid}/outcomes`, { meetingId: cid, summary: 'İptal öncesi', decisions: null })
      await http('POST', `/meetings/${cid}/cancel`)
      expectStatus('yazma', 'meeting.recordOutcome (iptal edilmiş toplantı → reddedilir)', 'POST', `/meetings/${cid}/outcomes`,
        await http('POST', `/meetings/${cid}/outcomes`, { meetingId: cid, summary: 'x', decisions: null }), [400, 409, 422], 'Rule.MeetingOutcomeCancelled')
      if (oc2.json?.data?.id) expectStatus('yazma', 'meeting.updateOutcome (iptal edilmiş toplantı → reddedilir)', 'PUT', `/meetings/outcomes/${oc2.json.data.id}`,
        await http('PUT', `/meetings/outcomes/${oc2.json.data.id}`, { summary: 'y', decisions: null }), [400, 409, 422], 'Rule.MeetingOutcomeCancelled')
    }
  } else skip('meeting.updateOutcome', 'PUT', '/meetings/outcomes/{id}', 'toplantı oluşmadı')

  // 5) değerlendirme önerisi düzenleme (tamamlanmış demo değerlendirmesi üzerinde; yoksa atlanır)
  const doneAs = (await http('GET', '/assessment-sessions?status=completed&page=1&pageSize=1')).json?.data?.items?.[0]
  if (doneAs?.id) {
    const rc = await write('yazma', 'assessment.createRecommendation (düzenleme için)', 'POST', `/assessment-sessions/${doneAs.id}/recommendations`, {
      leadId: doneAs.leadId ?? null, studentId: doneAs.studentId ?? null, recommendedProgramId: null, recommendedIntensity: 'haftada 1', rationale: 'Smoke gerekçe', recommendedBy: null })
    const rec = rc.json?.data
    if (rec?.id) {
      const up = await write('yazma', 'assessment.updateRecommendation', 'PUT', `/assessment-sessions/${doneAs.id}/recommendations/${rec.id}`, {
        recommendedProgramId: null, recommendedIntensity: 'haftada 3', rationale: 'Smoke gerekçe (düzenlendi)', recommendedBy: null, rowVersion: rec.rowVersion })
      record({ phase: 'yazma', id: 'assessment.updateRecommendation (alanlar güncel)', verb: 'PUT', url: `/assessment-sessions/${doneAs.id}/recommendations/${rec.id}`, status: up.status,
        outcome: up.json?.data?.recommendedIntensity === 'haftada 3' ? 'ok' : 'type', note: `yoğunluk=${up.json?.data?.recommendedIntensity}`, mismatches: [] })
      expectStatus('yazma', 'assessment.updateRecommendation (eski rowVersion → reddedilir)', 'PUT', `/assessment-sessions/${doneAs.id}/recommendations/${rec.id}`,
        await http('PUT', `/assessment-sessions/${doneAs.id}/recommendations/${rec.id}`, { recommendedProgramId: null, recommendedIntensity: 'x', rationale: null, recommendedBy: null, rowVersion: rec.rowVersion }), [400, 409, 422])
      const other = (await http('GET', '/assessment-sessions?page=1&pageSize=20')).json?.data?.items?.find(x => x.id !== doneAs.id)
      if (other) expectStatus('yazma', 'assessment.updateRecommendation (başka oturum yolundan → 404)', 'PUT', `/assessment-sessions/${other.id}/recommendations/${rec.id}`,
        await http('PUT', `/assessment-sessions/${other.id}/recommendations/${rec.id}`, { recommendedProgramId: null, recommendedIntensity: 'x', rationale: null, recommendedBy: null, rowVersion: up.json?.data?.rowVersion ?? 1 }), [404])
    }
  } else skip('assessment.updateRecommendation', 'PUT', '/assessment-sessions/{id}/recommendations/{id}', 'tamamlanmış değerlendirme yok')

  // 6) burs: ver → ayrıntı → düzenle (yüzde → sabit tutar); aynı anda ikisi reddedilir; eski rowVersion reddedilir
  const sType = (await ref('scholarship_type'))[0]?.id
  if (ctx.studentId && sType) {
    const gs = await write('yazma', 'finance.grantScholarship (düzenleme için)', 'POST', '/payments/scholarships', { studentId: ctx.studentId, scholarshipTypeId: sType, percentage: 10, amount: null, validFrom: day(0), validTo: day(200), note: 'Smoke burs' })
    const sch = gs.json?.data
    if (sch?.id) {
      const det = await write('yazma', 'finance.getScholarship', 'GET', `/payments/scholarships/${sch.id}`)
      const approverBefore = det.json?.data?.approvedBy
      const upd = await write('yazma', 'finance.updateScholarship', 'PUT', `/payments/scholarships/${sch.id}`, { scholarshipTypeId: sType, percentage: null, amount: 750, validFrom: day(0), validTo: day(300), note: 'Smoke burs (düzenlendi)', rowVersion: det.json?.data?.rowVersion })
      record({ phase: 'yazma', id: 'finance.updateScholarship (yüzde → sabit tutar)', verb: 'PUT', url: `/payments/scholarships/${sch.id}`, status: upd.status,
        outcome: upd.json?.data?.amount === 750 && upd.json?.data?.percentage == null ? 'ok' : 'type', note: `tutar=${upd.json?.data?.amount} yüzde=${upd.json?.data?.percentage}`, mismatches: [] })
      record({ phase: 'yazma', id: 'finance.updateScholarship (onaylayan sabit kalır)', verb: 'PUT', url: `/payments/scholarships/${sch.id}`, status: upd.status,
        outcome: upd.json?.data?.approvedBy === approverBefore ? 'ok' : 'type', note: `önce=${approverBefore} sonra=${upd.json?.data?.approvedBy}`, mismatches: [] })
      expectStatus('yazma', 'finance.updateScholarship (yüzde + tutar birlikte → reddedilir)', 'PUT', `/payments/scholarships/${sch.id}`,
        await http('PUT', `/payments/scholarships/${sch.id}`, { scholarshipTypeId: sType, percentage: 10, amount: 100, validFrom: day(0), validTo: null, note: null, rowVersion: upd.json?.data?.rowVersion }), [400, 422])
      expectStatus('yazma', 'finance.updateScholarship (eski rowVersion → reddedilir)', 'PUT', `/payments/scholarships/${sch.id}`,
        await http('PUT', `/payments/scholarships/${sch.id}`, { scholarshipTypeId: sType, percentage: 5, amount: null, validFrom: day(0), validTo: null, note: null, rowVersion: det.json?.data?.rowVersion }), [400, 409, 422])
    }
  } else skip('finance.updateScholarship', 'PUT', '/payments/scholarships/{id}', 'öğrenci/burs türü yok')

  // 7) taslak faturaya indirim: yüzde → sabit; toplam tek sayılır; tavanı aşan reddedilir; kesilen faturaya eklenemez
  if (ctx.studentId) {
    const inv = await write('yazma', 'finance.createInvoice (indirim için)', 'POST', '/payments/invoices', { issueDate: day(0), currency: 'TRY', studentId: ctx.studentId, guardianId: null, dueDate: day(14) })
    const iid = idOf(inv.json?.data)
    if (iid) {
      const ln = await write('yazma', 'finance.addInvoiceLine (indirim için)', 'POST', `/payments/invoices/${iid}/lines`, { description: 'Smoke indirimli', unitPrice: 1000, quantity: 1, studentPackageId: null, sortOrder: 1 })
      await write('yazma', 'finance.applyDiscount (yüzde 10)', 'POST', '/payments/discounts', { invoiceId: iid, isPercentage: true, value: 10, reason: 'Smoke indirimi' })
      await write('yazma', 'finance.applyDiscount (sabit 50)', 'POST', '/payments/discounts', { invoiceId: iid, isPercentage: false, value: 50, reason: 'Smoke sabit' })
      const after = (await http('GET', `/payments/invoices/${iid}`)).json?.data
      record({ phase: 'yazma', id: 'finance.applyDiscount (toplam: 1000 - 100 - 50 = 850, indirim tek sayıldı)', verb: 'GET', url: `/payments/invoices/${iid}`, status: 200,
        outcome: after?.discountTotal === 150 && after?.grandTotal === 850 && after?.discounts?.length === 2 ? 'ok' : 'type',
        note: `indirim=${after?.discountTotal} toplam=${after?.grandTotal} kayıt=${after?.discounts?.length}`, mismatches: [] })
      expectStatus('yazma', 'finance.applyDiscount (ara toplamı aşan → reddedilir)', 'POST', '/payments/discounts',
        await http('POST', '/payments/discounts', { invoiceId: iid, isPercentage: false, value: 900, reason: 'fazla' }), [400, 409, 422], 'Rule.DiscountExceedsSubtotal')
      expectStatus('yazma', 'finance.applyDiscount (yüzde > 100 → reddedilir)', 'POST', '/payments/discounts',
        await http('POST', '/payments/discounts', { invoiceId: iid, isPercentage: true, value: 101, reason: 'fazla' }), [400, 422])
      const discs = (await http('GET', `/payments/invoices/${iid}`)).json?.data?.discounts ?? []
      const fixed = discs.find(d => !d.isPercentage)
      if (fixed) {
        await write('yazma', 'finance.removeDiscount (taslak)', 'DELETE', `/payments/discounts/${fixed.id}`)
        const back = (await http('GET', `/payments/invoices/${iid}`)).json?.data
        record({ phase: 'yazma', id: 'finance.removeDiscount (toplam yeniden hesaplandı: 1000 - 100 = 900)', verb: 'GET', url: `/payments/invoices/${iid}`, status: 200,
          outcome: back?.discountTotal === 100 && back?.grandTotal === 900 && back?.discounts?.length === 1 ? 'ok' : 'type', note: `indirim=${back?.discountTotal} toplam=${back?.grandTotal}`, mismatches: [] })
      }
      if (ln.json?.data) {
        const cur = (await http('GET', `/payments/invoices/${iid}`)).json?.data
        await write('yazma', 'finance.issueInvoice (indirimli)', 'POST', `/payments/invoices/${iid}/issue`, { invoiceNo: `SMD-${RUN}`, rowVersion: cur?.rowVersion })
        expectStatus('yazma', 'finance.applyDiscount (kesilmiş fatura → reddedilir)', 'POST', '/payments/discounts',
          await http('POST', '/payments/discounts', { invoiceId: iid, isPercentage: true, value: 5, reason: 'geç' }), [400, 409, 422], 'Rule.DiscountNeedsDraftInvoice')
        const left = (await http('GET', `/payments/invoices/${iid}`)).json?.data?.discounts?.[0]
        if (left) expectStatus('yazma', 'finance.removeDiscount (kesilmiş fatura → 409)', 'DELETE', `/payments/discounts/${left.id}`,
          await http('DELETE', `/payments/discounts/${left.id}`), [409], 'Rule.DiscountRemoveNeedsDraftInvoice')
      }
    }
  } else skip('finance.applyDiscount', 'POST', '/payments/discounts', 'öğrenci yok')

  // 8) oda takvimi: yalnızca o odanın seansları (telafi/not seansları smoke odasına bağlandı)
  if (ctx.roomId) {
    const rc = await http('GET', `/scheduling/calendar/room/${ctx.roomId}?from=${encodeURIComponent(at(-1, 0))}&to=${encodeURIComponent(at(400, 0))}`)
    const rows = rc.json?.data ?? []
    record({ phase: 'yazma', id: 'scheduling.getRoomCalendar (yalnızca o odanın seansları)', verb: 'GET', url: `/scheduling/calendar/room/${ctx.roomId}`, status: rc.status,
      outcome: rc.status === 200 && Array.isArray(rows) && rows.every(x => x.roomId === ctx.roomId) ? 'ok' : 'type', note: `${rows.length} seans`, mismatches: [] })
  } else skip('scheduling.getRoomCalendar', 'GET', '/scheduling/calendar/room/{id}', 'oda yok')

  // 9) eğitmen hiyerarşisi: bağla → yanlış yoldaki eğitmenle sonlandırma 404 → başlangıçtan önceki tarih reddedilir → sonlandır → ikinci kez reddedilir
  if (ctx.educatorId) {
    const sup = await write('yazma', 'educator.create (denetçi)', 'POST', '/educators', { firstName: 'Smoke', lastName: `Denetçi${RUN}`, titleId: null, email: null, phone: null, employmentType: null, hireDate: null, primaryCampusId: ctx.campusId ?? null })
    const supId = idOf(sup.json?.data)
    if (supId) {
      const edge = (await write('yazma', 'educator.linkHierarchy', 'POST', `/educators/${ctx.educatorId}/hierarchy`, { supervisorId: supId, relationshipId: null, campusId: null, activeFrom: day(-10) })).json?.data
      if (edge?.id) {
        expectStatus('yazma', 'educator.endHierarchy (yoldaki eğitmen astı değil → 404)', 'PATCH', `/educators/${supId}/hierarchy/${edge.id}/end`,
          await http('PATCH', `/educators/${supId}/hierarchy/${edge.id}/end`, { endDate: day(0) }), [404])
        expectStatus('yazma', 'educator.endHierarchy (bitiş < başlangıç → reddedilir)', 'PATCH', `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`,
          await http('PATCH', `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`, { endDate: day(-11) }), [400, 409, 422], 'Rule.HierarchyEndBeforeStart')
        const ended = await write('yazma', 'educator.endHierarchy', 'PATCH', `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`, { endDate: day(-1) })
        record({ phase: 'yazma', id: 'educator.endHierarchy (isActive=false, kayıt durur)', verb: 'PATCH', url: `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`, status: ended.status,
          outcome: ended.json?.data?.isActive === false && ended.json?.data?.activeTo === day(-1) ? 'ok' : 'type', note: `isActive=${ended.json?.data?.isActive} activeTo=${ended.json?.data?.activeTo}`, mismatches: [] })
        const detail = (await http('GET', `/educators/${ctx.educatorId}`)).json?.data
        record({ phase: 'yazma', id: 'educator.get (sona eren ilişki geçmişte görünür)', verb: 'GET', url: `/educators/${ctx.educatorId}`, status: 200,
          outcome: (detail?.supervisors ?? []).some(x => x.id === edge.id && x.isActive === false) ? 'ok' : 'type', note: 'supervisors içinde isActive=false kayıt bekleniyor', mismatches: [] })
        expectStatus('yazma', 'educator.endHierarchy (zaten sona ermiş → reddedilir)', 'PATCH', `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`,
          await http('PATCH', `/educators/${ctx.educatorId}/hierarchy/${edge.id}/end`, { endDate: day(0) }), [400, 409, 422], 'Rule.HierarchyAlreadyEnded')
      }
    }
  } else skip('educator.endHierarchy', 'PATCH', '/educators/{id}/hierarchy/{edgeId}/end', 'eğitmen yok')

  // 9b) bakım ekibi: ekle → listele (rowVersion) → güncelle → bayat rowVersion reddedilir → ekipten çıkar (geçmişte kalır) → benim öğrencilerim / seanslardan eğitmenler
  if (ctx.studentId) {
    const roleId = ((await http('GET', '/reference-data/care_team_role?activeOnly=true')).json?.data ?? [])[0]?.id
    const edu = await write('yazma', 'educator.create (bakım ekibi üyesi)', 'POST', '/educators', { firstName: 'Smoke', lastName: `Bakım${RUN}`, titleId: null, email: null, phone: null, employmentType: null, hireDate: null, primaryCampusId: ctx.campusId ?? null })
    const eduId = idOf(edu.json?.data)
    if (roleId && eduId) {
      const base = `/students/${ctx.studentId}/care-team`
      const created = (await write('yazma', 'careTeam.assign', 'POST', base, { educatorId: eduId, roleId, isPrimary: false, activeFrom: day(-1), activeTo: null, campusId: null, grantTypeId: null, sourceAssignmentId: null, reason: null })).json?.data
      if (created?.id) {
        const list = (await http('GET', base)).json?.data ?? []
        const row = list.find(x => x.id === created.id)
        record({ phase: 'yazma', id: 'careTeam.list (rowVersion döner)', verb: 'GET', url: base, status: 200, outcome: row && row.rowVersion > 0 ? 'ok' : 'type', note: `rowVersion=${row?.rowVersion}`, mismatches: [] })
        const rv = row?.rowVersion ?? 1
        await write('yazma', 'careTeam.update', 'PUT', `${base}/${created.id}`, { roleId, isPrimary: false, activeFrom: day(-1), activeTo: null, campusId: null, reason: 'smoke', rowVersion: rv })
        expectStatus('yazma', 'careTeam.update (bayat rowVersion → reddedilir)', 'PUT', `${base}/${created.id}`,
          await http('PUT', `${base}/${created.id}`, { roleId, isPrimary: false, activeFrom: day(-1), activeTo: null, campusId: null, reason: 'smoke', rowVersion: rv }), [400, 409, 422])
        const cur = (await http('GET', `${base}/${created.id}`)).json?.data
        const rm = await http('DELETE', `${base}/${created.id}`, { reason: 'smoke', rowVersion: cur?.rowVersion ?? rv + 1 })
        record({ phase: 'yazma', id: 'careTeam.remove', verb: 'DELETE', url: `${base}/${created.id}`, status: rm.status, outcome: rm.status < 300 ? 'ok' : classify(rm.status), note: rm.status < 300 ? '' : errNote(rm), mismatches: [] })
        const hist = (await http('GET', `${base}?activeOnly=false`)).json?.data ?? []
        record({ phase: 'yazma', id: 'careTeam.list (sona eren atama geçmişte görünür)', verb: 'GET', url: `${base}?activeOnly=false`, status: 200,
          outcome: hist.some(x => x.id === created.id && x.status === 'ended') ? 'ok' : 'type', note: 'status=ended kayıt bekleniyor', mismatches: [] })
      }
    }
    const my = await http('GET', '/care-team/my-students')
    record({ phase: 'yazma', id: 'careTeam.myStudents', verb: 'GET', url: '/care-team/my-students', status: my.status, outcome: my.status === 200 && Array.isArray(my.json?.data) ? 'ok' : classify(my.status), note: `${(my.json?.data ?? []).length} öğrenci`, mismatches: [] })
    const se = await http('GET', `/students/${ctx.studentId}/session-educators`)
    record({ phase: 'yazma', id: 'students.sessionEducators', verb: 'GET', url: `/students/${ctx.studentId}/session-educators`, status: se.status, outcome: se.status === 200 && Array.isArray(se.json?.data) ? 'ok' : classify(se.status), note: `${(se.json?.data ?? []).length} eğitmen`, mismatches: [] })
  } else skip('careTeam.*', 'POST', '/students/{id}/care-team', 'öğrenci yok')

  // 10) kapalı gün eklerken planlı seansları iptal et: sayı önizlenir, seçilmezse seans kalır, seçilirse gerekçe = kaydın adı
  if (ctx.sessionTypeId && ctx.educatorId) {
    const d2 = day(330), d3 = day(331)
    const mk = async (d, t) => idOf((await http('POST', '/scheduling/sessions', {
      sessionTypeId: ctx.sessionTypeId, startsAt: `${d}T${t}:00Z`, endsAt: `${d}T${t}:40:00Z`,
      campusId: ctx.campusId, roomId: null, title: `Smoke Kapalı Gün ${RUN}`, isMakeup: false, participantStudentIds: [], educatorAssignments: [{ educatorId: ctx.educatorId, role: 'lead' }] })).json?.data)
    const sA = await mk(d2, '10'), sB = await mk(d3, '10')
    const imp = await write('yazma', 'scheduling.getCalendarEntryImpact', 'GET', `/scheduling/calendar-entries/impact?from=${encodeURIComponent(`${d2}T00:00:00Z`)}&to=${encodeURIComponent(nextDay(d2))}`)
    record({ phase: 'yazma', id: 'scheduling.getCalendarEntryImpact (planlı seans sayısı ≥ 1)', verb: 'GET', url: '/scheduling/calendar-entries/impact', status: imp.status,
      outcome: imp.json?.data?.plannedSessionCount >= 1 ? 'ok' : 'type', note: `sayı=${imp.json?.data?.plannedSessionCount}`, mismatches: [] })
    // seçenek işaretsiz: seans kalır
    const keep = await write('yazma', 'scheduling.createCalendarEntry (iptal seçeneği yok)', 'POST', '/scheduling/calendar-entries', { title: `Smoke Kapalı ${RUN} A`, entryType: 'closure', campusId: null, startsAt: `${d2}T00:00:00Z`, endsAt: nextDay(d2), isAllDay: true })
    const stA = sA ? (await http('GET', `/scheduling/sessions/${sA}`)).json?.data : null
    record({ phase: 'yazma', id: 'scheduling.createCalendarEntry (seçilmezse seans olduğu gibi kalır)', verb: 'GET', url: `/scheduling/sessions/${sA}`, status: 200,
      outcome: keep.json?.data?.cancelledSessionCount === 0 && stA?.status === 'scheduled' ? 'ok' : 'type', note: `iptal=${keep.json?.data?.cancelledSessionCount} durum=${stA?.status}`, mismatches: [] })
    // seçenek işaretli: iptal + gerekçe = ad; kayıt silinince seans geri gelmez
    const cut = await write('yazma', 'scheduling.createCalendarEntry (planlı seansları iptal et)', 'POST', '/scheduling/calendar-entries', { title: `Smoke Kapalı ${RUN} B`, entryType: 'closure', campusId: null, startsAt: `${d3}T00:00:00Z`, endsAt: nextDay(d3), isAllDay: true, cancelPlannedSessions: true })
    const stB = sB ? (await http('GET', `/scheduling/sessions/${sB}`)).json?.data : null
    record({ phase: 'yazma', id: 'scheduling.createCalendarEntry (iptal edildi, gerekçe = kaydın adı)', verb: 'GET', url: `/scheduling/sessions/${sB}`, status: 200,
      outcome: cut.json?.data?.cancelledSessionCount >= 1 && stB?.status === 'cancelled' && stB?.cancelReason === `Smoke Kapalı ${RUN} B` ? 'ok' : 'type', note: `iptal=${cut.json?.data?.cancelledSessionCount} durum=${stB?.status} gerekçe=${stB?.cancelReason}`, mismatches: [] })
    if (cut.json?.data?.id) {
      await http('DELETE', `/scheduling/calendar-entries/${cut.json.data.id}`)
      const after = sB ? (await http('GET', `/scheduling/sessions/${sB}`)).json?.data : null
      record({ phase: 'yazma', id: 'scheduling.deleteCalendarEntry (iptal edilen seans geri gelmez)', verb: 'DELETE', url: `/scheduling/calendar-entries/${cut.json.data.id}`, status: 200,
        outcome: after?.status === 'cancelled' ? 'ok' : 'type', note: `durum=${after?.status}`, mismatches: [] })
    }
    if (keep.json?.data?.id) await http('DELETE', `/scheduling/calendar-entries/${keep.json.data.id}`)
  } else skip('scheduling.getCalendarEntryImpact', 'GET', '/scheduling/calendar-entries/impact', 'ön koşul eksik')
  token = adminToken
}

// ── B→A kapanışı: seans notu/hedefi, akademik dönem, taslak sözleşme, modül görünürlüğü ──
// ── Okul danışmanlığı: okul → plan → sözleşme → ziyaret → gözlem → rapor → takip, silme engelleri, raporlama uçları ──
async function consultancyFlows() {
  const skip = (id, verb, url, note) => record({ phase: 'yazma', id, verb, url, status: null, outcome: 'skip', note, mismatches: [] })
  const refId = async (type) => ((await http('GET', `/reference-data/${type}?activeOnly=true`)).json?.data ?? [])[0]?.id ?? null
  const del = async (id, url) => {
    const r = await http('DELETE', url)
    record({ phase: 'yazma', id, verb: 'DELETE', url, status: r.status, outcome: r.status < 300 ? 'ok' : classify(r.status), note: r.status < 300 ? '' : errNote(r), mismatches: [] })
    return r
  }
  const blocked = async (id, url) => expectStatus('yazma', id, 'DELETE', url, await http('DELETE', url), [400, 409, 422])

  const inst = idOf((await write('yazma', 'consultancy.institution.create', 'POST', '/consultancy/institutions', { name: `Smoke Okul ${RUN}`, institutionTypeId: await refId('institution_type'), city: 'İstanbul', district: 'Merkez', contactName: null, contactPhone: null, contactEmail: null })).json?.data)
  if (!inst) { skip('consultancy.*', 'POST', '/consultancy/institutions', 'okul oluşmadı (modül kapalı olabilir)'); return }
  const planBody = (name) => ({ institutionId: inst, name, consultancyTypeId: null, periodStart: day(-10), periodEnd: day(60), scope: 'smoke', leadEducatorId: null })
  const plan = idOf((await write('yazma', 'consultancy.plan.create', 'POST', '/consultancy/plans', planBody(`Smoke Plan ${RUN}`))).json?.data)
  const draftPlan = idOf((await write('yazma', 'consultancy.plan.create (silinecek taslak)', 'POST', '/consultancy/plans', planBody(`Smoke Taslak ${RUN}`))).json?.data)
  if (!plan) return

  // sözleşme: taslak planda oluşur → plan silinemez (sözleşme FK'si); gönder → imzala
  const agrType = await refId('agreement_type')
  const agr = idOf((await write('yazma', 'consultancy.agreement.create', 'POST', '/consultancy/agreements', { consultancyPlanId: plan, title: `Smoke Sözleşme ${RUN}`, agreementTypeId: agrType, description: null, startDate: day(-10), endDate: day(60) })).json?.data)
  if (draftPlan) {
    const dAgr = idOf((await write('yazma', 'consultancy.agreement.create (taslak plan)', 'POST', '/consultancy/agreements', { consultancyPlanId: draftPlan, title: `Smoke Sözleşme2 ${RUN}`, agreementTypeId: agrType, description: null, startDate: null, endDate: null })).json?.data)
    if (dAgr) await blocked('consultancy.plan.delete (sözleşmesi var → reddedilir)', `/consultancy/plans/${draftPlan}`)
    if (dAgr) await del('consultancy.agreement.delete (taslak)', `/consultancy/agreements/${dAgr}`)
    // soft-delete edilmiş sözleşme de FK'yi tuttuğu için plan yine silinemez (500 değil, anlamlı hata)
    if (dAgr) await blocked('consultancy.plan.delete (silinmiş sözleşme de engeller)', `/consultancy/plans/${draftPlan}`)
  }
  await write('yazma', 'consultancy.plan.activate', 'POST', `/consultancy/plans/${plan}/activate`, {})
  if (agr) {
    await write('yazma', 'consultancy.agreement.send', 'POST', `/consultancy/agreements/${agr}/send`, {})
    await write('yazma', 'consultancy.agreement.sign', 'POST', `/consultancy/agreements/${agr}/sign`, { signedDate: day(-1), signedByName: 'Smoke Müdür', fileId: null })
  }

  // ziyaret: oluştur → düzenle → ayrıntı → gözlem ekle/oku/düzenle → gözlemliyken silinemez → gözlem sil → ziyaret sil
  const visitBody = (purpose) => ({ institutionId: inst, consultancyPlanId: plan, visitDate: day(1), visitorId: null, purpose })
  const v1 = idOf((await write('yazma', 'consultancy.visit.create', 'POST', '/consultancy/visits', visitBody('smoke ziyaret'))).json?.data)
  if (v1) {
    await write('yazma', 'consultancy.visit.update', 'PUT', `/consultancy/visits/${v1}`, { visitDate: day(2), visitorId: null, purpose: 'smoke ziyaret (düzenlendi)', consultancyPlanId: plan })
    const vd = (await http('GET', `/consultancy/visits/${v1}`)).json?.data
    record({ phase: 'yazma', id: 'consultancy.visit.get (düzenleme yansır)', verb: 'GET', url: `/consultancy/visits/${v1}`, status: 200, outcome: vd?.purpose === 'smoke ziyaret (düzenlendi)' && vd?.visitDate === day(2) ? 'ok' : 'type', note: `purpose=${vd?.purpose} date=${vd?.visitDate}`, mismatches: [] })
    const obsType = await refId('observation_type')
    const ob = idOf((await write('yazma', 'consultancy.observation.create', 'POST', `/consultancy/visits/${v1}/observations`, { schoolVisitId: v1, observation: 'smoke gözlem', observationTypeId: obsType, subject: 'Konu', recommendations: null })).json?.data)
    if (ob) {
      await write('yazma', 'consultancy.observation.update', 'PUT', `/consultancy/observations/${ob}`, { observation: 'smoke gözlem (düzenlendi)', observationTypeId: obsType, subject: 'Konu', recommendations: 'Öneri' })
      const g = (await http('GET', `/consultancy/observations/${ob}`)).json?.data
      const l = (await http('GET', `/consultancy/visits/${v1}/observations`)).json?.data ?? []
      record({ phase: 'yazma', id: 'consultancy.observation.get/list (düzenleme yansır)', verb: 'GET', url: `/consultancy/observations/${ob}`, status: 200, outcome: g?.observation === 'smoke gözlem (düzenlendi)' && l.some(x => x.id === ob) ? 'ok' : 'type', note: `${l.length} gözlem`, mismatches: [] })
      await blocked('consultancy.visit.delete (gözlemi var → reddedilir)', `/consultancy/visits/${v1}`)
      await del('consultancy.observation.delete', `/consultancy/observations/${ob}`)
    }
    await del('consultancy.visit.delete', `/consultancy/visits/${v1}`)
    expectStatus('yazma', 'consultancy.visit.get (silinen ziyaret → 404)', 'GET', `/consultancy/visits/${v1}`, await http('GET', `/consultancy/visits/${v1}`), [404])
  }

  // ziyaret + rapor + takip: raporlu/takipli ziyaret silinemez; tamamlanan ziyaret düzenlenemez/silinemez
  const v2 = idOf((await write('yazma', 'consultancy.visit.create (rapor/takip için)', 'POST', '/consultancy/visits', visitBody('smoke ziyaret 2'))).json?.data)
  if (v2) {
    // gözlem kilidi: tamamlanmış ziyarette düzenlenir, silinmez (409); rapor yazılınca düzenleme de kapanır
    const ob2 = idOf((await write('yazma', 'consultancy.observation.create (kilit için)', 'POST', `/consultancy/visits/${v2}/observations`, { schoolVisitId: v2, observation: 'kilit gözlemi', observationTypeId: null, subject: null, recommendations: null })).json?.data)
    await write('yazma', 'consultancy.visit.complete', 'POST', `/consultancy/visits/${v2}/complete`, {})
    if (ob2) {
      await write('yazma', 'consultancy.observation.update (tamamlanmış ziyarette düzenlenir)', 'PUT', `/consultancy/observations/${ob2}`, { observation: 'kilit gözlemi (düzenlendi)', observationTypeId: null, subject: null, recommendations: null })
      expectStatus('yazma', 'consultancy.observation.delete (tamamlanmış ziyarette → 409)', 'DELETE', `/consultancy/observations/${ob2}`, await http('DELETE', `/consultancy/observations/${ob2}`), [409])
      const vd = (await http('GET', `/consultancy/visits/${v2}`)).json?.data
      record({ phase: 'yazma', id: 'consultancy.visit.get (hasReports=false)', verb: 'GET', url: `/consultancy/visits/${v2}`, status: 200, outcome: vd?.hasReports === false ? 'ok' : 'type', note: `hasReports=${vd?.hasReports}`, mismatches: [] })
    }
    const rep = idOf((await write('yazma', 'consultancy.report.create (ziyareti)', 'POST', '/consultancy/reports', { title: `Smoke Rapor ${RUN}`, consultancyPlanId: plan, schoolVisitId: v2, summary: 'smoke özet', fileId: null })).json?.data)
    if (rep) {
      const g = (await http('GET', `/consultancy/reports/${rep}`)).json?.data
      record({ phase: 'yazma', id: 'consultancy.report.get', verb: 'GET', url: `/consultancy/reports/${rep}`, status: 200, outcome: g?.schoolVisitId === v2 && g?.summary === 'smoke özet' ? 'ok' : 'type', note: '', mismatches: [] })
      await blocked('consultancy.visit.delete (raporu var → reddedilir)', `/consultancy/visits/${v2}`)
      await blocked('consultancy.plan.delete (aktif plan → reddedilir)', `/consultancy/plans/${plan}`)
    }
    const fu = idOf((await write('yazma', 'consultancy.followUp.create', 'POST', '/consultancy/follow-ups', { title: `Smoke Takip ${RUN}`, consultancyPlanId: plan, schoolVisitId: v2, observationRecordId: null, description: null, dueDate: day(10), assignedTo: null })).json?.data)
    if (fu) {
      await write('yazma', 'consultancy.followUp.start', 'POST', `/consultancy/follow-ups/${fu}/start`, {})
      await blocked('consultancy.visit.delete (takibi var → reddedilir)', `/consultancy/visits/${v2}`)
    }
    if (rep && ob2) {
      expectStatus('yazma', 'consultancy.observation.update (raporlu ziyarette → 409)', 'PUT', `/consultancy/observations/${ob2}`, await http('PUT', `/consultancy/observations/${ob2}`, { observation: 'x', observationTypeId: null, subject: null, recommendations: null }), [409])
      const vd = (await http('GET', `/consultancy/visits/${v2}`)).json?.data
      record({ phase: 'yazma', id: 'consultancy.visit.get (hasReports=true)', verb: 'GET', url: `/consultancy/visits/${v2}`, status: 200, outcome: vd?.hasReports === true ? 'ok' : 'type', note: `hasReports=${vd?.hasReports}`, mismatches: [] })
    }
    expectStatus('yazma', 'consultancy.visit.update (tamamlanmış → reddedilir)', 'PUT', `/consultancy/visits/${v2}`, await http('PUT', `/consultancy/visits/${v2}`, { visitDate: day(3), visitorId: null, purpose: 'x', consultancyPlanId: plan }), [400, 409, 422])
    if (fu) await del('consultancy.followUp.delete', `/consultancy/follow-ups/${fu}`)
    if (rep) await del('consultancy.report.delete', `/consultancy/reports/${rep}`)
    expectStatus('yazma', 'consultancy.report.get (silinen rapor → 404)', 'GET', `/consultancy/reports/${rep}`, await http('GET', `/consultancy/reports/${rep}`), [404])
  }

  // raporlama uçları: sözleşme özeti, okul raporu, sonuçlar, ziyaret geçmişi bu akışın kaydını yansıtır
  const arr = async (url) => (await http('GET', url)).json?.data ?? []
  const agSum = await arr(`/consultancy/reporting/agreements?institutionId=${inst}`)
  record({ phase: 'yazma', id: 'consultancy.reporting.agreements', verb: 'GET', url: '/consultancy/reporting/agreements', status: 200, outcome: agSum.some(x => x.planId === plan && x.totalAgreements >= 1) ? 'ok' : 'type', note: `${agSum.length} plan`, mismatches: [] })
  const instRep = await arr('/consultancy/reporting/institutions')
  record({ phase: 'yazma', id: 'consultancy.reporting.institutions', verb: 'GET', url: '/consultancy/reporting/institutions', status: 200, outcome: instRep.some(x => x.institutionId === inst && x.totalPlans >= 2) ? 'ok' : 'type', note: `${instRep.length} kurum`, mismatches: [] })
  const outc = await arr(`/consultancy/reporting/outcomes?institutionId=${inst}`)
  record({ phase: 'yazma', id: 'consultancy.reporting.outcomes', verb: 'GET', url: '/consultancy/reporting/outcomes', status: 200, outcome: outc.some(x => x.planId === plan && x.visitCount >= 1 && x.completedVisitCount >= 1) ? 'ok' : 'type', note: `${outc.length} plan`, mismatches: [] })
  const hist = await arr(`/consultancy/reporting/visit-history?institutionId=${inst}&dateFrom=${day(-1)}&dateTo=${day(30)}`)
  record({ phase: 'yazma', id: 'consultancy.reporting.visitHistory', verb: 'GET', url: '/consultancy/reporting/visit-history', status: 200, outcome: hist.some(x => x.visitId === v2) ? 'ok' : 'type', note: `${hist.length} ziyaret`, mismatches: [] })

  // kurum silme: planı olan kurum silinemez; taslak plan silinince bile aktif plan + ziyaret kaldığı için engel sürer
  await blocked('consultancy.institution.delete (planı/ziyareti var → reddedilir)', `/consultancy/institutions/${inst}`)
  const emptyInst = idOf((await write('yazma', 'consultancy.institution.create (boş)', 'POST', '/consultancy/institutions', { name: `Smoke Boş Okul ${RUN}`, institutionTypeId: null, city: null, district: null, contactName: null, contactPhone: null, contactEmail: null })).json?.data)
  if (emptyInst) await del('consultancy.institution.delete (kayıtsız)', `/consultancy/institutions/${emptyInst}`)
}

// ── Kamp yönetimi: etkinleştir/pasifleştir/sil, dönem düzenle/sil, bekleme listesi, toplu tamamlama, eğitmen atama (409 kuralları dahil) ──
async function campFlows() {
  const skip = (id, verb, url, note) => record({ phase: 'yazma', id, verb, url, status: null, outcome: 'skip', note, mismatches: [] })
  const ok = (id, verb, url, cond, note = '') => record({ phase: 'yazma', id, verb, url, status: 200, outcome: cond ? 'ok' : 'type', note, mismatches: [] })
  const post = (id, url, body = {}) => write('yazma', id, 'POST', url, body)
  const conflict = async (id, verb, url, body) => expectStatus('yazma', id, verb, url, await http(verb, url, body), [409])
  const mkStudent = async (n) => idOf((await write('yazma', `camp.student (${n})`, 'POST', '/students', { firstName: 'Smoke', lastName: `Kamp${n}${RUN}`, studentNo: `SK${n}-${RUN}`, nationalId: null,
    birthDate: '2016-05-01', gender: null, primaryCampusId: ctx.campusId ?? null, statusId: null, leadId: null, notes: 'smoke-api' })).json?.data)

  const camp = idOf((await write('yazma', 'camps.create (yönetim)', 'POST', '/camps', { campusId: ctx.campusId, code: `SY-${RUN}`, name: `Smoke Yönetim Kampı ${RUN}`, description: 'smoke', location: 'Test', capacity: 10 })).json?.data)
  if (!camp) { skip('camps.*', 'POST', '/camps', 'kamp oluşmadı (modül kapalı olabilir)'); return }

  // dönem: düzenle / kontenjan kuralı / sil
  const spare = idOf((await post('camps.createPeriod (silinecek)', `/camps/${camp}/periods`, { name: 'Silinecek', startDate: day(60), endDate: day(70), capacity: 2 })).json?.data)
  if (spare) {
    await write('yazma', 'camps.updatePeriod', 'PUT', `/camps/periods/${spare}`, { name: 'Silinecek (düzenlendi)', startDate: day(61), endDate: day(71), capacity: 3 })
    const sp = (await http('GET', `/camps/periods/${spare}`)).json?.data
    ok('camps.getPeriod (düzenleme yansır)', 'GET', `/camps/periods/${spare}`, sp?.name === 'Silinecek (düzenlendi)' && sp?.capacity === 3, `ad=${sp?.name} kontenjan=${sp?.capacity}`)
  }

  // başlamış dönem (kontenjan 2): kayıt, kontenjan, bekleme listesi, tamamlama
  const live = idOf((await post('camps.createPeriod (başlamış)', `/camps/${camp}/periods`, { name: 'Devam Eden', startDate: day(-3), endDate: day(10), capacity: 2 })).json?.data)
  if (!live) { skip('camps.enrollment.*', 'POST', `/camps/${camp}/periods`, 'dönem oluşmadı'); return }
  const [s1, s2, s3] = [ctx.studentId ?? await mkStudent(1), await mkStudent(2), await mkStudent(3)]
  const enroll = (id, sid, status) => post(id, `/camps/periods/${live}/enrollments`, { studentId: sid, status })
  const en1 = idOf((await enroll('camps.enroll (kayıtlı 1)', s1, 'enrolled')).json?.data)
  const en2 = idOf((await enroll('camps.enroll (kayıtlı 2)', s2, 'enrolled')).json?.data)
  if (!en1 || !en2) { skip('camps.enrollment.*', 'POST', `/camps/periods/${live}/enrollments`, 'kayıt oluşmadı'); return }
  await conflict('camps.enroll (aynı öğrenci tekrar → 409)', 'POST', `/camps/periods/${live}/enrollments`, { studentId: s1, status: 'enrolled' })
  await conflict('camps.enroll (kontenjan dolu → 409)', 'POST', `/camps/periods/${live}/enrollments`, { studentId: s3, status: 'enrolled' })
  const en3 = idOf((await enroll('camps.enroll (bekleme listesi)', s3, 'waitlist')).json?.data)
  await conflict('camps.updatePeriod (kontenjan < kayıtlı → 409)', 'PUT', `/camps/periods/${live}`, { name: 'Devam Eden', startDate: day(-3), endDate: day(10), capacity: 1 })
  await conflict('camps.deletePeriod (kayıtlı öğrenci var → 409)', 'DELETE', `/camps/periods/${live}`)
  await conflict('camps.deactivate (dönemde kayıtlı öğrenci var → 409)', 'POST', `/camps/${camp}/deactivate`, {})
  await conflict('camps.delete (kayıtlı öğrenci var → 409)', 'DELETE', `/camps/${camp}`)
  await conflict('camps.activate (zaten aktif → 409)', 'POST', `/camps/${camp}/activate`, {})
  await conflict('camps.promote (kayıtlı olan → 409)', 'POST', `/camps/enrollments/${en2}/promote`, {})
  await post('camps.moveToWaitlist', `/camps/enrollments/${en1}/waitlist`)
  await conflict('camps.moveToWaitlist (zaten bekleme listesinde → 409)', 'POST', `/camps/enrollments/${en1}/waitlist`, {})
  await conflict('camps.complete (bekleme listesinde → 409)', 'POST', `/camps/enrollments/${en1}/complete`, {})
  await post('camps.promote (bekleme listesinden kayıtlıya)', `/camps/enrollments/${en1}/promote`)
  if (en3) await conflict('camps.promote (kontenjan hâlâ dolu → 409)', 'POST', `/camps/enrollments/${en3}/promote`, {})
  // toplu tamamlama: yalnızca 'kayıtlı' olanlar; başlamamış dönemde 409; bilinmeyen dönem 404
  if (spare) await conflict('camps.bulkComplete (dönem başlamadı → 409)', 'POST', `/camps/periods/${spare}/bulk-complete`, {})
  const bulk = await post('camps.bulkComplete', `/camps/periods/${live}/bulk-complete`)
  ok('camps.bulkComplete (2 kayıtlı öğrenci tamamlandı)', 'POST', `/camps/periods/${live}/bulk-complete`, bulk.json?.data === 2, `tamamlanan=${bulk.json?.data}`)
  const done = (await http('GET', `/camps/enrollments?campPeriodId=${live}&status=completed`)).json?.data
  ok('camps.listEnrollments (tamamlananlar, öğrenci adıyla)', 'GET', `/camps/enrollments?campPeriodId=${live}`, done?.totalCount === 2 && !!done?.items?.[0]?.studentName, `adet=${done?.totalCount} ad=${done?.items?.[0]?.studentName}`)
  expectStatus('yazma', 'camps.bulkComplete (bilinmeyen dönem → 404)', 'POST', '/camps/periods/00000000-0000-7000-8000-000000000001/bulk-complete', await http('POST', '/camps/periods/00000000-0000-7000-8000-000000000001/bulk-complete', {}), [404])
  if (en3) await post('camps.withdraw (bekleyen öğrenci)', `/camps/enrollments/${en3}/withdraw`)

  // eğitmen: ata / aynı kapsamda tekrar 409 / rol değiştir / kaldır
  if (ctx.educatorId) {
    const asg = idOf((await post('camps.assignEducator', `/camps/${camp}/educators`, { campId: camp, educatorId: ctx.educatorId, role: 'assistant', campPeriodId: null, campActivityId: null })).json?.data)
    await conflict('camps.assignEducator (aynı kapsamda tekrar → 409)', 'POST', `/camps/${camp}/educators`, { campId: camp, educatorId: ctx.educatorId, role: 'lead', campPeriodId: null, campActivityId: null })
    if (asg) {
      await write('yazma', 'camps.updateEducatorRole', 'PATCH', `/camps/educators/${asg}/role`, { role: 'lead' })
      const list = (await http('GET', `/camps/educators?campId=${camp}`)).json?.data
      ok('camps.listEducators (rol + ad)', 'GET', `/camps/educators?campId=${camp}`, list?.[0]?.role === 'lead' && !!list?.[0]?.educatorName, `rol=${list?.[0]?.role} ad=${list?.[0]?.educatorName}`)
      expectStatus('yazma', 'camps.updateEducatorRole (geçersiz rol → 400/422)', 'PATCH', `/camps/educators/${asg}/role`, await http('PATCH', `/camps/educators/${asg}/role`, { role: 'boss' }), [400, 422])
      await write('yazma', 'camps.removeEducator', 'DELETE', `/camps/educators/${asg}`)
    }
  } else skip('camps.educators', 'POST', `/camps/${camp}/educators`, 'eğitmen yok')

  // pasifleştir (bitmemiş dönemde kayıt kalmadı → olur) → pasif kampa kayıt 409 → etkinleştir → silme
  await write('yazma', 'camps.deactivate', 'POST', `/camps/${camp}/deactivate`, {})
  await conflict('camps.enroll (pasif kamp → 409)', 'POST', `/camps/periods/${live}/enrollments`, { studentId: await mkStudent(4), status: 'enrolled' })
  await write('yazma', 'camps.activate', 'POST', `/camps/${camp}/activate`, {})
  if (spare) await write('yazma', 'camps.deletePeriod (boş dönem)', 'DELETE', `/camps/periods/${spare}`)
  const delRes = await http('DELETE', `/camps/${camp}`)
  record({ phase: 'yazma', id: 'camps.delete (yalnızca tamamlanmış kayıt → silinir)', verb: 'DELETE', url: `/camps/${camp}`, status: delRes.status, outcome: delRes.status < 300 ? 'ok' : classify(delRes.status), note: delRes.status < 300 ? '' : errNote(delRes), mismatches: [] })
  expectStatus('yazma', 'camps.get (silinen kamp → 404)', 'GET', `/camps/${camp}`, await http('GET', `/camps/${camp}`), [404])
}

// ── Anketler: tanım → soru düzenleme → veli portalı (bekleyen, yanıt, tekrar, yabancı çocuk) → sonuçlar → kilit → silme; geri bildirim → KPI ──
async function surveyFlows() {
  const skip = (id, verb, url, note) => record({ phase: 'yazma', id, verb, url, status: null, outcome: 'skip', note, mismatches: [] })
  const adminToken = token
  const ok = (id, verb, url, cond, note = '') => record({ phase: 'yazma', id, verb, url, status: 200, outcome: cond ? 'ok' : 'type', note, mismatches: [] })
  const typeId = ((await http('GET', '/reference-data/survey_type?activeOnly=true')).json?.data ?? [])[0]?.id ?? null
  const q = (text, type, required, order, opts = []) => ({ questionText: text, questionType: type, isRequired: required, sortOrder: order, answerOptions: opts })
  const body = (title) => ({ title, target: 'guardian', typeId, description: 'smoke', questions: [
    q('Genel memnuniyet', 'rating', true, 1),
    q('İletişim', 'multiple_choice', true, 2, [{ optionText: 'Evet', optionValue: '10', sortOrder: 1 }, { optionText: 'Hayır', optionValue: '0', sortOrder: 2 }]),
    q('Öneri', 'text', false, 3)] })

  const created = await write('yazma', 'surveys.create', 'POST', '/surveys', body(`Smoke Anket ${RUN}`))
  const sid = idOf(created.json?.data)
  if (!sid) { skip('surveys.*', 'POST', '/surveys', 'anket oluşmadı (modül kapalı olabilir)'); return }
  expectStatus('yazma', 'surveys.create (çoktan seçmeli tek seçenek → reddedilir)', 'POST', '/surveys',
    await http('POST', '/surveys', { ...body('x'), questions: [q('Tek', 'multiple_choice', false, 1, [{ optionText: 'A', optionValue: null, sortOrder: 1 }])] }), [400, 422])

  // soru düzenleme: yanıt yokken soru/seçenek değişir
  let sv = (await http('GET', `/surveys/${sid}`)).json?.data
  ok('surveys.get (responseCount=0, 3 soru)', 'GET', `/surveys/${sid}`, sv?.responseCount === 0 && sv?.questions?.length === 3, `soru=${sv?.questions?.length} yanıt=${sv?.responseCount}`)
  const [qr, qc] = sv.questions
  await write('yazma', 'surveys.update (soru/seçenek düzenle)', 'PUT', `/surveys/${sid}`, {
    title: `Smoke Anket ${RUN}`, description: 'smoke (düzenlendi)', target: 'guardian', typeId, isActive: true, rowVersion: sv.rowVersion,
    questions: [
      q('Genel memnuniyet (düzenlendi)', 'rating', true, 1), 
      q('İletişim', 'multiple_choice', true, 2, [{ optionText: 'Evet', optionValue: '10', sortOrder: 1 }, { optionText: 'Kısmen', optionValue: '5', sortOrder: 2 }, { optionText: 'Hayır', optionValue: '0', sortOrder: 3 }]),
      q('Öneri', 'text', false, 3)].map((x, i) => ({ ...x, id: [qr.id, qc.id, sv.questions[2].id][i], answerOptions: x.answerOptions.map((o, j) => ({ ...o, id: i === 1 && j !== 1 ? qc.answerOptions[j === 0 ? 0 : 1]?.id : undefined })) })) })
  sv = (await http('GET', `/surveys/${sid}`)).json?.data
  ok('surveys.get (düzenleme yansır)', 'GET', `/surveys/${sid}`, sv?.questions?.[0]?.questionText === 'Genel memnuniyet (düzenlendi)' && sv?.questions?.[1]?.answerOptions?.length === 3,
    `q1=${sv?.questions?.[0]?.questionText} seçenek=${sv?.questions?.[1]?.answerOptions?.length}`)
  expectStatus('yazma', 'surveys.update (başka anketin soru kimliği → reddedilir)', 'PUT', `/surveys/${sid}`, await http('PUT', `/surveys/${sid}`, {
    title: 'x', target: 'guardian', isActive: true, rowVersion: sv.rowVersion, questions: [{ ...q('x', 'text', false, 1), id: '00000000-0000-7000-8000-000000000001' }] }), [400, 422])

  // veli portalı
  const guardianMe = await login(fx.guardian.username, fx.guardian.password, 'giriş (veli, anket)')
  const pend = (await http('GET', '/surveys/pending')).json?.data ?? []
  const mine = pend.find(p => p.surveyId === sid)
  ok('surveys.pending (veli: yeni anket bekliyor)', 'GET', '/surveys/pending', !!mine && mine.students.length >= 1 && mine.questions.length === 3, `${pend.length} bekleyen`)
  const child = mine?.students?.[0]?.studentId
  if (!mine || !child) { skip('surveys.responses', 'POST', '/surveys/responses', 'veli1 için bekleyen anket/çocuk yok'); token = adminToken; return }
  const ans = (rating = 5, text = 'smoke yanıt') => [
    { questionId: mine.questions[0].id, answerText: null, answerOptionId: null, numericValue: rating },
    { questionId: mine.questions[1].id, answerText: null, answerOptionId: mine.questions[1].answerOptions[0].id, numericValue: null },
    { questionId: mine.questions[2].id, answerText: text, answerOptionId: null, numericValue: null }]
  expectStatus('yazma', 'surveys.responses (zorunlu soru boş → reddedilir)', 'POST', '/surveys/responses',
    await http('POST', '/surveys/responses', { surveyId: sid, studentId: child, answers: [ans()[2]] }), [400, 422])
  expectStatus('yazma', 'surveys.responses (puan 9 → reddedilir)', 'POST', '/surveys/responses',
    await http('POST', '/surveys/responses', { surveyId: sid, studentId: child, answers: ans(9) }), [400, 422])
  await write('yazma', 'surveys.responses (veli yanıtlar)', 'POST', '/surveys/responses', { surveyId: sid, studentId: child, answers: ans() })
  expectStatus('yazma', 'surveys.responses (aynı çocuk için ikinci yanıt → reddedilir)', 'POST', '/surveys/responses',
    await http('POST', '/surveys/responses', { surveyId: sid, studentId: child, answers: ans() }), [400, 409, 422], 'Rule.SurveyAlreadySubmitted')
  // yabancı çocuk (aynı kurumda başka velinin çocuğu) → 403
  token = adminToken
  const foreign = ((await http('GET', '/students?page=1&pageSize=100')).json?.data?.items ?? []).find(x => x.studentNo === fx.isolation.foreignChildStudentNo)?.id
  token = (await http('POST', '/auth/login', { username: fx.guardian.username, password: fx.guardian.password })).json?.data?.accessToken ?? token
  if (foreign) expectStatus('izolasyon', 'surveys.responses (veli1 → başkasının çocuğu → 403)', 'POST', '/surveys/responses',
    await http('POST', '/surveys/responses', { surveyId: sid, studentId: foreign, answers: ans() }), [403], 'Rule.GuardianNoActiveAccess')
  const after = (await http('GET', '/surveys/pending')).json?.data ?? []
  const left = after.find(p => p.surveyId === sid)
  ok('surveys.pending (yanıtlanan çocuk listeden düşer)', 'GET', '/surveys/pending', !left || !left.students.some(x => x.studentId === child), `kalan=${left?.students?.length ?? 0}`)
  expectStatus('izolasyon', 'surveys.list (veli survey:read yok → 403)', 'GET', '/surveys', await http('GET', '/surveys?page=1&pageSize=5'), [403])
  expectStatus('izolasyon', 'surveys.results (veli → 403)', 'GET', `/surveys/${sid}/results`, await http('GET', `/surveys/${sid}/results`), [403])
  expectStatus('izolasyon', 'surveys.parentFeedback.list (veli survey listesi yetkisi yok → 403)', 'GET', '/surveys/parent-feedback', await http('GET', '/surveys/parent-feedback?page=1&pageSize=5'), [403])

  // geri bildirim: kendi çocuğunun seansı → eğitmene yazılır; yabancı eğitmen/seans reddedilir
  const sessions = ((await http('GET', `/portal/students/${child}/sessions?page=1&pageSize=50`)).json?.data ?? []).filter(x => x.status === 'completed')
  let credited = null
  if (sessions.length) {
    const fb = await write('yazma', 'surveys.parentFeedback.create (kendi çocuğunun seansı)', 'POST', '/surveys/parent-feedback', { guardianId: null, educatorId: null, sessionId: sessions[0].id, rating: 4, comment: `smoke ${RUN}` })
    credited = fb.json?.data?.educatorId ?? null
    ok('surveys.parentFeedback.create (seansın eğitmenine yazıldı)', 'POST', '/surveys/parent-feedback', fb.status < 300 && !!credited, `educatorId=${credited}`)
  } else skip('surveys.parentFeedback.create (seans)', 'POST', '/surveys/parent-feedback', 'veli1 çocuğunun tamamlanmış seansı yok')
  await write('yazma', 'surveys.parentFeedback.create (genel yorum)', 'POST', '/surveys/parent-feedback', { guardianId: null, educatorId: null, sessionId: null, rating: 5, comment: `smoke genel ${RUN}` })
  expectStatus('yazma', 'surveys.parentFeedback.create (puan 7 → reddedilir)', 'POST', '/surveys/parent-feedback', await http('POST', '/surveys/parent-feedback', { rating: 7 }), [400, 422])
  expectStatus('yazma', 'surveys.parentFeedback.create (puan/yorum yok → reddedilir)', 'POST', '/surveys/parent-feedback', await http('POST', '/surveys/parent-feedback', {}), [400, 422])
  token = adminToken
  if (ctx.educatorId) expectStatus('izolasyon', 'surveys.parentFeedback.create (veli → çocuğuyla ilgisiz eğitmen → 403)', 'POST', '/surveys/parent-feedback', await (async () => {
    token = (await http('POST', '/auth/login', { username: fx.guardian.username, password: fx.guardian.password })).json?.data?.accessToken ?? token
    const r = await http('POST', '/surveys/parent-feedback', { educatorId: ctx.educatorId, rating: 1, comment: 'smoke ilgisiz' })
    token = adminToken
    return r
  })(), [403], 'Rule.GuardianNoActiveAccess')
  token = (await http('POST', '/auth/login', { username: fx.guardian.username, password: fx.guardian.password })).json?.data?.accessToken ?? token
  const myFb = (await http('GET', '/surveys/parent-feedback/mine?page=1&pageSize=50&sortDirection=desc')).json?.data
  ok('surveys.parentFeedback.mine (yalnızca kendi kayıtları, ad dolu)', 'GET', '/surveys/parent-feedback/mine',
    (myFb?.items ?? []).some(f => f.comment === `smoke genel ${RUN}`) && (myFb?.items ?? []).every(f => f.guardianName), `${myFb?.totalCount} kayıt`)
  token = adminToken

  // yönetici: sonuçlar, kilit, KPI, silme
  const res = (await http('GET', `/surveys/${sid}/results`)).json?.data
  const rq = res?.questions?.find(x => x.questionType === 'rating')
  const cq = res?.questions?.find(x => x.questionType === 'multiple_choice')
  ok('surveys.results (soru bazında özet)', 'GET', `/surveys/${sid}/results`, res?.responseCount === 1 && rq?.average === 5 && cq?.distribution?.find(b => b.label === 'Evet')?.count === 1
    && res.questions.find(x => x.questionType === 'text')?.textAnswers?.includes('smoke yanıt'), `yanıt=${res?.responseCount} ort=${rq?.average}`)
  const resp = (await http('GET', `/surveys/responses?surveyId=${sid}&isSubmitted=true&page=1&pageSize=5`)).json?.data
  ok('surveys.responses.list (veli ve öğrenci adı dolu)', 'GET', '/surveys/responses', resp?.items?.length === 1 && !!resp.items[0].guardianName && !!resp.items[0].studentName, JSON.stringify(resp?.items?.[0] ?? null))
  const locked = (await http('GET', `/surveys/${sid}`)).json?.data
  ok('surveys.get (yanıt sonrası responseCount=1)', 'GET', `/surveys/${sid}`, locked?.responseCount === 1, `yanıt=${locked?.responseCount}`)
  expectStatus('yazma', 'surveys.update (yanıt alınmış anketin soruları → 409)', 'PUT', `/surveys/${sid}`, await http('PUT', `/surveys/${sid}`, {
    title: locked.title, target: 'guardian', isActive: true, rowVersion: locked.rowVersion, questions: [q('Yeni', 'text', false, 1)] }), [409], 'Rule.SurveyQuestionsLocked')
  await write('yazma', 'surveys.update (yanıt alınmış anket: yalnızca başlık)', 'PUT', `/surveys/${sid}`, { title: `Smoke Anket ${RUN} (kapalı)`, description: null, target: 'guardian', typeId, isActive: false, rowVersion: locked.rowVersion })

  if (credited) {
    const t0 = day(-60), t1 = day(1)
    const snap = await write('yazma', 'performance-kpi.compute (geri bildirim puanı KPI veli memnuniyetine yansır)', 'POST', `/performance-kpi/educators/${credited}/compute`, { educatorId: credited, periodStart: t0, periodEnd: t1 })
    const avg = snap.json?.data?.parentFeedbackAvg
    ok('kpi.parentFeedbackAvg (veli puanı yansıdı)', 'POST', `/performance-kpi/educators/${credited}/compute`, typeof avg === 'number' && avg >= 1 && avg <= 5, `parentFeedbackAvg=${avg}`)
  }

  const del = async (id, url) => { const r = await http('DELETE', url); record({ phase: 'yazma', id, verb: 'DELETE', url, status: r.status, outcome: r.status < 300 ? 'ok' : classify(r.status), note: r.status < 300 ? '' : errNote(r), mismatches: [] }) }
  await del('surveys.delete (yanıtlı anket → pasife alınır)', `/surveys/${sid}`)
  const kept = (await http('GET', `/surveys/${sid}`)).json?.data
  ok('surveys.get (yanıtlı anket silinmedi, pasif)', 'GET', `/surveys/${sid}`, kept?.isActive === false && kept?.responseCount === 1, `aktif=${kept?.isActive}`)
  const empty = idOf((await write('yazma', 'surveys.create (silinecek)', 'POST', '/surveys', body(`Smoke Boş Anket ${RUN}`))).json?.data)
  if (empty) {
    await del('surveys.delete (yanıtsız anket → silinir)', `/surveys/${empty}`)
    expectStatus('yazma', 'surveys.get (silinen anket → 404)', 'GET', `/surveys/${empty}`, await http('GET', `/surveys/${empty}`), [404])
  }
  void guardianMe
  token = adminToken
}

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
    // not yazmak için oturum açan kullanıcı bir eğitmen kaydına bağlı olmalı (ActingEducator). Demo yöneticisi kendi
    // (ders vermeyen) eğitmen kaydına bağlıdır (demo-seed people.mjs); bağlı değilse geçici olarak bağlanır ve sonra çözülür.
    if (sid && ctx.adminUserId) {
      const own = (await http('GET', `/educators/by-user/${ctx.adminUserId}`)).json?.data
      const link = own?.id
        ? { status: 200 }
        : await write('yazma', 'educators.linkUserAccount (not yazarı)', 'PUT', `/educators/${ctx.educatorId}/user-account`, { userId: ctx.adminUserId })
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
        if (!own?.id) await http('DELETE', `/educators/${ctx.educatorId}/user-account`)
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
  // Portal BEP özeti: yalnızca onaylı/yürürlükteki ve veliye açık planlar (taslak/incelemede/kapalı gelmez)
  for (const child of mine) {
    const r = await http('GET', `/portal/students/${child.id ?? child.studentId}/bep`)
    const plans = r.json?.data ?? []
    const bad = plans.filter(p => !['approved', 'active'].includes(p.status))
    record({ phase: 'izolasyon', id: `isolation.portal.bep (veli1 → ${child.firstName ?? ''})`, verb: 'GET', url: '/portal/students/{id}/bep', status: r.status,
      outcome: bad.length ? 'leak' : r.status === 200 ? 'ok' : r.status >= 500 ? '5xx' : '4xx',
      note: bad.length ? `onaysız plan veliye göründü: ${bad.map(p => p.status).join(', ')}` : `beklenen: yalnızca onaylı/aktif plan(lar) (${plans.length})`, mismatches: [] })
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
await dailyOpsFlows()
await consultancyFlows()
await surveyFlows()
await campFlows()
await sweep()
await isolation()
await guardianRound()
await finish()
