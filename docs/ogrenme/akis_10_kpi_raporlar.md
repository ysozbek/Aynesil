# Akış 10 — KPI ve raporlar: değerler nereden geliyor, panolar neyi gösteriyor

> **Modül anahtarı: `kpi` (Eğitmen KPI).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable kpi` (Akış 16). Rapor panoları (Hukuk, Finans vb.) bu bayraktan etkilenmez; yalnızca KPI menüsü ve `/api/performance-kpi` uçları kapanır.

## Bu akış ne anlatıyor?
Eğitmenlerin performansının nasıl ölçüldüğünü ve kurumun hangi ekranlarda hangi özet bilgiyi gördüğünü anlatır. KPI sayıları elle girilmez; seans, yoklama, hedef ve veli geri bildirimi gibi gerçek kayıtlardan **hesaplanır**.

## Kim yapar?
Kurum Yöneticisi ve KPI yetkisi olan kişiler. Eğitmen, demo'da kendi KPI panosunu okuma izniyle görür; hesaplama ve tanım yönetimi yapamaz.

## Adımlar
1. **Ölçütleri gör:** `KPI & Performans → KPI Tanımları`; her ölçütün adı, kategorisi ve birimi listelenir. Yetkiliyseniz bir ölçütü aktif/pasif yapabilir, **Yeni KPI tanımı** ile kuruma özel ölçüt ekleyebilir ve kendi ölçütünüzü **Düzenle** ile değiştirebilirsiniz (kod, ad, kategori, birim, isteğe bağlı JSON formül). Platformun hazır ölçütleri değiştirilemez. Not: formül alanı bugün hesaplamada kullanılmıyor; hesaplanan göstergeler Anlık Görüntüler'dekilerdir [doğrulanacak: özel tanımların hesaba katılması].
2. **Dönem hesapla:** `KPI & Performans → Performans Anlık Görüntüleri → Dönem Hesapla`; düğme içinde bulunulan ay için hesaplar (dönem seçimi yok). Sistem o dönemin kayıtlarını tarayıp her eğitmen için bir özet çıkarır ve saklar. Hesaplamazsanız panolar o dönem için boş kalır; otomatik hesaplayan bir arka plan işi kayıtlı değil.
3. **Panoya bak:** `KPI & Performans` sayfasında üç sekme var: **Yönetici Görünümü**, **Üst Yönetim Görünümü** ve **Sıralama** ("En Başarılı Eğitmenler"). Üstteki dönem seçiciyle önceki dönemlere geçilir, gelecek döneme geçilemez.
4. **Geçmişi incele:** `Performans Anlık Görüntüleri` listesi geçmiş dönem özetlerini tutar.

**Değerler nereden geliyor?** Eğitmen başına:
- **Seans:** dönemde tamamlanan seans sayısı.
- **Devam Oranı:** o seanslardaki öğrencilerin "Katıldı" ya da "Geç Geldi" olanlarının yüzdesi (yoklama alınmazsa düşük çıkar).
- **Hedef Başarı:** tamamlanan seanslardan en az bir hedefin işlendiği seansların yüzdesi (seansa hedef bağlamak bu yüzden önemli).
- **Veli Memnuniyeti:** o dönemde velilerin verdiği 1–5 puanın ortalaması (Akış 11).
- **Kullanım Oranı:** tamamlanan seansların, tamamlanan + iptal + gelmedi + yeniden planlanan seanslara oranı.
- Ayrıca programı tamamlayan öğrenci oranı hesaplanır.

## Diğer panolar (hangisi neyi gösterir?)
- **Ana Sayfa:** şu an yalnızca karşılama sayfası, veri göstermiyor.
- **CRM → Raporlar:** aday sayısı, dönüşüm oranı, kaynağa göre dönüşüm. **CRM ana sayfası:** pipeline özeti, yaklaşan takipler.
- **Hedefler (ana sayfa):** hedef sayısı, ulaşılan hedefler, başarı oranı, gelişim alanına göre kırılım.
- **Planlama Paneli:** bugünkü seanslar, bekleyen telafi talepleri. **Yoklama** ekranı devam özetidir.
- **Finans (ana sayfa):** finansal özet [doğrulanacak: hangi sayılar]. Gelir, paket ve kredi kullanım raporları sistemde var ama ayrı ekranı yok.
- **İzin → Raporlar:** kullanım ve trend. **Hukuk → Raporlar:** sözleşme, rıza ve imza özetleri. **Danışmanlık, Kamp, Kamera, Değerlendirme, Toplantı** ana sayfaları kendi sayılarını gösterir.
- Menüde "Performans" ve "Raporlar" adlı ana satırlar olsa da bunların gerçek bir ekranı yok; güncel KPI ekranı "KPI & Performans"tır.

## Dikkat
- Panoda sayı yoksa önce **Dönem Hesapla** yapın; değerler otomatik güncellenmez.
- Hesaplama aynı dönem için tekrar yapılırsa eski sonucun üzerine yazar.
- Yoklama ve seansa hedef bağlama gibi günlük işler eksikse KPI'lar gerçeği yansıtmaz.
- Veli memnuniyeti için veli geri bildirimi gerekir; ekrandan geri bildirim girilemiyor (Akış 11).

## Kendin dene
`demo_a`'da önceki 2 ayın KPI değerleri hazır hesaplanmış. `admin@demo-a.test` ile `KPI & Performans`'ı açın, sekmeler arasında gezin, dönemi geri alın. `Performans Anlık Görüntüleri`'nde **Dönem Hesapla** deyip sayıların değiştiğini izleyin. `egitmen@demo-a.test` ile girip yalnızca okuyabildiğinizi görün.

Teknik ayrıntılar: [teknik/akis_10_kpi_raporlar.md](teknik/akis_10_kpi_raporlar.md)
