-- =============================================================================
-- Aynesil Flyway V47 — students.development_report kesinleştirme (Y15)
-- =============================================================================
-- Veli portalı yalnızca kesinleşmiş gelişim raporlarını görür; taslaklar yalnızca personele görünür.
-- Mevcut raporların hangisinin taslak olduğu bilinmediğinden hepsi taslak (is_finalized = false) başlar:
-- personel yayımlamak istediğini "kesinleştir" ile açar (kasıtlı, gizlilik açısından güvenli varsayılan).
-- =============================================================================

alter table students.development_report
  add column if not exists is_finalized boolean not null default false,
  add column if not exists finalized_at timestamptz,
  add column if not exists finalized_by uuid;

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_development_report_finalized'
                  and conrelid = 'students.development_report'::regclass) then
    alter table students.development_report
      add constraint ck_development_report_finalized
      check (not is_finalized or finalized_at is not null);
  end if;
end $$;

-- Portal ve personel listeleri öğrenciye göre, kesinleşmiş olana göre süzer.
create index if not exists ix_development_report_student_finalized
  on students.development_report (student_id, is_finalized) where deleted_at is null;
