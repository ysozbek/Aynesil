# AyNesil — Kullanım Kılavuzu

Bu doküman **son kullanıcı** (kurum yöneticisi, eğitimci, veli, sekreter) içindir. Teknik ayrıntılar en sondaki **Teknik Ek** bölümündedir.

---

## 1. AyNesil Nedir?

AyNesil, özel eğitim kurumları için geliştirilmiş bir yönetim platformudur. Tek bir kurumdan birden fazla kampüse, aday öğrenciden faturaya, bireysel eğitim planından kamera izlemeye kadar günlük işlerin tamamını tek yerde yönetmenizi sağlar.

**Temel yapı:**
- **Kurum** → şirketiniz (örn. "Akran Hareket")
- **Kampüs** → kurumunuzun şubeleri
- **Kullanıcılar** → her kullanıcının bir veya birkaç rolü vardır; rol, neleri görüp yapabileceğini belirler
- **Modüller** → kurumunuzda açık olan bölümler (CRM, Takvim, Finans, Kamp, Kamera vb.)

> **Not:** Hiçbir veri kurumlar arasında görünmez. Her kullanıcı yalnızca kendi kurumunun verisini görür.

---

## 2. Giriş ve Temel Arayüz

### Giriş yapma
1. Tarayıcıdan sisteme gidin (örn. `https://uygulama-adresiniz`).
2. Kullanıcı adınız (genellikle e-posta adresiniz) ve şifrenizle **Giriş Yap**'a basın.
3. Girişten sonra **Ana Sayfa** açılır.

### Arayüzün ana bölümleri
- **Sol menü:** Rolünüze göre oluşur. Yetkiniz olmayan bölümler görünmez.
- **Üst bar:** Bildirimler, profil, kampüs seçici (birden fazla kampüse erişiminiz varsa).
- **İçerik alanı:** Seçtiğiniz bölümün ekranı.

### Yetkiler
- Neleri görüp yapabileceğinizi, rolünüze verilen izinler belirler. Rolün adı değil, içindeki izinler önemlidir.
- Yetkiniz olmayan bir ekrana girmeye çalışırsanız "yetkiniz yok" sayfası çıkar. Bu bir hata değil, izin eksikliğidir; yöneticinize başvurun.

---

## 3. Rollere Göre Tipik Kullanım

### 3.1 Kurum Yöneticisi / Sistem Yöneticisi
En geniş yetki sahibi. İlk kurulumda şu sırayı izleyin:

1. **Kurum Ayarları** → `Ayarlar` menüsünden kurum bilgileri, logo, zaman dilimi, varsayılan dil.
2. **Kampüsler** → `Kampüsler` → şubelerinizi ekleyin.
3. **Referans Veriler** → `Referans Veriler` → seans tipleri, terapi türleri, aday kaynakları gibi listeleri kurumunuza göre düzenleyin. Bu listeler sabit değildir; hepsi buradan yönetilir.
4. **Roller ve İzinler** → `Roller` → kurumunuza özel roller tanımlayın (ör. "Kampüs Müdürü", "Terapist", "Sekreter"). Her role hangi izinlerin verileceğini seçin.
5. **Kullanıcılar** → `Kullanıcılar` → personelinizi ekleyin, rol atayın. Kullanıcı adı tüm sistemde benzersiz olmalıdır; e-posta adresi kullanmanız önerilir.
6. **Menü Yönetimi** → `Menüler` → hangi menü öğelerinin görüneceğini düzenleyebilirsiniz.

### 3.2 CRM / Satış ekibi (Aday takibi)
`CRM` menüsü altında çalışır.

1. `CRM → Adaylar → Aday Ekle` ile gelen başvuruyu kaydedin (iletişim kişisi, çocuk bilgisi, kaynak, başvuru nedeni).
2. `CRM → Pipeline` ekranında adaylar aşama aşama ilerler. Aşama adları `Referans Veriler` ekranından ayarlanır.
3. Aday detayındaki `Aktiviteler` ve `Görüşmeler` sekmelerinde her telefon, e-posta ve görüşme kaydedilir.
4. Aday kayıt olmaya karar verdiğinde aday detayından **Öğrenciye Dönüştür**'e basın:
   - **Yeni öğrenci oluştur:** Öğrencinin adı, soyadı ve doğum tarihi adaydan gelir; kampüs ve başvuru nedeni aktarılır. "İletişim kişisini veli olarak ekle" işaretliyse veli de açılır ve öğrencinin birincil velisi olarak bağlanır. **Öğrenciyi Oluştur ve Dönüştür** ile hepsi tek seferde yapılır.
   - **Var olan öğrenciye bağla:** Öğrenci kaydı daha önce açıldıysa öğrenciyi adıyla arayıp seçin.

### 3.3 Değerlendirme Ekibi
`Değerlendirme` menüsünden:
1. `Şablonlar` → kullanacağınız değerlendirme formlarını oluşturun (bir kere yapılır).
2. `Değerlendirmeler → Oturum Oluştur` → aday veya öğrenci için değerlendirme planlayın.
3. Oturum detayında `Puanlama` sekmesinden formu doldurun, `Rapor` sekmesinden raporu hazırlayın, `Öneriler` sekmesinden program önerisi ekleyin.

### 3.4 Eğitimciler / Terapistler
1. `Takvim` → günlük, haftalık veya aylık seanslarınızı görün. Ay görünümünde bir güne tıklayınca o günün ayrıntısı açılır.
2. Seansa tıklayın → öğrenci bilgisi, hedefler, önceki notlar açılır.
3. Seans sonunda: **yoklama alın** (öğrenci geldi/gelmedi), **seans notu** yazın, **hedef ilerlemesi** girin. Seansta çalışılan hedef listede yoksa `Hedefler` sekmesindeki **Hedef Ekle** ile öğrencinin aktif hedeflerinden ekleyin. Sonra **Tamamla**'ya basın (önce yoklama, sonra Tamamla; bkz. 8.5).
4. Gelmeyen öğrenci için → `Telafi Talebi` oluşturun.
5. `İzin` menüsünden kendi izin taleplerinizi girin.

### 3.5 BEP (Bireysel Eğitim Planı) Sorumluları
1. `BEP → Yeni Plan` → öğrenci için dönemsel plan oluşturun.
2. `Hedefler → Hedef Kütüphanesi` → sık kullanılan hedef şablonlarını önceden hazırlayın.
3. Plana hedef ekleyin, sorumlu eğitimciyi atayın, hedef süresi belirleyin.
4. Seanslarda hedefler işlendikçe ilerleme güncellenir ve raporlanır.

### 3.6 Sekreter / Resepsiyon (Planlama)
1. `Planlama → Planlama Paneli` → bugünkü seansları görün. `Planlama → Takvim` → haftalık tablo.
2. **Öğrenciyi programa ekleme:** `Programlar → Kayıtlar → Kayıt Oluştur` → öğrenciyi ve programı seçin. Öğrencinin kurumda açık bir kaydı yoksa kayıt açılır ve program eklenir. Kaydı zaten varsa yeni kayıt açılmaz; program mevcut kayda eklenir. Ayrıntı: 8.3.
3. `Planlama → Seanslar → Seans Oluştur` → tek seferlik seans. Kampüs, oda, öğrenci ve sorumlu eğitimci formda adıyla seçilir. Grup seansı için ek öğrenci ve eğitimciler seans detayındaki `Katılımcılar` sekmesinden eklenir.
4. `Planlama → Tekrar Eden Planlar → Plan Oluştur` → haftalık tekrar eden seans düzeni. Öğrenciyi, aldığı programı, hizmeti, sorumlu eğitimciyi, kampüsü, odayı, günleri ve saati seçersiniz; örneğin "Her Pazartesi 10:00, Ayşe, Dil ve Konuşma, Pınar Öğretmen, 2 Nolu Oda". Ardından listede **Seansları Oluştur** ile bir tarih aralığı verirsiniz. Sistem seansları öğrenci, eğitimci ve hizmetle birlikte üretir. Eğitimcinin veya odanın o saatte dolu olduğu tarihleri atlar ve kaç tarihin atlandığını söyler.
5. `Odalar` → çakışma kontrolü için oda listesini güncel tutun.
6. `Telafi Talepleri` → gelen talepleri onaylayın/reddedin.

### 3.7 Finans / Muhasebe
1. `Finans → Paket Tanımları` → sattığınız seans paketlerini tanımlayın (ör. "20 Seanslık Bireysel Terapi Paketi"). **Paketi mutlaka bir programa bağlayın**; seans hakkı otomatik düşerken bu bağlantıya bakılır.
2. Öğrenciye paket satışı: `Öğrenci Paketleri → Paket Ata`. Öğrenciyi ve paketi seçersiniz; seans hakkı sayısı, fiyat ve son kullanım tarihi paketten gelir. Kaydedince paketteki haklar öğrenciye yüklenir. Paket atamak ödeme almaz; ödemeyi `Ödemeler` ekranından ayrıca kaydedin.
3. `Kredi Defteri` → öğrencinin kalan seans hakkı takip edilir. Seans tamamlanınca otomatik düşer (bkz. 8.5); gerekirse buradan elle düşülür, iade veya düzeltme yapılır.
4. `Faturalar` → fatura kesme.
5. `Ödemeler → Yeni Ödeme` → tahsilat kaydı. **Finansal kayıtlar silinmez**; yanlış kayıt için düzeltme (ters kayıt) yapılır.
6. `Burslar` ve `Promosyonlar` → indirim tanımları.

### 3.8 Hukuk / Sözleşme Yönetimi
1. `Hukuk → Sözleşme Şablonları` → standart sözleşme metinlerinizi şablon olarak kaydedin.
2. `Sözleşmeler → Yeni` → öğrenci için şablondan sözleşme üretin, imza için gönderin.
3. `Onamlar` → KVKK, kamera izni, foto-video izni gibi onayları öğrenci bazında kaydedin.
4. **İmzalanmış sözleşmeler ve onamlar değiştirilemez**; yeni versiyon oluşturulur, geçmiş kaybolmaz.

### 3.9 Kamp Yönetimi (modül açıksa)
1. `Kamplar → Yeni Kamp` → tarih, kapasite, konu belirleyin.
2. `Dönem Kayıtları` → öğrencileri döneme kaydedin.

### 3.10 Okul Danışmanlığı (modül açıksa)
Kurumunuzun dışarıdaki okullara verdiği danışmanlık hizmetini yönetir.
1. `Danışmanlık → Kurumlar → Yeni` → danışmanlık verdiğiniz okulu ekleyin.
2. `Planlar` → yıllık danışmanlık planı.
3. `Ziyaretler` → saha ziyaret kayıtları.
4. `Gözlemler`, `Raporlar`, `Takipler` → süreç takibi.
5. `Sözleşmeler` → okulla yapılan anlaşmalar.

### 3.11 Kamera Yönetimi (modül açıksa)
1. `Kameralar → Yeni` → odalara kamera tanımlayın.
2. `Yetkiler` → hangi velinin hangi kamerayı ne zaman izleyebileceğini tanımlayın.
3. `İzleme Geçmişi` → her izleme kayıt altına alınır (yasal zorunluluk).

### 3.12 KPI / Performans
1. `KPI Tanımları` → takip edeceğiniz ölçütleri tanımlayın.
2. `KPI & Performans` → dönemsel performans görünümü (Yönetici, Yönetim ve Sıralama sekmeleri).
3. `Performans Anlık Görüntüleri` → belirli dönemlerde hesaplanmış ölçümler.

### 3.13 Veli Portalı
Veliler ayrı bir arayüz görür: `Portal` menüsü altında.
1. `Çocuklarım` → kayıtlı tüm çocukları listelenir.
2. Çocuğa tıklayın → **Seanslar, Hedefler, Paketler, Belgeler** sekmeleri.
3. `Bildirimler` → yeni seans, ödeme, rapor bildirimleri.
4. Veli yalnızca kendi çocuklarının verisini görür.

---

## 4. Ortak Kavramlar

### Referans Veriler
"Seans tipi", "aday durumu", "ödeme yöntemi" gibi listeler sabit değildir; `Referans Veriler` ekranından yönetilir ve kurumunuza özel değer ekleyebilirsiniz.

> ⚠️ Bir değer kayıtlarda kullanıldıysa **silinemez**, yalnızca **pasifleştirilebilir**. Böylece geçmiş veri bozulmaz.

### Silinen kayıtlar
Çoğu kayıt "sil" dediğinizde kalıcı olarak silinmez, yalnızca listelerden kalkar. Yöneticiniz gerekirse geri alabilir.

### Dil
Arayüz şu an Türkçedir. Altyapı başka dilleri destekleyecek şekilde kurulmuştur (menüler ve referans veriler çevrilebilir), ancak İngilizce arayüz çevirisi henüz eklenmemiştir.

### Bildirimler
- `Bildirimler → Gelen Kutusu` → size gelen bildirimler.
- `Şablonlar` → yöneticinin otomatik bildirim şablonları tanımladığı yer (ör. "Seans yarın", "Ödeme gecikti").
- `Tercihler` → kendi bildirim kanallarınızı (e-posta, SMS, portal içi) ayarlayın.

### İşlem geçmişi
Önemli işlemler (öğrenci oluşturma, sözleşme imzalama, ödeme, izin onayı vb.) otomatik olarak kayıt altına alınır. Kimin ne zaman ne yaptığı geriye dönük sorgulanabilir.

---

## 5. Tipik İş Akışı — "Aday → Aktif Öğrenci" Örneği

1. **Sekreter:** `CRM → Adaylar → Aday Ekle` ile aday kaydı açar.
2. **CRM ekibi:** Aktivite kaydeder, görüşme yapar, pipeline'da ilerletir.
3. **Değerlendirme ekibi:** `Değerlendirme → Oturum Oluştur` ile değerlendirme planlar, formu doldurur.
4. **CRM ekibi:** Aday detayında **Öğrenciye Dönüştür** ile öğrenci ve veli kaydını aday bilgilerinden açar.
5. **Sekreter:** `Programlar → Kayıtlar → Kayıt Oluştur` ile öğrenciyi aldığı programa ekler.
6. **BEP sorumlusu:** Yeni öğrenci için BEP oluşturur, hedefler ekler.
7. **Hukuk:** Sözleşme ve onamları imzalatır.
8. **Finans:** Paket satışı ve ilk ödeme.
9. **Planlamacı:** Haftalık seansları planlar (Tekrar Eden Planlar veya tek tek Seans Oluştur).
10. **Eğitimci:** Seansı yapar, yoklama alır, hedef ilerlemesini günceller, seansı tamamlar.
11. **Veli:** Portaldan tüm bu süreci izler.

---

## 6. Sıkça Karşılaşılan Durumlar

| Durum | Yapılacak |
|---|---|
| Menü görünmüyor | Rolünüzde ilgili izin olmayabilir; yöneticinize sorun. |
| "Yetkiniz yok" sayfası çıkıyor | Aynı sebep: izin eksik. |
| Bir referans değeri silinmiyor | Zaten kullanımda. "Pasifleştir" seçeneğini kullanın. |
| Yanlış ödeme girildi | Silmeyin; düzeltme (ters) kaydı oluşturun. |
| İmzalı sözleşmede değişiklik gerekiyor | Yeni versiyon oluşturun; eskisi geçmişte kalır. |
| Kampüsler arası karışıklık var | Üst bardan doğru kampüsü seçin. |
| Veli portalda göremiyor | Veli, öğrencinin velisi olarak bağlı mı ve portal erişimi açık mı, kontrol edin. |
| Seans hakkı düşmedi | Yoklamada "Geldi" seçili mi, öğrencinin seansın programına bağlı ve hakkı kalan paketi var mı, kontrol edin (8.5). |
| "Bu kullanıcı adı alınmış" uyarısı | Kullanıcı adı başka bir kurumda da kullanılıyor olabilir. E-posta adresi kullanın. |

---

## 7. Yöneticiler İçin — Yeni Kurum/Kampüs Devreye Alma Özeti

Adım adım kurulum planı için: `docs/musteri/KURULUM_YOL_HARITASI.md`.

1. Kurum kaydı oluştur, kampüsleri ekle.
2. Referans verileri gözden geçir (seans tipleri, terapi türleri, aday kaynakları…).
3. Rolleri tanımla, izinleri ata.
4. Kullanıcıları ekle.
5. Menü görünürlüğünü düzenle.
6. Sözleşme ve onam şablonlarını yükle.
7. Programları, hizmetleri, paket tanımlarını ve fiyatları gir.
8. Bildirim şablonlarını ayarla.
9. Varsa kamera ve kamp modüllerini yapılandır.
10. Bir deneme öğrencisiyle uçtan uca aday → öğrenci → seans akışını dene.

---

## 8. Kavramsal Model — Program, Hizmet, Seans İlişkisi

Bu bölüm en çok karıştırılan konuyu netleştirir: "Program" sözcüğü sistemde **üç ayrı şeyi** anlatır.

### 8.1 Üç farklı "Program"

| Kavram | Ne demek? | Örnek |
|---|---|---|
| **Program** | Kurumun sunduğu hizmetlerden biri. Öğrenciye özel değildir; bir kere tanımlanır. | "Erken Çocukluk Terapi Programı", "Hareket ve Spor" |
| **Hizmet** | Programın içindeki seans türleri. Varsayılan süre ve haftalık sıklık burada tanımlanır. | "Erken Çocukluk Programı" içinde: Dil ve Konuşma Terapisi (haftada 2, 45 dk), Duyu Bütünleme (haftada 1, 60 dk) |
| **Öğrencinin programı** | Bir öğrencinin bir programa eklenmesi. | "Ayşe → Erken Çocukluk, 01.09.2026'dan beri, aktif" |

> **Benzetme:** Program = reçete şablonu, Hizmet = reçetedeki ilaçlar, Öğrencinin programı = reçetenin Ayşe'ye yazılmış kopyası.

### 8.2 Tam hiyerarşi

```
Kurum
   │
   ├── Program  ("Erken Çocukluk Terapi Programı")
   │     ├── Hizmet  ("Dil ve Konuşma Terapisi — haftada 2, 45 dk")
   │     ├── Hizmet  ("Duyu Bütünleme — haftada 1, 60 dk")
   │     └── Hizmet  ("Oyun Terapisi — haftada 1, 45 dk")
   │
   └── Öğrenci (Ayşe)
         │
         └── Kayıt  — Ayşe'nin kurumdaki dosyası (01.09.2026'dan beri aktif)
               │
               └── Öğrencinin programı  ("Ayşe → Erken Çocukluk Programı")
                     │
                     └── Tekrar Eden Plan
                           "Her Salı 10:00, 45 dk, Oda 3, Pınar Öğretmen,
                            Dil ve Konuşma Terapisi"
                                 │
                                 └── Seans — takvimdeki somut satır
                                       07.11.2026 Salı 10:00
                                       ├── Katılımcı     → Ayşe
                                       ├── Eğitimci      → Pınar Öğretmen
                                       ├── Seans Hedefi  → BEP hedefi
                                       ├── Yoklama       → Geldi / Gelmedi
                                       └── Seans Notu    → "Bugün 18 sözcük..."
```

### 8.3 Her katmanın tek cümlelik özeti

- **Kayıt** — Öğrencinin kurumdaki **dosyası**. "Bu çocuk kurumumuzda aktif mi, ne zaman geldi, ne zaman ayrıldı?" sorusunu cevaplar. Bir öğrencinin normalde **tek açık kaydı** olur; aldığı bütün programlar bu kaydın içinde durur.
  - Öğrenciyi programa eklemek için `Programlar → Kayıtlar → Kayıt Oluştur` kullanılır. Öğrencinin açık kaydı yoksa kayıt açılır ve program eklenir; varsa yeni kayıt açılmaz, program mevcut kayda eklenir. Kayıt detayındaki **Program Ata** da aynı işi yapar.
  - Öğrenci kurumdan ayrılınca kayıt **Kaydı Sonlandır** ile kapatılır.
- **Program** — Kurumun sunduğu hizmetlerden biri. Tüm öğrenciler için ortaktır, bir kere tanımlanır.
- **Hizmet** — Programın parçaları. Varsayılan süre ve haftalık sıklık burada tanımlanır.
- **Öğrencinin programı** — Öğrencinin hangi programlarda olduğu. **Bir öğrencinin birden fazla programı olabilir** (ör. hem Terapi hem Hareket ve Spor). Her birinin kendi başlangıç ve bitiş tarihi ve durumu (Aktif, Duraklatıldı, Tamamlandı, İptal) vardır.
- **Tekrar Eden Plan** — "Her Salı 10:00" gibi bir **tekrarlama kuralı**. Tek başına seans değildir; sistem bu kuraldan somut seansları **üretir**. Planda hangi hizmetin verileceği ve hangi eğitimcinin gireceği de tutulur ve üretilen her seansa aktarılır.
- **Seans** — Takvimdeki somut satır: belirli gün, saat, oda ve hizmet. Bireysel veya grup olabilir.
- **Katılımcı** — Seansa giren öğrenci(ler). Grup seansında birden fazla.
- **Seans eğitimcisi** — Seansı yürüten eğitimci(ler). Sorumlu eğitimcinin yanında yardımcı, gözlemci veya süpervizör de eklenebilir.
- **Yoklama** — Öğrenci geldi mi? Her öğrenci ve seans için bir kayıt.
- **Seans Hedefi** — "Bu seansta hangi BEP hedefleri üzerinde çalışıldı?" Seans ile öğrencinin hedefi arasındaki köprü.
- **Seans Notu** — Eğitimcinin notu. **Veliye Görünür** işaretliyse veli portalında görünür.
- **Telafi Talebi** — Kaçırılan seansın telafisi.

### 8.4 Program ≠ BEP (çok karıştırılır)

| Program / Öğrencinin programı | BEP (Bireysel Eğitim Planı) |
|---|---|
| Kurumun verdiği hizmetin **ticari ve operasyonel** tanımı. "Hangi hizmet?" | Öğrencinin **eğitsel** hedef belgesi. "Hangi beceriler?" |
| Finans ve planlama tarafı | Eğitim tarafı |
| Hizmet → Dil ve Konuşma Terapisi (**hizmet türü**) | Öğrenci Hedefi → "15 dk grup ortamında oturabilme" (**beceri**) |
| Onay akışı yok | Onay akışı var: Taslak → İnceleme Bekliyor → Onaylandı → Aktif → Revize Edildi → Kapatıldı |

**Köprü:** Seans sırasında hangi BEP hedefleri üzerinde çalışıldığı işaretlenir ve ilerleme kaydedilir. Yani **program seansı yapılırken BEP hedefi işlenir**; iki dünya burada buluşur.

### 8.5 Finansla bağlantı

- Öğrenciye bir **paket** satılır (ör. "20 seanslık Dil ve Konuşma paketi"). Paket bir programa bağlıdır.
- Seans **tamamlanınca**, yoklamada "Geldi", "Geç geldi" veya "Erken ayrıldı" olan her öğrencinin paketinden 1 seans hakkı otomatik düşer.
  - **Hangi paket:** öğrencinin, **seansın programına bağlı** aktif paketi. Birden fazla uygun paket varsa süresi en önce dolan kullanılır.
  - **Düşmeyen durumlar:** yoklama alınmamış, öğrenci gelmemiş veya mazeretli, seans "Gelmedi" olarak işaretlenmiş ya da öğrencinin o programa bağlı, hakkı kalan paketi yok. Seans detayında her öğrencinin yanında "1 seans hakkı düşüldü" ya da "Seans hakkı düşülmedi" yazar.
  - **Sıra önemli:** önce yoklama, sonra **Tamamla**. Tamamladıktan sonra alınan yoklama hak düşürmez; bu durumda Kredi Defteri'nden elle düşün.
- Özetle: **Program = ne sattığımız, Seans = ne verdiğimiz, Kredi Defteri = sayaç**.

### 8.6 Somut örnek — "Ayşe"

1. **CRM:** Aday kaydı, **Öğrenciye Dönüştür** ile Ayşe'nin öğrenci kaydına dönüştürüldü.
2. **Programa ekleme:** `Programlar → Kayıtlar → Kayıt Oluştur` ile Ayşe "Erken Çocukluk Programı"na eklendi. Ayşe'nin kurumda dosyası olmadığı için kayıt da açıldı (01.09.2026).
3. **Finans:** Ayşe'ye "40 Seanslık Erken Çocukluk Paketi" satıldı (paket bu programa bağlı).
4. **Planlama:** Sekreter haftalık düzeni tanımladı:
   - Her Salı 10:00, 45 dk, Oda 3, Pınar Öğretmen → Dil ve Konuşma Terapisi
   - Her Perşembe 11:00, 60 dk, Oda 1, Ali Öğretmen → Duyu Bütünleme
5. Sistem bu planlardan somut **seanslar** üretti.
   > Not: `Tekrar Eden Planlar` formunda öğrenci, program, hizmet, eğitimci ve oda seçilir. Plandan üretilen her seans bu bilgileri taşır. Hizmet, öğrencinin aldığı programın hizmetlerinden biri olmak zorundadır.
6. **BEP:** Koordinatör Ayşe için BEP açtı, hedefleri ekledi ve onayladı.
7. **Seans günü (07.11.2026 Salı 10:00):** Eğitimci yoklama alır, hangi hedeflere çalıştığını işaretler, not yazar, hedef ilerlemesini girer ve **Tamamla**'ya basar.
8. **Arka planda:** Seans tamamlanınca Ayşe'nin paketinden 1 seans hakkı düşer (seans detayında "1 seans hakkı düşüldü").
9. **Veli portalı:** Not **Veliye Görünür** işaretliyse veli görür. BEP onaylı ve veliye açıksa hedef ilerlemesini de görür.
10. **Sonra:** Ayşe "Hareket ve Spor" programına da başlarsa yine `Kayıt Oluştur` kullanılır; yeni kayıt açılmaz, program Ayşe'nin mevcut kaydına eklenir.

### 8.7 Aynı öğrenci birden fazla programda olabilir mi?

**Evet.** Bir öğrenci aynı anda:
- Erken Çocukluk Programı (aktif)
- Aile Danışmanlığı Programı (aktif)
- Yaz Kampı Programı (tamamlanmış)

şeklinde birden çok programda olabilir. Hepsi öğrencinin tek kaydının içindedir. Her birinin kendi seans düzeni, kendi paketi, kendi geçmişi vardır. **BEP tektir** (öğrenci bazlıdır) ve tüm programlardaki hedefleri kapsar.

---

## 9. Nereden Devam Etmeli?

- **Kurulum planı:** `docs/musteri/KURULUM_YOL_HARITASI.md`
- **Tüm dokümanların listesi:** `docs/README.md`
- **Modül bazlı ayrıntı:** sol menüdeki her ana modülün ilk ekranı o modülün panelidir; genel bakış oradan başlar.

---

## 10. Teknik Ek

Bu bölüm kurulum yapan, destek veren veya geliştirici ekip içindir. Son kullanıcının bilmesi gerekmez.

### 10.1 Ekrandaki adlar ve sistemdeki karşılıkları

| Ekranda | Sistemde (tablo / alan) |
|---|---|
| Kurum | `core.corporation` (her iş kaydında `corporation_id`) |
| Kampüs | `core.campus` |
| Program | `education.program` (türü: `program_type` referans verisi) |
| Hizmet | `education.program_service` (`default_duration_minutes`, `default_sessions_per_week`) |
| Kayıt | `education.enrollment` (açık kayıt: `ended_on` boş; durum: `enrollment_status`) |
| Öğrencinin programı | `education.student_program` (`active / paused / completed / cancelled`) |
| Tekrar Eden Plan | `scheduling.recurring_schedule` (`program_service_id`, eğitimciler V35 ile eklendi) |
| Seans | `scheduling.session` (`status`: `scheduled / completed / cancelled / no_show / rescheduled`; hizmet: `program_service_id`) |
| Katılımcı | `scheduling.session_participant` (`student_program_id`) |
| Seans eğitimcisi | `scheduling.session_educator` (`role`: `lead / assistant / observer / supervisor`) |
| Yoklama | `scheduling.attendance` (`present / absent / late / excused / left_early`) |
| Seans Notu, Veliye Görünür | `scheduling.session_note.parent_visible` |
| Seans Hedefi | `scheduling.session_goal` |
| BEP | `education.education_plan` (`draft → in_review → approved → active → revised → closed`) |
| Paket Tanımı | `finance.package_definition` (`program_id`) |
| Öğrenci Paketi | `finance.student_package` (`active / exhausted / expired / cancelled`) |
| Kredi Defteri | `finance.credit_ledger` (yalnızca ekleme yapılır; bakiye = `delta` toplamı) |

### 10.2 Yetki modeli
- İzin tabanlı rol yönetimi (RBAC): ekranlar ve işlemler `student:create` gibi izin kodlarına bağlıdır, rol adına bakılmaz.
- Yetkisiz istek API'de **403** döner; arayüz bunu "yetkiniz yok" sayfası olarak gösterir.
- Menü öğeleri veritabanından gelir (`iam.menu_item`, `required_permission`). Sayfası olmayan bir adrese giden menü öğesi arayüzde gizlenir.

### 10.3 Kurum ayrımı
- Tüm kurumlar aynı veritabanını paylaşır; kurum ayrımı satır düzeyi güvenlikle (RLS) yapılır. Kurum bilgisi olmayan sorgu hiç satır döndürmez.
- Kullanıcı adı **tüm sistemde** benzersizdir (`uq_user_account_username`, V36), çünkü giriş yalnızca kullanıcı adıyla yapılır.

### 10.4 Silme ve işlem geçmişi
- "Silinen" kayıtlar fiziksel olarak silinmez; `deleted_at` doldurulur ve listelerden gizlenir.
- Finansal kayıtlar, imzalı sözleşmeler ve onamlar hiç silinmez; düzeltme ters kayıt veya yeni versiyonla yapılır.
- Önemli değişiklikler veritabanı tetikleyicileriyle `core.audit_log` tablosuna yazılır.

### 10.5 Seans hakkı düşme kuralı (teknik)
- Seans `completed` olduğunda, yoklaması `present / late / left_early` olan her katılımcı için 1 kredi düşülür (`entry_type = consume`, `session_id` dolu).
- Program, katılımcının `student_program_id`'sinden, yoksa seansın `program_service_id`'sinden bulunur. Paket: `package_definition.program_id` bu programa eşit, `active`, süresi dolmamış, bakiyesi ≥ 1; birden fazlaysa `expires_on` en yakın olan.
- Aynı seans için aynı öğrenciye ikinci kez düşülmez.
- Kod: `src/Aynesil.Application/Features/Scheduling/SessionCreditConsumer.cs`.

### 10.6 Daha fazlası
- Mimari: `ARCHITECTURE.md` ve `docs/01…08`
- Veritabanı şeması: `db/`
