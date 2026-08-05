-- =============================================================================
-- AyNesil Platform :: Flyway V33 — Fix core.set_updated_at for tables without row_version
-- =============================================================================
-- Sorun: core.set_updated_at() IF koşulunda doğrudan new.row_version / old.row_version
-- kullanıyordu. PL/pgSQL bu alan erişimini, to_jsonb(new) ? 'row_version' false olsa bile
-- satır tipinde kolon yoksa "record new has no field row_version" ile düşürüyor.
-- Örnek: UPDATE ref.ref_value_tenant_override (aktif/pasif override) → 500.
--
-- Çözüm: row_version yokluğunu jsonb ile kontrol et; artışı jsonb_populate_record ile uygula.
-- =============================================================================

create or replace function core.set_updated_at()
returns trigger
language plpgsql
as $$
declare
  v_new jsonb;
  v_old jsonb;
  v_rv  integer;
begin
  new.updated_at := now();

  if tg_op = 'UPDATE' then
    v_new := to_jsonb(new);
    if v_new ? 'row_version' then
      v_old := to_jsonb(old);
      v_rv := (v_new->>'row_version')::integer;
      -- Bump only when the caller did not change the version (optimistic lock).
      if v_rv is not distinct from (v_old->>'row_version')::integer then
        new := jsonb_populate_record(new, jsonb_build_object('row_version', v_rv + 1));
      end if;
    end if;
  end if;

  return new;
end;
$$;

comment on function core.set_updated_at() is
  'BEFORE UPDATE: set updated_at; bump row_version when present and unchanged by caller.';
