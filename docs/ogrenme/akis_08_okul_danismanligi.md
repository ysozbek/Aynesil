# Akış 8 — Okul danışmanlığı: kurum → plan → ziyaret → gözlem → takip

> **Modül anahtarı: `consultancy` (Okul danışmanlığı).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable consultancy` (Akış 16).

## Bu akış ne anlatıyor?
Kurumun dışarıdaki okullara verdiği danışmanlık hizmetini anlatır: hangi okulla çalışıldığı, yıllık plan, okula yapılan ziyaretler, ziyarette alınan gözlemler, çıkan rapor ve yapılacak işlerin takibi. Okulla yapılan anlaşma da burada tutulur.

## Kim yapar?
Kurum Yöneticisi ve danışmanlık izni verilen kişiler. Demo'daki Koordinatör bu izinlere sahip; Eğitmen ve Muhasebe değil.

## Adımlar
1. **Okulu kaydet:** `Danışmanlık → Kurumlar → Yeni`; okulun adı ve iletişim bilgileri. Her şey bu okula bağlanır.
2. **Anlaşma yap:** `Danışmanlık → Sözleşmeler → Yeni`; taslak oluşur. Sözleşme sayfasında **Gönder** ("Gönderildi") → **İmzala** ("İmzalandı"). Süresi dolunca **Süresi Doldu**, vazgeçilirse **İptal Et**; yalnızca taslak sözleşme düzenlenir ya da silinir.
3. **Plan hazırla:** `Danışmanlık → Planlar → Yeni`; okul, dönem, amaçlar. Plan "Taslak" başlar; hazır olunca **Aktifleştir**, iş bitince **Tamamla**, vazgeçilirse **İptal Et**.
4. **Ziyaret planla:** `Danışmanlık → Ziyaretler` sayfasından yeni ziyaret kaydı açın (okul, isteğe bağlı aktif plan, tarih). Ziyaret "Planlandı" durumunda başlar. Ziyarete tıklayınca **ayrıntı sayfası** açılır: **Düzenle**, **Ziyareti Tamamla** ("Tamamlandı"), **Ziyareti İptal Et** ("İptal Edildi"), **Ziyareti Sil**. Yalnızca planlanmış ziyaret değiştirilir ya da silinir.
5. **Gözlem yaz:** ziyaret sayfasında **Gözlem Ekle** (tür, konu, gözlem, öneri); her gözlem **Düzenle** / **Sil** / **Takip Ekle** ile yönetilir. `Danışmanlık → Gözlemler` sayfası gözlemi olan ziyaretleri listeler ve ziyaret sayfasına götürür. İptal edilmiş ziyaretlere gözlem eklenemez.
6. **Rapor yaz:** ziyaret sayfasından **Rapor Yaz** ya da `Danışmanlık → Raporlar → Yeni`; özet ve bulgular. En az birinin dolu olması gerekir. Rapora tıklayınca özet açılır; rapor **Sil** ile kaldırılır.
7. **Takibe bağla:** `Danışmanlık → Takipler → Yeni`; yapılacak işi açın. Takip "Bekliyor" doğar; başlayınca **Başlat** ("Devam Ediyor"), bitince **Tamamla** ("Tamamlandı"), gerekirse **İptal Et**. `Takipler → Açık Takipler` raporu hâlâ bekleyen işleri toplar.
8. **Genel durum:** `Danışmanlık` ana sayfası özet verir; `Danışmanlık → Raporlar ve Özetler` dört sekmede sözleşme özeti, okul raporu, plan sonuçları ve ziyaret geçmişini gösterir.
9. **Silme:** plan (yalnızca taslak), ziyaret, rapor, takip (tamamlanmamış) ve kurum silinebilir; bağlı kayıt varsa silinmez ve ekran nedenini yazar.

## Dikkat
- Plan sırayla ilerler: önce taslak olan etkinleştirilir, yalnızca aktif olan tamamlanır. Kapanmış plan değiştirilemez.
- Sözleşme yalnızca gönderilmişse imzalanır, yalnızca imzalıysa "Süresi Doldu" olur.
- **Silme engelleri:** gözlemi, raporu ya da takibi olan ziyaret silinmez; ziyaret, sözleşme, rapor veya takibi olan plan silinmez (silinmiş sözleşme de sayılır); planı, ziyareti ya da sözleşmesi olan kurum silinmez. Önce bağlı kaydı silin ya da planı iptal edin.
- Ziyaretin planı değiştirilecekse yeni plan aynı okulun **aktif** planı olmalıdır.
- Tarih sırası hatalı girilirse (bitiş başlangıçtan önce) kayıt kabul edilmez.

## Kendin dene
`demo_a`'da uçtan uca akış kurulu: 1 okul → imzalı sözleşme → aktif plan → 2 ziyaret (biri tamamlanmış, gözlemli ve raporlu) → gözlem → rapor → başlatılmış takip. `koordinator@demo-a.test` → `Danışmanlık → Ziyaretler` → tamamlanmış ziyareti açın (gözlemi düzenleyin, **Takip Ekle**); planlı ikinci ziyarette **Düzenle** / **Ziyareti Tamamla** deneyin; birinci ziyareti **Sil**meyi deneyip engel mesajını görün. `Raporlar ve Özetler`'de dört sekmeyi gezin. Yeni bir okul, sonra plan ve sözleşme açıp sözleşmeyi **Gönder** → **İmzala** yapın.

Teknik ayrıntılar: [teknik/akis_08_okul_danismanligi.md](teknik/akis_08_okul_danismanligi.md)
