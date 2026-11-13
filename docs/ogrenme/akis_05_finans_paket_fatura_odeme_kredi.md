# Akış 5 — Paket satışı → fatura → ödeme → seans hakkı → iade ve iptal

## Bu akış ne anlatıyor?
Kurumun seans paketlerini tanımlamasını, öğrenciye satmasını, fatura kesip ödeme almasını ve öğrencinin "kaç seans hakkı kaldı" bilgisini (kredi) izlemesini anlatır. Para ve hak hareketleri hiç silinmez; hata olursa düzeltme kaydı atılır.

## Kim yapar?
Muhasebe ve Kurum Yöneticisi. Koordinatör Finans menüsünü görmez. Veli, izin verildiyse portalda çocuğunun paketlerini görür.

## Adımlar
1. **Paket tanımla:** `Finans → Paket Tanımları → Yeni`; ad, fiyat, seans sayısı ve **mutlaka bir program**. Program bağlı değilse seans yapılınca hak düşmez.
2. **Öğrenciye sat:** `Finans → Öğrenci Paketleri → Paket Ata`; öğrenciyi ve paketi seçin. Seans hakları öğrenciye yüklenir. Bu işlem ödeme almaz; ödeme ayrıca girilir.
3. **Fatura hazırla:** `Finans → Faturalar → Fatura Oluştur` (öğrenci, düzenleme ve vade tarihi; fatura taslak açılır); fatura sayfasında satırları ekleyin, sonra **Faturayı Kes**. Kesilince fatura numarası verilir ve değiştirilemez hale gelir.
4. **Ödemeyi gir:** `Finans → Ödemeler → Yeni Ödeme`; öğrenci, (varsa) fatura, yöntem ve tutar. Ödeme "Bekliyor" doğar; paranın geldiğini görünce **Onayla** deyin. Faturaya bağlıysa fatura otomatik "Kısmi Ödeme" ya da "Ödendi" olur.
5. **Seans hakkını izle:** `Finans → Kredi Defteri` her hareketi (Verildi, Tüketildi, İade, Düzeltme, Süresi Doldu) gösterir; kalan hak bu hareketlerin toplamıdır. Seans tamamlanınca hak kendiliğinden düşer (Akış 4). Elle işlem gerekirse **Kredi Ver** (bonus) ya da **Kredi Düzelt** kullanılır.
6. **Hata düzelt:**
   - Yanlış fatura: **İptal Et** (gerekçe yazılır) → "İptal Edildi".
   - Para iadesi: ödemenin sayfasında **İade Et**; tutar ve gerekçe girilir. Talep `Finans → İade Talepleri`nde "Bekliyor" görünür; yetkili **İşleme al** ya da **Başarısız işaretle** der. İadeler silinmez.
   - Paketten vazgeçme: **Paketi İptal Et**.
   - Yanlış hak hareketi: silinmez, **Kredi Düzelt** ile ters kayıt girilir.
7. **İndirim tanımla:** `Burslar` (yüzde ya da sabit tutar) ve `Promosyonlar` (kod, tarih aralığı, aktif/pasif).

## Dikkat
- Ödenmiş fatura doğrudan iptal edilemez: "Önce iade işlemi yapın." Kesilmiş faturayı iptal ederken gerekçe zorunludur.
- İade yalnızca onaylanmış ödeme için istenebilir ve kalan iade edilebilir tutarı aşamaz.
- **Paketi İptal Et** sadece paketi kapatır; kalan seans haklarını sıfırlamaz ya da para iadesi yapmaz. İadeyi ayrıca yapmalısınız.
- İşleme alınan iadeler ödeme tutarının tamamına ulaşırsa ödeme "İade Edildi" olur; başarısız işaretleme yalnızca iadenin durumunu değiştirir.
- Menüde "İadeler" ve "Finans Raporları" satırları kapalı; bu bilgileri sadece ilgili sayfalarda görebilirsiniz.

## Kendin dene
`demo_a`'da 3 paket tanımı, 8 öğrenci paketi, 8 fatura (taslak, kısmi, ödenmiş, iptal, kesilmiş), 6 ödeme ve 31 hak hareketi var.
1. `muhasebe@demo-a.test` → `Faturalar`: taslakta **Faturayı Kes**; ödenmiş faturada **İptal Et**'i deneyip engeli görün.
2. `Ödemeler`: bekleyen ödemede **Onayla**; bağlı faturanın durumunun değiştiğini izleyin. Onaylı ödemede fazla tutarla **İade Et** deneyin.
3. `Öğrenci Paketleri → Paket Ata`, sonra `Kredi Defteri`'nde yeni "Verildi" satırını görün.
4. `koordinator@demo-a.test` ile girip Finans menüsünün görünmediğini kontrol edin.

Teknik ayrıntılar: [teknik/akis_05_finans_paket_fatura_odeme_kredi.md](teknik/akis_05_finans_paket_fatura_odeme_kredi.md)
