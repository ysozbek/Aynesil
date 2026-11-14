# AyNesil — Müşteri Demo Senaryosu

Bu doküman müşteri sunumunu **adım adım** anlatır: hangi menüye girilecek, neye tıklanacak, müşteriye ne söylenecek.

Menü ve buton adları ekranda göründüğü gibi yazılmıştır (ör. **Planlama → Tekrar Eden Planlar**).

> **Önemli:** Tablolardaki bütün adımlar müşterinin önünde canlı yapılabilir. Ancak canlı adımlar hazır veriye dayanır; bu yüzden **bölüm 1'deki hazırlığı demodan bir gün önce** yapın.

---

## 0. Demodan önce: Sistemin tamamını anlatan tek resim

Ekranlara geçmeden önce bu resmi gösterin (tahtaya çizin ya da tek slayt yapın):

```
Aday  →  Öğrenci  →  Program (ne alıyor?)  →  Haftalık Plan  →  Seans
                                                                  ├─ Gelişim (BEP hedefleri)
                                                                  └─ Para (paketten seans hakkı düşer)
```

**Söylenecek:**
> "Sistemin tamamı bu beş kutu. Gördüğünüz her ekran bu kutulardan birinin ayrıntısı. Bugün size üç hikâye üzerinden bu kutuları göstereceğim."

---

## 1. Demo verisi hazırlığı (demodan 1 gün önce)

Aşağıdakileri sisteme önceden girin. Hiçbir ekran ID istemiyor; kişi ve kayıtlar adla aranıp seçiliyor.

**Hazırlık listesi:**

- [ ] **Kampüs:** 1 kampüs (Ayarlar tarafında, **Kampüsler**)
- [ ] **Odalar:** 2 oda (**Planlama → Odalar → Oda Ekle**)
- [ ] **Eğitimciler:** 3 eğitimci (**Eğitimciler → Eğitimci Ekle**)
- [ ] **Program:** "Erken Çocukluk Programı" (**Programlar → Program Ekle**), içinde 2 hizmet: "Konuşma Terapisi (45 dk, haftada 2)" ve "Duyu Bütünleme (60 dk, haftada 1)" (program detayı → **Hizmetler** sekmesi → **Hizmet Ekle**)
- [ ] **Veliler:** 3 veli (**Veliler → Veli Ekle**)
- [ ] **Öğrenciler:** 3 öğrenci (**Öğrenciler → Öğrenci Ekle**). Her birine veli bağlayın: öğrenci detayı → **Veliler** sekmesi → **Veli Bağla** (veli adla aranır; önce **Veliler → Veli Ekle** ile velileri oluşturun)
- [ ] **Adaylar:** 2 aday (**CRM → Adaylar → Aday Ekle**)
  - Biri **dönüştürülmüş** olsun: aday detayı → **Öğrenciye Dönüştür** → **Yeni öğrenci oluştur** (öğrenci ve veli aday bilgileriyle açılır)
  - Diğeri **yeni** kalsın. Bu aday canlı demoda kullanılacak.
- [ ] **Programa ekleme:** Öğrencileri programa ekleyin: **Programlar → Kayıtlar → Kayıt Oluştur**, öğrenci ve program listeden seçilir. Öğrencinin kaydı zaten varsa yeni kayıt açılmaz; program mevcut kayda eklenir
- [ ] **Seanslar:** Önümüzdeki hafta için 4–5 seans (**Planlama → Seanslar → Seans Oluştur**). Öğrenci, sorumlu eğitimci ve oda formda adıyla seçilir; ek katılımcılar seans detayındaki **Katılımcılar** sekmesinden eklenir
  - Biri **bugüne** denk gelsin. Canlı demoda yoklama bu seansta alınacak.
- [ ] **Hedefler:** Bir öğrenciye 2–3 hedef (**Hedefler → Öğrenci Hedefleri → Hedef Ekle**)
- [ ] **BEP:** Aynı öğrenci için bir BEP planı (**BEP / IEP → Plan Oluştur**), hedefleri ekleyin (önce **Hedefler → Öğrenci Hedefleri** ile öğrenciye hedef tanımlayın), onaylayın (**Onayla**, onaylayan eğitimci adla seçilir), sonra **Veliye Göster**
- [ ] **Paket ve seans hakkı:** **Finans → Paket Tanımları**'nda en az bir paket olsun (ör. "20 Seanslık Dil ve Konuşma") ve **demo seansının programına bağlı** olsun; yoksa tamamlanınca hak düşmez. Demo öğrencisine **Öğrenci Paketleri → Paket Ata** ile bu paketi atayın; canlı da gösterilebilir.
- [ ] **Telafi talebi:** 1 bekleyen telafi talebi
- [ ] **Veli kullanıcısı:** Bir veli için portal hesabı açın. Veli detayı → **Portal Erişimi** sekmesi → **Erişim Aç** ve tüm kutuları işaretleyin (Oturumlar, Devam, Raporlar, BEP/IEP, Finans)
- [ ] **Tarayıcı:** İki ayrı pencere hazırlayın. Birinde yönetici, diğerinde (gizli pencerede) veli oturumu açık olsun.

---

## 2. Hikâye 1 — "Yeni bir çocuk geldi" (≈10 dk)

**Sorunu tanımlayın:**
> "Şu an bir veli aradığında bilgileri bir deftere ya da Excel'e yazıyorsunuz. Kimin geri aranacağı, kimin kayıt olduğu kaybolabiliyor."

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Aday ekle | CRM → Adaylar → **Aday Ekle**. İletişim kişisi, çocuk adı, doğum tarihi, telefon, kaynak ve başvuru nedenini doldurun. **Kaydet**. | "Veli telefonu açtığı anda kaydı burada açıyoruz. Nereden geldiğini de seçiyoruz; böylece hangi reklamın işe yaradığını görebiliyorsunuz." |
| 2 | Görüşme kaydı | Aday detayı → **Aktiviteler** sekmesi → **Aktivite Kaydet**. Bir takip tarihi girin. | "Kim aradı, ne konuşuldu, ne zaman tekrar aranacak; hepsi burada. Takip tarihi gelince sistem hatırlatıyor." |
| 3 | Satış hunisi | CRM → **Pipeline**. | "Tüm adayları aşama aşama görüyorsunuz. Hangi aşamada kaç kişi var, bir bakışta görünüyor." |
| 4 | Öğrenciye dönüşüm | 1. adımda eklediğiniz aday → **Öğrenciye Dönüştür**. **Yeni öğrenci oluştur** seçili; bilgilerin adaydan geldiğini gösterin, "İletişim kişisini veli olarak ekle" işaretli kalsın → **Öğrenciyi Oluştur ve Dönüştür**. | "Veli kayıt olmaya karar verince tek tıkla öğrenci ve veli kaydı açılıyor; telefonda aldığımız bilgileri tekrar yazmıyoruz. Adayın geçmişi de kaybolmuyor." |
| 5 | Öğrenci kartı | Öğrenciler → öğrenciyi açın. **Genel Bakış**, **Veliler**, **Programlar** sekmelerini gezin. | "Çocuğun bütün dosyası tek yerde: veliler, acil iletişim, tanılar, raporlar." |
| 6 | Programa ekleme | Programlar → **Kayıtlar** → **Kayıt Oluştur**. Öğrenciyi ve programı listeden seçin. | "Kayıt, çocuğun kurumdaki dosyası; aldığı programlar bu dosyanın içinde duruyor. Çocuk ikinci bir programa başlarsa aynı yerden ekliyoruz, sistem yeni dosya açmıyor, mevcut kayda ekliyor." |

**Sonucu söyleyin:**
> "Telefondan kayda kadar her şey tek bir yerde ve kimse bir şeyi unutmuyor."

**İsteğe bağlı:** Değerlendirme → **Oturum Oluştur** artık aday ya da öğrenciyi adla seçtiriyor. Değerlendirme formu önceden tanımlıysa gösterilebilir; değilse şöyle geçin: "Değerlendirme formlarını da sisteme tanımlıyoruz, bir sonraki görüşmede detayına gireriz."

---

## 3. Hikâye 2 — "Bir haftanın işleyişi" (≈10 dk)

**Sorunu tanımlayın:**
> "Hangi çocuğun hangi gün hangi hocayla olduğu, kim gelmedi, kimin telafisi var; bunlar genelde WhatsApp'ta ve kâğıtta."

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Günün tablosu | Planlama → **Planlama Paneli**. "Bugünkü Seanslar" listesini gösterin. | "Sabah girdiğinizde bugün kimin kiminle hangi odada olduğunu görüyorsunuz." |
| 2 | Takvim | Planlama → **Takvim**. **Okul** görünümünde **Hafta**'yı seçin. | "Tüm kurumun haftası. Aynı odaya iki seans konamaz, sistem engelliyor." |
| 3 | Seansa gir | Bugünkü seansı açın. **Genel Bakış**, **Katılımcılar** sekmeleri. | "Eğitimci seansa girdiğinde öğrencisini ve bilgilerini burada görüyor." |
| 4 | Yoklama | **Devam** sekmesi → **Katıldı** / **Gelmedi** / **Geç**. | "Yoklama tek tıkla. Gelmeyen çocuk için telafi süreci buradan başlıyor." |
| 5 | Hedef işaretleme | **Hedefler** sekmesi → üstteki kutudan öğrencinin hedefini seçip **Hedef Ekle**, ardından çalışılan hedeflerin kutusunu işaretleyin. | "Bu seansta hangi hedefler üzerinde çalışıldığını işaretliyor. Gelişim raporu buradan oluşuyor." |
| 6 | Seans notu | **Notlar** sekmesi → **Not Ekle** → **Veliye Görünür** işaretli. | "Hocanın notu. İsterse veliye de gösteriyor; veli akşam telefondan okuyor." |
| 7 | Seansı tamamla | Önce yoklamada "Geldi" seçili olsun, sonra **Tamamla**. Öğrencinin yanında "1 seans hakkı düşüldü" çıkar. | "Seans tamamlanınca velinin paketinden bir seans hakkı otomatik düşüyor." |
| 8 | Telafi | Planlama → **Telafi Talepleri** → bekleyen talep → **Onayla**. | "Kaçırılan seansların telafisi kaybolmuyor; kim, ne zaman, onaylandı mı, hepsi kayıtlı." |

**Sonucu söyleyin:**
> "Hafta planı, yoklama, not, telafi; hepsi tek ekranda ve geriye dönük aranabilir."

**İsteğe bağlı gösterilebilir:**
- **Tekrar Eden Planlar:** Form öğrenci, program, hizmet, sorumlu eğitimci, kampüs ve oda seçtiriyor. "Seansları Oluştur" denince öğrenci, eğitimci ve hizmet seanslara otomatik ekleniyor; eğitimcinin ya da salonun dolu olduğu tarihler atlanıp sayısı gösteriliyor. Liste ekranında her plan "Ayşe · Dil ve Konuşma · Pınar Öğretmen · Sal, Per 10:00 · Oda 3" şeklinde okunur; müşteriye bu listeyi göstermek iyi bir giriş olur.
- **Takvim'in Eğitimci/Öğrenci görünümleri:** Kişi adla aranıyor; "Pınar Öğretmen'in haftası" gösterilebilir. **Ay** görünümü de var; ayın doluluğunu göstermek için kullanılabilir.

---

## 4. Hikâye 3 — "Kim ne görüyor?" (≈8 dk)

**Sorunu tanımlayın:**
> "Veliler sürekli 'çocuğum nasıl gidiyor, kaç seansım kaldı' diye arıyor. Yönetici de genel durumu görmek için herkesten ayrı rapor istiyor."

### 4a. Veli gözünden (gizli penceredeki veli oturumu)

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 1 | Veli anasayfası | **Veli Portalı → Anasayfa**. Yaklaşan Seans, Paket Bakiyesi ve Aktif Hedef kartlarını gösterin. | "Veli giriş yaptığında ilk bunu görüyor: kaç seansı kaldı, sıradaki seans ne zaman." |
| 2 | Çocuğun detayı | **Çocuklarım** → çocuğa tıklayın. Seanslar, Hedefler, Paketler, Belgeler. | "Hocanın 'veliye görünür' dediği notlar (Seanslar sekmesinde seansın altında; düzenlenmişse 'düzenlendi' yazar), onaylı BEP hedefleri ve ilerleme burada." |
| 3 | Gizlilik | **Söyleyin.** | "Veli yalnızca kendi çocuğunu görüyor. Neyi görebileceğine de kurum karar veriyor: finans, kamera, raporlar ayrı ayrı açılıp kapatılabiliyor." |

### 4b. Yönetici gözünden

| # | Ne yapılır | Nasıl | Söylenecek |
|---|---|---|---|
| 4 | BEP | **BEP / IEP** → hazır planı açın. **Hedefler**, **Onaylar**, **Revizyonlar** sekmeleri. | "BEP taslaktan onaya kadar takip ediliyor. Her değişiklik yeni sürüm oluyor, eskisi silinmiyor." |
| 5 | CRM raporu | CRM → **Raporlar** → tarih aralığı seçin. Dönüşüm oranını gösterin. | "Kaç aday geldi, kaçı kayıt oldu, hangi kaynaktan geldi." |
| 6 | Finans özeti | **Finans Paneli**. | "Gelir, satılan paketler, vadesi geçen faturalar tek ekranda." |

**Sonucu söyleyin:**
> "Veli aramadan bilgiye ulaşıyor, siz de herkese ayrı ayrı sormadan kurumun durumunu görüyorsunuz."

---

## 5. Kapanış (≈5 dk)

**Söylenecek:**
> "Bugün gördüğünüz, sistemin altyapısı ve ana akışları. Ekranları sizin günlük işinize göre sadeleştirmek istiyoruz. Bunun için size üç soru soracağım."

Müşteriye sorun ve cevapları not alın:

1. **Sistemi en çok kim kullanacak?** Sekreter mi, eğitimci mi, yönetici mi?
2. **Sabah sisteme girdiğinizde ilk ne görmek istersiniz?**
3. **Seans odasında tablet ya da telefon kullanılacak mı?**

Ayrıca müşterinin kullandığı kelimeleri yazın. "Kayıt", "seans hakkı", "haftalık program" gibi ifadeleri nasıl söylüyorlarsa, ekranlarda o kelimeleri kullanacağız.

---

## 6. Demoda yapılmaması gerekenler

- **Menüyü baştan sona gezmeyin.** Kamp, kamera, danışmanlık, KPI, izinler, toplantılar: "Bunlar ek modüller, ihtiyaca göre açılıyor" deyip geçin.
- **Teknik terim kullanmayın:** enrollment, student program, recurring schedule, GUID, tenant, RBAC demeyin.
