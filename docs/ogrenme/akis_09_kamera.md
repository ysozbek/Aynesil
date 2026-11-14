# Akış 9 — Kamera: kurulum → derslik ataması → veli izleme yetkisi → izleme

> **Modül anahtarı: `camera` (Kamera).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable camera` (Akış 16). Kapalıyken öğrenci kamera rızası (Akış 2) yine alınabilir; yalnızca kamera/yetki/izleme uçları kapanır.

## Bu akış ne anlatıyor?
Derslikteki kameraların sisteme tanıtılmasını, hangi derslik ya da seansta çalıştıklarının belirlenmesini ve velilere belirli bir süre için çocuklarını izleme yetkisi verilmesini anlatır. Her izleme kaydedilir; bu bir yasal gerekliliktir. **Canlı yayın izleme ekranı henüz yok** (aşağıya bakın).

## Kim yapar?
Kurum Yöneticisi (kamera, atama, yetki). Veli yalnızca kendisine yetki verildiyse ve çocuğunun kamera izni açıksa izleyebilir. Eğitmen ve Koordinatör kamera menüsünü demo'da varsayılan olarak yönetici kadar görmeyebilir [doğrulanacak].

## Adımlar
1. **Kamerayı tanıt:** `Kameralar → Yeni Kamera`; kod, ad, tür, kampüs, oda, yayın adresi. Kamera pasife alınabilir.
2. **Derslik ata:** kamera sayfasında **Oda Ata** ile hangi dersliği izlediğini seçin. Seansa özel atama da yapılabilir; atamalar kamera sayfasında "Oda Atamaları" ve "Seans Atamaları" altında listelenir, kaldırılabilir.
3. **Rızayı kontrol et (şart):** veliye yetki vermeden önce öğrencinin **kamera izleme rızası** (KVKK) `Hukuk → Rızalar`'da "Verildi" olmalı (Akış 2).
4. **Portal izni:** velinin portal erişiminde **Kamera** kutusu açık olmalı (Akış 2).
5. **Yetki ver:** `Kameralar → Yetkiler → Yeni Yetki`; veli, öğrenci, erişim türü, **geçerlilik başlangıcı ve bitişi**. Bitiş tarihi zorunludur, süresiz yetki verilemez. Listede "Şu An Geçerli" görülür; **İptal Et** ile yetki geri alınır.
6. **İzleme geçmişi:** `Kameralar → İzleme Geçmişi`; kim, ne zaman başladı, ne zaman bitirdi, ne kadar sürdü (veli ve personel ayrı gösterilir). `Kameralar` ana sayfası kamera ve yetki sayılarını verir.
7. **İzleme:** uygulamada henüz bir canlı yayın oynatıcı ekranı yok. Eskiden kamera sayfasında çıkan **Canlı İzle** düğmesi var olmayan bir sayfaya gidip 404 veriyordu; bu yüzden kaldırıldı. Yayın adresi alanı yalnızca bilgi olarak saklanır; izleme kaydı (İzleme Geçmişi) bir oynatıcı ekranı yazıldığında oradan oluşacak.

## Dikkat
- Rıza yoksa yetki verilemez: "İzleme yetkisi vermeden önce geçerli bir kamera izleme onayı (KVKK) gereklidir." Rıza sonradan geri alınırsa erişim reddedilir: "Kamera izleme onayı geri çekilmiş. Erişim reddedildi."
- Veli portalında kamera kutusu kapalıysa: "Velinin bu öğrenci için portalda kamera izleme erişimi etkin değil."
- Bitiş tarihi başlangıçtan sonra olmalı; süresi dolan yetki "Süresi Doldu" görünür.
- İptal edilmiş yetki yeniden iptal edilemez.

## Kendin dene
`demo_a`'da 2 kamera, derslik ataması ve `veli1@demo-a.test` için izleme yetkisi var. `admin@demo-a.test` ile `Kameralar → Yetkiler`'de yetkiyi ve `İzleme Geçmişi`'ni görün. Rızası geri alınmış öğrencinin velisine yetki vermeyi deneyip uyarıyı okuyun. Kamera sayfasında artık **Canlı İzle** düğmesi olmadığını görün. `veli1@demo-a.test` ile portalda kamera göründüğünü kontrol edin.

Teknik ayrıntılar: [teknik/akis_09_kamera.md](teknik/akis_09_kamera.md)
