# AyNesil — Kullanım Kılavuzu

Bu doküman **son kullanıcı** (kurum yöneticisi, koordinatör, eğitimci, muhasebe, sekreter, veli) içindir. Teknik ayrıntılar en sondaki **Teknik Ek** bölümündedir. Kurumun ilk kurulumu için: `docs/musteri/KURULUM_KILAVUZU.md` (Bölüm B).

Menü adları ekrandaki gibi yazılmıştır (ör. **Seans Planlama → Seanslar**). Menü adı ile sayfa başlığı farklıysa parantez içinde sayfa başlığı da verilmiştir.

---

## 1. AyNesil Nedir?

AyNesil, özel eğitim kurumları için geliştirilmiş bir yönetim platformudur. Tek bir kurumdan birden fazla kampüse, aday öğrenciden faturaya, bireysel eğitim planından veli portalına kadar günlük işlerin tamamını tek yerde yönetmenizi sağlar.

**Temel yapı:**
- **Kurum** → şirketiniz
- **Kampüs** → kurumunuzun şubeleri
- **Kullanıcılar** → her kullanıcının bir veya birkaç rolü vardır; rol, neleri görüp yapabileceğini belirler
- **Modüller** → kurumunuzda açık olan bölümler

> **Not:** Hiçbir veri kurumlar arasında görünmez. Her kullanıcı yalnızca kendi kurumunun verisini görür.

### Ek modüller
Şu bölümler **ek modüldür** ve kurumunuz için ayrıca açılır: **Kamp**, **Okul Danışmanlığı**, **Kamera**, **KPI & Performans** (ayrıca altyapı olarak anketler ve bakım ekibi). Yeni kurumda kapalı gelirler. Açıp kapatmayı yalnızca AyNesil teknik ekibi yapar; kurum yöneticisi `Platform Yönetimi → Kurumlar → kurum → Ayarlar` sayfasındaki **Modüller** listesinde hangisinin açık olduğunu görür ama değiştiremez. Kapalı bir modülün menüsü görünmez, rolünüzde izni olsa bile.

---

## 2. Giriş, Şifre ve Temel Arayüz

### Giriş yapma
1. Tarayıcıdan sisteme gidin (örn. `https://uygulama-adresiniz`).
2. Kullanıcı adınız (genellikle e-posta adresiniz) ve şifrenizle **Giriş Yap**'a basın.
3. Girişten sonra **Ana Sayfa** açılır.

### İlk girişte şifre değiştirme
Hesabınızı yöneticiniz açtıysa size bir **geçici şifre** verilir. Bu şifreyle ilk girişte sistem sizi doğrudan **şifre değiştirme** sayfasına götürür; başka sayfa açılmaz. Yeni şifrenizi belirleyin, sonra yeni şifrenizle yeniden giriş yapın.

### Şifre değiştirme
Sağ üstteki baş harfinizin olduğu yuvarlağa tıklayın → **Şifre Değiştir**. Değişiklikten sonra yeniden giriş yaparsınız.

### Şifremi unuttum
Giriş ekranında **Şifremi unuttum** bağlantısı vardır. **Şu an e-posta gönderilmediği için bu bağlantı size ulaşmaz** (ekranda da yazar). Şifrenizi unuttuysanız kurum yöneticinize başvurun; yönetici AyNesil destek ekibinden yeni geçici şifre ister.

### Arayüzün ana bölümleri
- **Sol menü:** Rolünüze göre oluşur. Yetkiniz olmayan bölümler görünmez.
- **Üst bar:** Bildirimler, profil (şifre değiştirme, çıkış).
- **İçerik alanı:** Seçtiğiniz bölümün ekranı.

### Yetkiler
- Neleri görüp yapabileceğinizi, rolünüze verilen izinler belirler. Rolün adı değil, içindeki izinler önemlidir.
- Yetkiniz olmayan bir ekrana girmeye çalışırsanız "yetkiniz yok" sayfası çıkar. Bu bir hata değil, izin eksikliğidir; yöneticinize başvurun.
- Rolünüz değişirse değişiklik en geç bir saat içinde görünür; hemen görmek için çıkış yapıp yeniden girin.

---

## 3. Rollere Göre Tipik Kullanım

### 3.1 Kurum Yöneticisi
En geniş yetki sahibi. Yönetim ekranları `Platform Yönetimi` menüsündedir.

1. **Kurum ayarları** → `Platform Yönetimi → Kurumlar → kurum → Ayarlar`: dil, para birimi, saat dilimi, vergi dairesi ve numarası. **Modüller** listesi burada salt okunur görünür.
2. **Kampüsler** → `Platform Yönetimi → Kampüsler`: şubelerin adres ve telefonu, yeni şube.
3. **Referans veriler** → `Platform Yönetimi → Referans Veriler`: seans tipleri, aday kaynakları, ödeme yöntemleri gibi listeler.
   - Sistemle gelen değerler **tüm kurumlarda ortaktır**: düzenleyemez ve silemezsiniz, yalnızca **kendi kurumunuz için pasifleştirebilirsiniz** (listelerde görünmez olur).
   - Çoğu listeye **kendi değerinizi ekleyebilirsiniz**; eklediğinizi düzenler ya da pasifleştirirsiniz. Cinsiyet, entegrasyon türü ve bildirim kanalı listeleri sabittir.
4. **Roller** → `Platform Yönetimi → Roller → Rol Ekle`, sonra **İzinler** sekmesinden izinleri seçin. Kurumunuzla birlikte **Yönetici** ve **Veli (portal)** rolleri hazır gelir.
5. **Kullanıcılar** → `Platform Yönetimi → Kullanıcılar → Kullanıcı Ekle`: kullanıcı adı (tüm sistemde benzersiz; e-posta önerilir), ad soyad, şifre, kampüs. Sonra kullanıcı sayfasında **Rol Ata**. Kişi ilk girişte şifresini değiştirir. Ayrılan personel için silmek yerine **Askıya Al**.
6. **Eğitmen ve veli hesapları** → hesap, kişinin kendi kartından açılır ve bağlanır (bkz. 3.14). Bu iş kullanıcı yönetimi izni gerektirdiği için yöneticiye aittir.
7. **Menü Yönetimi** → `Platform Yönetimi → Menü Yönetimi`: sistemle gelen menü öğeleri **salt okunurdur** (düzenlenemez, gizlenemez, silinemez). Yalnızca kurumunuza özel öğe ekleyebilirsiniz. Normalde dokunmanız gerekmez.

**Yöneticinin yapamadıkları (AyNesil teknik ekibinin işi):** yeni kurum açmak, kurumu etkinleştirmek veya pasifleştirmek, ek modülleri açıp kapatmak, şifresini unutan bir kullanıcıya yeni geçici şifre vermek.

### 3.2 CRM / Satış ekibi (Aday takibi)
`CRM` menüsü altında çalışır.

1. `CRM → Adaylar → Aday Ekle` ile gelen başvuruyu kaydedin (iletişim kişisi, çocuk bilgisi, kaynak, başvuru nedeni).
2. `CRM → Pipeline` ekranında adaylar aşama aşama ilerler. Aşama adları referans verilerden gelir.
3. Aday detayındaki `Aktiviteler` ve `Görüşmeler` sekmelerinde her telefon, e-posta ve görüşme kaydedilir. Planlı bir görüşme **Tamamla**, **İptal**, **Ertele** ya da **Gelmedi** ile kapatılır.
4. Aday kayıt olmaya karar verdiğinde aday detayından **Öğrenciye Dönüştür**'e basın:
   - **Yeni öğrenci oluştur:** Öğrencinin adı, soyadı ve doğum tarihi adaydan gelir; kampüs ve başvuru nedeni aktarılır. "İletişim kişisini veli olarak ekle" işaretliyse veli de açılır ve öğrencinin birincil velisi olarak bağlanır. **Öğrenciyi Oluştur ve Dönüştür** ile hepsi tek seferde yapılır.
   - **Var olan öğrenciye bağla:** Öğrenci kaydı daha önce açıldıysa öğrenciyi adıyla arayıp seçin.
5. `CRM → CRM Raporları`: aday sayısı, dönüşüm oranı, kaynağa göre dönüşüm.

### 3.3 Değerlendirme Ekibi
`Değerlendirme` menüsünden:
1. `Şablonlar` → kullanacağınız değerlendirme formlarını oluşturun (bir kere yapılır).
2. `Oturumlar → Oturum Oluştur` → aday veya öğrenci için değerlendirme planlayın.
3. Oturum detayında **Başlat**, `Puanlama` sekmesinden formu doldurup **Tamamla**; `Rapor` sekmesinden raporu hazırlayıp **Raporu Sonuçlandır**; `Öneriler` sekmesinden program önerisi ekleyin. Sonuçlandırılan rapor değiştirilemez.

### 3.4 Eğitimciler / Terapistler
Seans notu yazabilmeniz için kullanıcı hesabınızın **eğitmen kaydınıza bağlı** olması gerekir. Bağlı değilse not yazarken "bir yönetici bu kullanıcıyı eğitmen kaydına bağlamalıdır" uyarısı çıkar; yöneticinize söyleyin.

1. `Seans Planlama → Takvim` → günlük, haftalık veya aylık seanslarınızı görün. Ay görünümünde bir güne tıklayınca o günün ayrıntısı açılır.
2. Seansa tıklayın → öğrenci bilgisi, hedefler, önceki notlar açılır.
3. Seans sonunda sırasıyla:
   - **Devam** sekmesi (yoklama): her öğrenci için Katıldı / Gelmedi / Geç Geldi.
   - **Hedefler** sekmesi: çalışılan hedefleri işaretleyin, ilerleme girin. Hedef listede yoksa **Hedef Ekle** ile öğrencinin aktif hedeflerinden ekleyin; yanlış eklenen hedefi kartındaki **✕** ile seanstan çıkarın (öğrencinin hedefi silinmez).
   - **Notlar** sekmesi → **Not Ekle**. Veli görsün istiyorsanız **Veliye Görünür**'ü işaretleyin (bkz. "Seans notları" aşağıda).
   - **Tamamla** (önce yoklama, sonra Tamamla; bkz. 8.5).
4. Gelmeyen öğrenci için → `Seans Planlama → Telafi Seansları` (sayfa: Telafi Talepleri) → talep oluşturun.
5. `İzin Yönetimi → İzin Talepleri` → kendi izin taleplerinizi girin.

**Seans notları:**
- Notunuzu sonradan **kalem simgesiyle düzenleyebilirsiniz**. Yalnızca notu yazan eğitmen düzenler; başkasının notunu yalnızca yönetici düzeltebilir.
- Metni düzenlenen notta **Düzenlendi** yazar. Yalnızca "Veliye Görünür" işaretini değiştirmek düzenleme sayılmaz.
- **Veliye Görünür** not, velinin portalında çocuğun **Seanslar** sekmesinde o seansın altında görünür; düzenlenmişse velide de "düzenlendi" yazar. İşareti kaldırırsanız not portaldan kalkar.
- **Her veliye görünür not bir öğrenciye bağlanır** ve yalnızca o öğrencinin velilerine gösterilir. Tek öğrencili seansta öğrenci kendiliğinden seçilir. **Grup seansında** "Veliye Görünür"ü işaretleyince **öğrenciyi seçmeniz zorunludur**; seçmezseniz sistem uyarır. Öğrenci seçilmemiş not yalnızca iç nottur, hiçbir veli görmez.

### 3.5 BEP (Bireysel Eğitim Planı) Sorumluları
1. **Akademik dönem** → `BEP / IEP → Akademik Dönemler`: **Dönem Ekle**, **Düzenle**, **Güncel dönem yap**, **Sil**. Tek bir güncel dönem olur. BEP bağlı dönem ve güncel dönem silinemez; önce başka dönemi güncel yapın.
2. `Hedefler → Hedef Kütüphanesi` ve `Hedef Şablonları` → sık kullanılan hedefleri önceden hazırlayın. `Hedefler → Öğrenci Hedefleri` → öğrenciye hedef açın.
3. `BEP / IEP → Yeni Plan` → öğrenci, akademik dönem, başlık ve tarihler. Plan "Taslak" başlar. Plan sayfasında **Hedefler** sekmesinden hedefleri ekleyin.
4. **İncelemeye Gönder** → onaylayacak kişi **Onayla** ya da **Reddet** → onaylı planı **Aktifleştir**.
   - **Onaylayan kişiyi sistem kendisi belirler: o an giriş yapmış olan kişidir.** Formda onaylayan seçilmez.
   - Bu yüzden BEP onaylayacak ya da reddedecek herkesin (koordinatör dahil) bir **eğitmen kaydı olmalı ve kullanıcı hesabı bu kayda bağlı olmalıdır**. Bağlı değilse sistem uyarır; yöneticinize söyleyin.
5. **Veliye göstermek** için plandaki **Veli Görüntülüyor** anahtarını açın. Yalnızca onaylı ya da aktif plan veliye açılabilir.
6. Dönem içinde **İnceleme Ekle**; plan değişecekse **Revize Et**; iş bitince **Kapat**.

### 3.6 Sekreter / Resepsiyon (Planlama)
1. `Seans Planlama` ana sayfası → bugünkü seanslar ve bekleyen telafi talepleri. `Seans Planlama → Takvim` → haftalık tablo.
2. **Öğrenciyi programa ekleme:** `Programlar → Kayıtlar → Kayıt Oluştur` → öğrenciyi ve programı seçin. Öğrencinin kurumda açık bir kaydı yoksa kayıt açılır ve program eklenir. Kaydı zaten varsa yeni kayıt açılmaz; program mevcut kayda eklenir. Ayrıntı: 8.3.
3. `Seans Planlama → Seanslar → Seans Oluştur` → tek seferlik seans. Kampüs, oda, öğrenci ve sorumlu eğitimci formda adıyla seçilir. Grup seansı için ek öğrenci ve eğitimciler seans detayındaki `Katılımcılar` sekmesinden eklenir.
4. `Seans Planlama → Tekrarlayan Programlar` (sayfa: Tekrar Eden Planlar) → **Plan Oluştur** ile haftalık tekrar eden seans düzeni. Öğrenciyi, aldığı programı, hizmeti, sorumlu eğitimciyi, kampüsü, odayı, günleri ve saati seçersiniz; örneğin "Her Pazartesi 10:00, Ayşe, Dil ve Konuşma, Pınar Öğretmen, 2 Nolu Oda". Ardından listede **Seansları Oluştur** ile bir tarih aralığı verirsiniz. Sistem seansları öğrenci, eğitimci ve hizmetle birlikte üretir. Eğitimcinin veya odanın o saatte dolu olduğu tarihleri atlar ve kaç tarihin atlandığını söyler.
5. `Seans Planlama → Odalar` → çakışma kontrolü için oda listesini güncel tutun.
6. **Telafi** → `Seans Planlama → Telafi Seansları` (sayfa: Telafi Talepleri):
   - Bekleyen talebi **Onayla** ya da **Reddet** (gerekçeyle).
   - Onaylanan talepte **Seans ata**: öğrencinin önümüzdeki 90 gündeki planlı seansları listelenir; telafi seansını seçin. Talep "Planlandı" olur. (Telafi seansını önce `Seanslar → Seans Oluştur` ile öğrenciyle birlikte açın.)
   - Telafi yapılınca **Tamamla** ile "Tamamlandı" yapın.
   - Seçilen seansta talebin öğrencisi katılımcı değilse sistem atamayı reddeder.

### 3.7 Finans / Muhasebe
1. `Finans → Paket Kataloğu` (sayfa: Paket Tanımları) → sattığınız seans paketlerini tanımlayın (ör. "20 Seanslık Bireysel Terapi Paketi"). **Paketi mutlaka bir programa bağlayın**; seans hakkı otomatik düşerken bu bağlantıya bakılır.
2. Öğrenciye paket satışı: `Finans → Öğrenci Paketleri → Paket Ata`. Öğrenciyi ve paketi seçersiniz; seans hakkı sayısı, fiyat ve son kullanım tarihi paketten gelir. Kaydedince paketteki haklar öğrenciye yüklenir. Paket atamak ödeme almaz; ödemeyi ayrıca kaydedin.
3. `Finans → Kredi Defteri` → öğrencinin kalan seans hakkı. Seans tamamlanınca otomatik düşer (bkz. 8.5); gerekirse buradan bonus verilir ya da düzeltme yapılır.
4. `Finans → Faturalar → Fatura Oluştur` → fatura taslak açılır; satırları ekleyip **Faturayı Kes**. Kesilen fatura değiştirilemez.
   - **Fatura iptalinde gerekçe yazmak zorunludur.** Ödenmiş fatura doğrudan iptal edilemez; önce iade yapılır.
5. `Finans → Tahsilatlar` (sayfa: Ödemeler) → **Ödeme Ekle** ile tahsilat kaydı; para gelince **Onayla**. **Finansal kayıtlar silinmez**; yanlış kayıt için düzeltme (ters kayıt) yapılır.
6. **İade:** ödemenin sayfasında **İade Et** (tutar ve gerekçe). Talep `Finans → İadeler` (sayfa: İade Talepleri) ekranında "Bekliyor" görünür; yetkili **İşleme al** ya da **Başarısız işaretle** der. İadeler silinmez.
7. `Finans → Burslar` ve `Finans → Kampanyalar` → indirim tanımları.

### 3.8 Sözleşme, Rıza ve Raporlar
Menü: `Sözleşme ve Rıza Yönetimi`.
1. `Sözleşme Şablonları` → **Yeni şablon** (kod, tür, başlık, metin). Metin değişince **Yeni sürüm**; eski sözleşmeler eski sürümde kalır. Şablonlar silinmez.
2. `Rıza Şablonları` → KVKK, kamera izleme, foto-video gibi onam metinleri; aynı şekilde **Düzenle** ve **Yeni sürüm**.
3. `Öğrenci Sözleşmeleri → Yeni Sözleşme` → öğrenci, güncel şablon, imzalayan veli ve tarihler. Sözleşme **Taslak** açılır.
   - **Taslakken** sayfada **Düzenle** ve **Sil** düğmeleri görünür. Gönderilmiş, imzalanmış ya da aktif sözleşme düzenlenemez ve silinemez.
   - Akış: **Gönder** → **İmzala** (imza yöntemi ve imzalayan) → **Etkinleştir**. Süresi dolunca **Süresini bitir**, bozulunca **Feshet**.
4. **Rızalar:** öğrenci kartı → **Rızalar** sekmesi → **Rıza ver** (tür ya da onam şablonu, **veli**, geçerlilik). Verilmiş rıza **Rızayı geri al** ile geri çekilir; yeniden rıza için yeni kayıt açılır. `Rıza Beyanları` tüm öğrencilerin rızalarını listeler.
5. **İmzalanmış sözleşmeler ve verilmiş rızalar değiştirilemez**; geçmiş kaybolmaz.
6. **Gelişim raporu:** öğrenci kartı → **Gelişim Raporları** → rapor ekleyin; hazır olunca **Kesinleştir**. **Veli yalnızca kesinleşmiş raporları görür.** Kesinleşen rapor bir daha düzenlenemez; kesinleştirmeden önce kontrol edin.

### 3.9 Kamp Yönetimi (modül açıksa)
1. `Kamp Yönetimi → Kamp Tanımları` → tarih, kapasite, konu.
2. Kamp dönemine öğrenci kaydı.
Not: Kampın dönem, etkinlik, yoklama ve rapor işlemlerinin bir kısmı henüz ekrandan yapılamıyor.

### 3.10 Okul Danışmanlığı (modül açıksa)
Kurumunuzun dışarıdaki okullara verdiği danışmanlık hizmetini yönetir.
1. `Danışmanlık → Kurumlar` → danışmanlık verdiğiniz okul.
2. `Danışmanlık Planları`, `Okul Ziyaretleri`, `Gözlemler`, `Danışmanlık Raporları`, `Takip Aktiviteleri`, `Danışmanlık Sözleşmeleri`.
Not: Ziyaret tamamlama, gözlem ayrıntısı gibi bazı işlemler henüz ekrandan yapılamıyor.

### 3.11 Kamera Yönetimi (modül açıksa)
1. `Kamera Yönetimi → Kameralar` → kamerayı tanımlayın, **Oda Ata** ile dersliğe bağlayın.
2. `İzleme Yetkileri` → hangi velinin hangi öğrenciyi hangi tarihler arasında izleyebileceği. Bitiş tarihi zorunludur.
   - **Yetki vermeden önce öğrencinin, o veli adına verilmiş ve süresi geçmemiş "Kamera İzleme" rızası olmalıdır**; yoksa yetki verilemez. Velinin portal erişiminde **Kamera** kutusu da açık olmalıdır.
   - Veli rızasını geri alırsa o velinin açık izleme yetkileri kendiliğinden iptal edilir.
3. `İzleme Kayıtları` → her izleme kayıt altına alınır.
Not: Canlı izleme ekranı henüz yoktur.

### 3.12 KPI / Performans (modül açıksa)
1. `KPI & Performans → KPI Tanımları` → takip edilen ölçütler. Sistemle gelen ölçütler değiştirilemez; **Yeni KPI tanımı** ile kurumunuza özel ölçüt ekleyip **Düzenle** ile değiştirebilirsiniz.
2. `Performans Anlık Görüntüleri → Dönem Hesapla` → içinde bulunulan ayın değerlerini hesaplar. **Değerler kendiliğinden hesaplanmaz**; panoda sayı yoksa önce bunu yapın. Aynı ay tekrar hesaplanırsa eski sonucun üzerine yazılır.
3. `KPI & Performans` → Yönetici, Üst Yönetim ve Sıralama sekmeleri; dönem seçiciyle önceki aylar.

### 3.13 İzin Yönetimi
1. `İzin Yönetimi → İzin Talepleri → Yeni` → izin alacak eğitmen, izin türü, tarihler, neden.
2. Yönetici ya da koordinatör talep sayfasında etkilenen seansları görür, **Onayla** ya da **Reddet** (red için neden yazılır). **Onaylayan kişi o an giriş yapmış olan kişidir**; formda seçilmez.
3. `İzin Bakiyeleri` → **Bakiye Tanımla** ile eğitmen + izin türü + yıl için yıllık hak girilir; **Hak Güncelle** ile değiştirilir; yıl sonunda **Devir İşlemi**. Bakiye tanımlanmamış eğitmende bakiye kontrolü yapılmaz.
4. `İzin Takvimi` ve `İzin Raporları`.

### 3.14 Eğitmen ve veli hesaplarını bağlama (yönetici)
Eğitmenin ve velinin sisteme kendi adıyla girebilmesi için kartlarına bir **kullanıcı hesabı bağlanır**. Bu iş kullanıcı yönetimi izni gerektirir; kurum yöneticisi yapar.

1. Eğitmen ya da veli kartını açın → **Genel Bakış** → **Kullanıcı hesabı** bölümü.
2. Seçenekler:
   - **Yeni kullanıcı oluştur:** kullanıcı adı (e-posta önerilir), ad soyad ve **rol**. Veli için **Veli (portal)** rolü önceden seçilidir. Hesap açılır ve karta bağlanır.
   - **Mevcut kullanıcıyı bağla:** kişinin hesabı zaten açıldıysa onu seçin.
   - **Bağlantıyı kaldır:** yalnızca bağ kalkar; hesap silinmez.
3. Yeni hesapta **geçici şifre bir kez gösterilir** (**Kopyala** düğmesi var). Pencereyi kapatınca bir daha görünmez. Şifreyi kişiye güvenli bir yoldan iletin: yüz yüze, telefonla ya da SMS ile; kullanıcı adıyla aynı mesajda göndermeyin. Kişi ilk girişte şifresini değiştirmek zorundadır.

Kurallar:
- Bir hesap en fazla bir eğitmen ve en fazla bir veli kaydına bağlanır.
- Hesabı bağlanmamış eğitmen **seans notu yazamaz** ve **BEP onaylayamaz**.
- Veli için ikinci adım: veli kartı → **Portal Erişimi** → çocuk için **Erişim Aç** (bkz. 3.15). "Erişim Aç" tek başına hesap açmaz; hesap yoksa sarı uyarı çıkar.
- Veli hesabına verilen rol portal iznini içermiyorsa sistem hesabı açmaz.

### 3.15 Veli Portalı
Veliler ayrı bir arayüz görür: `Veli Portalı` menüsü (Özet, Çocuklarım, Bildirimler).
1. `Özet` → yaklaşan seans, okunmamış bildirimler, paket bakiyesi, aktif hedef sayısı.
2. `Çocuklarım` → çocuğa tıklayın → **Seanslar, Hedefler, Paketler, Belgeler, Toplantılar** sekmeleri.
3. `Bildirimler` → veliye iletilenler.
4. Veli yalnızca kendi çocuklarının verisini ve yalnızca kurumun açtığı bölümleri görür. Kurum her çocuk için ayrı ayrı şu kutuları açar: **Oturumlar** (seanslar ve veliye görünür notlar), **Devam**, **Raporlar** (yalnızca kesinleşmiş gelişim raporları), **BEP/IEP** (yalnızca onaylı ve veliye açılmış plan), **Finans** (paketler), **Kamera**.
5. Grup seansında veli yalnızca kendi çocuğuna bağlanmış notları görür; diğer çocuklar için yazılanlar ve iç notlar görünmez.

---

## 4. Ortak Kavramlar

### Referans Veriler
"Seans tipi", "aday durumu", "ödeme yöntemi" gibi listeler sabit değildir; `Platform Yönetimi → Referans Veriler` ekranından yönetilir.

> ⚠️ Sistemle gelen değerler tüm kurumlarda ortaktır: düzenlenemez, yalnızca kurumunuz için **pasifleştirilir**. Kendi eklediğiniz değerler de kullanıldıysa **silinmez**, pasifleştirilir. Böylece geçmiş veri bozulmaz.

### Silinen kayıtlar
Çoğu kayıt "sil" dediğinizde kalıcı olarak silinmez, yalnızca listelerden kalkar. Ödeme, fatura, kredi hareketi, iade, imzalı sözleşme ve verilmiş rıza ise hiç silinmez.

### Dil
Arayüz şu an Türkçedir. Altyapı başka dilleri destekleyecek şekilde kurulmuştur, ancak İngilizce arayüz çevirisi henüz eklenmemiştir.

### Bildirimler
- `Bildirimler → Gelen Kutusu` → size gelen bildirimler.
- `Bildirimler → Şablonlar` → yöneticinin bildirim şablonları tanımladığı yer.
- `Bildirimler → Bildirim Tercihleri` → kendi bildirim kanallarınız.
- **Şu an e-posta ve SMS gönderilmiyor**, sistem olaylarından (ör. "seans yarın") otomatik bildirim de üretilmiyor. Şablonlar ileride kullanılmak üzere hazırlanabilir.

### İşlem geçmişi
Önemli işlemler (öğrenci oluşturma, sözleşme imzalama, ödeme, izin onayı, not düzenleme vb.) otomatik olarak kayıt altına alınır. Kimin ne zaman ne yaptığı geriye dönük sorgulanabilir.

---

## 5. Tipik İş Akışı — "Aday → Aktif Öğrenci" Örneği

1. **Sekreter:** `CRM → Adaylar → Aday Ekle` ile aday kaydı açar.
2. **CRM ekibi:** Aktivite kaydeder, görüşme yapar, pipeline'da ilerletir.
3. **Değerlendirme ekibi:** `Değerlendirme → Oturumlar → Oturum Oluştur` ile değerlendirme planlar, formu doldurur.
4. **CRM ekibi:** Aday detayında **Öğrenciye Dönüştür** ile öğrenci ve veli kaydını aday bilgilerinden açar.
5. **Sekreter:** `Programlar → Kayıtlar → Kayıt Oluştur` ile öğrenciyi aldığı programa ekler.
6. **Koordinatör:** Sözleşmeyi hazırlatıp imzalatır, rızaları alır (öğrenci kartı → Rızalar).
7. **Muhasebe:** Paket satışı ve ilk tahsilat.
8. **Planlamacı:** Haftalık seansları planlar (Tekrarlayan Programlar veya tek tek Seans Oluştur).
9. **BEP sorumlusu:** BEP oluşturur, hedefler ekler; onaylayan kişi kendi hesabıyla onaylar.
10. **Yönetici:** Veliye portal hesabı açar ve çocuk için portal erişimini verir.
11. **Eğitimci:** Seansı yapar, yoklama alır, hedef ilerlemesini girer, veliye görünür not yazar, seansı tamamlar.
12. **Veli:** Portaldan seansları, notları, onaylı BEP'i ve kesinleşmiş raporları izler.

---

## 6. Sıkça Karşılaşılan Durumlar

| Durum | Yapılacak |
|---|---|
| Menü görünmüyor | Rolünüzde ilgili izin olmayabilir; yöneticinize sorun. Kamp, Danışmanlık, Kamera, KPI menüleri ayrıca modül kapalıysa görünmez. |
| "Eğitimciler" menüsü görünmüyor | Bu menü bugün kullanıcı listesini görme iznini de istiyor; yöneticiniz rolünüze ekleyebilir. |
| "Yetkiniz yok" sayfası çıkıyor | İzin eksik. |
| İlk girişte başka sayfa açılmıyor | Geçici şifreyle girdiniz; önce şifrenizi değiştirin. |
| Şifremi unuttum, e-posta gelmedi | E-posta şu an gönderilmiyor. Kurum yöneticinize başvurun. |
| Seans notu yazarken "eğitmen kaydına bağlı değil" uyarısı | Yönetici, eğitmen kartı → Kullanıcı hesabı bölümünden hesabınızı bağlamalı. |
| BEP'i onaylarken aynı uyarı | Onaylayan kişinin de eğitmen kaydı olmalı ve hesabına bağlı olmalı. |
| Grup seansında veliye görünür not kaydedilmiyor | Notun hangi öğrenci için olduğunu seçin. |
| Veli seans notunu göremiyor | Not "Veliye Görünür" mü, doğru öğrenciye mi bağlı, velinin "Oturumlar" kutusu açık mı? |
| Veli gelişim raporunu göremiyor | Rapor **Kesinleştir** ile kesinleştirildi mi, velinin "Raporlar" kutusu açık mı? |
| Bir referans değeri düzenlenemiyor | Sistemle gelen ortak bir değer; yalnızca pasifleştirilebilir. Kendi değerinizi ekleyin. |
| Bir referans değeri silinmiyor | Zaten kullanımda. "Pasifleştir" seçeneğini kullanın. |
| Fatura iptal edilemiyor | Gerekçe yazın. Fatura ödenmişse önce iade yapın. |
| Yanlış ödeme girildi | Silmeyin; iade ya da düzeltme kaydı oluşturun. |
| İmzalı sözleşmede değişiklik gerekiyor | Feshedip yeni sözleşme açın. Yalnızca taslak sözleşme düzenlenir. |
| Akademik dönem silinmiyor | Güncel dönemse önce başka dönemi güncel yapın; BEP bağlıysa silinemez. |
| Veli portalda göremiyor | Veli öğrenciye bağlı mı, kullanıcı hesabı açık mı, portal erişimi açık mı, kontrol edin. |
| Seans hakkı düşmedi | Yoklamada "Geldi" seçili mi, öğrencinin seansın programına bağlı ve hakkı kalan paketi var mı, kontrol edin (8.5). |
| KPI panosu boş | `Performans Anlık Görüntüleri → Dönem Hesapla`. |
| "Bu kullanıcı adı alınmış" uyarısı | Kullanıcı adı başka bir kurumda da kullanılıyor olabilir. E-posta adresi kullanın. |

---

## 7. Yöneticiler İçin — Yeni Kurum Devreye Alma

Adım adım kurulum planı, önerilen roller, hesap bağlama ve müşteriden istenecek bilgiler tablosu: `docs/musteri/KURULUM_KILAVUZU.md` (Bölüm B).

Kısa sıra: kurum ayarları ve kampüsler → referans verileri gözden geçir → roller → odalar, programlar, hizmetler, paketler → akademik dönem → eğitmen kayıtları ve hesapları (BEP onaylayacak koordinatör dahil) → diğer personel → sözleşme ve rıza şablonları → izin bakiyeleri → öğrenciler, veliler, kayıtlar, paketler → tekrarlayan programlar → veli portal hesapları → bir öğrenciyle uçtan uca deneme.

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
- **Seans Notu** — Eğitimcinin notu. **Veliye Görünür** işaretliyse veli portalında, çocuğun **Seanslar** sekmesinde ilgili seansın altında görünür (Seanslar bölümü kurum tarafından o veliye açıksa). Her veliye görünür not bir öğrenciye bağlanır ve yalnızca o öğrencinin velilerine gösterilir (grup seansında öğrenciyi seçmeniz gerekir; öğrencisiz not iç nottur). Not sonradan düzenlenirse velide **düzenlendi** yazar; görünürlük kapatılırsa not portaldan kalkar.
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
   > Not: `Tekrarlayan Programlar` (sayfa: Tekrar Eden Planlar) formunda öğrenci, program, hizmet, eğitimci ve oda seçilir. Plandan üretilen her seans bu bilgileri taşır. Hizmet, öğrencinin aldığı programın hizmetlerinden biri olmak zorundadır.
6. **BEP:** Koordinatör Ayşe için BEP açtı, hedefleri ekledi ve kendi hesabıyla onayladı (koordinatörün eğitmen kaydı hesabına bağlı olduğu için onaylayan olarak o kaydedildi).
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

- **Kurulum (sunucu ve kurum):** `docs/musteri/KURULUM_KILAVUZU.md`
- **Ekranı henüz olmayan işlemler:** `docs/ekransiz-endpointler.md`
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
| Seans Notu, Veliye Görünür | `scheduling.session_note.parent_visible`; bağlı öğrenci `student_id` (V56; grup seansında veliye görünür not için zorunlu, boşsa hiçbir veliye gösterilmez); düzenlendi bilgisi `edited_at`/`edited_by` (V55, yalnızca metin değişince) |
| Notu yazan / düzenleyen | `authored_by` = oturumdaki kullanıcıya bağlı eğitmen (`educators.educator.user_id`, `ActingEducator`); başkasının notunu `session_note:delete` sahibi düzenler |
| BEP onay/red | `education.education_plan_approval`; onaylayan = oturumdaki kullanıcıya bağlı eğitmen (formdan alınmaz) |
| Gelişim raporu | `is_finalized`, `finalized_at` (V47); portal yalnızca kesinleşmişleri döner |
| Fatura iptal gerekçesi | `void_reason` (V46), boş olamaz |
| Eğitmen/veli ↔ kullanıcı | `educators.educator.user_id`, `students.guardian.user_id` (V48, her kullanıcı en fazla bir kayıt) |
| İlk girişte şifre değiştirme | `iam.user_account.must_change_password` (V50), JWT `mcp` |
| Modül görünürlüğü | `core.corporation.settings.modules` (V53), yalnızca `devops/set_modules.py` / `new_tenant.py` yazar |
| Seans Hedefi | `scheduling.session_goal` |
| BEP | `education.education_plan` (`draft → in_review → approved → active → revised → closed`) |
| Paket Tanımı | `finance.package_definition` (`program_id`) |
| Öğrenci Paketi | `finance.student_package` (`active / exhausted / expired / cancelled`) |
| Kredi Defteri | `finance.credit_ledger` (yalnızca ekleme yapılır; bakiye = `delta` toplamı) |

### 10.2 Yetki modeli
- İzin tabanlı rol yönetimi (RBAC): ekranlar ve işlemler `student:create` gibi izin kodlarına bağlıdır, rol adına bakılmaz.
- Yetkisiz istek API'de **403** döner; arayüz bunu "yetkiniz yok" sayfası olarak gösterir.
- Menü öğeleri veritabanından gelir (`iam.menu_item`, `required_permission_id`, `feature_flag` = modül kodu). Sayfası olmayan bir adrese giden menü öğesi arayüzde gizlenir. Platform menü öğeleri (`corporation_id` boş) kurum tarafından değiştirilemez.
- Platform referans değerleri (`ref.ref_value.corporation_id` boş) kurumca düzenlenemez; kurum yalnızca `ref_value_tenant_override` ile kendisi için pasifleştirir. Kurum değeri eklenebilen türler `ref_type.allows_tenant_values = true` olanlardır.
- Platform izinleri (`corporation:create`, `corporation:delete`) hiçbir kurum rolüne atanamaz.

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
