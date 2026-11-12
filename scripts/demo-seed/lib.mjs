// Demo seed ortak yardımcıları: HTTP istemcisi (oturum başına token), adım sarmalayıcı, SQL, rastgelelik.
import fs from 'node:fs'
import path from 'node:path'
import { execFileSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

export const here = path.dirname(fileURLToPath(import.meta.url))
export const fx = JSON.parse(fs.readFileSync(path.join(here, 'fixtures.json'), 'utf8'))
export const API = (process.env.DEMO_API || fx.apiBase).replace(/\/$/, '')
export const DB_CONTAINER = process.env.DB_CONTAINER || 'aynesil-db'

export class Client {
  constructor(label) { this.label = label; this.token = null }
  async raw(method, p, body) {
    const res = await fetch(API + p, {
      method,
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...(this.token ? { Authorization: `Bearer ${this.token}` } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(60_000),
    })
    const text = await res.text()
    let json = null
    try { json = text ? JSON.parse(text) : null } catch { /* JSON değil */ }
    return { status: res.status, json, text }
  }
  async login(username, password) {
    const r = await this.raw('POST', '/auth/login', { username, password })
    if (!r.json?.data?.accessToken) throw new Error(`giriş başarısız (${username}): HTTP ${r.status} ${r.text.slice(0, 200)}`)
    this.token = r.json.data.accessToken
    this.me = r.json.data
    return r.json.data
  }
  /** Başarısızsa fırlatır; başarılıysa `data` döner. */
  async call(method, p, body) {
    const r = await this.raw(method, p, body)
    if (r.status >= 400 || r.json?.success === false) {
      const msg = [r.json?.message, r.json?.errors ? JSON.stringify(r.json.errors) : ''].filter(Boolean).join(' ') || r.text.slice(0, 300)
      const e = new Error(`${method} ${p} → HTTP ${r.status}: ${msg}`); e.status = r.status; throw e
    }
    return r.json?.data
  }
  get(p) { return this.call('GET', p) }
  post(p, b) { return this.call('POST', p, b ?? {}) }
  put(p, b) { return this.call('PUT', p, b) }
  patch(p, b) { return this.call('PATCH', p, b) }
  /** Sayfalı ya da düz liste → dizi. */
  async list(p, pageSize = 100) {
    const out = []
    for (let page = 1; page < 50; page++) {
      const d = await this.get(p + (p.includes('?') ? '&' : '?') + `page=${page}&pageSize=${pageSize}`)
      if (Array.isArray(d)) return d
      out.push(...(d?.items ?? []))
      if (!d?.hasNextPage) break
    }
    return out
  }
}
export const idOf = (d) => (typeof d === 'string' ? d : d?.id)

// ── Adım günlüğü ────────────────────────────────────────────────────────────
export const log = { ok: 0, fail: [], skip: 0, known: [] }
export async function step(label, fn) {
  try { const r = await fn(); log.ok++; return r }
  catch (e) { log.fail.push(`${label}: ${e.message}`); console.log(`   ✗ ${label}: ${e.message.slice(0, 260)}`); return undefined }
}
export const section = (t) => console.log(`\n▶ ${t}`)
export const info = (t) => console.log(`   ${t}`)

/** Var olanı bul (anahtar fonksiyonuyla), yoksa oluştur. Dönüş: öğe (en az id). */
export async function ensure(listPath, matchFn, createFn, label) {
  const items = await client_list(listPath)
  const found = items.find(matchFn)
  if (found) { log.skip++; return found }
  const created = await createFn()
  return typeof created === 'string' ? { id: created } : created
  async function client_list(p) { return ensure.client.list(p) }
}

// ── SQL (owner rolü; RLS'yi aşar — yalnızca API'nin yapamadığı işler için) ─
export function sql(query) {
  const user = process.env.POSTGRES_USER || 'aynesil_owner'
  return execFileSync('docker', ['exec', '-i', DB_CONTAINER, 'psql', '-U', user, '-d', process.env.POSTGRES_DB || 'aynesil', '-v', 'ON_ERROR_STOP=1', '-Atq'],
    { input: query, encoding: 'utf8' }).trim()
}
export const q = (s) => `'${String(s).replace(/'/g, "''")}'`
export const sqlLog = []   // README'ye / konsola: SQL ile yapılanlar
export function sqlStep(what, query) { sqlLog.push(what); return sql(query) }

// ── Tarih yardımcıları ─────────────────────────────────────────────────────
export const dayStr = (n) => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10)
/** Yerel saat (İstanbul = UTC+3) → ISO. n gün sonrası, h:m yerel saat. */
export function at(n, h, m = 0) {
  const d = new Date(Date.now() + n * 864e5)
  d.setUTCHours(h - 3, m, 0, 0)
  return d.toISOString()
}
export function weekdayOf(n) { return new Date(Date.now() + n * 864e5).getUTCDay() } // 0=Pzr

// ── Deterministik rastgelelik (tekrarlanabilir veri) ───────────────────────
export function rng(seed) { let s = seed >>> 0; return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296) }
export const pick = (r, arr) => arr[Math.floor(r() * arr.length)]
export const trAscii = (s) => s.replace(/ç/g, 'c').replace(/ğ/g, 'g').replace(/ı/g, 'i').replace(/ö/g, 'o').replace(/ş/g, 's').replace(/ü/g, 'u')
  .replace(/Ç/g, 'C').replace(/Ğ/g, 'G').replace(/İ/g, 'I').replace(/Ö/g, 'O').replace(/Ş/g, 'S').replace(/Ü/g, 'U').toLowerCase().replace(/[^a-z0-9]+/g, '.')

export async function count(c, p) {
  const d = await c.get(p + (p.includes('?') ? '&' : '?') + 'page=1&pageSize=1')
  return Array.isArray(d) ? d.length : (d?.totalCount ?? 0)
}

/** Bilinen backend hatası: başarısızlığı beklenir; seed hata saymaz, özetin sonunda 'bilinen hatalar' olarak listelenir. */
export async function knownBug(label, fn) {
  try { return await fn() } catch (e) { log.known.push(`${label}: ${e.message}`); return undefined }
}
