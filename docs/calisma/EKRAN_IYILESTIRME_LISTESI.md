# AyNesil — Ekran İyileştirme Listesi

Bu liste ekranların neden karışık geldiğini somut olarak gösterir. Kaynak: `frontend/aynesil-web/src/views/` altındaki ekranlar ve `src/i18n/locales/tr.ts` içindeki Türkçe metinler.

Üç başlık var:

1. **Çalışmayan veya eksik akışlar:** Kullanıcı işini bitiremiyor.
2. **ID isteyen ekranlar:** Kullanıcıdan isim seçmesi yerine uzun bir kod yazması isteniyor.
3. **Kafa karıştıran terimler:** Aynı kelime farklı anlamlarda ya da aynı şey farklı kelimelerle anılıyor.

Öncelik sırası da bu. Önce 1, sonra 2, sonra 3.

---

## 1. Çalışmayan veya eksik akışlar

| # | Ekran | Sorun | Etkisi |
|---|---|---|---|
| 1.1 | **Finans → Öğrenci Paketleri → Paket Ata** | **Düzeltildi.** Paket Ata sayfası eklendi: öğrenci ve paket listeden seçiliyor; kredi, fiyat ve son kullanım tarihi paketten doluyor. Kaydedince paketteki kredi otomatik yükleniyor. Aynı ekranda düzeltilenler: Paket Ata ve İptal butonları yanlış yetki adına baktığı için hiç görünmüyordu; "Paket" sütunu boş geliyordu; iptal isteği backend'in beklemediği veriyle gidiyordu; öğrenci filtresi ID yerine listeden seçiliyor. | Paket detay sayfası yok; satıra tıklayınca açılan bir şey olmadığı için tıklama kaldırıldı. Paket atamak ödeme kaydı oluşturmaz, ödeme ayrıca girilir. |
| 1.2 | **Planlama → Tekrar Eden Planlar → Plan Oluştur** | **Düzeltildi (V35).** Formda öğrenci, öğrencinin aldığı program, o programın hizmeti, sorumlu eğitimci (isteğe bağlı ek eğitimciler), kampüs ve oda listeden seçiliyor. Plandan seans üretilirken öğrenci katılımcı olarak, eğitimciler ve hizmet de seansa otomatik ekleniyor. Salon ya da eğitimci o saatte doluysa o tarih atlanıyor ve sonuç ekranında kaç tarihin neden atlandığı Türkçe yazılıyor. Hizmet öğrencinin programına ait değilse ya da öğrencinin programı aktif değilse işlem reddediliyor. | Atlanan tarihlerin hangi günler olduğu listelenmiyor, yalnızca sayısı gösteriliyor. |
| 1.3 | **Tekrar Eden Planlar → Sıklık** | **Düzeltildi.** "Günlük" seçeneği kaldırıldı (veritabanı kabul etmiyordu). "2 Haftada Bir" seçeneği eklendi. | — |
| 1.7 | **Planlama → Tekrar Eden Planlar** (liste) | **Düzeltildi.** Backend liste cevabı artık adları da gönderiyor. Liste şu sütunlarla yeniden düzenlendi: Öğrenci (altında program · hizmet), Sorumlu Eğitimci, Gün/Saat (ör. "Sal, Per · 10:00"), Yer (oda, altında kampüs), Seans Türü, Geçerlilik, Oluşturulan seans sayısı, Durum. Sütun başlığında görünen anahtar metni hatası da giderildi. | — |
| 1.4 | **CRM → Aday → Öğrenciye Dönüştür** | **Düzeltildi.** Ekranda iki seçenek var: **Yeni öğrenci oluştur** (varsayılan) ve **Var olan öğrenciye bağla**. Yeni öğrencide ad, soyad ve doğum tarihi adaydan doluyor; kampüs ve başvuru nedeni aktarılıyor. İsteğe bağlı olarak iletişim kişisi veli olarak oluşturulup birincil veli ve mali sorumlu olarak bağlanıyor. Tek tıkla öğrenci açılıyor, veli bağlanıyor ve aday dönüştürülüyor. | Adımlar ayrı istekler, tek işlem değil. Ortada bir adım hata verirse açılan öğrenci kaybolmuyor: ekran "Var olan öğrenciye bağla"ya geçiyor ve o öğrenciyi seçili getiriyor, tekrar denemek ikinci bir öğrenci açmıyor. |
| 1.5 | **Seans → Hedefler sekmesi** | **Düzeltildi.** Sekmenin üstünde hedef seçme kutusu ve **Hedef Ekle** butonu var. Listede seanstaki öğrencilerin henüz seansa eklenmemiş aktif hedefleri çıkıyor; grup seansında hedefin başında öğrenci adı yazıyor. Eklenen hedef "çalışıldı" olarak işaretli gelir. Öğrencinin aktif hedefi yoksa "Öğrenciye hedef tanımla" bağlantısı çıkıyor. | Seansta öğrenci yoksa önce Katılımcılar sekmesinden öğrenci eklenmeli. |
| 1.6 | **Takvim → Ay görünümü** | **Düzeltildi.** Ay görünümü Pazartesi'den başlayan haftalarla geliyor; her günde ilk 3 seans, fazlası için "+N daha" yazıyor. Gün numarasına tıklayınca o günün Gün görünümü açılıyor. Aynı düzeltmede: Gün görünümü hiç seans getirmiyordu ve Hafta görünümü son günü göstermiyordu (bitiş tarihi backend'e dahil edilmeyecek şekilde gidiyordu); seanslar UTC saatine göre yerleştirildiği için Türkiye'de 3 saat erken satırda görünüyordu. Hafta artık Pazartesi başlıyor. | Gün ve Hafta görünümleri 07:00–20:00 arasını gösteriyor; bu saatlerin dışındaki seanslar yalnızca Ay ve Ajanda görünümünde görünür. |
| 1.8 | **Finans → Faturalar** (liste, detay) ve **Finans Paneli** | **Düzeltildi.** Backend fatura listesi ve detayında artık ödenen ve kalan tutarı da gönderiyor; ekran toplam tutarı backend'deki adıyla (`grandTotal`) okuyor. Aynı ekranda düzeltilenler: tarih filtresi backend'e yanlış adla gittiği için çalışmıyordu; "Vadesi Geçmiş" filtresi ve Finans Paneli'ndeki vadesi geçen faturalar kutusu hiç sonuç getirmiyordu (artık vadesi geçmiş kesilmiş/kısmi ödenmiş faturaları listeliyor); paneldeki "Tümünü Gör" bağlantısı listeyi filtresiz açıyordu. | — |
| 1.9 | **Programlar → Kayıtlar → Kayıt Oluştur** | **Düzeltildi.** Bu ekran hem kuruma kayıt dosyası açıyor hem öğrenciyi programa ekliyordu; ikinci programı eklemek için kullanılınca öğrenciye ikinci bir kayıt dosyası açılıyordu. Artık öğrenci seçilince açık kaydı aranıyor: varsa formda "aktif kaydı var, program bu kayda eklenecek" uyarısı çıkıyor, kampüs/durum/kayıt tarihi alanları gizleniyor ve program mevcut kayda ekleniyor. Sayfa açıklaması da "programa eklemek için Kayıt Oluştur'u kullanın" diyor. | Menü adı hâlâ "Kayıtlar"; ad değişikliği terim kararıyla birlikte yapılacak (3.1 "Kayıt"). |

---

## 2. ID isteyen ekranlar

Bu ekranlarda kullanıcıdan isim listesinden seçim yapması yerine bir **ID (UUID)** yazması bekleniyor. Normal bir kullanıcı ID bilmez. Ekranların "karışık" ve "boş" görünmesinin en büyük sebebi bu.

**Önerilen çözüm:** Hepsi için tek bir ortak bileşen yapılsın. Yazdıkça isim önerisi getiren ve seçince ID'yi arka planda tutan bir arama kutusu olsun (ör. `StudentPicker`, `GuardianPicker`, `EducatorPicker`, `RoomPicker`). Kayıtlar ekranındaki (`EnrollmentListView.vue`) öğrenci ve program seçimi zaten listeden yapılıyor; o desen örnek alınabilir.

**Durum:** Bölüm 2 tamamlandı. Ortak bileşenler `components/pickers/` altında: `EntityPicker`, `StudentPicker`, `EducatorPicker`, `GuardianPicker`, `RoomPicker`, `LeadPicker`. Yazdıkça isim önerisi getiriyorlar. Listedeki 15 ekranın hiçbiri artık ID istemiyor.

| # | Ekran | İstenen ID | Dosya | Durum |
|---|---|---|---|---|
| 2.1 | CRM → Aday → **Öğrenciye Dönüştür** | Öğrenci | `crm/LeadConvertView.vue` | Düzeltildi. Kayıt yoksa "Yeni öğrenci oluştur" bağlantısı var. |
| 2.2 | Değerlendirme → **Oturum Oluştur** | Aday veya Öğrenci | `assessment/SessionFormView.vue` | Düzeltildi. Aday ya da öğrenci adla aranıyor; biri seçilince diğeri kapanıyor. Adaylarda yalnızca dönüştürülmemiş olanlar listeleniyor. |
| 2.3 | Öğrenci detayı → Veliler → **Veli Bağla** | Veli | `students/StudentDetailView.vue` | Düzeltildi. Veli listede yoksa "Yeni veli oluştur" bağlantısı var. |
| 2.4 | Planlama → **Seans Oluştur** → Oda | Oda | `scheduling/SessionFormView.vue` | Düzeltildi. Kampüs, oda, öğrenci ve sorumlu eğitimci seçiliyor. Form önceden backend'in beklediği alanları göndermediği için kayıt yapamıyordu; o da düzeltildi. |
| 2.5 | Seans detayı → **Katılımcılar** | Öğrenci ve Eğitimci | `scheduling/SessionDetailView.vue` | Düzeltildi |
| 2.6 | Planlama → **Takvim** (Kampüs / Eğitimci / Öğrenci görünümü) | Kampüs, Eğitimci veya Öğrenci | `scheduling/CalendarView.vue` | Düzeltildi. Seçince takvim kendiliğinden yükleniyor. |
| 2.7 | Planlama → **Devam Paneli** | Öğrenci | `scheduling/AttendanceDashboardView.vue` | Düzeltildi |
| 2.8 | Finans → **Kredi Defteri** | Öğrenci | `finance/CreditLedgerView.vue` | Düzeltildi. Kredi Ver / Düzelt pencerelerinde paket de öğrencinin aktif paketlerinden seçiliyor. |
| 2.9 | Finans → **Öğrenci Paketleri** (filtre) | Öğrenci | `finance/StudentPackageListView.vue` | Düzeltildi (madde 1.1 ile) |
| 2.10 | Finans → **Ödeme Ekle** | Fatura | `finance/PaymentFormView.vue` | Düzeltildi. Önce öğrenci seçiliyor, fatura o öğrencinin iptal edilmemiş faturalarından seçiliyor. |
| 2.11 | Hedefler → **Hedef Paneli** | Öğrenci | `goals/GoalDashboardView.vue` | Düzeltildi |
| 2.12 | Hedefler → **Öğrenci Hedefi** formu | Şablon, Üst Hedef | `goals/StudentGoalFormView.vue` | Düzeltildi. Öğrenci adla aranıyor. Şablon hedef kütüphanesinde aranıyor ve seçilince hedef ifadesi, kategori ve gelişim alanı boşsa dolduruluyor. Üst hedef yalnızca kısa vadeli hedeflerde, öğrencinin uzun vadeli hedeflerinden seçiliyor. |
| 2.13 | BEP → **Onayla / Reddet**, **Hedef Ekle**, **İnceleme Ekle** | Onaylayan, İnceleyen, Öğrenci Hedefi | `bep/EducationPlanDetailView.vue` | Düzeltildi. Onaylayan ve inceleyen eğitimci adla aranıyor (veritabanında ikisi de eğitimci kaydına bağlı). Hedef Ekle'de öğrencinin plana henüz eklenmemiş hedefleri listeleniyor ve vade hedeften dolduruluyor; hedef yoksa "Hedef Ekle" sayfasına bağlantı var. |
| 2.14 | Kameralar → **Yetkiler** | Veli ve Öğrenci | `cameras/ViewingAuthorizationsView.vue` | Düzeltildi |
| 2.15 | Kamera detayı → Oda | Oda | `cameras/CameraDetailView.vue` | Düzeltildi. Kameranın kampüsü varsa yalnızca o kampüsün odaları listeleniyor. |

**Ek not:** 2.7, 2.8 ve 2.11 numaralı ekranlar artık açılışta bir öğrenci arama kutusu gösteriyor; öğrenci seçilince veriler kendiliğinden yükleniyor. Üçü de adres satırında `?studentId=...` kabul ediyor, böylece öğrenci detayından doğrudan bu ekranlara bağlantı verilebilir.

---

## 3. Kafa karıştıran terimler

### 3.1 Aynı kelime, farklı anlamlar

| Kelime | Şu an kullanıldığı yerler | Öneri |
|---|---|---|
| **Kayıt** | Kuruma kayıt ("Kayıtlar", "Kayıt Oluştur") · Kampüse kayıt ("Kampüs Kayıtları") · Kampa kayıt ("Kamp Kayıtları") · Genel veri satırı ("Kayıt bulunamadı", "Kayıt Bilgileri", "Erişim Kayıtları", "İlerleme Kayıtları") | Kuruma kayıt → **Kurum Kaydı** · Kampüse kayıt → **Kampüs Ataması** · Kampa kayıt → **Kamp Katılımı** · "Kayıt bulunamadı" → **Sonuç bulunamadı** · "Kayıt Bilgileri" → **Oluşturma Bilgileri** · "Erişim Kayıtları" → **Erişim Geçmişi** · "İlerleme Kayıtları" → **İlerleme Ölçümleri** |
| **Plan** | BEP planı · Tekrar Eden Planlar · İki ekranda da aynı "Plan Oluştur" butonu | BEP → **BEP** (veya "Eğitim Planı") · Tekrar Eden Planlar → **Haftalık Seans Düzeni** |
| **Şablon** | Değerlendirme, hedef, bildirim ve sözleşme şablonları. Menüde tek başına "Şablonlar" yazıyor. | Menüde her zaman neyin şablonu olduğu yazılsın: **Değerlendirme Şablonları**, **Hedef Şablonları** |
| **Takip** | CRM'de "Bekleyen Takip" (aranacak aday) · Toplantılarda "Takip Görevleri" | CRM → **Geri Arama** · Toplantı → **Toplantı Görevleri** |

### 3.2 Aynı şey, farklı kelimeler

| Kavram | Şu an kullanılan kelimeler | Öneri (tek kelime) |
|---|---|---|
| Terapi/eğitim buluşması | "Seans" (Planlama) · "Oturum" (Öğrenci Paneli'nde "Yaklaşan Oturumlar", Veli Portal Erişimi'nde "Oturumlar") | **Seans**. Değerlendirme için de **Değerlendirme Seansı** ya da yalnızca **Değerlendirme** |
| Öğretmen/terapist | "Eğitimci" (formlar, V8 menü kaydı "Eğitimciler") · "Eğitmen" (V11 menü kayıtları "Eğitmenler", "Eğitmen Listesi"; İzin modülü: "Eğitmen izin talepleri", "Eğitmen adı ara") | Tek bir kelime seçilsin. Müşteriye sorun. Karar verilince menü kayıtları (migration), `tr.ts` ve dokümanlar birlikte güncellenmeli. Kullanım kılavuzu şimdilik "Eğitimci" kullanıyor. |
| Yoklama | "Devam Takibi" (menü) · "Devam" (sekme) · "Devam Kaydı" · "Devam Paneli" | **Yoklama** |
| Ana ekran | "Kontrol Paneli" · "… Paneli" · "Anasayfa" (veli) | Yönetim tarafında **Anasayfa**, modüllerde **Özet** |
| Satış süreci | Menüde "Pipeline", sayfa başlığında "Satış Hunisi" | **Aday Süreci** |
| Program ataması | "Program Ata" · "Atanmış Programlar" · "Aktif Öğrenci Programı" · öğrenci sekmesinde "Programlar" | Her yerde **Aldığı Programlar** / **Program Ekle** |

### 3.3 Müşterinin anlamayacağı teknik veya çeviri kokan kelimeler

| Şu an | Öneri |
|---|---|
| Kredi · Kredi Defteri · Kredi Ver · Kredi Düzelt | **Seans Hakkı** · **Seans Hakkı Hareketleri** · **Hak Tanımla** · **Hak Düzelt** |
| Ustalık Kriteri | **Başarı Ölçütü** |
| Vade (Kısa / Uzun Vadeli) | **Süre** (Kısa / Uzun Vadeli) |
| Puanlama Modeli: "Toplam (sum)", "Ortalama (average)" | Parantez içindeki İngilizce kaldırılsın |
| Gateway Referansı | **Banka İşlem No** |
| Öğrenci GUID, UUID, … ID | Hiçbir ekranda görünmesin (bkz. Bölüm 2) |
| Ek Ayarlar (JSON), Özellik Bayrağı, Rota | Yalnızca sistem yöneticisi ekranlarında kalsın, son kullanıcıya gösterilmesin |

> **Not:** Terimleri değiştirmeden önce müşteriye sorun. Hangi kelimeyi kullanıyorlarsa ("seans hakkı" mı, "ders hakkı" mı?) o kelime seçilsin. Metinlerin tamamı `tr.ts` dosyasında ve backend'deki çeviri kataloğunda (`ref.i18n_message`) duruyor. Kod mantığına dokunmadan değiştirilebilir.

---

## 4. Önerilen sıra

1. **Akışları tamamla (Bölüm 1):** 1.1, 1.2, 1.3, 1.5, 1.7 ve 1.8 tamamlandı. Bölüm 1'in tamamı bitti.
2. **Ortak seçici bileşenler (Bölüm 2):** Tamamlandı.
3. **Terim düzeltmeleri (Bölüm 3):** Müşteri demosundan sonra, müşterinin kelimeleriyle tek seferde.
4. **Büyük ekranları bölmek:** `StudentDetailView.vue` 1852 satır. Sekme başına ayrı bileşene bölünmeli. Bu, yukarıdakiler bittikten sonra yapılmalı.
