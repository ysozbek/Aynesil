-- =====================================================================
-- AyNesil Platform :: V7 — IAM email lookup helper
-- Adds iam.find_user_by_email() SECURITY DEFINER function.
-- Required by the RequestPasswordResetCommand to locate a user by email
-- address without an active RLS tenant context (unauthenticated flow).
-- =====================================================================

CREATE OR REPLACE FUNCTION iam.find_user_by_email(p_email citext)
RETURNS TABLE(
    id               uuid,
    corporation_id   uuid,
    username         citext,
    email            citext,
    full_name        text,
    status           text,
    preferred_locale text
)
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = iam, core, public
AS $$
    SELECT id, corporation_id, username, email, full_name, status, preferred_locale
    FROM iam.user_account
    WHERE email = p_email
      AND deleted_at IS NULL
    LIMIT 1;
$$;

GRANT EXECUTE ON FUNCTION iam.find_user_by_email(citext) TO aynesil_app;
