#!/usr/bin/env node
/**
 * AyNesil demo veri seti — gerçek API üzerinden (SQL yalnızca API'nin yapamadığı yerlerde).
 *   node scripts/demo-seed/seed.mjs [--reset] [--only demo_a] [--phase people,sessions,...]
 * İdempotent: var olanı atlar (doğal anahtarla bulur). --reset: demo kurumlarını (owner/SQL) silip baştan kurar.
 */
import { execFileSync } from 'node:child_process'
import path from 'node:path'
import { Client, fx, here, log, sql, sqlLog, section, info, q, clearMustChange } from './lib.mjs'
import { setupIdentity } from './phases/identity.mjs'
import { setupPeople } from './phases/people.mjs'
import { setupSessions } from './phases/sessions.mjs'
import { setupLearning } from './phases/learning.mjs'
import { setupCrm } from './phases/crm.mjs'
import { setupFinance } from './phases/finance.mjs'
import { setupLegal } from './phases/legal.mjs'
import { setupOps } from './phases/ops.mjs'
import { setupCamp } from './phases/camp.mjs'
import { setupExtras } from './phases/extras.mjs'

const args = process.argv.slice(2)
const flag = (n) => args.includes(n)
const opt = (n) => (args.includes(n) ? args[args.indexOf(n) + 1] : null)
const ROOT = path.resolve(here, '../..')
const onlyTenant = opt('--only')
const phases = (opt('--phase') || '').split(',').filter(Boolean)
const want = (p) => !phases.length || phases.includes(p)

function corpExists(code) { return sql(`select count(*) from core.corporation where code=${q(code)}`) !== '0' }

function resetTenant(code) {
  if (!corpExists(code)) return
  info(`--reset: ${code} siliniyor (owner/SQL)`)
  // Tüm tenant tablolarını corporation_id ile temizle; denetim tetikleyicileri/immutable kayıtlar nedeniyle replica modunda.
  sql(`
    do $$
    declare cid uuid; r record; n int := 0;
    begin
      select id into cid from core.corporation where code=${q(code)};
      set local session_replication_role = replica;
      for r in select table_schema s, table_name t from information_schema.columns
               where column_name='corporation_id' and table_schema not in ('core','pg_catalog','information_schema','hangfire')
                 and table_name in (select table_name from information_schema.tables where table_type='BASE TABLE' and table_schema=columns.table_schema)
      loop execute format('delete from %I.%I where corporation_id=%L', r.s, r.t, cid); end loop;
      delete from core.audit_log where corporation_id = cid;
      delete from core.campus where corporation_id=cid;
      delete from core.corporation where id=cid;
    end $$;`)
}

async function ensureTenant(code, t) {
  if (corpExists(code)) { info(`${code}: kurum zaten var`); return }
  info(`${code}: kurum oluşturuluyor (devops/new_tenant.py)`)
  const adminU = fx.users.admin
  const a = ['devops/new_tenant.py', '--code', code, '--name', t.name,
    '--admin-username', `${adminU.local}@${t.domain}`, '--admin-name', adminU.fullName[code], '--admin-password', fx.password]
  for (const c of t.campuses) a.push('--campus', c)
  execFileSync('python3', a, { cwd: ROOT, stdio: 'pipe' })
}

const tenants = Object.entries(fx.tenants).filter(([c]) => !onlyTenant || c === onlyTenant)
const summary = {}
for (const [code, t] of tenants) {
  section(`${code} — ${t.name}`)
  if (flag('--reset')) resetTenant(code)
  await ensureTenant(code, t)
  // new_tenant.py yöneticisi must_change_password=true açar; seed'in oturumu açılabilsin diye demo kullanıcılarında kapatılır.
  const firstLogin = `${fx.users.firstLogin.local}@${t.domain}`
  clearMustChange(code, [firstLogin])
  const c = new Client(code)
  await c.login(`${fx.users.admin.local}@${t.domain}`, fx.password)
  const ctx = { code, t, c, ref: null, ids: {} }
  const run = async (name, fn) => { if (want(name)) { section(`${code} · ${name}`); try { await fn(ctx) } catch (e) { log.fail.push(`${code}/${name}: ${e.message}`); console.log(`   ✗ FAZ DURDU (${name}): ${e.message}`) } } }
  await setupIdentity(ctx)           // her zaman: ref, roller, kampüs/derslik haritası
  section(`${code} · people`); await setupPeople(ctx)
  clearMustChange(code, [firstLogin])   // /users ile açılan demo kullanıcıları (ilk giriş örneği hariç)
  await run('sessions', setupSessions)
  await run('learning', setupLearning)
  await run('crm', setupCrm)
  await run('finance', setupFinance)
  await run('legal', setupLegal)
  await run('ops', setupOps)
  await run('camp', setupCamp)
  await run('extras', setupExtras)
  clearMustChange(code, [firstLogin])
  summary[code] = ctx.ids.summary || {}
}
console.log(`\n══ Özet ══\nbaşarılı adım: ${log.ok} · atlanan (zaten var): ${log.skip} · hata: ${log.fail.length}`)
if (log.fail.length) { console.log('\nHatalar:'); log.fail.forEach((f) => console.log(' - ' + f.slice(0, 300))) }
if (log.known.length) { console.log('\nBilinen backend hataları (seed bunları atlar):'); [...new Set(log.known)].forEach((f) => console.log(' - ' + f.slice(0, 260))) }
if (sqlLog.length) { console.log('\nSQL ile yapılanlar:'); [...new Set(sqlLog)].forEach((s) => console.log(' - ' + s)) }
console.log('\nGiriş: <kullanıcı>@<alan adı> / ' + fx.password + '  (ör. admin@demo-a.test)')
process.exit(log.fail.length ? 2 : 0)
