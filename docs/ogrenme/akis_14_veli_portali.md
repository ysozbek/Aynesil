# Akış 14 — Veli portalı: velinin gözünden sistem

## Bu akış ne anlatıyor?
Velinin sisteme girip çocuğunun seanslarını, hedeflerini, paketlerini, belgelerini ve toplantılarını nasıl gördüğünü anlatır. Veli yalnızca kendisine bağlı çocukları ve yalnızca kurumun açtığı bölümleri görür.

## Kim yapar?
Veli. (Portalı açmak: Kurum Yöneticisi, bkz. Akış 2 ve aşağıdaki "Portal hesabını açmak".)

## Portal hesabını açmak (yönetici)
Portal iki ayrı adımdan oluşur ve **ikisi de** gerekir:
1. **Hesap:** veli kartı → Genel Bakış → **Kullanıcı hesabı** → **Yeni kullanıcı oluştur**. Kullanıcı adı olarak velinin e-postası önerilir; rol olarak **Veli (portal)** önseçilidir (her kurumda hazır gelir, yalnızca portal izinleri vardır). Sistem bir **geçici şifre** üretir ve **bir kez** gösterir; e-posta henüz gönderilmediği için şifreyi veliye siz iletirsiniz (telefon, yüz yüze). Velinin zaten bir kullanıcısı varsa **Mevcut kullanıcıyı bağla** ile seçilir.
2. **Çocuk bazında erişim:** veli kartı → **Portal Erişimi** → çocuk için **Erişim Aç** ve görünecek bölümleri seçin.
"Erişim Aç" tek başına hesap açmaz; hesap yoksa Portal Erişimi sekmesinde sarı bir uyarı çıkar ve veli giriş yapamaz. Yönetici açtığı hesapta veli ilk girişte **şifre değiştirmeye zorlanır** (başka sayfa açılmaz); yeni şifreyle yeniden giriş yapar. Sonra da sağ üstteki baş harfinden **Şifre Değiştir**'e gidebilir.

## Adımlar
1. **Giriş yap:** veli, kullanıcı adı ve şifresiyle girer. Menüde yalnızca **Veli Portalı** grubu görünür: **Özet**, **Çocuklarım**, **Bildirimler**.
2. **Karşılama sayfası:** `Portal` açılış sayfası yaklaşan seansı, okunmamış bildirimleri, paket bakiyesini ve aktif hedef sayısını gösterir. Bağlı çocuk yoksa "Kurum yöneticinizle iletişime geçin." yazar.
3. **Çocuğu seç:** `Portal → Çocuklarım`; kayıtlı çocukların listesi. Birine tıklayınca çocuğun sayfası açılır.
4. **Sekmeleri gez** (hangi sekmelerin göründüğünü kurum belirler):
   - **Seanslar:** yaklaşan seanslar ve geçmiş. Eğitmenin **Veliye Görünür** işaretlediği notlar geçmişteki seansın altında görünür; sonradan düzenlenmişse "düzenlendi" yazar. Dahili notlar görünmez; grup seansında yalnızca o çocuğa bağlanan notlar gelir, diğer çocuklar için yazılanlar görünmez.
   - **Hedefler:** hedefler, ilerleme ve tamamlananlar (BEP/IEP kutusu açıksa; yalnızca veliye açılmış onaylı plan görünür).
   - **Paketler:** aktif paketler, son kullanım tarihi, kalan seans hakkı (Finans kutusu açıksa).
   - **Belgeler:** paylaşılan belgeler ve raporlar (Raporlar kutusu açıksa). Veli yalnızca **kesinleşmiş** gelişim raporlarını görür.
   - **Toplantılar:** katıldığı toplantılar.
5. **Bildirimler:** `Portal → Bildirimler`'de kendine iletilenler.
6. **Kamera:** yetkisi varsa çocuğunu izleyebilir (Akış 9); portalda bu için bir sekme yok.

## Dikkat
- Bir sekme görünmüyorsa genellikle kurum o kutuyu kapatmıştır (Akış 2 / veli kartı → Portal Erişimi). Hata değildir.
- Başka çocuğa erişmeye çalışırsa "Bu öğrenciye erişim yetkiniz bulunmuyor." yanıtı alır.
- Toplantılar sekmesi her veliye açıktır (izin kutusu yok).
- Taslak gelişim raporu ya da veliye açılmamış BEP velide görünmez.
- Portalda anket doldurma, rapor indirme gibi ek işlevler görmedim [doğrulanacak].
- Bir kullanıcı en fazla bir veli kaydına bağlanabilir. Veli hesabına verilen rol "portal:access" iznini içermiyorsa sistem hesabı açmaz ve nedenini söyler.
- Veli hesabı yalnızca portalı görür; personel ekranlarına (öğrenciler vb.) erişemez.

## Kendin dene
- `veli1@demo-a.test` (iki çocuk, kamera yetkili) ile girin; `Çocuklarım`'dan iki çocuğu da gezin.
- `veli2@demo-a.test` (tek çocuk) ile girip daha az çocuk görünmesini karşılaştırın.
- Yönetici hesabıyla (`admin@demo-a.test`) bir velinin **Portal Erişimi**'nde bir kutuyu kapatıp veliyle yeniden girerek ilgili sekmenin kaybolduğunu görün.
- Uçtan uca: yönetici olarak hesabı olmayan bir veli açın (ör. bir babayı), Portal Erişimi'nde uyarıyı görün, **Erişim Aç** deyin, sonra Genel Bakış → Kullanıcı hesabı → Yeni kullanıcı oluştur ile hesap açıp geçici şifreyi not edin. Çıkış yapıp o hesapla girin, Çocuklarım'da çocuğu görün, ardından Şifre Değiştir'i deneyin.

Teknik ayrıntılar: [teknik/akis_14_veli_portali.md](teknik/akis_14_veli_portali.md)
