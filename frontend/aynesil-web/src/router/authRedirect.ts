/**
 * Giriş/çıkış yönlendirme kuralları. Saf fonksiyonlar: router ve store'a bağımlı değil, `npm test` ile denenir.
 *
 * Neden: ilk girişte şifre değiştirilip çıkış yapılınca giriş adresi `/login?redirect=/account/password` oluyordu;
 * yeni girişte (mcp artık false) kullanıcı ana sayfa yerine yine şifre ekranına gidiyordu.
 */

/** Girişten sonra geri dönülmeyecek sayfalar: kimlik doğrulama ekranları ve şifre değiştirme. */
const NO_RETURN_PATHS = ['/login', '/forgot-password', '/reset-password', '/account/password']

function isNoReturn(path: string): boolean {
  const bare = path.split(/[?#]/)[0].replace(/\/+$/, '') || '/'
  return NO_RETURN_PATHS.includes(bare)
}

/**
 * Giriş sonrası gidilecek adres. `redirect` yalnızca uygulama içi bir yol olabilir (`/` ile başlar, `//` ile
 * başlamaz); kimlik doğrulama ve şifre değiştirme sayfaları geçersiz sayılır → ana sayfa.
 */
export function safeLoginRedirect(redirect: unknown): string {
  if (typeof redirect !== 'string' || !redirect.startsWith('/') || redirect.startsWith('//')) return '/'
  return isNoReturn(redirect) ? '/' : redirect
}

/**
 * Oturum düştüğünde giriş sayfasına taşınacak dönüş adresi; dönülmeyecek sayfalarda `null` (redirect parametresi
 * eklenmez).
 */
export function returnPathFor(pathWithQuery: string): string | null {
  return isNoReturn(pathWithQuery) ? null : pathWithQuery
}

/**
 * İlk giriş kuralı: hesap şifre değiştirmeye zorunluyken (JWT "mcp") yalnızca şifre değiştirme sayfası açılır.
 * Bayrak kapalıyken hiçbir yönlendirme yapmaz.
 */
export function passwordChangeRedirect(
  state: { isAuthenticated: boolean; mustChangePassword: boolean },
  toName: unknown,
): { name: 'account-password' } | undefined {
  if (state.isAuthenticated && state.mustChangePassword && toName !== 'account-password') {
    return { name: 'account-password' }
  }
  return undefined
}
