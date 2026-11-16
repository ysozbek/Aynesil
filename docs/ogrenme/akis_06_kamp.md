# Akış 6 — Kamp: kamp → dönem → kayıt → etkinlik → katılım → yoklama → tamamlama → rapor

> **Modül anahtarı: `camps` (Kamp).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable camps` (Akış 16).

## Bu akış ne anlatıyor?
Yaz kampı gibi kısa süreli programların tanımlanmasını, her dönem için kontenjanlı kayıt alınmasını ve kontenjan dolunca bekleme listesi tutulmasını anlatır. Dönem düzenleme, kayıt durumu yönetimi, dönem sonu toplu tamamlama, eğitmen atama, kampı pasifleştirme/silme; ayrıca günlük işler — etkinlikler, etkinliğe katılım, günlük yoklama, kamp sonu öğrenci raporu ve kamp performans raporu — ekrandan yapılır.

## Kim yapar?
Koordinatör ve Kurum Yöneticisi. Eğitmen, kamp yoklaması ve etkinlik katılımı kaydı için yetkilendirilmiştir; bu ekranları kullanır.

## Adımlar
1. **Kamp oluştur:** `Kamp Yönetimi → Yeni Kamp`; kod, ad, tür, konum, kapasite, açıklama.
2. **Dönem ekle / düzenle / sil:** kampın sayfasında **Dönem Ekle**; dönem adı, başlangıç ve bitiş tarihi, kontenjan. Satırdaki **Düzenle** ve **Sil** ile değiştirin. Kamp birden fazla dönemden oluşabilir.
3. **Öğrenciyi kaydet:** dönemin kayıt ekranında **Kayıt Yap**; öğrenciyi seçin. Yer varsa "Kayıtlı", yoksa "Bekleme Listesi" durumuna alınır.
4. **Bekleme listesini yönet:** yer açılınca bekleyen kayıtta **Listeye Al** (kaydı "Kayıtlı" yapar). Kayıtlıyı beklemeye almak için **Bekleme Listesine Al**. Vazgeçen için **Kaydı İptal Et** ("Ayrıldı") yer açar.
5. **Durumu izle:** `Kamp Yönetimi` ana sayfasında toplam kamp, aktif kamp, toplam kayıt ve dönem sayıları; kampın sayfasında dönemler ve görevli eğitmenler görünür.
6. **Tamamlama:** satırdaki **Tamamla** tek kaydı, dönem sayfasındaki **Dönemi Tamamla** dönem sonunda tüm kayıtlı öğrencileri "Tamamlandı" yapar (bekleyenler etkilenmez).
7. **Eğitmen:** kamp sayfasında **Eğitmen Ata**; rol satırdan değişir, **Atamayı Kaldır** çıkarır.
8. **Kampı pasifleştir / etkinleştir / sil:** kamp listesi ya da kamp sayfasından.
9. **Etkinlik:** kamp sayfasında dönem satırından **Etkinlik ve Yoklama** → **Etkinlikler** → **Etkinlik Ekle** (ad, başlangıç/bitiş, yer, kontenjan); satırdan **Düzenle** / **Sil**.
10. **Katılım:** etkinlik satırında **Katılım**: dönemin öğrencileri listelenir. Satır başına durumu seçip **Kaydet** (tekil) ya da **Değişiklikleri Kaydet** (toplu); **Kaydı olmayanları "Katıldı" yap** toplu doldurur. Kaydı olan satırın durumunu değiştirmek düzeltmedir.
11. **Günlük yoklama:** aynı sayfada **Günlük Yoklama** sekmesi; günü seçin, öğrencilerin durumunu (geldi, gelmedi, geç geldi, mazeretli) seçip **Yoklamayı Kaydet** deyin. Yeni satırlar toplu girilir, o güne daha önce girilmiş olanlar düzeltilir. **Devam Özeti** sekmesi öğrenci başına gün sayısı ve devam yüzdesini verir.
12. **Kamp sonu raporu:** kayıt ekranında **tamamlanmış** satırdaki **Rapor** → önceki raporlar görünür, **özet** yazıp kaydedersiniz. Rapor yalnızca tamamlanmış kayıt için yazılır ve sonradan değiştirilmez.
13. **Performans:** `Kamp Yönetimi` panosunda **Performans Raporu**: kamp bazında dönem, kayıt, tamamlanan, ayrılan, tamamlama ve devam yüzdesi.

## Dikkat
- Kontenjan doluyken "Kayıtlı" kaydı reddedilir: "Kamp dönemi kontenjanı dolu. 'Bekleme listesi' durumunu kullanın."
- Aynı öğrenciyi aynı döneme ikinci kez yazarsanız "Öğrenci bu kamp döneminde zaten kayıtlı veya bekleme listesinde." uyarısı çıkar.
- **Listeye Al** düğmesi yer yoksa "Kamp dönemi kontenjanı hâlâ dolu." der; önce başka bir kaydı iptal edin.
- **Engeller (hepsi ekranda sebebiyle yazar):** kontenjan, kayıtlı sayısının altına indirilemez; kayıtlı/bekleyen öğrencisi ya da etkinliği olan dönem silinemez; kayıtlı/bekleyen öğrencisi olan kamp silinemez; devam eden/başlamamış dönemlerinde kayıtlı ya da bekleyen öğrencisi olan kamp pasifleştirilemez; pasif kampa kayıt alınamaz; başlamamış dönem toplu tamamlanamaz; yalnızca kayıtlı öğrenci beklemeye alınır/tamamlanır.
- Kamp kaydı, kurumun normal öğrenci/program kaydı (Akış 4) değildir; ikisi ayrı tutulur.
- **Günlük işlerin kuralları:** katılım ve yoklama yalnızca dönemin **kayıtlı** öğrencileri içindir; tamamlanmış, iptal edilmiş ya da bekleme listesindeki öğrenci için girilemez ve düzeltilemez ("… aktif olarak kayıtlı değil"). Bu yüzden yoklamayı ve katılımı **tamamlamadan önce** girin. Rapor yalnızca tamamlanmış kayıt için yazılır ("Kamp sonu raporu yalnızca tamamlanmış kayıt için yazılabilir."). Aynı gün için aynı öğrenciye ikinci yoklama girilemez (toplu giriş mevcut kaydı atlar); etkinlik kontenjanı dolunca yeni katılım reddedilir.
- Menüde "Kamp Devam Takibi" ve "Kamp Raporları" satırları olabilir ama ayrı ekrana bağlı değil; yoklama ve etkinlik dönem sayfasında, rapor kayıt ekranında, performans panoda.

## Kendin dene
`demo_a`'da 1 kamp var (4 dönem, 4 etkinlik, 10 kayıt, biri bekleme listesinde) ve **"Temmuz Dönemi" baştan sona işlenmiş**: etkinlik katılımı, üç günlük yoklama, tamamlama ve 3 rapor.
1. `koordinator@demo-a.test` → `Kamp Yönetimi → Kamplar` → kampı açın; dönemleri ve eğitmenleri görün.
2. "Temmuz Dönemi" satırında **Etkinlik ve Yoklama**: etkinlikleri, **Katılım**'ı, **Günlük Yoklama** (ilk gün) ve **Devam Özeti**'ni inceleyin. Katılım listesinde bir durumu değiştirip **Kaydet** (düzeltme).
3. Aynı dönemin **Kayıtlar** ekranında tamamlanmış satırların **Rapor** düğmesi: yazılmış raporları okuyun; raporu olmayan öğrenci için yenisini yazın.
4. "Haziran Dönemi (tamamlanacak)": **Günlük Yoklama**'da ilk iki günün girildiğini görün; **Kayıtlar → Dönemi Tamamla**'dan sonra aynı gün yoklamayı değiştirmeyi deneyin (engel iletisi).
5. Bekleme listesindeki kayıtta **Listeye Al**'ı, aynı öğrenciyi yeniden kaydetmeyi, boş "Eylül Hafta Sonu (taslak)" dönemini düzenleyip silmeyi ve kampı pasifleştirmeyi deneyin.
6. Panodan **Performans Raporu**'nu açın.

Teknik ayrıntılar: [teknik/akis_06_kamp.md](teknik/akis_06_kamp.md)
