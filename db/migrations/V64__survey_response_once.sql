-- =============================================================================
-- AyNesil Platform :: Flyway V64 — Anket yanıtı tekilliği (veritabanında)
-- =============================================================================
-- Sorun (docs/raporlar/2026-11-17_dogrulama.md D7): "anket × yanıtlayan × öğrenci × seans
--   başına bir gönderilmiş yanıt" kuralı (ops.survey_response tablo yorumu) yalnızca
--   uygulamada denetleniyordu (SubmitSurveyResponseCommandHandler.EnsureNotAnsweredBeforeAsync).
--   Çift tıklama ya da eşzamanlı gönderim iki yanıt yazabiliyor, sonuç özeti şişiyordu.
--
-- Çözüm: gönderilmiş yanıtlar için tekil ifade indeksi. Yanıtlayan anahtarı uygulamadaki
--   kuralla aynıdır: veli yanıtında guardian_id, personel yanıtında respondent_user_id.
--   Öğrenci/seans boş olabilir; NULLS NOT DISTINCT (PG15+) boşları eşit sayar.
--   Taslak yanıtlar (submitted_at IS NULL) kapsam dışıdır.
--   API: ihlal (23505, uq_survey_response_once) → 409 Rule.SurveyAlreadySubmitted.
--
-- Mevcut veride çift gönderim varsa indeks kurulamaz: migration açık bir mesajla durur;
--   çiftler incelenip (yanıtlar silinmez, taslağa çevrilebilir) migration yeniden çalıştırılır.
-- =============================================================================

do $$
declare
  v_dupes bigint;
begin
  select count(*) into v_dupes
    from (select 1
            from ops.survey_response
           where submitted_at is not null
           group by survey_id, coalesce(guardian_id, respondent_user_id), student_id, session_id
          having count(*) > 1) d;
  if v_dupes > 0 then
    raise exception 'V64: ops.survey_response içinde % grup çift gönderilmiş yanıt var; uq_survey_response_once kurulamaz. Çiftleri inceleyin (ör. fazlalığın submitted_at değerini NULL yapın) ve migration''ı yeniden çalıştırın.', v_dupes;
  end if;
end $$;

create unique index if not exists uq_survey_response_once
  on ops.survey_response (survey_id, (coalesce(guardian_id, respondent_user_id)), student_id, session_id)
  nulls not distinct
  where submitted_at is not null;

comment on index ops.uq_survey_response_once is
  'Gönderilmiş yanıt tekilliği: anket × yanıtlayan (veli ya da kullanıcı) × öğrenci × seans (V64).';
