/**
 * Kurumda kapalı modülün sayfasına adres çubuğundan girilirse /403'e yönlendirme kuralı.
 * Saf fonksiyon: router ve store'a bağımlı değil, böylece `npm test` ile (Node test koşucusu) denenir.
 */
export interface ModuleGuardDeps {
  isAuthenticated: boolean
  /** GET /modules/me (önbellekli); yüklenmemişse bekler. */
  load: () => Promise<void>
  isEnabled: (code: string) => boolean
}

/** `meta.module` varsa ve modül kapalıysa `{ name: 'forbidden' }`, aksi halde `undefined` (geçişe izin). */
export async function moduleRedirect(
  meta: { module?: unknown },
  deps: ModuleGuardDeps,
): Promise<{ name: 'forbidden' } | undefined> {
  if (!meta.module || !deps.isAuthenticated) return undefined
  await deps.load()
  return deps.isEnabled(String(meta.module)) ? undefined : { name: 'forbidden' }
}
