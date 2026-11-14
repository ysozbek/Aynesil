# Akış 3 — Değerlendirme → BEP (plan, hedefler, onay) → ilerleme takibi

## Bu akış ne anlatıyor?
Çocuğun önce bir form üzerinden değerlendirilmesini, sonuca göre Bireysel Eğitim Planı'nın (BEP) hazırlanıp onaylanmasını ve planın hedeflerinde çocuğun ne kadar ilerlediğinin kaydedilmesini anlatır. Veli de onaylı planı portalda görebilir.

## Kim yapar?
Değerlendirme ekibi (Koordinatör/Yönetici) değerlendirmeyi yapar. BEP'i Koordinatör hazırlar ve onaylar. Eğitmen hedeflere ilerleme girer ve plana inceleme notu ekler; plan oluşturup onaylayamaz.

## Adımlar
1. **Şablon hazırla (bir kez):** `Değerlendirme → Şablonlar` ile bölümlere ve maddelere ayrılmış değerlendirme formu oluşturun. Aynı formu her çocukta kullanırsınız.
2. **Oturum oluştur:** `Değerlendirme → Değerlendirmeler → Oturum Oluştur`; şablonu ve aday ya da öğrenciyi seçin. İkisinden biri mutlaka seçilmeli.
3. **Oturumu yürüt:** oturum sayfasında **Başlat** → **Puanlama** sekmesinde maddeleri doldurup **Yanıtları Kaydet** → **Tamamla**. Başlamamış oturum tamamlanamaz.
4. **Rapor ve öneri:** **Rapor** sekmesinde **Rapor Oluştur**, sonra **Raporu Sonuçlandır**. **Öneriler** sekmesinde **Öneri Ekle** ile çocuğa uygun program, yoğunluk ve gerekçeyi yazın. Bu öneri, programa kayıt kararınıza dayanak olur.
5. **Hedefleri hazırla:** `Hedefler → Hedef Kütüphanesi` ve `Hedef Şablonları` hazır hedef metinleridir. `Hedefler → Öğrenci Hedefleri`'nden çocuğa başlangıç değeri, hedef değer ve tarihle hedef açın.
6. **BEP oluştur:** `BEP → Plan Oluştur`; öğrenci, akademik dönem, başlık, başlangıç ve bitiş tarihi. Plan "Taslak" olarak başlar. Plan sayfasında **Hedefler** sekmesinden **Hedef Ekle** ile çocuğun hedeflerini plana bağlayın.
7. **Onaya çıkar:** **İncelemeye Gönder** ("İnceleme Bekliyor") → onaylayan kişi **Onayla** ("Onaylandı") ya da **Reddet** (plan "Taslak"a döner) → onaylı planı **Aktifleştir** ("Aktif").
8. **Veliye göster:** plandaki "Veli Görüntülüyor" anahtarını açın. Yalnızca "Onaylandı" ya da "Aktif" plan veliye açılabilir.
9. **İlerleme gir:** ilerleme çoğu zaman seans içinde girilir (Akış 4). Doğrudan girmek için `Hedefler → Öğrenci Hedefleri →` hedef → **İlerleme Kaydet**: ölçüm tarihi ve değeri, tamamlanma yüzdesi, eğilim (İyileşiyor / Sabit / Gerileme), not. `Hedefler` ana sayfası başarı oranını ve trendi gösterir.
10. **Dönem sonunda:** plana **İnceleme Ekle** ile değerlendirme notu yazın. Plan değişecekse **Revize Et** (değişiklik özeti yazılır, sürüm artar); işi bitince **Kapat**. Bir hedef tamamlandığında **Durumu Değiştir** ile "Ulaşıldı" yapılır.

## Dikkat
- Plan sırayla ilerler: Taslak → İnceleme Bekliyor → Onaylandı → Aktif. Sırayı atlamaya çalışırsanız sistem plan hangi durumdayken ne yapılabileceğini söyleyen bir uyarı verir.
- Sonuçlandırılmış değerlendirme raporu bir daha değiştirilemez.
- Taslak plan revize edilemez ve kapatılamaz.
- Akademik dönem tanımlı olmalı. Dönemler `BEP → Akademik Dönemler` ekranından yönetilir: **Dönem Ekle**, **Düzenle**, **Güncel dönem yap** (önceki güncel dönemin işareti kalkar) ve **Sil**. Eğitim planı bağlı bir dönem ve güncel dönem silinemez; önce başka dönemi güncel yapın. Bitiş tarihi başlangıçtan sonra olmalıdır.

## Kendin dene
- `koordinator@demo-a.test`: `BEP`'te 8 plan var (taslak, incelemede, onaylı, aktif). Taslağı **İncelemeye Gönder** → **Onayla** → **Aktifleştir**; sonra "Veli Görüntülüyor"u açın.
- `egitmen@demo-a.test`: `Hedefler → Öğrenci Hedefleri`'nde bir hedefe **İlerleme Kaydet**. BEP'te plan oluşturma düğmesi olmadığını, yalnızca **İnceleme Ekle**'yi görün.
- `Değerlendirme`: 4 oturumdan birini Başlat → Puanlama → Tamamla → Rapor sırasıyla geçin.
- `veli1@demo-a.test` ile `Portal → Çocuklarım → Hedefler`'e bakıp veli gözünü görün.

Teknik ayrıntılar: [teknik/akis_03_degerlendirme_bep_ilerleme.md](teknik/akis_03_degerlendirme_bep_ilerleme.md)
