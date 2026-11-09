-- =============================================================================
-- AyNesil Platform :: Flyway V36 — Kullanıcı adı tüm sistemde benzersiz
-- =============================================================================
-- Sorun:
--   Giriş, iam.authenticate_user(username) ile TÜM kurumlarda kullanıcı adına
--   göre yapılıyor; ancak user_account.username yalnızca kurum içinde benzersizdi
--   (unique (corporation_id, username)). İki kurumda aynı kullanıcı adı olursa
--   giriş fonksiyonu rastgele birini döndürür.
--
-- Çözüm:
--   1. Çakışan kullanıcı adı varsa migration durur ve listeyi verir
--      (otomatik yeniden adlandırma yapılmaz: kullanıcıların giriş bilgisi değişir).
--   2. unique (username) eklenir; eski (corporation_id, username) kısıtı kaldırılır.
--      citext olduğu için büyük/küçük harf duyarsızdır. Silinmiş (deleted_at dolu)
--      hesaplar da adı tutmaya devam eder (eski kısıtla aynı davranış).
--   3. iam.username_exists() — uygulama rolü RLS nedeniyle başka kurumun
--      kullanıcılarını göremez; kullanıcı oluşturmadan önce bu SECURITY DEFINER
--      fonksiyonla tüm sistemde kontrol yapılır.
--
-- Idempotent.
-- =============================================================================


-- ── Step 1: Çakışma kontrolü ────────────────────────────────────────────────
DO $$
DECLARE
    v_dupes text;
BEGIN
    SELECT string_agg(format('%s (%s kurum)', username, cnt), ', ')
      INTO v_dupes
      FROM (
        SELECT username, count(DISTINCT corporation_id) AS cnt
          FROM iam.user_account
         GROUP BY username
        HAVING count(*) > 1
      ) d;

    IF v_dupes IS NOT NULL THEN
        RAISE EXCEPTION
            'V36: Birden fazla kurumda kullanılan kullanıcı adları var: %. '
            'Bu hesapları elle yeniden adlandırın (ör. UPDATE iam.user_account SET username = ''admin@kurumkodu'' WHERE id = ...) ve migration''ı tekrar çalıştırın.',
            v_dupes;
    END IF;
END
$$;


-- ── Step 2: Global unique kısıtı ────────────────────────────────────────────
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
         WHERE conrelid = 'iam.user_account'::regclass
           AND conname  = 'uq_user_account_username'
    ) THEN
        ALTER TABLE iam.user_account
            ADD CONSTRAINT uq_user_account_username UNIQUE (username);
    END IF;
END
$$;

-- Eski (corporation_id, username) kısıtı artık gereksiz (global kısıt onu kapsar).
DO $$
DECLARE
    v_name text;
BEGIN
    FOR v_name IN
        SELECT c.conname
          FROM pg_constraint c
         WHERE c.conrelid = 'iam.user_account'::regclass
           AND c.contype  = 'u'
           AND c.conname <> 'uq_user_account_username'
           AND (
               SELECT array_agg(a.attname::text ORDER BY a.attname)
                 FROM unnest(c.conkey) k
                 JOIN pg_attribute a
                   ON a.attrelid = c.conrelid AND a.attnum = k
           ) = ARRAY['corporation_id', 'username']
    LOOP
        EXECUTE format('ALTER TABLE iam.user_account DROP CONSTRAINT %I', v_name);
    END LOOP;
END
$$;


-- ── Step 3: Tüm sistemde kullanıcı adı kontrolü (RLS bypass) ────────────────
CREATE OR REPLACE FUNCTION iam.username_exists(p_username citext)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, public
AS $$
    SELECT EXISTS (SELECT 1 FROM iam.user_account WHERE username = p_username);
$$;

REVOKE ALL ON FUNCTION iam.username_exists(citext) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION iam.username_exists(citext) TO aynesil_app;
