# Akış 6 — Kamp: kamp → dönem → kayıt / bekleme listesi → etkinlik → yoklama

> **Modül anahtarı: `camps` (Kamp).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable camps` (Akış 16).

## Bu akış ne anlatıyor?
Yaz kampı gibi kısa süreli programların tanımlanmasını, her dönem için kontenjanlı kayıt alınmasını ve kontenjan dolunca bekleme listesi tutulmasını anlatır. Dönem düzenleme, kayıt durumu yönetimi, dönem sonu toplu tamamlama, eğitmen atama ve kampı pasifleştirme/silme ekrandan yapılır. Etkinlik ve devam takibi sistemde var ama **henüz ekranı yok**.

## Kim yapar?
Koordinatör ve Kurum Yöneticisi. Eğitmen, kamp devam ve katılım kaydı için yetkilendirilmiştir (ama ekran yok).

## Adımlar
1. **Kamp oluştur:** `Kamp Yönetimi → Yeni Kamp`; kod, ad, tür, konum, kapasite, açıklama.
2. **Dönem ekle / düzenle / sil:** kampın sayfasında **Dönem Ekle**; dönem adı, başlangıç ve bitiş tarihi, kontenjan. Satırdaki **Düzenle** ve **Sil** ile değiştirin. Kamp birden fazla dönemden oluşabilir.
3. **Öğrenciyi kaydet:** dönemin kayıt ekranında **Kayıt Yap**; öğrenciyi seçin. Yer varsa "Kayıtlı", yoksa "Bekleme Listesi" durumuna alınır.
4. **Bekleme listesini yönet:** yer açılınca bekleyen kayıtta **Listeye Al** (kaydı "Kayıtlı" yapar). Kayıtlıyı beklemeye almak için **Bekleme Listesine Al**. Vazgeçen için **Kaydı İptal Et** ("Ayrıldı") yer açar.
5. **Durumu izle:** `Kamp Yönetimi` ana sayfasında toplam kamp, aktif kamp, toplam kayıt ve dönem sayıları; kampın sayfasında dönemler ve görevli eğitmenler görünür.
6. **Tamamlama:** satırdaki **Tamamla** tek kaydı, dönem sayfasındaki **Dönemi Tamamla** dönem sonunda tüm kayıtlı öğrencileri "Tamamlandı" yapar (bekleyenler etkilenmez).
7. **Eğitmen:** kamp sayfasında **Eğitmen Ata**; rol satırdan değişir, **Atamayı Kaldır** çıkarır.
8. **Kampı pasifleştir / etkinleştir / sil:** kamp listesi ya da kamp sayfasından.
9. **Etkinlik, yoklama, rapor:** her dönemin etkinlikleri, günlük devam takibi, katılım ve kayıt başına öğrenci raporu sistemde var; **ekranı yok, yalnızca API'de** (bkz. teknik dosya).

## Dikkat
- Kontenjan doluyken "Kayıtlı" kaydı reddedilir: "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın."
- Aynı öğrenciyi aynı döneme ikinci kez yazarsanız "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde." uyarısı çıkar.
- **Listeye Al** düğmesi yer yoksa "Kamp dönemi kontenjanı hâlâ dolu." der; önce başka bir kaydı iptal edin.
- **Engeller (hepsi ekranda sebebiyle yazar):** kontenjan, kayıtlı sayısının altına indirilemez; kayıtlı/bekleyen öğrencisi ya da etkinliği olan dönem silinemez; kayıtlı/bekleyen öğrencisi olan kamp silinemez; devam eden/başlamamış dönemlerinde kayıtlı ya da bekleyen öğrencisi olan kamp pasifleştirilemez; pasif kampa kayıt alınamaz; başlamamış dönem toplu tamamlanamaz; yalnızca kayıtlı öğrenci beklemeye alınır/tamamlanır.
- Kamp kaydı, kurumun normal öğrenci/program kaydı (Akış 4) değildir; ikisi ayrı tutulur.
- Menüde "Kamp Devam Takibi" ve "Kamp Raporları" satırları olabilir ama karşılarında çalışan bir ekran yok.

## Kendin dene
`demo_a`'da 1 kamp var (4 dönem, 4 etkinlik, 10 kayıt, biri bekleme listesinde).
1. `koordinator@demo-a.test` → `Kamp Yönetimi → Kamplar` → kampı açın; dönemleri ve eğitmenleri görün.
2. Dönemin kayıt ekranında bekleme listesindeki kayıtta **Listeye Al**'ı deneyin: yer yoksa uyarıyı, bir kaydı iptal edip tekrar deneyince başarıyı görün.
3. Aynı öğrenciyi aynı döneme yeniden kaydetmeyi deneyin.
4. "Haziran Dönemi (tamamlanacak)" döneminde **Dönemi Tamamla**'yı deneyin; "Eylül Hafta Sonu (taslak)" boş dönemini düzenleyip silin; kampı pasifleştirmeyi deneyin (bekleyen kayıt varken engel iletisini görün).

Teknik ayrıntılar: [teknik/akis_06_kamp.md](teknik/akis_06_kamp.md)
