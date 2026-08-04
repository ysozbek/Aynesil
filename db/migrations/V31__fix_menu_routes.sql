-- =============================================================================
-- AyNesil Platform :: Flyway V31 — Fix menu item routes to match Vue Router
-- =============================================================================
-- Sorun: V8-V21 migration'larında seed edilen menü item route'ları
-- frontend'de oluşturulan Vue Router route'larıyla eşleşmiyor.
-- Eşleşmeyen route'lar 404 veya boş sayfa gösteriyor.
--
-- Değişiklik tipleri:
--   FIX      : Route düzeltildi, sayfa var ama path yanlış
--   DEACTIVATE: Karşılık gelen frontend route yok (devre dışı bırakıldı)
--
-- Idempotent: tüm UPDATE'ler WHERE ile hedeflenmiş.
-- =============================================================================

-- ── V8: dashboard → / (root path, not /dashboard) ────────────────────────────

UPDATE iam.menu_item SET route = '/'
WHERE code = 'dashboard' AND corporation_id IS NULL;

-- ── V9 (CRM): /crm/interviews yok — deactivate ────────────────────────────────

UPDATE iam.menu_item SET is_active = false
WHERE code = 'crm-interviews' AND corporation_id IS NULL;

-- ── V10 (Students): route düzeltmeleri ───────────────────────────────────────

-- /students/guardians yok; guardians kendi top-level route'u (/guardians)
UPDATE iam.menu_item SET is_active = false
WHERE code = 'students-guardians' AND corporation_id IS NULL;

-- /students/case-management yok
UPDATE iam.menu_item SET is_active = false
WHERE code = 'students-case-management' AND corporation_id IS NULL;

-- /students/portal-access yok (ayrı admin sayfası yok)
UPDATE iam.menu_item SET is_active = false
WHERE code = 'students-portal-access' AND corporation_id IS NULL;

-- /students/surveys ve /students/feedback yok (V17'den)
UPDATE iam.menu_item SET is_active = false
WHERE code IN ('students-surveys', 'students-feedback') AND corporation_id IS NULL;

-- ── V11 (Educators/Programs): route düzeltmeleri ────────────────────────────

UPDATE iam.menu_item SET is_active = false
WHERE code IN ('educators-hierarchy', 'educators-utilization') AND corporation_id IS NULL;

UPDATE iam.menu_item SET is_active = false
WHERE code = 'programs-enrollments' AND corporation_id IS NULL;

-- ── V12 (Goals / Education Plans): route düzeltmeleri ───────────────────────

-- education-plans top-level ve sub-items → /bep (Vue Router kullanıyor)
UPDATE iam.menu_item SET route = '/bep'
WHERE code = 'education-plans' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/bep'
WHERE code = 'plans-list' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/bep/new'
WHERE code = 'plans-new' AND corporation_id IS NULL;

-- /education-plans/academic-periods ve /education-plans/reports yok
UPDATE iam.menu_item SET is_active = false
WHERE code IN ('plans-academic', 'plans-reports') AND corporation_id IS NULL;

-- /goals/analytics yok
UPDATE iam.menu_item SET is_active = false
WHERE code = 'goals-analytics' AND corporation_id IS NULL;

-- ── V13 (Scheduling): route düzeltmeleri ────────────────────────────────────

-- /scheduling/recurring → /scheduling/recurring-schedules
UPDATE iam.menu_item SET route = '/scheduling/recurring-schedules'
WHERE code = 'scheduling-recurring' AND corporation_id IS NULL;

-- /scheduling/makeup → /scheduling/makeup-requests
UPDATE iam.menu_item SET route = '/scheduling/makeup-requests'
WHERE code = 'scheduling-makeup' AND corporation_id IS NULL;

-- ── V14 (Finance/Payments): route düzeltmeleri ──────────────────────────────
-- payments top-level zaten V27'de /finance yapıldı.
-- Sub-item'lar hâlâ /payments/... → /finance/... olarak düzeltilmeli.

UPDATE iam.menu_item SET route = '/finance/student-packages'
WHERE code = 'payments-packages' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/packages'
WHERE code = 'payments-definitions' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/invoices'
WHERE code = 'payments-invoices' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/payments'
WHERE code = 'payments-transactions' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/credits'
WHERE code = 'payments-credits' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/scholarships'
WHERE code = 'payments-scholarships' AND corporation_id IS NULL;

UPDATE iam.menu_item SET route = '/finance/promotions'
WHERE code = 'payments-promotions' AND corporation_id IS NULL;

-- /payments/refunds ve /payments/reports yok
UPDATE iam.menu_item SET is_active = false
WHERE code IN ('payments-refunds', 'payments-reports') AND corporation_id IS NULL;

-- ── V17 (Notifications): route düzeltmeleri ─────────────────────────────────

-- /notifications/triggers yok (henüz frontend yok)
UPDATE iam.menu_item SET is_active = false
WHERE code = 'notifications-triggers' AND corporation_id IS NULL;

-- ── V18 (Meetings): route düzeltmeleri ──────────────────────────────────────

-- meetings-list seeds '/meetings' but the list is at '/meetings/list'
UPDATE iam.menu_item SET route = '/meetings/list'
WHERE code = 'meetings-list' AND corporation_id IS NULL;

-- ── V20 (Cameras): route düzeltmeleri ───────────────────────────────────────

-- /cameras/viewing-logs → /cameras/viewing-history
UPDATE iam.menu_item SET route = '/cameras/viewing-history'
WHERE code = 'camera-viewing-logs' AND corporation_id IS NULL;

-- ── V21 (Camps): route düzeltmeleri ─────────────────────────────────────────

-- /camps/definitions → /camps/list
UPDATE iam.menu_item SET route = '/camps/list'
WHERE code = 'camps.definitions' AND corporation_id IS NULL;

-- Kamp sub-item'ları için Vue Router route yok (enrollments/attendance/reports)
UPDATE iam.menu_item SET is_active = false
WHERE code IN ('camps.enrollments', 'camps.attendance', 'camps.reports')
  AND corporation_id IS NULL;

-- ── V25 (Legal): consent-templates deactivate ───────────────────────────────

-- /legal/consent-templates yok, mevcut route /legal/consents
UPDATE iam.menu_item SET is_active = false
WHERE code = 'legal.consent_templates' AND corporation_id IS NULL;

-- ── Translations güncelle: 'Dashboard' label'ını koruyoruz (route değişti) ──

-- Herhangi bir translation güncelleme gerekmez;
-- sadece route path değişti, label aynı kalıyor.
