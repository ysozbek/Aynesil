# Sistem Turu — Kişisel Öğrenme Kılavuzu

Bu klasör kişiseldir ve commit edilmez. Sade metinler ekran dilindedir; teknik ayrıntılar `teknik/` altındadır.

## Önerilen okuma sırası
1. [Genel bakış](00_genel_bakis.md) — sistem, modüller, roller (5 dk)
2. [Sözlük](sozluk.md) — terimleri bir kez tarayın
3. Çocuğun yolculuğu sırasıyla:
   1. [Akış 1 — Aday → öğrenciye dönüşüm](akis_01_crm_aday_ogrenci.md)
   2. [Akış 2 — Öğrenci, veli, sözleşme, portal erişimi](akis_02_ogrenci_veli_sozlesme_portal.md)
   3. [Akış 3 — Değerlendirme → BEP → ilerleme](akis_03_degerlendirme_bep_ilerleme.md)
   4. [Akış 4 — Program, seans, yoklama, telafi](akis_04_program_seans_yoklama_telafi.md)
   5. [Akış 5 — Paket, fatura, ödeme, kredi](akis_05_finans_paket_fatura_odeme_kredi.md)
   6. [Akış 14 — Veli portalı](akis_14_veli_portali.md) (veli gözüyle özet)
4. Diğer iş akışları:
   - [Akış 6 — Kamp](akis_06_kamp.md)
   - [Akış 7 — Toplantı ve izin](akis_07_toplanti_takip_izin.md)
   - [Akış 8 — Okul danışmanlığı](akis_08_okul_danismanligi.md)
   - [Akış 9 — Kamera](akis_09_kamera.md)
   - [Akış 15 — Eğitmenler](akis_15_egitmenler.md)
5. Yönetim ve ölçüm:
   - [Akış 16 — Yönetim (kullanıcı, rol, liste, kampüs)](akis_16_yonetim.md)
   - [Akış 10 — KPI ve raporlar](akis_10_kpi_raporlar.md)
   - [Akış 13 — Bildirimler](akis_13_bildirimler.md)
6. Ekranı olmayan modüller (kısa okuma):
   - [Akış 11 — Anketler](akis_11_anketler.md)
   - [Akış 12 — Bakım ekibi](akis_12_bakim_ekibi.md)

## Önce bunları bilin
Tüm akışlarda "Kendin dene" için demo hesapları: `admin@demo-a.test`, `koordinator@demo-a.test`, `egitmen@demo-a.test`, `muhasebe@demo-a.test`, `veli1@demo-a.test`, `veli2@demo-a.test` — şifre `DemoSifre!2026`. Demo verisini kurmak: `scripts/demo-seed/run.sh`.

## Dikkat çeken eksikler (tur sırasında bulundu)
- ~~Veli ve eğitmen kayıtlarını kullanıcı hesabına bağlayan ekran yok~~ → eklendi: kartta "Kullanıcı hesabı" (Akış 14, 15, 16).
- Anketler ve bakım ekibinin ekranı yok; kamp ve kamerada kısmi ekran var (bkz. [eşleme tablosu](teknik/eslesme_tablosu.md)). Bu altı yarım modül (kamp, okul danışmanlığı, kamera, anketler, bakım ekibi, KPI) **kurum bazında açılıp kapanır** ve yeni kurumda kapalı başlar (Akış 16, adım 8). Hukuk (şablon, taslak sözleşme düzenle/sil, rıza), telafi, iade, izin bakiyesi, KPI tanımı, akademik dönemler ve seans notu/hedef düzenleme artık ekranlı; güncel ekransız uç listesi [../ekransiz-endpointler.md](../ekransiz-endpointler.md).
- ~~Kamera sayfasındaki "Canlı İzle" düğmesi 404'e gidiyor~~ → canlı izleme ekranı olmadığı için düğme kaldırıldı (Akış 9).
- ~~Menü ↔ sayfa uyuşmazlıkları~~ → V49 ile hizalandı (eşleme tablosu).
- E-posta/SMS gönderilmiyor, olaylardan bildirim üretilmiyor (Akış 13).

## Teknik klasör
- [teknik/eslesme_tablosu.md](teknik/eslesme_tablosu.md) — controller / ekran / akış eşlemesi
- `teknik/akis_XX_*.md` — her akışın API, izin, durum kodu, tablo, dosya yolu ve "[doğrulanacak]" notları

İlerleme kaydı: [_ilerleme.md](_ilerleme.md)
