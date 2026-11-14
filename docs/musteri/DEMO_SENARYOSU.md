# AyNesil — Müşteri Demo Senaryosu

Bu doküman müşteri sunumunu **adım adım** anlatır: hangi menüye girilecek, neye tıklanacak, müşteriye ne söylenecek.

Menü ve buton adları ekranda göründüğü gibi yazılmıştır (ör. **Seans Planlama → Tekrarlayan Programlar**). Menü adı ile sayfa başlığı farklıysa parantez içinde sayfa başlığı verilmiştir.

> **Önemli:** Tablolardaki bütün adımlar müşterinin önünde canlı yapılabilir. Canlı adımlar hazır demo verisine dayanır; **bölüm 1'deki hazırlığı demodan bir gün önce** yapın.

---

## 0. Demodan önce: Sistemin tamamını anlatan tek resim

Ekranlara geçmeden önce bu resmi gösterin (tahtaya çizin ya da tek slayt yapın):

```
Aday  →  Öğrenci  →  Program (ne alıyor?)  →  Haftalık Plan  →  Seans
                                                                  ├─ Gelişim (BEP hedefleri)
                                                                  └─ Para (paketten seans hakkı düşer)
```

**Söylenecek:**
> "Sistemin tamamı bu beş kutu. Gördüğünüz her ekran bu kutulardan birinin ayrıntısı. Bugün size üç hikâye üzerinden bu kutuları göstereceğim."

---

## 1. Demo hazırlığı (demodan 1 gün önce)

Demo verisi elle girilmez; hazır bir betik iki örnek kurum kurar: **Demo Gökkuşağı Merkezi** (`demo_a`) ve **Demo Papatya Merkezi** (`demo_b`). Her birinde 15 öğrenci, 6 eğitmen, 20 aday, yaklaşık 225 seans (son 8 hafta + önümüzdeki 2 hafta), BEP'ler, paketler, faturalar, sözleşmeler, rızalar, telafi talepleri ve portal hesabı olan veliler vardır. Ek modüllerin hepsi açıktır.

```bash
scripts/demo-seed/run.sh            # kur / eksikleri tamamla
scripts/demo-seed/run.sh --reset    # demo kurumlarını silip baştan kur (seans tarihleri bugüne göre yeniden üretilir)
```

**Giriş hesapları ve ortak şifre:** `scripts/demo-seed/README.md` → "Giriş bilgileri". Demoda kullanacağınız hesaplar: kurum yöneticisi (`admin@demo-a.test`), eğitmen (`egitmen@demo-a.test`), veli (`veli1@demo-a.test`: iki çocuklu, kamera yetkili).

> Demo sunucusu canlı sunucu değildir. Demo betiğini müşterinin canlı sunucusunda çalıştırmayın; şifre herkesçe bilinir.

**Hazırlık listesi (betikten sonra):**

- [ ] **Betik çalıştı:** sonunda hata özeti yok. Demodan önceki gün `--reset` ile çalıştırırsanız seanslar demo gününe göre yeniden dağılır.
- [ ] **Bugüne bir seans:** Betik bugüne seans koymayabilir. **Seans Planlama → Seanslar → Seans Oluştur** ile bugün için bir seans açın: kampüs, oda, öğrenci, sorumlu eğitimci olarak **Merve Çalışkan** (`egitmen@demo-a.test`'in bağlı olduğu eğitmen). Öğrenci olarak **paketi olan** bir öğrenci seçin ve seansın programı paketin programıyla aynı olsun (**Finans → Öğrenci Paketleri**'nden kontrol edin). Yoksa tamamlanınca hak düşmez.
- [ ] **Yöneticiyi bir eğitmen kaydına bağlayın:** Demo yöneticisi hiçbir eğitmen kaydına bağlı değildir; bu haliyle **seans notu yazamaz ve BEP onaylayamaz**. **Eğitimciler → Eğitmen Listesi** → Merve dışındaki bir eğitmen → **Genel Bakış → Kullanıcı hesabı → Mevcut kullanıcıyı bağla** → `admin@demo-a.test`. (İsterseniz bunu demoda canlı yapın; bkz. Hikâye 3, adım 7.)
- [ ] **Yeni aday:** **CRM → Adaylar**'da "Yeni" aşamasında bir aday olduğunu görün (canlı dönüşüm için). Yoksa **Aday Ekle** ile bir tane açın.
- [ ] **Bekleyen telafi:** **Seans Planlama → Telafi Seansları**'nda "Bekliyor" durumunda bir talep olduğunu görün.
- [ ] **İncelemede bir BEP:** **BEP / IEP → Plan Listesi**'nde "İnceleme Bekliyor" durumunda bir plan olduğunu görün.
- [ ] **Taslak gelişim raporu:** veli1'in çocuklarından birinin kartında (**Gelişim Raporları**) taslak bir rapor olsun; yoksa **Rapor Ekle** ile açın. Canlıda kesinleştirip velide göstereceksiniz.
- [ ] **Tarayıcı:** iki pencere hazırlayın. Birinde yönetici, diğerinde (gizli pencerede) `veli1@demo-a.test` oturumu açık olsun.

---

## 2. Hikâye 1 — "Yeni bir çocuk geldi" (≈10 dk)

**Sorunu tanımlayın:**
> "Şu an bir veli aradığında bilgileri bir deftere ya da Excel'e yazıyorsunuz. Kimin geri aranacağı, kimin kayıt olduğu kaybolabiliyor."

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Aday ekle | CRM → Adaylar → **Aday Ekle**. İletişim kişisi, çocuk adı, doğum tarihi, telefon, kaynak ve başvuru nedenini doldurun. **Kaydet**. | "Veli telefonu açtığı anda kaydı burada açıyoruz. Nereden geldiğini de seçiyoruz; böylece hangi reklamın işe yaradığını görebiliyorsunuz." |
| 2 | Görüşme kaydı | Aday detayı → **Aktiviteler** sekmesi → **Aktivite Kaydet**. Bir takip tarihi girin. | "Kim aradı, ne konuşuldu, ne zaman tekrar aranacak; hepsi burada." |
| 3 | Satış hunisi | CRM → **Pipeline**. | "Tüm adayları aşama aşama görüyorsunuz. Hangi aşamada kaç kişi var, bir bakışta görünüyor." |
| 4 | Öğrenciye dönüşüm | 1. adımda eklediğiniz aday → **Öğrenciye Dönüştür**. **Yeni öğrenci oluştur** seçili; bilgilerin adaydan geldiğini gösterin, "İletişim kişisini veli olarak ekle" işaretli kalsın → **Öğrenciyi Oluştur ve Dönüştür**. | "Veli kayıt olmaya karar verince tek tıkla öğrenci ve veli kaydı açılıyor; telefonda aldığımız bilgileri tekrar yazmıyoruz. Adayın geçmişi de kaybolmuyor." |
| 5 | Öğrenci kartı | Öğrenciler → öğrenciyi açın. **Genel Bakış**, **Veliler**, **Programlar**, **Rızalar** sekmelerini gezin. | "Çocuğun bütün dosyası tek yerde: veliler, tanılar, raporlar, imzalı izinler." |
| 6 | Programa ekleme | Programlar → **Kayıtlar** → **Kayıt Oluştur**. Öğrenciyi ve programı listeden seçin. | "Kayıt, çocuğun kurumdaki dosyası; aldığı programlar bu dosyanın içinde duruyor. Çocuk ikinci bir programa başlarsa aynı yerden ekliyoruz, sistem yeni dosya açmıyor." |
| 7 | Rıza | Öğrenci kartı → **Rızalar** → **Rıza ver** → tür "Kamera İzleme", veli, geçerlilik. | "KVKK, kamera, fotoğraf izinlerini çocuk ve veli bazında tutuyoruz. Geri alınırsa o veliye verilmiş kamera izni de kendiliğinden kapanıyor." |

**Sonucu söyleyin:**
> "Telefondan kayda kadar her şey tek bir yerde ve kimse bir şeyi unutmuyor."

**İsteğe bağlı:** Değerlendirme → **Oturumlar**'da hazır bir oturumu açıp **Puanlama** ve **Rapor** sekmelerini gösterin.

---

## 3. Hikâye 2 — "Bir haftanın işleyişi" (≈10 dk)

**Sorunu tanımlayın:**
> "Hangi çocuğun hangi gün hangi hocayla olduğu, kim gelmedi, kimin telafisi var; bunlar genelde WhatsApp'ta ve kâğıtta."

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Günün tablosu | **Seans Planlama** (ana sayfa: Planlama Paneli). "Bugünkü Seanslar" listesini gösterin. | "Sabah girdiğinizde bugün kimin kiminle hangi odada olduğunu görüyorsunuz." |
| 2 | Takvim | Seans Planlama → **Takvim**. **Okul** görünümünde **Hafta**'yı seçin. | "Tüm kurumun haftası. Aynı odaya iki seans konamaz, aynı eğitmen iki yerde olamaz; sistem engelliyor." |
| 3 | Seansa gir | Bugünkü seansı açın. **Genel Bakış**, **Katılımcılar** sekmeleri. | "Eğitimci seansa girdiğinde öğrencisini ve bilgilerini burada görüyor." |
| 4 | Yoklama | **Devam** sekmesi → **Katıldı** / **Gelmedi** / **Geç Geldi**. | "Yoklama tek tıkla. Gelmeyen çocuk için telafi süreci buradan başlıyor." |
| 5 | Hedef işaretleme | **Hedefler** sekmesi → üstteki kutudan öğrencinin hedefini seçip **Hedef Ekle**, ardından çalışılan hedefin kutusunu işaretleyin. Yanlış eklenen hedefi **✕** ile çıkarabilirsiniz. | "Bu seansta hangi hedefler üzerinde çalışıldığını işaretliyor. Gelişim raporu buradan oluşuyor." |
| 6 | Seans notu | **Notlar** sekmesi → **Not Ekle** → **Veliye Görünür** işaretli. Kaydettikten sonra kalem simgesiyle bir kelimeyi değiştirin; **Düzenlendi** rozeti çıkar. | "Hocanın notu. İsterse veliye de gösteriyor; veli akşam telefondan okuyor. Not sonradan düzeltilirse velide de 'düzenlendi' yazıyor, kimse sessizce değiştiremiyor." |
| 7 | Seansı tamamla | Önce yoklamada "Katıldı" seçili olsun, sonra **Tamamla**. Öğrencinin yanında "1 seans hakkı düşüldü" çıkar. | "Seans tamamlanınca velinin paketinden bir seans hakkı otomatik düşüyor." |
| 8 | Telafi | Seans Planlama → **Telafi Seansları** (sayfa: Telafi Talepleri) → bekleyen talep → **Onayla**, sonra **Seans ata** ile öğrencinin ileri tarihli seanslarından birini seçin. | "Kaçırılan seansın telafisi kaybolmuyor: onaylandı, hangi seansta yapılacak, yapıldı mı; hepsi kayıtlı." |

Not: 6. adımı yönetici hesabıyla yapıyorsanız yöneticinin bir eğitmen kaydına bağlı olması gerekir (Hazırlık listesi). Bağlı değilse sistem uyarır. Bu uyarı da gösterilebilir: "Notu kimin yazdığını sistem giriş yapan kişiden alıyor, formda seçtirmiyor."

**Sonucu söyleyin:**
> "Hafta planı, yoklama, not, telafi; hepsi tek ekranda ve geriye dönük aranabilir."

**İsteğe bağlı gösterilebilir:**
- **Tekrarlayan Programlar** (sayfa: Tekrar Eden Planlar): form öğrenci, program, hizmet, sorumlu eğitimci, kampüs ve oda seçtiriyor. "Seansları Oluştur" denince seanslar üretiliyor; eğitimcinin ya da odanın dolu olduğu tarihler atlanıp sayısı gösteriliyor. Liste ekranında her plan "Ayşe · Dil ve Konuşma · Pınar Öğretmen · Sal, Per 10:00 · Oda 3" şeklinde okunur.
- **Grup seansı:** demo verisinde "Demo grup seansı" var. Grup seansında veliye görünür not yazarken notun hangi çocuk için olduğu **seçilmek zorunda**. "Her veli yalnızca kendi çocuğu için yazılan notu görür."
- **Takvim'in Eğitimci/Öğrenci görünümleri** ve **Ay** görünümü.

---

## 4. Hikâye 3 — "Kim ne görüyor?" (≈10 dk)

**Sorunu tanımlayın:**
> "Veliler sürekli 'çocuğum nasıl gidiyor, kaç seansım kaldı' diye arıyor. Yönetici de genel durumu görmek için herkesten ayrı rapor istiyor."

### 4a. Veli gözünden (gizli penceredeki veli oturumu)

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Veli anasayfası | **Veli Portalı → Özet**. Yaklaşan seans, paket bakiyesi ve aktif hedef kartlarını gösterin. | "Veli giriş yaptığında ilk bunu görüyor: kaç seansı kaldı, sıradaki seans ne zaman." |
| 2 | Çocuğun detayı | **Çocuklarım** → çocuğa tıklayın. **Seanslar** sekmesinde Hikâye 2'de yazdığınız notu gösterin ("düzenlendi" işaretiyle). **Hedefler**, **Paketler**, **Belgeler**. | "Hocanın 'veliye görünür' dediği notlar seansın altında. Hocanın kendine yazdığı iç notlar burada yok." |
| 3 | Gizlilik | **Söyleyin.** | "Veli yalnızca kendi çocuğunu görüyor. Neyi görebileceğine de kurum karar veriyor: seanslar, devam, raporlar, BEP, finans, kamera her çocuk için ayrı ayrı açılıp kapatılabiliyor." |

### 4b. Yönetici gözünden

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 4 | BEP onayı | **BEP / IEP → Plan Listesi** → "İnceleme Bekliyor" plan → **Onayla** → **Aktifleştir** → **Veli Görüntülüyor**'u açın. **Onaylar** sekmesinde onaylayanın adını gösterin. | "BEP taslaktan onaya kadar takip ediliyor. Onaylayanı sistem giriş yapan kişiden alıyor; kimse başkası adına onay veremiyor. Her değişiklik yeni sürüm oluyor." |
| 5 | Gelişim raporu | Veli1'in çocuğunun kartı → **Gelişim Raporları** → taslak raporda **Kesinleştir**. Sonra veli penceresini yenileyip **Belgeler** sekmesinde raporu gösterin. | "Rapor kesinleşmeden veli görmüyor. Kesinleşince artık değiştirilemiyor ve velinin ekranına düşüyor." |
| 6 | Finans özeti | **Finans** (ana sayfa: Finans Paneli). İsterseniz **Faturalar**'da bir faturayı **İptal Et**'e basıp gerekçe istendiğini gösterin (vazgeçin). | "Gelir, satılan paketler, vadesi geçen faturalar tek ekranda. Fatura silinmiyor; iptal ederken gerekçe zorunlu." |
| 7 | Hesap bağlama (isteğe bağlı) | **Eğitimciler → Eğitmen Listesi** → bir eğitmen → **Genel Bakış → Kullanıcı hesabı → Yeni kullanıcı oluştur**. Geçici şifrenin bir kez gösterildiğini gösterin. | "Personel hesabını yönetici açıyor. Geçici şifre bir kez görünüyor; kişi ilk girişte kendi şifresini belirlemek zorunda." |
| 8 | CRM raporu | CRM → **CRM Raporları** → tarih aralığı seçin. | "Kaç aday geldi, kaçı kayıt oldu, hangi kaynaktan geldi." |

**Sonucu söyleyin:**
> "Veli aramadan bilgiye ulaşıyor, siz de herkese ayrı ayrı sormadan kurumun durumunu görüyorsunuz."

---

## 5. Kapanış (≈5 dk)

**Söylenecek:**
> "Bugün gördüğünüz, sistemin altyapısı ve ana akışları. Ekranları sizin günlük işinize göre sadeleştirmek istiyoruz. Bunun için size üç soru soracağım."

Müşteriye sorun ve cevapları not alın:

1. **Sistemi en çok kim kullanacak?** Sekreter mi, eğitimci mi, yönetici mi?
2. **Sabah sisteme girdiğinizde ilk ne görmek istersiniz?**
3. **Seans odasında tablet ya da telefon kullanılacak mı?**

Ayrıca müşterinin kullandığı kelimeleri yazın. "Kayıt", "seans hakkı", "haftalık program" gibi ifadeleri nasıl söylüyorlarsa not edin. Kurulum için müşteriden istenecek bilgiler tablosu: `docs/musteri/KURULUM_KILAVUZU.md` B.12.

---

## 6. Demoda yapılmaması gerekenler

- **Menüyü baştan sona gezmeyin.** Kamp, kamera, danışmanlık, KPI: "Bunlar ek modüller, kurumunuz için ayrıca açılıyor" deyip geçin. Demo kurumunda hepsi açıktır, ama yeni bir kurumda kapalı gelirler.
- **Yarım modülleri vaat etmeyin.** Kamerada **canlı izleme ekranı yok**; kamp ve okul danışmanlığında günlük işlemlerin bir kısmı henüz ekrandan yapılamıyor; anketlerin ekranı yok. Sorulursa "yol haritasında" deyin (ayrıntı: `docs/ekransiz-endpointler.md`).
- **"Şifremi unuttum"u göstermeyin.** E-posta sağlayıcısı bağlı olmadığı için bağlantı gitmez.
- **KPI panosunu açacaksanız** önce **Performans Anlık Görüntüleri → Dönem Hesapla** deyin; değerler kendiliğinden hesaplanmaz.
- **Teknik terim kullanmayın:** enrollment, student program, recurring schedule, GUID, tenant, RBAC demeyin.

---

## 7. Sorulursa gösterilebilecek ekranlar

| Konu | Nerede |
|---|---|
| Sözleşme şablonu ve yeni sürüm | Sözleşme ve Rıza Yönetimi → **Sözleşme Şablonları** |
| Onam (rıza) şablonları | Sözleşme ve Rıza Yönetimi → **Rıza Şablonları** |
| Taslak sözleşmeyi düzenleme/silme | **Öğrenci Sözleşmeleri** → taslak sözleşme → **Düzenle** / **Sil** (yalnızca taslakta) |
| İade işleme | Finans → **İadeler** → **İşleme al** / **Başarısız işaretle** |
| İzin bakiyesi tanımlama | İzin Yönetimi → **İzin Bakiyeleri** → **Bakiye Tanımla** |
| İzin onayı (onaylayan giriş yapan kişi) | İzin Yönetimi → **İzin Talepleri** → talep → **Onayla** |
| Kuruma özel KPI tanımı | KPI & Performans → **KPI Tanımları** → **Yeni KPI tanımı** |
| Akademik dönemler, güncel dönem | BEP / IEP → **Akademik Dönemler** |
| İlk girişte şifre değiştirme | `scripts/demo-seed/README.md`'deki "ilk giriş örneği" hesabıyla giriş yapın |
| Modül durumu | Platform Yönetimi → Kurumlar → kurum → **Ayarlar** → Modüller (salt okunur) |
