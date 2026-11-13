# Akış 7 — Toplantı → sonuç → takip görevleri; izin talebi → onay

Bu dosyada iki küçük akış var: önce toplantılar, sonra personel izinleri.

## A. Toplantı → sonuç → takip görevleri

### Bu akış ne anlatıyor?
Kurum içi, veli, aday ya da dış toplantıların planlanmasını, kimin katıldığının işaretlenmesini, kararların yazılmasını ve kararlardan çıkan işlerin sahibi ve son tarihiyle takip edilmesini anlatır.

### Kim yapar?
Kurum Yöneticisi ve Koordinatör. Eğitmen toplantıları yalnızca görür.

### Adımlar
1. **Toplantı planla:** `Toplantılar → Yeni Toplantı`; başlık, tür, başlangıç ve bitiş, konum ya da link.
2. **Katılımcı ekle:** toplantı sayfasında **Katılımcı Ekle**; kurum çalışanı, veli, aday ya da dışarıdan biri olabilir. Toplantıdan sonra herkes için **Katıldı** ya da **Katılmadı** işaretlenir.
3. **Toplantıyı kapat:** **Tamamla**, ya da toplantı olmayacaksa **İptal Et**.
4. **Sonucu yaz:** **Sonuç Ekle**; özet ve alınan kararlar.
5. **Takip görevi aç:** **Görev Ekle**; yapılacak iş, sorumlu kişi, son tarih. Görevler "Açık → Devam Ediyor → Tamamlandı" ilerler.
6. **Görevleri izle:** `Toplantılar → Takip Görevleri` tüm bekleyen ve gecikmiş görevleri toplar; iş bitince **Tamamla**. `Toplantılar → Takvim` aylık/haftalık görünümü verir.

### Dikkat
- İptal edilmiş toplantı güncellenemez ya da tamamlanamaz; tamamlanmış toplantı iptal edilemez.
- Tamamlanmış ya da iptal edilmiş bir takip görevi yeniden açılamaz; yanlış kapattıysanız yeni görev açın.

### Kendin dene
`koordinator@demo-a.test` ile `Toplantılar`'a girin (4 toplantı; görevler tamamlanmış, gecikmiş ve açık). `Takip Görevleri`'nde gecikmiş bir görevi **Tamamla**. Yeni toplantı açıp katılımcı ekleyin, **Tamamla** → **Sonuç Ekle** → **Görev Ekle** sırasını izleyin. `egitmen@demo-a.test` ile yalnızca görüntüleyebildiğinizi görün.

## B. İzin talebi → onay

### Bu akış ne anlatıyor?
Eğitmenlerin yıllık, hastalık, ücretsiz ya da saatlik izin istemesini, yöneticinin bu izin yüzünden etkilenecek seansları görüp onaylamasını ve izin hakkının (bakiye) takip edilmesini anlatır.

### Kim yapar?
Eğitmen talep eder (kendi izni). Koordinatör ve Kurum Yöneticisi onaylar ya da reddeder ve bakiyeleri yönetir.

### Adımlar
1. **Talep gir:** `İzin → Talepler → Yeni`; izin alacak **eğitmeni seçin** (otomatik gelmez), izin türü, gün ya da saat, tarihler, neden.
2. **Gerekirse düzenle:** yalnızca "Bekliyor" durumundaki talep düzenlenebilir; vazgeçilirse **İptal Et**.
3. **Etkiyi gör ve karar ver:** yönetici talep sayfasında önce o tarihlerdeki seansların listesini (seans etkisi) görür, sonra **Onayla** ya da **Reddet** der; red için neden yazılır.
4. **Bakiyeyi izle:** `İzin → Bakiyeler`; hak, kullanılan ve kalan görülür. Yönetici **Bakiye Tanımla** ile eğitmen + izin türü + yıl için yıllık hakkı açar (aynı kombinasyon için ikinci kayıt açılamaz), mevcut hakkı **Hak Güncelle** ile değiştirir, yıl sonunda **Devir İşlemi** yapabilir. Bakiye kaydı olmayan eğitmende bakiye kontrolü yapılmaz; bu yüzden yıl başında hak tanımlamak gerekir.
5. **Takvim ve rapor:** `İzin → Takvim` izinleri takvimde, `İzin → Raporlar` kullanım ve trendleri gösterir.

### Dikkat
- Aynı eğitmen için aynı tarihlerde bekleyen ya da onaylı başka talep varsa yenisi reddedilir.
- Onay sırasında bakiye yetmezse: "Yetersiz izin bakiyesi…" uyarısı çıkar. Eğitmenin hiç bakiye kaydı yoksa bu kontrol yapılmıyor gibi [doğrulanacak].
- Onaylanan izin seansları otomatik iptal etmez veya başkasına devretmez; yönetici etkilenen seansları kendisi düzenler [doğrulanacak].

### Kendin dene
`demo_a`'da 8 bakiye ve 4 talep (bekleyen, onaylı, reddedilmiş, iptal) var. `koordinator@demo-a.test` ile bekleyen talebi açıp seans etkisini görün, **Onayla**; bakiyenin düşüşünü `İzin → Bakiyeler`'de izleyin. Bakiyeden büyük miktarlı yeni bir talebi onaylamayı deneyip uyarıyı görün. `egitmen@demo-a.test` ile kendi adınıza talep girmeyi deneyin.

Teknik ayrıntılar: [teknik/akis_07_toplanti_takip_izin.md](teknik/akis_07_toplanti_takip_izin.md)
