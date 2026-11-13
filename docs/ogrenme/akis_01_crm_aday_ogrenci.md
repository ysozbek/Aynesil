# Akış 1 — Aday → görüşme → öğrenciye dönüşüm

## Bu akış ne anlatıyor?
Kuruma başvuran bir çocuğun önce "aday" olarak kaydedildiğini, aileyle yapılan arama ve görüşmelerin takip edildiğini, aile karar verince adayın öğrenci kaydına dönüştüğünü anlatır. Amaç, kimsenin unutulmaması ve kaç başvurunun gerçekten öğrenci olduğunu görebilmek.

## Kim yapar?
Kurum Yöneticisi ve Koordinatör (satış/kayıt tarafı). Eğitmen, Muhasebe ve Veli bu bölümü görmez.

## Adımlar
1. **Aday kaydet:** `CRM → Adaylar → Aday Ekle`. İletişim kişisini, telefon/e-postayı, çocuğun adını ve doğum tarihini, başvuru nedenini ve nereden geldiğini (kaynak) yazın. Böylece aile kaybolmaz.
2. **Aktivite kaydet:** aday sayfasında **Aktiviteler** sekmesi → **Aktivite Kaydet**. Aradığınız ya da yazdığınız her şeyi ve gerekiyorsa "takip tarihi"ni girin. Takibi olan adaylar `CRM → Aktiviteler` ve CRM ana sayfasındaki "Yaklaşan Takipler"de görünür; unutulanlar "Süresi Geçmiş" olur.
3. **Durumu ilerlet:** aday sayfasında **Durum Değiştir**. Durum ve istersen pipeline aşaması seçilir. `CRM → Pipeline` ekranı adayları aşamalara göre sütun sütun gösterir.
4. **Görüşme planla:** **Görüşmeler** sekmesi → **Görüşme Planla** (kampüs ve tarih). Geçmiş bir tarih seçemezsiniz.
5. **Görüşmeyi tamamla:** görüşme yapıldıktan sonra **Görüşmeyi Tamamla**; sonucu ve önerinizi yazın. Karar kaydı olarak kalır.
6. **Öğrenciye dönüştür:** aday sayfasında **Öğrenciye Dönüştür**. İki seçenek var:
   - *Yeni öğrenci oluştur:* bilgiler adaydan dolu gelir. "İletişim kişisini veli olarak ekle"yi işaretlerseniz veli de açılır ve öğrencinin birincil velisi ve mali sorumlusu olur. **Öğrenciyi Oluştur ve Dönüştür** ile hepsi tek seferde biter.
   - *Var olan öğrenciye bağla:* öğrenci kaydı zaten açıldıysa adıyla arayıp seçin ve **Dönüştür**'e basın.
7. **Sonuçlara bak:** `CRM → Raporlar`'da tarih aralığı seçip kaç aday geldiğini, kaçının öğrenci olduğunu ve hangi kaynaktan geldiklerini görürsünüz.

## Dikkat
- Bir aday yalnızca bir kez dönüştürülebilir; tekrar denerseniz "Aday zaten öğrenciye dönüştürülmüş." uyarısı çıkar.
- Dönüşüm yarıda kalırsa (öğrenci oluştu ama aday bağlanamadı) ekran öğrenciyi seçili bırakır. Yeniden "Yeni öğrenci" demeyin; mevcut öğrenciyle **Dönüştür**'e basın, yoksa aynı çocuk için iki kayıt olur.
- Tamamlanmış bir görüşme iptal edilemez, iptal edilmiş görüşme de tamamlanamaz. Planlı görüşmede **Tamamla**, **Ertele** (yeni görüşme zamanı), **Gelmedi** ve **İptal** düğmeleri vardır.
- Dönüşüm velinin portal girişini açmaz; bu, veli kartından ayrıca yapılır (Akış 2).

## Kendin dene
`koordinator@demo-a.test` ile girin (şifre `DemoSifre!2026`). `CRM → Adaylar`'da 20 aday var, 4'ü zaten öğrenciye dönüşmüş.
1. Dönüşmemiş bir adaya aktivite kaydedin (takip tarihi yarın olsun).
2. Ona ileri tarihli görüşme planlayıp tamamlayın.
3. **Öğrenciye Dönüştür** → "Yeni öğrenci" + "veli olarak ekle" → `Öğrenciler`'de yeni öğrenciyi ve velisini bulun.
4. Dönüşmüş bir adayda yeniden dönüştürmeyi deneyip uyarıyı görün.

Teknik ayrıntılar: [teknik/akis_01_crm_aday_ogrenci.md](teknik/akis_01_crm_aday_ogrenci.md)
