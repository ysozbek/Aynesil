# AyNesil — Kullanım Kılavuzu

Bu doküman **son kullanıcı** (kurum yöneticisi, eğitmen, veli, sekreter) içindir. Teknik mimari için `ARCHITECTURE.md` ve `docs/` klasörüne bakın.

---

## 1. AyNesil Nedir?

AyNesil, özel eğitim kurumları için geliştirilmiş **çok-kiracılı (multi-tenant) bir yönetim platformudur**. Tek bir kurumdan birden fazla kampüse, aday öğrenciden faturaya, bireysel eğitim planından kamera izlemeye kadar günlük operasyonun tamamını tek yerde yönetmenizi sağlar.

**Temel yapı:**
- **Kurum (Corporation)** → şirketiniz (örn. "Akran Hareket")
- **Kampüs (Campus)** → kurumunuzun şubeleri
- **Kullanıcılar** → roller üzerinden yetkilendirilir (RBAC)
- **Modüller** → kurumun satın aldığı/aktifleştirdiği özellikler (CRM, Takvim, Finans, Kamp, Kamera, vb.)

> **Not:** Hiçbir veri kurumlar arası görünmez. Her kullanıcı sadece kendi kurumunun verisini görür.

---

## 2. Giriş ve Temel Arayüz

### Giriş yapma
1. Tarayıcıdan sisteme gidin (örn. `https://uygulama-adresiniz`).
2. E-posta ve şifrenizle **Giriş Yap**.
3. Başarılı girişte **Dashboard** açılır.

### Arayüzün ana bölümleri
- **Sol menü:** Rolünüze göre dinamik olarak oluşur. Yetkiniz olmayan modüller görünmez.
- **Üst bar:** Bildirimler, profil, kurum/kampüs seçici (birden fazla erişiminiz varsa).
- **İçerik alanı:** Seçtiğiniz modülün ekranı.

### Yetki (Permission) mantığı
- **Rol adına göre değil, izin adına göre** çalışır. Yani "Admin" olmak değil, `student:create` izninin olması önemlidir.
- Bir ekrana girmeye çalışırsanız ve yetkiniz yoksa **403** sayfası görürsünüz. Bu bir hata değildir — yetki eksikliğidir. Yöneticinize başvurun.

---

## 3. Rollere Göre Tipik Kullanım

### 3.1 Kurum Yöneticisi / Sistem Yöneticisi
En geniş yetki sahibi. İlk kurulumda şu sırayı izleyin:

1. **Kurum Ayarları** → `Ayarlar` menüsünden kurum bilgileri, logo, zaman dilimi, varsayılan dil.
2. **Kampüsler** → `Kampüsler` → şubelerinizi ekleyin.
3. **Referans Veriler** → `Referans Veriler` → seans tipleri, terapi türleri, lead kaynakları gibi listeleri kurumunuza göre özelleştirin. **Kodda hiçbir liste gömülü değil** — hepsi buradan yönetilir.
4. **Roller ve İzinler** → `Roller` → kurumunuza özel roller tanımlayın (ör. "Kampüs Müdürü", "Terapist", "Sekreter"). Her role hangi izinler verileceğini seçin.
5. **Kullanıcılar** → `Kullanıcılar` → personelinizi ekleyin, rol atayın.
6. **Menü Yönetimi** → `Menüler` → kullanıcı rollerine göre hangi menü öğelerinin görüneceğini özelleştirebilirsiniz.

### 3.2 CRM / Satış ekibi (Aday takibi)
`CRM` menüsü altında çalışır.

1. `CRM → Adaylar → Aday Ekle` ile gelen başvuruyu kaydedin (iletişim kişisi, çocuk bilgisi, kaynak, başvuru nedeni).
2. `CRM → Pipeline` ekranında adaylar aşama aşama ilerler. Aşama adları kurumunuza göre `Referans Veri` ekranından ayarlanır.
3. Aday detayındaki `Aktiviteler` ve `Görüşmeler` sekmelerinde her telefon, e-posta ve görüşme kaydedilir.
4. Aday kayıt olmaya karar verdiğinde:
   - Önce `Öğrenciler → Öğrenci Ekle` ile öğrenci kaydını açın, gerekirse `Veliler → Veli Ekle` ile veliyi ekleyin.
   - Sonra aday detayından **Öğrenciye Dönüştür** ile adayı bu öğrenciye bağlayın.
   - Not: Dönüştürme ekranı şu an öğrencinin ID'sini istiyor. ID, öğrenci detay sayfasının adres çubuğundaki son kısımdır.

### 3.3 Değerlendirme Ekibi
`Değerlendirme` menüsünden:
1. `Şablonlar` → kullanacağınız değerlendirme formlarını oluşturun (bir kere yapılır).
2. `Değerlendirmeler → Oturum Oluştur` → aday veya öğrenci için değerlendirme planlayın.
3. Oturum detayında `Puanlama` sekmesinden formu doldurun, `Rapor` sekmesinden raporu hazırlayın, `Öneriler` sekmesinden program önerisi ekleyin.

### 3.4 Eğitmenler / Terapistler
1. `Takvim` → günlük/haftalık seanslarınızı görün.
2. Seansa tıklayın → öğrenci bilgisi, hedefler, önceki notlar açılır.
3. Seans sonunda: **yoklama al** (öğrenci geldi/gelmedi), **seans notu** yaz, **hedef ilerlemesi** gir.
4. Gelmeyen öğrenci için → `Telafi Talebi` oluşturun.
5. `İzin` menüsünden kendi izin taleplerinizi girin.

### 3.5 BEP (Bireysel Eğitim Planı) Sorumluları
1. `BEP → Yeni Plan` → öğrenci için dönemsel plan oluşturun.
2. `Hedefler → Hedef Kütüphanesi` → sık kullanılan hedef şablonlarını önceden hazırlayın.
3. Plana hedef ekleyin, sorumlu eğitmeni atayın, hedef süresi belirleyin.
4. Seans ilerledikçe hedef yüzdesi otomatik güncellenir, raporlanır.

### 3.6 Sekreter / Resepsiyon (Planlama)
1. `Planlama → Planlama Paneli` → bugünkü seansları görün. `Planlama → Takvim` → haftalık tablo.
2. `Planlama → Seanslar → Seans Oluştur` → tek seferlik seans. Öğrenci ve eğitimci, seans detayındaki `Katılımcılar` sekmesinden eklenir.
3. `Planlama → Tekrar Eden Planlar → Plan Oluştur` → haftalık tekrar eden seans düzeni. Öğrenciyi, aldığı programı, hizmeti, sorumlu eğitimciyi, kampüsü, odayı, günleri ve saati seçersiniz; örneğin "Her Pazartesi 10:00, Ayşe, Dil ve Konuşma, Pınar Öğretmen, 2 Nolu Oda". Ardından listede **Seansları Oluştur** ile bir tarih aralığı verirsiniz. Sistem seansları öğrenci, eğitimci ve hizmetle birlikte üretir. Eğitimcinin veya odanın o saatte dolu olduğu tarihleri atlar ve kaç tarihin atlandığını söyler.
4. `Odalar` → çakışma kontrolü için oda listesini güncel tutun.
5. `Telafi Talepleri` → gelen talepleri onaylayın/reddedin.

### 3.7 Finans / Muhasebe
1. `Finans → Paketler` → sattığınız seans paketlerini tanımlayın (ör. "20 Seanslık Bireysel Terapi Paketi").
2. Öğrenciye paket satışı: `Öğrenci Paketleri → Paket Ata`. Öğrenciyi ve paketi seçersiniz; kredi sayısı, fiyat ve son kullanım tarihi paketten gelir. Kaydedince paketteki kredi öğrenciye yüklenir. Paket atamak ödeme almaz; ödemeyi `Ödemeler` ekranından ayrıca kaydedin.
3. `Kredi Defteri` → öğrencinin kalan seans hakkı takip edilir (seans yapıldıkça düşer).
4. `Faturalar` → fatura kesme.
5. `Ödemeler → Yeni Ödeme` → tahsilat kaydı. **Finansal kayıtlar silinmez** — yanlış kayıt için düzeltme (ters kayıt) yapılır.
6. `Burslar` ve `Promosyonlar` → indirim tanımları.

### 3.8 Hukuk / Sözleşme Yönetimi
1. `Hukuk → Sözleşme Şablonları` → standart sözleşme metinlerinizi şablon olarak kaydedin.
2. `Sözleşmeler → Yeni` → öğrenci için şablondan sözleşme üretin, imza için gönderin.
3. `Onamlar (Consents)` → KVKK, kamera izni, foto-video izni gibi onayları öğrenci bazında kaydedin.
4. **İmzalanmış sözleşmeler ve onamlar değiştirilemez** — yeni versiyon oluşturulur, geçmiş kaybolmaz.

### 3.9 Kamp Yönetimi (modül aktifse)
1. `Kamplar → Yeni Kamp` → tarih, kapasite, konu belirleyin.
2. `Dönem Kayıtları` → öğrencileri döneme kaydedin.

### 3.10 Okul Danışmanlığı (modül aktifse)
Kurumunuzun dışarıdaki okullara verdiği danışmanlık hizmetini yönetir.
1. `Danışmanlık → Kurumlar → Yeni` → danışmanlık verdiğiniz okulu ekleyin.
2. `Planlar` → yıllık danışmanlık planı.
3. `Ziyaretler` → saha ziyaret kayıtları.
4. `Gözlemler`, `Raporlar`, `Takipler` → süreç takibi.
5. `Sözleşmeler` → okulla yapılan anlaşmalar.

### 3.11 Kamera Yönetimi (modül aktifse)
1. `Kameralar → Yeni` → odalara kamera tanımlayın.
2. `Yetkiler` → hangi velinin hangi kamerayı ne zaman izleyebileceğini tanımlayın.
3. `İzleme Geçmişi` → her izleme loglanır (yasal zorunluluk).

### 3.12 KPI / Performans
1. `KPI → Tanımlar` → takip edeceğiniz metrikleri tanımlayın.
2. `Dashboard` → dönemsel performans görünümü.
3. `Snapshot'lar` → belirli tarih aralığındaki ölçümler.

### 3.13 Veli (Parent Portal)
Veliler ayrı bir arayüz görür: `Portal` menüsü altında.
1. `Çocuklarım` → kayıtlı tüm çocukları listelenir.
2. Çocuğa tıkla → **Seanslar, Hedefler, Paketler, Belgeler** sekmeleri.
3. `Bildirimler` → yeni seans, ödeme, rapor bildirimleri.
4. Veli yalnızca kendi çocuklarının verisine erişebilir.

---

## 4. Ortak Kavramlar

### Referans Veriler
"Seans tipi", "lead durumu", "ödeme yöntemi" gibi listeler **kodlanmamış**, veritabanında **referans veri** olarak tutulur. `Referans Veriler` ekranından kurumunuza özel değer ekleyebilirsiniz.

> ⚠️ Bir referans değeri gerçek kayıtlarda kullanıldıysa **silinemez**, sadece **pasifleştirilebilir**. Bu, geçmiş verinin bozulmamasını garanti eder.

### Soft Delete (Yumuşak Silme)
Çoğu kayıt "sil" dediğinizde fiziksel olarak silinmez; `IsDeleted` işaretlenir. Yöneticiniz gerekirse geri alabilir.

### Dil
Sistem Türkçe ve İngilizce destekler. Üst bardan dil değiştirebilirsiniz. Referans veriler ve menüler de çevrilidir.

### Bildirimler
- `Bildirimler → Gelen Kutusu` → size gelen bildirimler.
- `Şablonlar` → yöneticinin otomatik bildirim şablonları tanımladığı yer (ör. "Seans yarın", "Ödeme gecikti").
- `Tercihler` → kendi bildirim kanallarınızı (e-posta, SMS, portal içi) ayarlayın.

### Audit / Denetim
Kritik işlemler (öğrenci oluşturma, sözleşme imzalama, ödeme, izin onayı vb.) otomatik olarak loglanır. Geriye dönük sorumluluk sorgulanabilir.

---

## 5. Tipik İş Akışı — "Aday → Aktif Öğrenci" Örneği

1. **Sekreter:** `CRM → Yeni Aday` kaydı açar.
2. **CRM ekibi:** Aktivite kaydeder, görüşme yapar, pipeline'da ilerletir.
3. **Değerlendirme ekibi:** `Değerlendirme → Yeni Seans` planlar, formu doldurur.
4. **CRM ekibi:** Öğrenci kaydını açar, ardından aday detayında **Öğrenciye Dönüştür** ile adayı bu öğrenciye bağlar.
5. **BEP sorumlusu:** Yeni öğrenci için BEP oluşturur, hedefler ekler.
6. **Hukuk:** Sözleşme + onamları imzalatır.
7. **Finans:** Paket satışı + ilk ödeme.
8. **Planlamacı:** Haftalık seansları planlar (Tekrar Eden Planlar veya tek tek Seans Oluştur).
9. **Eğitmen:** Seans gerçekleştirir, hedef ilerlemesi günceller.
10. **Veli:** Portal'dan tüm bu süreci izler.

---

## 6. Sıkça Karşılaşılan Durumlar

| Durum | Yapılacak |
|---|---|
| Menü görünmüyor | Rolünüzde ilgili iznin olmayabilir → yöneticiye sorun. |
| 403 Yetkisiz hatası | Aynı sebep: izin eksik. |
| Bir referans değeri silinmiyor | Zaten kullanımda. "Pasifleştir" seçeneğini kullanın. |
| Yanlış ödeme girildi | Silmeyin — düzeltme (ters) kaydı oluşturun. |
| İmzalı sözleşmede değişiklik gerekiyor | Yeni versiyon oluşturun; eskisi tarihçede kalır. |
| Kampüsler arası çakışma var | Her kullanıcı kendi kampüsünü seçmeli; üst bardan kampüs değiştirin. |
| Veli portaldan göremiyor | İlgili öğrencinin velisi olarak tanımlı mı, kontrol edin. |

---

## 7. Yöneticiler İçin — Yeni Kurum/Kampüs Devreye Alma Özeti

1. Kurum kaydı oluştur → kampüsleri ekle.
2. Referans verileri gözden geçir (seans tipleri, terapi türleri, lead kaynakları…).
3. Rolleri tanımla, izinleri ata.
4. Kullanıcıları davet et.
5. Menü görünürlüğünü role göre düzenle.
6. Sözleşme ve onam şablonlarını yükle.
7. Paket tanımlarını ve fiyatları gir.
8. Bildirim şablonlarını ayarla.
9. Varsa kamera ve kamp modüllerini yapılandır.
10. Test kullanıcısıyla uçtan uca bir aday→öğrenci akışı dene.

---

## 8. Kavramsal Model — Program, Hizmet, Seans İlişkisi

Bu bölüm "Program" sözcüğünün sistemde **üç ayrı anlama geldiği** için en çok karıştırılan konuyu netleştirir.

### 8.1 Üç farklı "Program"

| Kavram | Ne demek? | Örnek |
|---|---|---|
| **Program** | Kurumun **sunduğu hizmet kataloğundaki bir ürün/paket tanımı**. Öğrenciye özel değil, bir "menü kalemi". | "Erken Çocukluk Terapi Programı", "Otizm Bireysel Programı" |
| **Program Hizmeti** (Program Service) | Bir programın **alt bileşenleri**. Programın içindeki tekil hizmet türleri. Varsayılan süre ve haftalık frekans burada tanımlanır. | "Erken Çocukluk Programı" içinde → Konuşma Terapisi (haftada 2, 45 dk), Duyu Bütünleme (haftada 1, 60 dk) |
| **Öğrenci-Program** (Student Program) | Bir **öğrencinin bir programa dahil edilmesi**. Yani kurumun programına belirli bir öğrencinin bağlanması. | "Ayşe → Erken Çocukluk, 01.09.2026 – devam ediyor, aktif" |

> **Benzetme:** `Program` = reçete şablonu, `Program Hizmeti` = reçetedeki ilaçlar, `Öğrenci-Program` = reçetenin Ayşe'ye yazılmış kopyası.

### 8.2 Tam hiyerarşi

```
Kurum (Corporation)
   │
   ├── Program  ("Erken Çocukluk Terapi Programı")
   │     ├── Program Hizmeti  ("Konuşma Terapisi — haftada 2, 45 dk")
   │     ├── Program Hizmeti  ("Duyu Bütünleme — haftada 1, 60 dk")
   │     └── Program Hizmeti  ("Oyun Terapisi — haftada 1, 45 dk")
   │
   └── Öğrenci (Ayşe)
         │
         ├── Kayıt (Enrollment)  — Ayşe'nin kuruma genel kayıt dosyası
         │     status: active, enrolled_on: 01.09.2026
         │
         └── Öğrenci-Program  ("Ayşe → Erken Çocukluk Programı")
               start_date: 01.09.2026, status: active
                     │
                     └── Düzenli Program (Recurring Schedule)
                           "Her Salı 10:00, 45 dk, Oda 3, Pınar Öğretmen,
                            Konuşma Terapisi"
                                 │
                                 └── Seans (Session) — somut takvim satırı
                                       07.11.2026 Salı 10:00
                                       (program hizmeti = Konuşma Terapisi)
                                       ├── Katılımcı (Participant)  → Ayşe
                                       ├── Eğitmen (Educator)       → Pınar Öğretmen
                                       ├── Seans Hedefi (Goal)      → BEP hedefi
                                       ├── Yoklama (Attendance)     → present/absent
                                       └── Seans Notu (Note)        → "Bugün 18 sözcük..."
```

### 8.3 Her katmanın tek cümlelik özeti

- **Kayıt (Enrollment)** — Öğrencinin kuruma **genel kayıt dosyası**. "Burada aktif mi, ne zaman geldi/ayrıldı?" sorusunu cevaplar. Bir öğrencinin tipik olarak tek kaydı olur.
- **Program** — Kurumun kataloğundaki hizmet tanımı. Tüm öğrenciler için ortak, bir kere tanımlanır.
- **Program Hizmeti** — Programın parçaları. Varsayılan süre (`default_duration_minutes`) ve haftalık frekans (`default_sessions_per_week`) burada şablonlanır.
- **Öğrenci-Program** — Öğrencinin hangi programlara dahil olduğu. **Bir öğrencinin birden fazla olabilir** (ör. hem Erken Çocukluk hem Aile Danışmanlığı). Her birinin kendi başlangıç/bitiş tarihi ve statüsü (`active / paused / completed / cancelled`) vardır.
- **Düzenli Program (Recurring Schedule)** — "Her Salı 10:00" gibi **tekrarlama kuralı**. Tek başına seans değil; sistem bu kuraldan haftalık somut seansları **üretir**. Bir öğrenci-programına bağlanır. Hangi hizmetin verileceği ve hangi eğitimcilerin gireceği de planda tutulur; üretilen her seansa kopyalanır.
- **Seans (Session)** — Takvimdeki somut satır. Belirli gün, saat, oda. Bireysel veya grup olabilir. Hangi hizmetin verildiği `program_service_id` ile bağlanır.
- **Seans Katılımcısı** — Seansa giren öğrenci(ler). Grup seansında N satır.
- **Seans Eğitmeni** — Seansı yürüten eğitmen(ler): lead, assistant, observer, supervisor.
- **Yoklama (Attendance)** — Öğrenci geldi mi? Her öğrenci × seans için bir kayıt.
- **Seans Hedefi** — "Bu seansta hangi BEP hedefleri üzerinde çalışıldı?" Seans ile öğrenci hedefi arasındaki köprü.
- **Seans Notu** — Eğitmenin notu. `parent_visible = true` ise veli portalında görünür.
- **Telafi Talebi (Makeup)** — Kaçırılan seansın telafisi. Kaçırılan ve telafi seansları arasındaki bağ.

### 8.4 Program ≠ BEP (çok karıştırılır)

| Program / Öğrenci-Program | BEP (Education Plan) |
|---|---|
| Kurumun verdiği hizmetin **ticari/operasyonel** tanımı. "Hangi paket?" | Öğrencinin **pedagojik** hedef dokümanı. "Hangi beceriler?" |
| Finans ve planlama tarafı | Pedagojik taraf |
| Program Hizmeti → Konuşma Terapisi (**hizmet tipi**) | Öğrenci Hedefi → "15 dk grup ortamında oturabilme" (**beceri**) |
| Onay akışı yok | Onay akışı var: `draft → in_review → approved → active → revised → closed` |

**Köprü:** Seans sırasında (`Seans Hedefi`) hangi BEP hedefleri üzerinde çalışıldığı işaretlenir → ilerleme (`goal_progress`) kaydedilir. Yani **program seansı yapılırken BEP hedefi işlenir**. İki dünya burada buluşur.

### 8.5 Finansla bağlantı

- Öğrenciye bir **paket** satılır (ör. "20 seanslık Konuşma Terapisi paketi") — bir program hizmetine karşılık gelir.
- Her tamamlanan seans + mevcut yoklama → `Kredi Defteri`nden bir kredi düşer.
- Formül: **Program = ne sattığımız, Seans = ne teslim ettiğimiz, Kredi Defteri = sayaç**.

### 8.6 Somut örnek — "Ayşe"

1. **CRM:** Ayşe için öğrenci kaydı açıldı, aday kaydı bu öğrenciye dönüştürüldü.
2. **Kayıt:** Ayşe için enrollment açıldı (status = active).
3. **Öğrenciyi programa ekle:** Ayşe + "Erken Çocukluk Programı" → `Öğrenci-Program` kaydı, 01.09.2026.
4. **Finans:** Ayşe'ye "40 Seanslık Erken Çocukluk Paketi" satıldı → paket + ödeme planı.
5. **Planlama:** Sekreter haftalık düzeni tanımlar:
   - Her Salı 10:00, 45 dk, Oda 3, Pınar Öğretmen → Konuşma Terapisi
   - Her Perşembe 11:00, 60 dk, Oda 1, Ali Öğretmen → Duyu Bütünleme
6. Sistem bu kurallardan haftalık somut **seanslar** üretir.
   > Not: `Tekrar Eden Planlar` formunda öğrenci, program, hizmet, eğitimci ve oda seçilir. Plandan üretilen her seans bu bilgileri otomatik taşır. Hizmet, öğrencinin aldığı programın hizmetlerinden biri olmak zorundadır.
7. **BEP:** Koordinatör Ayşe için BEP açar → hedefleri ekler → onaylar.
8. **Seans günü (07.11.2026 Salı 10:00):**
   - Eğitmen, hangi hedeflere çalıştığını işaretler
   - Yoklama alır
   - Not yazar
   - Hedef ilerlemesini günceller
9. **Arka planda:** Seans "completed" olunca Ayşe'nin 40 kredisinden 1 düşer.
10. **Veli portalı:** Not `parent_visible = true` ise veli görür. BEP onaylı + görünür ise hedef ilerlemesini de görür.

### 8.7 Aynı öğrenci birden fazla programda olabilir mi?

**Evet.** Bir öğrenci aynı anda:
- Erken Çocukluk Programı (aktif)
- Aile Danışmanlığı Programı (aktif)
- Yaz Kampı Programı (tamamlanmış)

şeklinde birden çok programda olabilir. Her birinin kendi seans düzeni, kendi paketi, kendi tarihçesi vardır. **BEP tektir** (öğrenci bazlıdır), tüm programlardaki hedefleri kapsar.

---

## 9. Nereden Devam Etmeli?

- **Mimari / geliştirici dokümantasyonu:** `ARCHITECTURE.md`, `docs/`
- **Veritabanı şeması:** `db/`
- **Modül bazlı detay:** sol menüdeki her ana modülün ilk ekranı o modülün "dashboard"udur; genel bakış oradan başlar.

> Bu kılavuz "nasıl kullanılır" sorusuna cevap verir. "Nasıl kurulur / nasıl geliştirilir" için teknik dokümanlara geçin.
