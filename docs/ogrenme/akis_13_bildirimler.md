# Akış 13 — Bildirimler: gelen kutusu, şablonlar ve tercihler

## Bu akış ne anlatıyor?
Kullanıcıların kendilerine gelen bildirimleri okumasını, yöneticinin bildirim metni şablonlarını hazırlamasını ve herkesin hangi bildirimleri almak istediğini ayarlamasını anlatır. **Önemli:** e-posta/SMS gönderen bir sağlayıcı henüz bağlı değil; bugün sistem olayları (yeni seans, ödeme gecikti gibi) kendiliğinden bildirim üretmiyor.

## Kim yapar?
Gelen kutusu ve tercihler: sisteme giren herkes (Eğitmen, Veli dahil). Şablonlar: Kurum Yöneticisi.

## Adımlar
1. **Gelen kutusuna bak:** `Bildirimler → Gelen Kutusu`. Okunmamışlar vurgulu; "Sadece Okunmamış/Sadece Okunmuş" ve tarih filtresi var.
2. **Okundu işaretle:** tek tek **Okundu İşaretle** ya da **Tümünü Okundu İşaretle**. Bir bildirime tıklayınca ayrıntısı ve okunma zamanı görünür.
3. **Tercihlerini ayarla:** `Bildirimler → Tercihler`; hangi kategoriden hangi kanaldan (portal içi, e-posta, SMS) bildirim alacağınızı seçin.
4. **Şablon hazırla (yönetici):** `Bildirimler → Şablonlar → Yeni Şablon`; kod, kategori, tür ve her dil için konu ve içerik. İçerikte kişiye göre değişen alanlar (ör. ad) kullanılabilir.
5. **Panoya bak:** `Bildirimler` ana sayfası toplam, okunmamış, okunma oranı ve şablon sayısını gösterir. Veli de `Portal → Bildirimler`'de kendi bildirimlerini görür.

## Dikkat
- **E-posta ve SMS gönderilmiyor.** Davet ve parola sıfırlama gibi bildirimler yalnızca günlüğe yazılır; kullanıcıya ulaşmaz. Gerçek sağlayıcı bağlanana kadar bu böyle.
- Olay → bildirim bağlantısı (ör. "seans yarın") için tetikleyici ayarları arka planda var ama ekranı yok ve olayları bildirime çeviren bir işleyici bulamadım [doğrulanacak]. Bu yüzden gelen kutusunu bugün yalnızca demo verisi doldurur.
- Menüdeki "Tetikleyiciler" satırı pasifleştirildi.
- Şablonu silmeden önce emin olun; kullanılan şablonlar için ayrı bir koruma görmedim [doğrulanacak].

## Kendin dene
`demo_a`'da bildirim şablonları ve 7 örnek bildirim var. `koordinator@demo-a.test` ile `Bildirimler → Gelen Kutusu`'nu açıp **Okundu İşaretle** deneyin; `Şablonlar`'dan yeni şablon ekleyin; `Tercihler`'i açın. Veli gözü için `veli1@demo-a.test` ile `Portal → Bildirimler`.

Teknik ayrıntılar: [teknik/akis_13_bildirimler.md](teknik/akis_13_bildirimler.md)
