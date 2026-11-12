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
    return { status: res.status, json, text, ms: Date.now() - t0 }
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
    listCache.set(listPath, { status: res.status, id: arr.find(x => x?.id)?.id, empty: !arr.length })
  }
  const c = listCache.get(listPath)
  if (!c.id) throw new Skip(c.status >= 400 ? `kaynak liste ${listPath} → HTTP ${c.status}` : `kaynak liste ${listPath} boş (veri yok)`)
  return c.id
}
async function resolveParam(ep, expr) {
  const name = expr.split('.').pop()
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
async function login() {
  const res = await http('POST', '/auth/login', { username: fx.admin.username, password: fx.admin.password })
  const ep = byId['post:auth.login'] // auth.service yoksa frontend tipi store'dadır; zarf kontrolü yeterli
  const { mismatches } = checkAgainst(ep, res)
  const d = res.json?.data
  for (const k of ['accessToken', 'refreshToken', 'expiresAt', 'fullName', 'userId', 'corporationId'])
    if (typeof d?.[k] !== 'string') mismatches.push(`data.${k}: string bekleniyor (LoginResult)`)
  record({ phase: 'giriş', id: 'auth.login', verb: 'POST', url: '/auth/login', status: res.status,
    outcome: res.status >= 400 || res.status === 0 ? classify(res.status) : mismatches.length ? 'type' : 'ok',
    note: res.status >= 400 || res.status === 0 ? errNote(res) : '', mismatches })
  if (!d?.accessToken) { console.error('Giriş başarısız:', res.status, errNote(res)); await finish(true) }
  token = d.accessToken
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

async function writeFlows() {
  // A) kamp + dönem
  let r = await write('yazma', 'camp.create', 'POST', '/camps', {
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

async function sweep() {
  const gets = endpoints.filter(e => e.verb === 'get' && (!only || e.id.startsWith(only)))
  for (const ep of gets) {
    let url
    try { url = await buildUrl(ep) } catch (e) {
      if (e instanceof Skip) { record(await probeRoute(ep, e.message)); continue }
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
    record({ phase: 'GET', id: ep.id, verb: 'GET', url, status: res.status, outcome, note, mismatches, file: ep.file, ms: res.ms })
  }
}

// ── Rapor ───────────────────────────────────────────────────────────────────
async function finish(abort = false) {
  fs.mkdirSync(path.join(here, 'out'), { recursive: true })
  const cnt = (list, o) => list.filter(r => r.outcome === o).length
  const groups = {}
  for (const r of results) { const g = r.phase === 'GET' ? r.id.split('.')[0] : `(${r.phase})`; (groups[g] ||= []).push(r) }
  const rows = Object.entries(groups).sort().map(([g, l]) => ({ grup: g, toplam: l.length, basarili: cnt(l, 'ok'), '4xx': cnt(l, '4xx'), '5xx': cnt(l, '5xx'), tipUyusmazligi: cnt(l, 'type'), atlandi: cnt(l, 'skip') }))
  const total = { grup: 'TOPLAM', toplam: results.length, basarili: cnt(results, 'ok'), '4xx': cnt(results, '4xx'), '5xx': cnt(results, '5xx'), tipUyusmazligi: cnt(results, 'type'), atlandi: cnt(results, 'skip') }
  console.table([...rows, total])
  const unvalidated = results.filter(r => r.outcome === 'ok' && /boş dizi/.test(r.note || '')).length
  console.log(`Not: 'başarılı' sayısının ${unvalidated} tanesinde liste boştu; öğe alanları doğrulanamadı.`)
  const bad = results.filter(r => r.outcome !== 'ok')
  let md = `# Duman testi raporu\n\n- Tarih: ${new Date().toISOString()}\n- API: ${API}\n- Kurum: ${fx.tenant.code}\n\n`
  md += `| Grup | Toplam | Başarılı | 4xx | 5xx | Tip uyuşmazlığı | Atlandı |\n|---|---|---|---|---|---|---|\n`
  for (const r of [...rows, total]) md += `| ${r.grup} | ${r.toplam} | ${r.basarili} | ${r['4xx']} | ${r['5xx']} | ${r.tipUyusmazligi} | ${r.atlandi} |\n`
  md += `\n> 'Başarılı' çağrıların ${unvalidated} tanesinde liste boş döndü; o çağrılarda öğe alanları doğrulanamadı.\n`
  md += `\n## Başarısız / uyuşmayan / atlanan çağrılar\n\n| Sonuç | HTTP | Çağrı | Ayrıntı |\n|---|---|---|---|\n`
  const order = { '5xx': 0, '4xx': 1, type: 2, neterr: 3, skip: 4 }
  for (const r of bad.sort((a, b) => (order[a.outcome] ?? 9) - (order[b.outcome] ?? 9))) {
    const detail = [r.note, ...(r.mismatches || [])].filter(Boolean).join(' · ').replace(/\|/g, '\\|')
    md += `| ${r.outcome} | ${r.status ?? '-'} | ${r.verb} ${r.id} \`${r.url}\` | ${detail} |\n`
  }
  fs.writeFileSync(path.join(here, 'out/report.json'), JSON.stringify({ at: new Date().toISOString(), api: API, results }, null, 2))
  fs.writeFileSync(path.join(here, 'out/report.md'), md)
  console.log(`\nRapor: scripts/smoke-api/out/report.md  (json: out/report.json)`)
  process.exit(abort || (strict && bad.some(r => r.outcome !== 'skip')) ? 1 : 0)
}

await login()
await setup()
await writeFlows()
await sweep()
await finish()
