-- =====================================================================
-- V56 — Seans notu öğrenciye bağlanır (grup seansı sızıntısı)
--
-- Veli portalı parent_visible notları seansın tüm katılımcılarının velilerine gösteriyordu; grup
-- seansında bir çocuk için yazılan not diğer çocukların velilerine sızabilirdi.
-- session_note.student_id (nullable): notun hangi öğrenciyle ilgili olduğu; portal yalnızca
-- student_id velinin kendi çocuğu olan notları gösterir. Seansın katılımcısı olma kuralı uygulamada
-- (handler) doğrulanır; FK yalnızca kiracı bütünlüğü içindir (composite, #86).
--
-- Mevcut veliye görünür notlar:
--   • tek katılımcılı seans  → student_id o öğrenciyle doldurulur (görünür kalır)
--   • diğerleri (grup / katılımcısız) → parent_visible = false (görünmez); sayısı NOTICE olarak raporlanır.
-- Idempotent.
-- =====================================================================
alter table scheduling.session_note add column if not exists student_id uuid;

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'fk_session_note_student_id_tnt'
                    and conrelid = 'scheduling.session_note'::regclass) then
    alter table scheduling.session_note
      add constraint fk_session_note_student_id_tnt foreign key (corporation_id, student_id)
      references students.student (corporation_id, id) on delete no action;
  end if;
end $$;

comment on column scheduling.session_note.student_id is
  'Notun ilgili olduğu öğrenci (seansın katılımcısı). Veli portalı yalnızca bu öğrencinin velilerine gösterir; NULL + parent_visible portalda görünmez.';

do $$
declare
  v_linked  bigint;
  v_hidden  bigint;
begin
  with single as (
    select session_id, min(student_id::text)::uuid as student_id
      from scheduling.session_participant
     group by session_id
    having count(*) = 1
  ), upd as (
    update scheduling.session_note n
       set student_id = s.student_id
      from single s
     where n.session_id = s.session_id
       and n.student_id is null
       and n.parent_visible
       and n.deleted_at is null
    returning 1
  )
  select count(*) into v_linked from upd;

  with upd as (
    update scheduling.session_note
       set parent_visible = false
     where parent_visible
       and student_id is null
       and deleted_at is null
    returning 1
  )
  select count(*) into v_hidden from upd;

  raise notice 'V56: % veliye görünür not tek katılımcılı seansta öğrenciye bağlandı; % grup/katılımcısız seans notu veliye KAPATILDI (parent_visible=false).', v_linked, v_hidden;
end $$;
