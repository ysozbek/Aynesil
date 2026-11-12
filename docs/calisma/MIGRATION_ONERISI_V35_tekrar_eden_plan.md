# Migration Önerisi — V35: Tekrar Eden Plana Eğitimci ve Hizmet

**Durum:** Uygulandı: `db/migrations/V35__recurring_schedule_service_and_educators.sql`. Bu dosya karar kaydı olarak saklanıyor.

## Neden gerekli?

"Her Salı 10:00, Ayşe, Pınar Hoca, Konuşma Terapisi, Oda 3" gibi bir haftalık düzenin tek seferde kurulabilmesi için tekrar eden planın şu dört bilgiyi tutması gerekiyor:

| Bilgi | Bugün `scheduling.recurring_schedule` içinde var mı? |
|---|---|
| Öğrenci (öğrencinin aldığı program üzerinden) | Var (`student_program_id`) |
| Oda, kampüs, seans türü | Var |
| **Hizmet** (Konuşma Terapisi, Duyu Bütünleme…) | **Yok** |
| **Eğitimci** | **Yok** |

Hizmet ve eğitimci bilgisi bugün yalnızca tek tek seanslarda tutuluyor: `session.program_service_id` ve `session_educator`. Plandan seans üretilirken bu bilgiler plandan alınamıyor, çünkü planda yok.

## Önerilen şema değişikliği

İki değişiklik var. İkisi de mevcut tabloların yapısını takip ediyor.

1. `scheduling.recurring_schedule` tablosuna **`program_service_id`** kolonu. Boş bırakılabilir, böylece mevcut planlar etkilenmez.
2. Yeni tablo **`scheduling.recurring_schedule_educator`**. Yapısı `scheduling.session_educator` ile aynı: bir plana birden fazla eğitimci ve rol (lead, assistant, observer, supervisor) atanabilir.

```sql
-- =============================================================================
-- V35__recurring_schedule_service_and_educators.sql
--
-- Adds the two pieces a recurring schedule needs to materialise complete
-- sessions: the program service delivered and the assigned educators.
--   1. scheduling.recurring_schedule.program_service_id (nullable)
--   2. scheduling.recurring_schedule_educator (mirrors scheduling.session_educator)
--
-- Audit: scheduling schema is covered by the baseline trg_audit loop
-- (99_triggers_rls_policies.sql) only for tables existing at baseline time,
-- so trg_audit is attached explicitly below.
-- =============================================================================

-- ── Step 1: program service on the rule ──────────────────────────────────────

alter table scheduling.recurring_schedule
  add column if not exists program_service_id uuid
    references education.program_service(id);

comment on column scheduling.recurring_schedule.program_service_id is
  'Service delivered by sessions generated from this rule; copied to session.program_service_id.';

-- ── Step 2: educators on the rule ────────────────────────────────────────────

create table if not exists scheduling.recurring_schedule_educator (
  id                    uuid primary key default core.uuid_generate_v7(),
  corporation_id        uuid not null references core.corporation(id),
  recurring_schedule_id uuid not null references scheduling.recurring_schedule(id) on delete cascade,
  educator_id           uuid not null references educators.educator(id),
  role                  text not null default 'lead'
                          check (role in ('lead','assistant','observer','supervisor')),
  unique (recurring_schedule_id, educator_id)
);

comment on table scheduling.recurring_schedule_educator is
  'Educators assigned to a recurring schedule; copied to session_educator when sessions are generated.';

create index if not exists ix_recurring_schedule_educator_educator
  on scheduling.recurring_schedule_educator(educator_id);

-- ── Step 3: RLS ──────────────────────────────────────────────────────────────

alter table scheduling.recurring_schedule_educator enable row level security;

drop policy if exists tenant_isolation on scheduling.recurring_schedule_educator;
create policy tenant_isolation on scheduling.recurring_schedule_educator
  using  (corporation_id = core.current_corporation_id())
  with check (corporation_id = core.current_corporation_id());

-- ── Step 4: audit ────────────────────────────────────────────────────────────

create or replace trigger trg_audit
  after insert or update or delete on scheduling.recurring_schedule_educator
  for each row execute function core.audit_trigger();
```

**Not:** Yeni tabloda `updated_at` trigger'ı yok, çünkü `session_educator` gibi bu tabloda da güncelleme alanı yok. Satırlar eklenir ve silinir, güncellenmez.

## Onaylanırsa yapılacak kod değişiklikleri

Migration'ın yanında şu katmanlar sırayla güncellenecek. Her katmandan sonra durup açıklayacağım.

1. **Domain:** `RecurringSchedule` entity'sine `ProgramServiceId` alanı ve `Educators` koleksiyonu, ayrıca yeni `RecurringScheduleEducator` entity'si.
2. **Infrastructure:** EF Core Fluent konfigürasyonu ve `IAppDbContext` / `AynesilDbContext` içine yeni DbSet.
3. **Application:**
   - `CreateRecurringScheduleCommand` hizmet ve eğitimci listesini alacak.
   - Doğrulama: Seçilen hizmet, öğrencinin aldığı programa ait olmalı. Başka programın hizmeti seçilemez.
   - Seans üretilirken hizmet `session.program_service_id` alanına, eğitimciler `session_educator` tablosuna kopyalanacak.
4. **API:** `CreateRecurringScheduleRequest` yeni alanları taşıyacak.
5. **Frontend:** Formda öğrencinin programı seçilince o programın hizmetleri listelenecek. Eğitimciler isimle seçilecek.

## Dikkat edilmesi gereken bir nokta: eğitimci çakışması

Veritabanında `scheduling.session_educator` için bir çakışma trigger'ı var (`trg_educator_overlap`). Bir eğitimci aynı saatte iki seansa atanırsa hata veriyor. Toplu seans üretimi tek bir kayıt işlemi olduğu için, tek bir çakışan seans **tüm üretimi** iptal ettirir.

Öneri: Üretim sırasında oda çakışmasında yapıldığı gibi eğitimci çakışması da önceden kontrol edilsin. Çakışan tarih atlanıp sonuç mesajında bildirilsin, örneğin: "12 seans oluşturuldu, 2 tarih eğitimci çakışması nedeniyle atlandı."

## Kararınız gereken sorular

1. **Bu şema değişikliğini onaylıyor musunuz?**
2. **Bir plana birden fazla eğitimci atanabilsin mi?** Öneri: evet, `session_educator` ile aynı yapı. Formda varsayılan olarak tek bir "Sorumlu Eğitimci" alanı gösterilir, isteyen yardımcı eğitimci ekler.
3. **Eğitimci çakışmasında ne olsun?** Öneri: çakışan tarih atlansın ve bildirilsin. Alternatif: üretim tamamen durdurulsun ve kullanıcıya hangi tarihlerde çakışma olduğu gösterilsin.
