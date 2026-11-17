// Çalıştır: npm test  (Node yerleşik test koşucusu; ek bağımlılık yok)
// Hata: ilk girişte şifre değiştirilip çıkış yapılınca giriş adresi /login?redirect=/account/password oluyordu;
// yeni girişte kullanıcı ana sayfa yerine yine şifre ekranına gidiyordu.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { safeLoginRedirect, returnPathFor, passwordChangeRedirect } from '../src/router/authRedirect.ts'

/** Router guard'ın ilk iki adımı (oturum + şifre kuralı) ve LoginView'in giriş sonrası hedefi. */
function guard(to, auth) {
  if (to.requiresAuth && !auth.isAuthenticated) return { name: 'login', query: { redirect: to.path } }
  return passwordChangeRedirect(auth, to.name)
}

test('ilk giriş → şifre değiştir → çıkış → giriş: ana sayfa açılır', () => {
  // 1) İlk giriş: mcp açık, ana sayfa yerine şifre sayfasına gider.
  let auth = { isAuthenticated: true, mustChangePassword: true }
  assert.deepEqual(guard({ name: 'dashboard', path: '/', requiresAuth: true }, auth), { name: 'account-password' })
  assert.equal(guard({ name: 'account-password', path: '/account/password', requiresAuth: true }, auth), undefined)

  // 2) Şifre değişti, çıkış: oturum düşerken bulunulan sayfa şifre sayfasıdır; dönüş adresi taşınmaz.
  assert.equal(returnPathFor('/account/password'), null)
  auth = { isAuthenticated: false, mustChangePassword: false }

  // 3) Yeni giriş: mcp kapalı. Eski bir bağlantıda redirect=/account/password kalmış olsa bile ana sayfa.
  auth = { isAuthenticated: true, mustChangePassword: false }
  const target = safeLoginRedirect('/account/password')
  assert.equal(target, '/')
  assert.equal(guard({ name: 'dashboard', path: target, requiresAuth: true }, auth), undefined)
})

test('mcp kapalıyken şifre sayfası gönüllü olarak açılabilir (yönlendirme yok)', () => {
  const auth = { isAuthenticated: true, mustChangePassword: false }
  assert.equal(guard({ name: 'account-password', path: '/account/password', requiresAuth: true }, auth), undefined)
  assert.equal(guard({ name: 'students', path: '/students', requiresAuth: true }, auth), undefined)
})

test('mcp açıkken her sayfa şifre sayfasına; oturumsuzken kural uygulanmaz', () => {
  assert.deepEqual(passwordChangeRedirect({ isAuthenticated: true, mustChangePassword: true }, 'students'), { name: 'account-password' })
  assert.equal(passwordChangeRedirect({ isAuthenticated: false, mustChangePassword: true }, 'students'), undefined)
})

test('giriş sonrası hedef: uygulama içi yollar korunur, giriş/şifre sayfaları ve dış adresler ana sayfaya döner', () => {
  assert.equal(safeLoginRedirect('/students/42?tab=goals'), '/students/42?tab=goals')
  assert.equal(safeLoginRedirect(undefined), '/')
  assert.equal(safeLoginRedirect(''), '/')
  assert.equal(safeLoginRedirect(['/a']), '/')
  for (const p of ['/login', '/login?x=1', '/forgot-password', '/reset-password?token=t', '/account/password', '/account/password/', '/account/password#x']) {
    assert.equal(safeLoginRedirect(p), '/', p)
  }
  for (const p of ['//evil.example/x', 'https://evil.example', 'javascript:alert(1)']) {
    assert.equal(safeLoginRedirect(p), '/', p)
  }
})

test('oturum düşünce dönüş adresi: normal sayfalar taşınır, şifre ve giriş sayfaları taşınmaz', () => {
  assert.equal(returnPathFor('/camps/periods/1?tab=x'), '/camps/periods/1?tab=x')
  assert.equal(returnPathFor('/account/password'), null)
  assert.equal(returnPathFor('/reset-password?token=abc'), null)
})

test('router, LoginView ve api.service bu kuralları kullanıyor', () => {
  const router = readFileSync(new URL('../src/router/index.ts', import.meta.url), 'utf8')
  assert.match(router, /passwordChangeRedirect\(auth, to\.name\)/)
  const login = readFileSync(new URL('../src/views/auth/LoginView.vue', import.meta.url), 'utf8')
  assert.match(login, /safeLoginRedirect\(route\.query\.redirect\)/)
  const api = readFileSync(new URL('../src/services/api.service.ts', import.meta.url), 'utf8')
  assert.match(api, /returnPathFor\(/)
  assert.match(api, /\/auth\/logout'\)\) \{\s*return Promise\.reject\(error\)/)
})
