# Bağımsız Doğrulama — 2026-11-17

> Kapsam: `docs/uyusmazlik-dogrulama.md` doğrulamasından (2026-11-13, son ele alınan iş `2d08625` Y15) bu yana gelen **66 commit** (`c26eca8` … `a3cf139`; 369 dosya, +24 374 / −1 663 satır). Alanlar: hesap bağlama (V48), eksik ekranlar ve ilk girişte şifre (V50–V51), kamera rızası (V52), modül görünürlüğü (V53), seans notu/veli notları (V55–V56), pasif kurum ve yönetici şifre sıfırlama (V57), kurulum sertleştirmesi (compose/nginx/Redis, kurum verisinin migration'lardan çıkması), karar turu (2026-11-15) ve B grubu modülleri: bakım ekibi (V59), okul danışmanlığı (V60), anketler (V61), kamp (V62).
> Kodda değişiklik yapılmadı. Canlı denemede açılan iki geçici kullanıcı deneme sonunda silindi.

## Özet

| | |
|---|---|
| Çalıştırılan komutlar | **Hepsi yeşil** (aşağıdaki tablo) |
| Yeni bulgu | **10**: Orta 2, Düşük 6, Bilgi 2. Kritik ya da yüksek bulgu yok |
| Kiracı sızıntısı | Bulunmadı (kod okuması, 34/34 izolasyon çağrısı, DB değişmezleri) |
| Veli portalında başka çocuğun verisi | Bulunmadı |
| Modül kapalıyken uç/rota erişimi | Kapalı (6/6 modül controller'ı `RequireModule`; rotalar `meta.module`; menü bayrakları eksiksiz) |
| Dosya adı büyük/küçük harf uyuşmazlığı | **Yok** |

En önemli iki bulgu:

- **D1 (Orta):** Yönetici şifre sıfırladığında, kullanıcıyı askıya aldığında ya da sildiğinde yalnızca yenileme token'ları iptal ediliyor. Elde kalan erişim token'ı **60 dakika** boyunca geçerli. Canlı denendi: sıfırlama ve askıya almanın ardından eski token'la istek **200** döndü. Kurum pasifleştirmede bu boşluk `StaleTenantMiddleware` ile kapatılmış, kullanıcı düzeyinde karşılığı yok.
- **D2 (Orta):** 20 güncelleme komutu istekte `RowVersion` alıyor ve doğrulayıcı `> 0` istiyor, ama handler bu değeri hiç karşılaştırmıyor. Eşzamanlı düzenlemede son yazan kazanıyor. Bu turda ekranı yapılan 5 tanesi de bu durumda: kamp, kamp etkinliği, danışmanlık planı, kurum (okul), anket.

---

## Çalıştırılan komutlar

| Komut | Sonuç |
|---|---|
| `dotnet build Aynesil.sln` | ✅ 0 hata, 5 uyarı: NU1603 xunit runner (2 proje, 2'şer kez), CS0618 Hangfire `AddOrUpdate`. Önceki CS8629/CS8602/CS8625 ve güvenlik açığı (NU190x) uyarıları yok |
| `dotnet test Aynesil.sln --filter "Category!=Integration"` | ✅ 741/741 (önceki tur 539) |
| `dotnet test tests/Aynesil.IntegrationTests` (Docker, PG17 + Flyway) | ✅ 91/91 (önceki tur 58) |
| `npm run type-check` | ✅ temiz |
| `npm run build` | ✅ temiz (3,3 sn) |
| `npm test` | ✅ 28/28 (modül rota koruması, `/403`) |
| `bash db/validation/smoke_test.sh` | ✅ GREEN. `NO SCHEMA DRIFT`, `TENANT RLS INVARIANT OK`, `APP ACCESS INVARIANT OK`, `GLOBAL-ROW DELETE INVARIANT OK` |
| `scripts/demo-seed/run.sh --build --reset` | ✅ 2620 adım, 0 hata |
| `scripts/smoke-api/run.sh` | ✅ 641 çağrı: 634 başarılı, 4xx 0, 5xx 0, tip uyuşmazlığı 0, **izolasyon ihlali 0 (34/34)**, 7 atlama (kaynak id yok; rotaların hepsi yoklamada mevcut). 56 başarılı yanıtta liste boş olduğu için öğe alanları doğrulanamadı |
| `scripts/smoke-proxy/run.sh` | ✅ İki istemci ayrı sayaçlara düşüyor; compose ağı dışından gelen sahte `X-Forwarded-For` yok sayılıyor (11. denemede 429); `auth_session.ip_address` ve API log'u gerçek istemci IP'sini gösteriyor |

Çalışma ağacı komutlardan sonra temiz. Frontend çağrıları `extract-endpoints.mjs` ile scratchpad'e yeniden üretildi: 590 çağrı. Commit'li `endpoints.json` ile dosya yolu dışında **birebir aynı**, yani güncel.

---

## Yöntem (ilk raporun yöntemi yeniden çalıştırıldı)

| Kontrol | Sonuç |
|---|---|
| **Rota eşleştirme** | 37 controller'dan 598 backend ucu (sınıf başına `[Route]`, aynı dosyada birden fazla controller dahil). Frontend'in 590 servis çağrısının **tamamı eşleşti (0 eşleşmeyen)**. Frontend'in çağırmadığı uç: **9** (ilk turda 65): şablon silme ×2, `GET camps/{id}/periods`, `reference-data/{type}/default`, `cameras/assignments`, `auth/register`, `auth/verify-email`, health. `auth/change-password` view'dan doğrudan çağrılıyor (`ChangePasswordView.vue:28`), çıkarıcı yalnızca `.ts` dosyalarını taradığı için listede göründü |
| **İzin kodları** | `Permissions.cs` 327 = `constants/permissions.ts` 327 = migration/seed 327; üçü aynı. Sabit dışında yazılmış izin literal'i yok (tek eşleşme `tr.ts`'teki açıklama metni). Canlı DB kataloğunda koddaki listede olmayan 4 eski izin var (D10) |
| **Durum değerleri** | `statuses.ts`'teki 32 sabitin 31'i DB check kısıtlarıyla birebir aynı. 32.si türetilmiş `ViewingAuthorizationState` (önceki turdaki gibi). Yeni sabitler (`CareAssignmentStatus`, kamp katılım/yoklama, danışmanlık) dahil. Sabit dışında yazılmış 122 durum karşılaştırmasının hepsi bir DB check değerinde geçiyor |
| **i18n** | `tr.ts` 2355 anahtar; statik `t()` kullanımı 4289, tanımsız anahtar **0**. Tek aday (`CalendarView.vue:387`, `'…types.' + e.entryType`) birleştirme ile kuruluyor, değerler tanımlı. 62 dinamik anahtarın yeni modüllere ait olanları DB değer kümeleriyle karşılaştırıldı: kamp yoklama/katılım/eğitmen rolü, anket hedefi/soru tipi, modül adları, takvim kaydı tipleri. Eksik yok |
| **İstek gövdesi / aktör alanları** | `TenantContractTests` bütün MediatR isteklerini yansımayla tarıyor ve geçiyor (`CorporationId` ve 30 aktör adı yasak). API katmanındaki istek record'ları ayrıca tarandı. Aktör adı taşıyanlar (`ConductedBy`, `PreparedBy`, `RecommendedBy`, `DiagnosedBy`, `PerformedBy`) aktör değil, "işi yapan uzman" seçimi; ilk raporda da böyle kabul edilmişti. Yeni modüllerde `EducatorId`/`LeadEducatorId` atanan kişi; kiracı bileşik FK ile korunuyor |
| **Yeni tablolarda RLS** | Bu aralıkta yeni tablo yok; yalnızca kolon eklendi (V50, V52, V55, V56) ve V56 bileşik FK ekliyor. `drift.sql` değişmezleri yeşil |
| **Menü ↔ modül** | Canlı DB'de modül rotası olup `feature_flag`'i boş global menü öğesi: **0**. Alt öğenin bayrağı üstünden farklı olan öğe: **0**. Bayraklı öğeler: camps 7, consultancy 9, camera 5, surveys 4, care_team 1, kpi 9 |

### Dosya adı büyük/küçük harf tutarlılığı

- **tsconfig:** `forceConsistentCasingInFileNames` `tsconfig.json`'da açıkça yazılmamış. `@vue/tsconfig/tsconfig.json:54`'ten `true` olarak geliyor; etkin değer `tsc --showConfig` ile doğrulandı. TS 5.9.3.
- **Aynı adın farklı harfli iki dosyası/dizini (git index):** yok.
- **Git index ile disk arasında harf farkı** (macOS'ta `core.ignorecase=true`, Linux'ta sorun çıkarır): yok.
- **Frontend içe aktarmaları:** `.ts`/`.vue`/`.js`/`.css` içindeki 1785 göreli ve `@/` içe aktarma, dinamik `import()` dahil, git'teki gerçek adlarla harfe duyarlı karşılaştırıldı. **0 uyuşmazlık**, 0 eksik dosya. Bu tarama `vue-tsc`'nin görmediği CSS ve varlık yollarını da kapsıyor.
- **Backend ve devops:** `.csproj`/`.sln` referansları; Dockerfile, compose, `.sh`, `.py`, `.mjs` ve nginx şablonlarındaki depo yolları karşılaştırıldı: **0 uyuşmazlık**. `.resx` adları `ResourceManager` taban adlarıyla (`Aynesil.Application.Resources.Messages|FieldNames|EnumLabels`) aynı.

---

## Yeni bulgular

| # | Önem | Modül | Kategori | Konum | Sorun | Etki | Önerilen düzeltme |
|---|---|---|---|---|---|---|---|
| D1 | **Orta** | Kullanıcılar / Auth | güvenlik (oturum) | `Api/Authorization/PermissionAuthorizationHandler.cs` (izinler yalnızca JWT `perm` claim'inden); `Application/Features/Users/Commands/ResetUserPasswordCommand.cs:65`; `UserStatusCommands.cs:74-75` (yorum: "kicked out immediately"); `Api/appsettings.json:40` (`AccessTokenExpiryMinutes: 60`); `Api/Middleware/StaleTenantMiddleware.cs` (yalnızca kurum durumu) | Şifre sıfırlama, askıya alma ve silme yalnızca yenileme token'larını iptal ediyor (`RevokeAllAsync`). Erişim token'ı ve içindeki izinler süresi dolana kadar geçerli. **Canlı deneme:** geçici kullanıcı giriş yaptı. Yönetici şifresini sıfırladıktan ve kullanıcıyı askıya aldıktan sonra eski erişim token'ıyla `GET /api/modules/me` ve `GET /api/menus/me` **200** döndü. `UserPasswordResetTests` yalnızca yenilemenin reddini sınıyor | Ele geçirilmiş bir hesap için yapılan sıfırlama ya da askıya alma, saldırganın elindeki oturumu en fazla 60 dakika kesmiyor. Rol veya izin kaldırma da aynı süre gecikmeli etkili oluyor. Kurum pasifleştirmede bu süre 1 dakikaya indirilmişken kullanıcı düzeyinde karşılığı yok | `StaleTenantMiddleware`'in benzeri bir kullanıcı denetimi ekleyin: `user_account.status` ve bir "token geçerlilik başlangıcı" (ör. `sessions_revoked_at`) okunur, JWT `iat` ile karşılaştırılır, kullanıcı başına 1 dk önbellekte tutulur. Sıfırlama, askıya alma, silme ve rol değişikliği bu değeri günceller. Bu **şema kararı gerektirir** (yeni kolon). Alternatifi JWT'deki oturum kimliğinin (`auth_session`) `revoked_at`'ine bakmak. Kısa vadede erişim token süresini 15 dk'ya indirmek etkiyi azaltır. `UserStatusCommands.cs:74`'teki yanıltıcı yorumu düzeltin, `UserPasswordResetTests`'e "eski erişim token'ı reddedilir" testi ekleyin |
| D2 | **Orta** | Kamp, Danışmanlık, Anketler (+ eski modüller) | iş kuralı (eşzamanlılık) | Bu turda ekranı yapılanlar: `Camps/Commands/CampCommands.cs:84-128` (`UpdateCampCommand`), `CampActivityCommands.cs:93-115` (`UpdateCampActivityCommand`), `Consultancy/Commands/ConsultancyPlanCommands.cs:103-112`, `InstitutionCommands.cs:91-99`, `Surveys/Commands/SurveyCommands.cs:116` (`UpdateSurveyDefinitionCommand`). Önceden de böyle olanlar: `UpdateMeetingCommand`, `UpdateLeaveRequestCommand`, `UpdateAssessmentTemplate`/`SetAssessmentTemplateActive`, `Update`/`Start`/`Complete`/`CancelAssessmentSession`, `Update`/`FinalizeAssessmentReport`, `UpdateCameraCommand`, `UpdateConsultancyAgreementCommand`, `UpdateFollowUpActivityCommand`, `UpdateConsentTemplateCommand`, `UpdateContractTemplateCommand` | Komut `int RowVersion` alıyor ve doğrulayıcı `GreaterThan(0)` istiyor, ama handler `entity.RowVersion != req.RowVersion` karşılaştırmasını yapmıyor. Projenin deseni elle karşılaştırma (`Corporation`, `Session`, `Lead`, `SessionNote` vb. yapıyor). `docs/ekransiz-endpointler.md:135` kamp etkinliği düzenlemesi için "`rowVersion` gerekir" diyor; uygulama bunu sağlamıyor | Aynı kaydı iki kişi düzenlerse ikincisi uyarı almadan birincinin değişikliğini eziyor. İstemci bayat bir `rowVersion` gönderse de kayıt yazılıyor | Bu 20 handler'a projenin standart kontrolünü ekleyin (`ValidationException` + `MsgKeys.Error.Concurrency`). Tekrarlanmaması için `TenantContractTests` benzeri bir sözleşme testi önerilir: `RowVersion` alan her komutun handler'ı bu alanı okumalı (yansıma ya da kaynak taraması; kullandığım tarama betiği tek dosyalık bir Python betiği) |
| D3 | Düşük | Kamp | iş kuralı | `Camps/Commands/CampEnrollmentCommands.cs:63-68`; `db/layer2_sped/11_camps.sql:45` (`unique (camp_period_id, student_id)`) | Kayıt handler'ı yalnızca `withdrawn` olmayan mevcut kaydı arıyor; çekilmiş öğrencinin aynı döneme yeniden kaydını izinli sayıyor. DB'deki tekil kısıt ise durumdan bağımsız. Sonuçta INSERT 23505'e düşüyor ve genel **409 `Error.DuplicateKey`** dönüyor. Kural modülün ilk sürümünden (`6084003`) kalma, ama kamp ekranları bu turda açıldığı için artık kullanıcıya ulaşabiliyor | Kaydı yanlışlıkla çekilen öğrenci aynı döneme yeniden alınamıyor ve kullanıcı anlamlı bir mesaj görmüyor | Ürün kararı: (a) yeniden kayıt istenmiyorsa handler `withdrawn`'ı da kapsasın ve `StudentAlreadyInCampPeriod` (ya da yeni bir "çekilmiş kayıt var" mesajı) dönsün; (b) isteniyorsa çekilmiş satır `enrolled`/`waitlist`'e geri alınsın (yeni INSERT yerine). Şema değişmez |
| D4 | Düşük | Kamp | iş kuralı (durum geçişi) | `CampEnrollmentCommands.cs:177-195` (tekil `CompleteEnrollment`) ve `:214-215` (toplu tamamlamada başlama kontrolü var); `views/camps/CampEnrollmentView.vue:229-233` | Toplu tamamlama başlamamış dönemde 409 `CampPeriodNotStarted` veriyor; **tekil tamamlama bu kontrolü yapmıyor**. Ekranda da "Tamamla" düğmesi dönem durumundan bağımsız gösteriliyor | Kamp başlamadan bir kayıt `completed` yapılabiliyor. Sonra o kayda yoklama girilemiyor ama kamp sonu raporu yazılabiliyor ("rapor yalnızca tamamlanmış kayıt için" kuralı anlamını yitiriyor) | Tekil tamamlamaya aynı `StartDate > today` kontrolünü ekleyin; düğmeyi başlamamış dönemde gizleyin (`CampPeriodDetailView`'daki `periodPhase` kullanılabilir) |
| D5 | Düşük | Kamp | iş kuralı (sahiplik) | `CampEnrollmentCommands.cs:89` (oluşturma) ve `:242-250` (`UpdateEnrollmentPackage`) | `StudentPackageId`'nin kaydın öğrencisine ait olduğu doğrulanmıyor. Bileşik FK yalnızca aynı kiracıyı garanti ediyor | Bir kamp kaydı başka bir öğrencinin paketine bağlanabilir. Bugün finans tarafında bu bağ kullanılmıyor ve ekrandan çağrılmıyor; etki ileride kredi tüketimi bağlanınca doğar | Her iki handler'da `finance.student_package.student_id == enrollment.StudentId` kontrolü yapılsın (aksi 422) |
| D6 | Düşük | Kamp | iş kuralı (yarış) | `CampEnrollmentCommands.cs:70-82` (oluşturma), `:136-143` (`PromoteFromWaitlist`) | Kontenjan "say, sonra ekle" ile denetleniyor; kilit ya da DB kısıtı yok | Aynı anda yapılan iki kayıt kontenjanı bir aşabilir | Dönem satırını `SELECT … FOR UPDATE` ile kilitleyin ya da işlemi serializable yapın. Düşük trafikte kabul edilebilir; not olarak kalabilir |
| D7 | Düşük | Anketler | iş kuralı (yarış) | `Surveys/Commands/SurveyCommands.cs:371-388` (`EnsureNotAnsweredBeforeAsync`); canlı DB `ops.survey_response` indeksleri (tekil indeks yok) | "Anket × yanıtlayan × öğrenci × seans başına bir yanıt" kuralı yalnızca uygulamada uygulanıyor | Çift tıklama ya da eşzamanlı gönderim iki yanıt yazabilir; sonuç özetindeki sayılar şişer | **Şema kararı:** `unique nulls not distinct (survey_id, coalesce(guardian_id, respondent_user_id), student_id, session_id) where submitted_at is not null` (ifade indeksi; PG15+). Ya da istemcide gönderim düğmesini kilitlemekle yetinin |
| D8 | Düşük | Hesap bağlama | tutarlılık (izin) | `components/shared/UserAccountLinkCard.vue:53,140` | "Hesap oluştur" düğmesi `managePermission + user:create + user:update` ile açılıyor. Form ise rol listesini `GET /roles` ile yüklüyor ve bu uç `role:read` istiyor | `role:read`'i olmayan bir kullanıcı formu açar; rol listesi 403 ile boş gelir ve kaydedemez. Bugünkü seed'de `user:create` yalnızca `admin`'de ve onda `role:read` da var, yani şu an etkisiz | `canCreate`'e `can(Permissions.Roles.Read)` ekleyin ya da bağlama ekranı için rol listesini `user:create` ile okunabilir küçük bir uçtan alın |
| D9 | Düşük | Bakım ekibi / Kurulum | yapılandırma | `devops/set_modules.py` (`--disable care_team`); `students.user_can_access_student()` (`care_team_abac_enabled` ayarına bakıyor, modül bayrağına bakmıyor); `docs/musteri/KURULUM_KILAVUZU.md:679-697` | Erişim kısıtı (ABAC) ile `care_team` modülü birbirinden bağımsız. Kılavuz kısıtı açmadan önce modülün açık olmasını şart koşuyor, ama tersini, yani kısıt açıkken modülü kapatmayı engelleyen ya da uyaran bir şey yok | Kısıt açık kurumda modül kapatılırsa eğitmenler klinik kayıtları göremez ve bakım ekibi ekranları/uçları 403 verdiği için kurum bunu kendisi düzeltemez | `set_modules.py --disable care_team` kurumda `care_team_abac_enabled = true` görürse dursun (`--force` olmadan) ve uyarsın. `--list` çıktısında kısıt durumu da gösterilsin |
| D10 | Bilgi | Doküman, izin kataloğu | tutarlılık | `docs/musteri/KURULUM_KILAVUZU.md:556-568, 667-670`; canlı DB `iam.permission` | (1) Kılavuzun "Bilinen eksikler / Yarım modüller" tablosu güncel değil: kamp için "dönem, etkinlik, katılım, yoklama … (26 uç) ekransız", danışmanlık için "ziyaret ayrıntısı, tamamlama … ekransız" yazıyor; bu ekranlar 2026-11-16/17'de yapıldı. "Diğer ekransız işler" listesindeki taslak faturaya indirim, burs güncelleme, takvim kayıtları, oda takvimi, toplu iptal/oda değişikliği, portal BEP özeti ve hiyerarşi bitirme 2026-11-15'te ekrana kavuştu. "Ne zaman açmalı?" bölümündeki kamp/danışmanlık maddesi de bayat. (2) Canlı izin kataloğu 331 satır; koddaki 327'ye ek olarak eski `finance:read`, `finance:write`, `session:write` (49'ar rolde) ve `refdata:manage` (25 rolde) var. Hiçbir uç bunları istemiyor | (1) Kurulumu yapan kişi müşteriye yanlış "eksik" listesi gösterir. (2) Rol düzenleme ekranında anlamsız izinler görünür; işlevsel risk yok | (1) A.10 tablosunu ve listeyi `docs/ekransiz-endpointler.md`'nin güncel haliyle eşitleyin. (2) Ya `Permissions.cs`'e "kullanımdan kalktı" olarak eklenip arayüzde gizlensin ya da bir migration ile rollerden kaldırılsın (izin satırı silinmez) |

`tsconfig.json`'da `forceConsistentCasingInFileNames` değerinin açıkça yazılması önerilir (Bilgi). Bugün değer `@vue/tsconfig`'ten miras geliyor; paket sürümü değişirse sessizce kaybolabilir.

---

## Doğrulanan alanlar (sorun bulunmadı)

### Güvenlik

- **Hesap bağlama (V48, `Common/Identity/UserAccountLinks.cs`):**
  - Bağlanan kullanıcı oturumdaki kurumdan olmalı (`TenantUserGuard`, aksi 404) ve bileşik FK bunu ikinci kez garanti ediyor.
  - Rol, aynı kurumun ya da global olmalı. Veli hesabı için seçilen rolde `portal:access` zorunlu.
  - Geçici şifre CSPRNG ile üretiliyor (14 karakter), bir kez dönüyor, log'a yazılmıyor. `must_change_password = true`.
  - Oluşturma ve bağlama tek işlemde (yarım kalmış hesap bırakmıyor).
  - Uç izinleri arayüzle aynı: eğitmen için `educator:update`, veli için `guardian:manage_portal`; her ikisinde `user:update`, oluşturmada ayrıca `user:create`.
  - Kullanıcı adı kurumlar arasında tekil (`iam.username_exists`).
- **Veli portalı:**
  - Seans notları yalnızca `parent_visible`, silinmemiş ve **`student_id` = istenen çocuk** olanlardan geliyor (`PortalExtendedQueries.cs:131-137`). Grup seansında öğrenci seçilmeden veliye görünür not yazılamıyor (`SessionNoteStudent.ResolveAsync`, 422). V56 eski grup notlarını veliye kapattı.
  - Not düzenlemeyi yalnızca yazar ya da `session_note:delete` sahibi yapabiliyor; `rowVersion` kontrol ediliyor.
  - Anket yanıtında veli kimliği oturumdan türetiliyor (`PortalGuardian.ResolveAsync`). Başka velinin id'si gelirse 403. `StudentId` ve `SessionId` velinin erişimi olan çocuğa ait olmalı. Soru ve seçenek ankete ait olmalı (Y6 korunuyor).
  - Veli geri bildiriminde de aynı kurallar uygulanıyor.
  - Gelişim raporları yalnızca kesinleşmiş olanlar ve yalnızca `CanViewReports` açıksa (Y15 korunuyor). Smoke veli turu 12/12.
- **Modül görünürlüğü:**
  - 6 modül controller'ının hepsi sınıf düzeyinde `[RequireModule]` taşıyor (Kamp, Danışmanlık, Kamera, Anket, Bakım Ekibi, KPI).
  - Modül verisini başka bir uçtan sızdıran sorgu yok. Tek dış kullanım rıza geri alınınca izleme yetkisinin iptali (`StudentConsentCommands`); modül kapalı olsa da çalışması doğru.
  - Kurum yöneticisinin `PUT corporations/{id}/settings` isteğindeki `modules` yok sayılıyor (`CorporationModules.PreserveModules`). Genel `PUT corporations/{id}` ayarlara dokunmuyor.
  - Frontend rotalarının hepsinde `meta.module` var; `npm test` bunu ve `/403`'ü doğruluyor. Menü bayrakları canlı DB'de eksiksiz.
- **Pasif kurum (V57):** `iam.corporation_is_active` SECURITY DEFINER, `PUBLIC`'ten geri alınmış. Giriş, yenileme ve oturum içi istekler (1 dk önbellek) entegrasyon testlerinde geçiyor. `/api/auth` istisnası yalnızca kimlik doğrulama uçlarını kapsıyor.
- **İlk girişte şifre (V50):** `PasswordChangeRequiredMiddleware` ve `MustChangePasswordTests` geçiyor.
- **Kurulum sertleştirmesi:**
  - `smoke-proxy` uçtan uca yeşil: güvenilen vekil, sahte başlığın reddi, gerçek IP'nin kaydı.
  - Compose'da DB ve Redis yayınlanmıyor, API ve web yalnızca 127.0.0.1'de.
  - API konteyneri yalnızca kendi `environment` listesini alıyor; sahip rolü bilgisi yok.
- **Kamera rızası (V52):** izleme başlarken yetkili velinin rızası aranıyor; rıza geri alınınca o velinin açık yetkileri iptal ediliyor ve gerekçe yazılıyor.
- **Hata yanıtları:** yeni 409/422 kuralları kodlu `MsgKey` ile dönüyor. Smoke testinde 5xx yok.

### Tutarlılık

- Rota, DTO, istek, durum ve izin karşılaştırması (yukarıdaki yöntem tablosu): 0 fark.
- `endpoints.json` güncel.
- Kamp/danışmanlık/anket ekranlarının çağırdığı her uç mevcut. Kamp performans rotası (`camp:read`) uçla aynı izni istiyor.

### İş kuralları

- **Kamp:**
  - Pasifleştirme, silme, dönem düzenleme/silme, bekleme listesi, toplu tamamlama ve yoklama/katılım düzeltmeleri için 409 kuralları uygulanıyor; `campFlows` ve `campDailyFlows` smoke akışları bunları sınıyor.
  - Kayıt geçişleri domain'de doğrulanıyor: `enrolled ⇄ waitlist`, `enrolled → completed`, terminal durumdan çıkış yok. İstisnalar D3 ve D4.
  - Yoklama ve katılım DB'de tekil (`(enrollment, date)`, `(activity, enrollment)`).
- **Danışmanlık:** tamamlanmış ziyaretin gözlemi kilitli (düzenleme/silme 409); bağlı kayıt varken silme anlamlı hatayla reddediliyor.
- **Anketler:** yanıt gelen anketin soruları kilitli; zorunlu soru ve 1–5 puan denetimi var; tekrar yanıt 422 (yarış için D7).
- **Bakım ekibi:** komutlar `rowVersion`'ı karşılaştırıyor. Ön filtre DB kuralını izliyor (`CareTeamPreFilterTests`).
- **Toplantı:** iptal edilmiş toplantıya sonuç girilemiyor; tamamlanmışa girilebiliyor.

---

## Kapsam ve sınırlar

- D1 canlı yığında iki geçici kullanıcıyla denendi; kullanıcılar deneme sonunda silindi (`DELETE /api/users/{id}`, 200). Diğer bulgular kod okuması ve canlı DB sorgusuyla doğrulandı. D3–D7 için yazma denemesi yapılmadı (demo verisini değiştirirdi).
- Bir düğmenin kontrol ettiği izin ile ucun istediği izin, yalnızca bu turda değişen ekranların yüksek riskli düğmelerinde elle karşılaştırıldı (hesap bağlama, kamp kayıt/performans, anket). Bütün ekranlar sistematik olarak karşılaştırılmadı; ilk rapordaki 3. sınır sürüyor.
- Yanıt alanı karşılaştırması `ProducesResponseType` taşımayan uçlarda yalnızca smoke testiyle çalışma anında yapılabildi; 56 boş listede öğe alanları doğrulanamadı.
- Önceki turdan açık kalan #88 (promosyon kullanım limiti, şema kararı bekliyor) bu turda ele alınmadı.
