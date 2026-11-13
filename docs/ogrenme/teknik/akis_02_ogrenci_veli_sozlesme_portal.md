# Akış 2 — Öğrenci + veli kaydı → sözleşme ve rızalar → veli portal erişimi

**Ne işe yarar:** Öğrencinin dosyasını açmak, velileri bağlamak, imzalı sözleşme/rıza altyapısını kurmak ve velinin portaldan çocuğunu görmesini sağlamak.

**Kim yapar:** Sekreter/koordinatör (öğrenci, veli), hukuk/yönetici (sözleşme/rıza), kurum yöneticisi (portal erişimi). Demo'da `admin` ve `koordinator` hepsini yapabilir; `educator` yalnızca okur (`student:read`, `guardian:read`).

**Menü:** `Öğrenciler` (`students`, izin `student:read`; alt: `students-list`, `students-new`) · `Hukuk` (`legal`, izin `student_contract:read`; alt: `legal.contracts`, `legal.consents`, …). Router ayrıca `/guardians` (Veliler) ekranını açar; menü seed'inde veli satırı pasif (`students-guardians`, V31) — veli yönetimine öğrenci kartından veya doğrudan `/guardians` adresinden gidilir [doğrulanacak: güncel menüde ayrı bir "Veliler" girişi var mı].

## Durumlar

| Varlık | Durumlar |
|---|---|
| Öğrenci (`student_status`, ref veri) | prospect → active → on_hold / graduated / terminated. Geçiş kuralı yalnızca "değer gerçekten `student_status` türünden olmalı"; sıra zorunluluğu kodda görülmedi [doğrulanacak]. Her değişiklik `students.student_status_history`'ye yazılır. |
| Sözleşme (`ContractStatus`) | `draft` → `sent` → `signed` → `active` → `expired` ; `terminated` (feshedilebilir) |
| Rıza (`ConsentState`) | `granted` → `withdrawn` ; `expired` |
| Gelişim raporu | taslak → kesinleşmiş (geri dönüş yok) |

## Adım adım

### A. Öğrenci kaydı
1. `Öğrenciler → Öğrenci Ekle` (`/students/new`, `student:create`): ad, soyad, TC kimlik no, doğum tarihi, cinsiyet, birincil kampüs, not. (Aday dönüşümünde bu form adaydan dolu gelir — Akış 1.)
2. Öğrenci kartı sekmeleri: **Genel Bakış** (tanılar, gelişim profili, acil iletişim, tıbbi/gelişim/kurum raporları), **Veliler**, **Programlar** (kayıtlar — Akış 4), **Vaka Yönetimi** (vaka notları, `case_note:*`), **Geçmiş** (durum geçmişi).
3. **Durum Değiştir** (`student:change_status`): yeni durum + sebep.
4. Kampüs: **Kampüse Kayıt**, **Transfer Et**, **Kaydı Bitir** (`student:update`). Aynı kampüse çift aktif kayıt: "Öğrenci bu kampüste zaten aktif kayıtlı."

### B. Veli kaydı ve bağlama
1. Veli yoksa `Veliler → Veli Ekle` (`guardian:create`): ad, soyad, TC, e-posta, telefon, meslek, adres.
2. Öğrenci kartı → **Veliler** sekmesi → **Veli Bağla**: veli, yakınlık, **Birincil Veli**, **Velayeti Var**, **Mali Sorumlu**, **Portal Erişimi** işaretleri. "Bağlantıyı Düzenle" / "Bağlantıyı Kaldır" ile değiştirilir.

### C. Sözleşme
1. **Şablon:** `Hukuk → Sözleşme Şablonları` (`TemplateListView`, sözleşme ve onam için ortak): **Yeni şablon** (`contract_template:create`; kod `^[a-z0-9_]+$`, tür, geçerlilik, başlık + metin `tr`), güncel sürümde **Düzenle** (`PUT …/{id}`, `:update`; kullanılmış/arşiv sürümde backend reddeder) ve **Yeni sürüm** (`POST …/{id}/new-version`, `:version`; güncel sürüm arşivlenir, metin kopyalanır). "Arşiv sürümlerini de göster" süzgeci sürüm geçmişini listeler. Silme ekranda yok (imzalı/rızalı kayıtlar kalıcı; `DELETE` uçları [ekransız-endpointler](../ekransiz-endpointler.md) C grubunda).
2. `Hukuk → Sözleşmeler → Yeni Sözleşme` (`student_contract:generate`): öğrenci, şablon, başlangıç/bitiş. Bitiş başlangıçtan önce olamaz.
3. Sözleşme detayında düğmeler (izne bağlı): **Gönder** (`:send`, draft→sent) → **İmzala** (`:sign`, sent→signed; imza yöntemi *Islak İmza / Elektronik İmza / Tıkla ve Kabul Et*, imzalayan adı, imzalı belge) → **Feshet** (`:terminate`). İmza uyarısı: "Bu işlem geri alınamaz."
4. **Etkinleştir** (`:activate`, signed→active) ve **Süresini bitir** (`:expire`, active→expired) sözleşme detayında düğme olarak vardır (onay penceresiyle; geçersiz geçiş hatası sayfada gösterilir).
5. Sözleşme yalnızca **taslakken** silinebilir; aktif olanlar fesihle kapatılır.

### D. Rıza (KVKK, kamera, foto-video …)
- `Hukuk → Rızalar` **listeler** (öğrenci, tür, şablon, rıza tarihi, geçerlilik, zorunlu/opsiyonel). Rıza **verme** (`POST /api/legal/consents/grant`, `student_consent:grant`) ve **geri çekme** (`…/{id}/withdraw`, `:withdraw`) öğrenci kartındaki **Rızalar** sekmesindedir (`StudentConsentsPanel`; sekme `student_consent:read` ile görünür): tür ya da güncel şablon + veli + geçerlilik; kamera rızasında veli zorunlu. Kanıt dosyası ekleme (`…/{id}/evidence`) ekranda yok (dosya yükleme akışı yok).
- Kamera izleme rızaya bağlıdır (Akış 9). Doğrulanan ilişki: `ViewingAuthorization` verilirken **o veli adına** verilmiş, süresi geçmemiş `camera_viewing` rızası aranır (`ViewingCommands.cs`, kapı 2); izleme başlarken rıza yeniden kontrol edilir (öğrenci düzeyinde, `CameraConsentWithdrawn`). Rızayı geri almak açık yetki kaydını iptal etmez, izlemeyi engeller.

### E. Veli portal erişimi
1. Veli kartı → **Portal Erişimi** sekmesi → öğrenci satırında **Erişim Aç** (`guardian:manage_portal`). Kutular: Oturumlar, Devam, Raporlar, BEP/IEP, Finans, Kamera. Sonradan **İzinleri Güncelle**, **Erişimi Kapat**.
2. Erişim açılınca `students.guardian_portal_access` kaydı oluşur (kapatınca `RevokedAt` dolar) ve öğrenci-veli bağındaki `portal_access` işareti açılır.
3. Erişim Aç **kullanıcı hesabı açmaz** (`GrantPortalAccessCommand` yalnızca `guardian_portal_access` + `student_guardian.portal_access` yazar; doğrulandı). Giriş için `students.guardian.user_id` ↔ `iam.user_account` bağı gerekir: V48 ile `api/guardians/{id}/user-account` uçları (bağla / kaldır / rol seçip oluştur + tek seferlik geçici şifre) ve veli kartında **Kullanıcı hesabı** bölümü eklendi; hesap yoksa Portal Erişimi sekmesi uyarır. Yeni hesabın rolü `portal:access` içermeli; her kurumda `guardian` (Veli (portal)) rolü hazır. Ayrıntı: Akış 14 teknik. Demo-seed bağı artık API ile kurar.
4. Veli girince **Veli Portalı** grubunu (Özet / Çocuklarım / Bildirimler, V49) görür (Akış 13 ve Akış 4/5/6'daki veli görünümleri).

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Sözleşme taslak değilken gönder | "Yalnızca taslak sözleşmeler gönderilebilir. Mevcut durum: '{0}'." |
| Gönderilmemiş sözleşmeyi imzala | "Yalnızca gönderilmiş sözleşmeler imzalanabilir. Mevcut durum: '{0}'." |
| İmzasızı etkinleştir | "Yalnızca imzalanmış sözleşmeler etkinleştirilebilir. Mevcut durum: '{0}'." |
| Aktif olmayanı süresi doldu yap | "Yalnızca aktif sözleşmelerin süresi dolabilir. Mevcut durum: '{0}'." |
| Sonlandırılamayan durum | "'{0}' durumundaki bir sözleşme sonlandırılamaz." |
| Taslak olmayanı sil | "Yalnızca taslak sözleşmeler silinebilir. Aktif sözleşmeler için sonlandırma işlemini kullanın." |
| Verilmemiş rızayı geri çek | "Yalnızca verilmiş bir onay geri çekilebilir. Mevcut durum: '{0}'." |
| Verilmemiş rızaya kanıt | "Kanıt yalnızca verilmiş bir onaya eklenebilir." |
| Arşivlenmiş şablonu değiştir | "'{0}' v{1} sözleşme/onay şablonu arşivlenmiş ve değiştirilemez." |
| Kesinleşmiş gelişim raporunu düzenle | "Kesinleşmiş gelişim raporu değiştirilemez." / "Gelişim raporu zaten kesinleşmiş." |
| Portal erişimi olmayan veli çağırır | `Rule.GuardianNoPortalAccess` → 403 (veli o öğrenciyi göremez) |

- **Gelişim raporu → veli:** Öğrenci kartı → Gelişim Raporları → **Kesinleştir**. "Kesinleşen rapor veliye görünür hale gelir ve artık düzenlenemez." Veli taslakları göremez (son commit: bakım ekibi süzgeci düzeltmesi).
- İmzalı sözleşme ve rıza **değiştirilmez**; yeni sürüm açılır (CLAUDE.md alan kuralı).
- Kullanıcı adı **tüm kurumlarda benzersiz**; veli kullanıcısı için e-posta kullanın.

## Koddaki yeri

- Vue: `views/students/*` (`StudentListView`, `StudentFormView`, `StudentDetailView`, `StudentDashboardView`, `StudentTimelineView`), `views/guardians/*`, `views/legal/*`
- Store/servis: `stores/student.store.ts`, `guardian.store.ts`, `case.store.ts`, `contract.store.ts`, `consent.store.ts`, `signature.store.ts`; `services/student.service.ts`, `legal.service.ts`
- Controller: `StudentsController.cs`, `GuardiansController.cs`, `LegalController.cs`
- Command/Query: `Application/Features/Students/Commands/` (`StudentCommands`, `StudentStatusCommands`, `GuardianLinkCommands`, `PortalAccessCommands`, `DevelopmentReportCommands`, …), `Features/Legal/Commands/` (`StudentContractCommands`, `StudentConsentCommands`, `*TemplateCommands`)
- Domain: `Domain/Modules/Students/Entities/`, `Domain/Modules/Legal/Entities/`
- DB: `students.student`, `student_campus`, `student_status_history`, `guardian`, `student_guardian`, `guardian_portal_access`, `emergency_contact`, `diagnosis`, `developmental_profile`, `medical_report`, `development_report`, `external_institution_report`, `case_note`; `legal.contract_template(+_translation)`, `student_contract`, `consent_template(+_translation)`, `student_consent`

## Kendin dene (demo)

- `koordinator@demo-a.test`: 15 öğrenci/23 veli var. Bir öğrenciyi açıp Veliler sekmesinde birincil veliyi ve portal anahtarını görün; gelişim raporunda **Kesinleştir** deneyin.
- `admin@demo-a.test`: `Hukuk → Sözleşmeler`: 8 sözleşme farklı durumlarda (taslak/gönderilmiş/imzalı/aktif/feshedilmiş). Taslakta **Gönder**, gönderilmişte **İmzala**; imzalı olanda tekrar **Gönder** deneyip hata mesajını görün.
- `Hukuk → Rızalar`: kamera dahil rızalar, 1'i geri çekilmiş (yalnızca görüntüleme).
- Veli tarafı: `veli1@demo-a.test` (2 kardeş, kamera yetkili) ve `veli2@demo-a.test` (tek öğrenci) ile giriş yapıp `Portal → Çocuklarım`.
