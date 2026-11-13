-- =============================================================================
-- AyNesil Platform :: V52 — İzleme yetkisi iptal gerekçesi
-- =============================================================================
-- Kamera rızası geri alındığında o veliye ait açık izleme yetkileri iptal edilir (gerekçe: rıza geri alındı).
-- Gerekçe iptal kaydında saklanır; kayıt silinmez (denetim izi).
-- =============================================================================

ALTER TABLE media.viewing_authorization
    ADD COLUMN IF NOT EXISTS revoked_reason text;

COMMENT ON COLUMN media.viewing_authorization.revoked_reason IS
    'İptal gerekçesi (ör. rıza geri alındı); is_revoked = true iken dolu olabilir.';
