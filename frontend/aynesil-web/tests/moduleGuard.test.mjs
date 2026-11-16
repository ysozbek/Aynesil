// Çalıştır: npm test  (Node yerleşik test koşucusu; ek bağımlılık yok)
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { moduleRedirect } from '../src/router/moduleGuard.ts'

const deps = (enabled, over = {}) => ({
  isAuthenticated: true,
  load: async () => {},
  isEnabled: (c) => enabled.includes(c),
  ...over,
})

test('modül kapalıyken sayfa /403 (forbidden) rotasına gider', async () => {
  for (const m of ['camps', 'consultancy', 'camera', 'kpi', 'care_team', 'surveys']) {
    assert.deepEqual(await moduleRedirect({ module: m }, deps([])), { name: 'forbidden' }, m)
  }
})

test('modül açıkken geçişe izin verilir; yalnızca ilgili modül sayılır', async () => {
  assert.equal(await moduleRedirect({ module: 'camps' }, deps(['camps'])), undefined)
  assert.deepEqual(await moduleRedirect({ module: 'camps' }, deps(['kpi'])), { name: 'forbidden' })
})

test('modül bayrağı olmayan rota ve oturumsuz istek etkilenmez', async () => {
  assert.equal(await moduleRedirect({}, deps([])), undefined)
  assert.equal(await moduleRedirect({ module: 'camps' }, deps([], { isAuthenticated: false })), undefined)
})

test('durum yüklenmemişse karar öncesinde yüklenmesi beklenir (adres çubuğundan ilk giriş)', async () => {
  let enabled = []
  const d = deps([], { load: async () => { await Promise.resolve(); enabled = ['kpi'] }, isEnabled: (c) => enabled.includes(c) })
  assert.equal(await moduleRedirect({ module: 'kpi' }, d), undefined)
})

test('yükleme hatası → modül kapalı sayılır (/403)', async () => {
  // modules.store.load hata yutar ve enabled boş kalır
  assert.deepEqual(await moduleRedirect({ module: 'kpi' }, deps([], { load: async () => {} })), { name: 'forbidden' })
})

// ── Router kaynağı: modül sayfalarının hepsi meta.module taşır ────────────────────────────────
const src = readFileSync(new URL('../src/router/index.ts', import.meta.url), 'utf8')
const routes = [...src.matchAll(/\{\s*path: '([^']+)',((?:(?!\bpath: ').)*?)\n\s*\},?/gs)].map((m) => ({ path: m[1], body: m[2] }))

const expected = [
  ['camps', 'Camps'],
  ['cameras', 'Camera'],
  ['consultancy', 'Consultancy'],
  ['kpi', 'Kpi'],
  ['care-team', 'CareTeam'],
  ['surveys', 'Surveys'],
  ['portal/surveys', 'Surveys'],
  ['portal/feedback', 'Surveys'],
]

for (const [prefix, mod] of expected) {
  test(`'${prefix}' rotalarının tümü meta.module = Modules.${mod}`, () => {
    const mine = routes.filter((r) => r.path === prefix || r.path.startsWith(prefix + '/'))
    assert.ok(mine.length > 0, `${prefix} rotası bulunamadı`)
    for (const r of mine) assert.match(r.body, new RegExp(`module: Modules\\.${mod}\\b`), `${r.path} modül korumasız`)
  })
}

test('modül dışı rotalara yanlışlıkla modül bayrağı konmamış', () => {
  const prefixes = expected.map((e) => e[0])
  for (const r of routes.filter((r) => r.body.includes('module: Modules.'))) {
    assert.ok(prefixes.some((p) => r.path === p || r.path.startsWith(p + '/')), `${r.path} beklenmeyen modül bayrağı`)
  }
})

test('/403 rotası forbidden adıyla tanımlı', () => {
  assert.match(src, /path: '\/403',\s*name: 'forbidden'/)
})
