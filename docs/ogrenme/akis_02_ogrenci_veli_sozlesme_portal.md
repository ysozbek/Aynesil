# Akış 2 — Öğrenci ve veli kaydı → sözleşme ve rızalar → veli portalı

## Bu akış ne anlatıyor?
Öğrencinin dosyasının açılmasını, velilerin bağlanmasını, sözleşme ve izinlerin (rıza) alınmasını ve velinin çocuğunu portaldan görebilmesini anlatır. Bu, kuruma kabul edilen bir çocuğun "resmi" başlangıcıdır.

## Kim yapar?
Koordinatör (öğrenci ve veli kaydı), Kurum Yöneticisi (sözleşme, rıza, portal erişimi). Eğitmen öğrenci ve veli bilgisini yalnızca görür.

## Adımlar
1. **Öğrenci ekle:** `Öğrenciler → Öğrenci Ekle`. Ad, soyad, doğum tarihi, birincil kampüs gibi bilgiler girilir. (Adaydan dönüştürdüyseniz bu iş zaten yapılmıştır.)
2. **Öğrenci kartını doldur:** öğrenci sayfasında **Genel Bakış** (tanılar, gelişim profili, acil iletişim, raporlar), **Veliler**, **Programlar**, **Vaka Yönetimi** ve **Geçmiş** sekmeleri var. Öğrencinin durumu **Durum Değiştir** ile güncellenir; kampüs işlemleri **Kampüse Kayıt**, **Transfer Et**, **Kaydı Bitir** ile yapılır.
3. **Veli bağla:** veli yoksa önce `Veliler → Veli Ekle`. Sonra öğrenci kartında **Veliler** sekmesi → **Veli Bağla**: yakınlık, "Birincil Veli", "Velayeti Var", "Mali Sorumlu" seçilir. Kimin kime ne olduğu böylece net olur.
4. **Sözleşme hazırla:** önce şablon gerekir: `Hukuk → Sözleşme Şablonları → Yeni şablon` (kod, tür, başlık, metin; sonradan **Düzenle** ya da **Yeni sürüm**). Sonra `Hukuk → Sözleşmeler → Yeni Sözleşme`; öğrenci, güncel şablon, imzalayan veli ve tarihler seçilir. Sözleşme "Taslak" doğar.
5. **Taslağı düzelt (gerekirse):** sözleşme **Taslak**ken sayfada **Düzenle** (imzalayan veli ve tarihler) ve **Sil** düğmeleri görünür. Gönderilmiş, imzalanmış ya da aktif sözleşme düzenlenemez ve silinemez (sistem "'…' durumundaki bir sözleşme değiştirilemez" ya da "yalnızca taslak sözleşmeler silinebilir" der); o aşamada **Feshet**/yeni sözleşme kullanılır.
6. **Sözleşmeyi yürüt:** sözleşme sayfasında **Gönder** ("Gönderildi") → aile imzalayınca **İmzala** ("İmzalandı"; imza yöntemi ve imzalayanın adı yazılır). İmzalı sözleşmeyi **Etkinleştir** ("Aktif"); süresi dolunca **Süresini bitir**, bozulunca **Feshet**.
7. **Rızaları al ve kontrol et:** öğrenci kartı → **Rızalar** sekmesi → **Rıza ver** (tür ya da güncel onam şablonu, veli, geçerlilik tarihi); verilmiş rıza **Rızayı geri al** ile geri çekilir. `Hukuk → Rızalar` tüm öğrencilerin KVKK, kamera, foto-video izinlerini listeler. Kamera izlemesi bu izne bağlıdır (Akış 9): kamera rızası **veli seçilerek** verilir. Onam şablonları `Hukuk → Rıza Şablonları`ndadır.
8. **Gelişim raporu:** öğrenci kartı → Gelişim Raporları → **Kesinleştir**. Kesinleşen rapor veliye görünür olur.
9. **Portal erişimini aç:** `Veliler →` veli kartı → **Portal Erişimi** sekmesi → öğrenci için **Erişim Aç**. Velinin neleri göreceğini kutularla seçersiniz: Oturumlar, Devam, Raporlar, BEP/IEP, Finans, Kamera. Sonra **İzinleri Güncelle** veya **Erişimi Kapat** yapılabilir.

## Dikkat
- İmzalanmış sözleşme ve verilmiş rıza değiştirilmez. Sözleşme yalnızca "Taslak"ken silinebilir; sonrası için **Feshet** kullanılır. Sıra bozulursa (ör. taslak olmayan sözleşmeyi **Gönder**) sistem mevcut durumu söyleyen bir uyarı verir.
- Kesinleşmiş gelişim raporu bir daha düzenlenemez; kesinleştirmeden önce kontrol edin.
- **Portal iki adımdır:** "Erişim Aç" çocuk bazında neyin görüneceğini belirler ama **kullanıcı hesabı açmaz**. Velinin giriş yapabilmesi için veli kartı → Genel Bakış → **Kullanıcı hesabı** bölümünden hesap açılmalı (ya da mevcut bir kullanıcı bağlanmalı). Hesap yoksa Portal Erişimi sekmesi sarı bir uyarı gösterir. Geçici şifre bir kez gösterilir; ayrıntı Akış 14.
- Geri alınan rıza değişmez; yeniden rıza için yeni kayıt açılır. Yeni şablon sürümü eskisini arşivler; imzalı sözleşmeler eski sürümde kalır.
- Kullanıcı adı tüm kurumlarda benzersiz olmalı; veli için e-posta kullanın.

## Kendin dene
- `koordinator@demo-a.test`: 15 öğrenci ve 23 veli var. Bir öğrenciyi açıp **Veliler** sekmesini gezin, gelişim raporunda **Kesinleştir**'i deneyin.
- `admin@demo-a.test`: `Hukuk → Sözleşmeler`'de 8 sözleşme farklı durumlarda. Taslakta **Gönder**, gönderilmişte **İmzala**, imzalıda **Etkinleştir** yapın; imzalı olanda **Gönder**'i deneyip uyarıyı görün. `Hukuk → Rızalar`'da biri geri alınmış bir kamera rızası var.
- Veli gözü: `veli1@demo-a.test` (iki çocuklu, kamera yetkili) ve `veli2@demo-a.test` ile girip `Portal → Çocuklarım`'a bakın.

Teknik ayrıntılar: [teknik/akis_02_ogrenci_veli_sozlesme_portal.md](teknik/akis_02_ogrenci_veli_sozlesme_portal.md)
