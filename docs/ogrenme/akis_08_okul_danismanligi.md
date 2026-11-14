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
4. **Ziyaret planla:** `Danışmanlık → Ziyaretler` sayfasından yeni ziyaret kaydı açın (okul, tarih). Ziyaret "Planlandı" durumunda başlar; yapılınca "Tamamlandı", yapılamazsa "İptal Edildi" olur. Planlanmamış ziyaret değiştirilemez.
5. **Gözlem yaz:** `Danışmanlık → Gözlemler`; ziyarette gördüklerinizi kaydedin.
6. **Rapor yaz:** `Danışmanlık → Raporlar → Yeni`; özet ve bulgular. En az birinin dolu olması gerekir.
7. **Takibe bağla:** `Danışmanlık → Takipler → Yeni`; yapılacak işi açın. Takip "Bekliyor" doğar; başlayınca **Başlat** ("Devam Ediyor"), bitince **Tamamla** ("Tamamlandı"), gerekirse **İptal Et**. `Takipler → Açık Takipler` raporu hâlâ bekleyen işleri toplar.
8. **Genel durum:** `Danışmanlık` ana sayfası özet verir.

## Dikkat
- Plan sırayla ilerler: önce taslak olan etkinleştirilir, yalnızca aktif olan tamamlanır. Kapanmış plan değiştirilemez.
- Sözleşme yalnızca gönderilmişse imzalanır, yalnızca imzalıysa "Süresi Doldu" olur.
- Ziyaret, gözlem ve takip için ekranlarda her adımın düğmesi yok: ziyareti tamamlama/iptal ekranda durum seçimiyle yapılıyor görünüyor, gözlem ekleme düğmesi ise listede bulunmuyor [doğrulanacak].
- Tarih sırası hatalı girilirse (bitiş başlangıçtan önce) kayıt kabul edilmez.

## Kendin dene
`demo_a`'da 1 okul, plan, 2 ziyaret, gözlem, takip görevi ve sözleşme var. `koordinator@demo-a.test` → `Danışmanlık → Kurumlar`'dan okulu açın; `Planlar`'da planı görün; `Takipler`'de takibi **Başlat** → **Tamamla**. Yeni bir okul, sonra plan ve sözleşme açıp sözleşmeyi **Gönder** → **İmzala** yapın.

Teknik ayrıntılar: [teknik/akis_08_okul_danismanligi.md](teknik/akis_08_okul_danismanligi.md)
