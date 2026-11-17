/**
 * Sekmeler arası jeton eşitleme. Yenileme jetonu her kullanımda döner (rotation): bir sekme yenileyince diğer
 * sekmenin bellekteki yenileme jetonu geçersizleşir. Yenilemeden önce localStorage'da daha yeni bir çift varsa
 * onu kullanmak, ikinci sekmenin gereksiz yere oturumdan atılmasını önler (erişim jetonu 15 dk: V63 sonrası sık).
 * Saf fonksiyon: `npm test` ile denenir.
 */
export interface TokenPairLike {
  accessToken: string | null
  refreshToken: string | null
}

/** Depodaki çift bellektekinden farklı ve tamsa onu döndürür; aksi halde null (yenileme yapılmalı). */
export function newerStoredTokens(memory: TokenPairLike, stored: TokenPairLike): { accessToken: string; refreshToken: string } | null {
  if (!stored.accessToken || !stored.refreshToken) return null
  if (stored.refreshToken === memory.refreshToken) return null
  return { accessToken: stored.accessToken, refreshToken: stored.refreshToken }
}
