# Akış 5 — Paket satışı → fatura → ödeme → kredi düşümü → iade/iptal

**Ne işe yarar:** Seans paketi tanımlamak ve öğrenciye satmak, faturalamak, tahsilatı kaydetmek, seans hakkını (kredi) izlemek; iade, iptal ve düzeltme yapmak.

**Kim yapar:** Muhasebe (`finance` rolü) ve yönetici. Demo muhasebe rolü: `finance*`, `invoice*`, `refund*`, `credit_ledger*`, `package_definition*`, `student_package*`, `scholarship*`, `promotion*`, `discount*` + öğrenci/veli/kampüs/kayıt/program okuma. Koordinatör **finansı göremez** (demo rol kuralı bu önekleri dışlar).

**Menü:** router `/finance` (menü kodu `payments`, V27 ile rota `/finance`, etiket "Finans"); alt: `payments-packages` (Öğrenci Paketleri), `payments-definitions` (Paket Tanımları), `payments-invoices`, `payments-transactions` (Ödemeler), `payments-credits` (Kredi Defteri), `payments-scholarships`, `payments-promotions`. `payments-refunds` menüsü V51 ile `/finance/refunds` rotasına bağlanıp **yeniden açıldı**; `payments-reports` **pasif** (V31): finans raporları için ayrı ekran yok.

## Durumlar

| Varlık | Geçişler |
|---|---|
| Paket tanımı | aktif ↔ pasif (**Aktifleştir/Pasifleştir**); pasif tanım satılamaz |
| Öğrenci paketi (`StudentPackageStatus`) | `active` → `exhausted` (kredi bitince, otomatik) · `expired` (süre) · `cancelled` (**Paketi İptal Et**; `exhausted`/`expired` iptal edilemez) |
| Fatura (`InvoiceStatus`) | `draft` → **Faturayı Kes** → `issued` → `partial` → `paid` ; `overdue` (gecikme) ; `void` (**İptal Et**, gerekçe zorunlu) |
| Ödeme (`PaymentStatus`) | `pending` → (`authorized`) → `captured` (**Onayla**) · `failed` · `refunded` |
| İade | `pending` → `processed` / `failed` |
| Kredi hareketi (`CreditEntryType`) | `grant` (+), `consume` (−), `refund` (+), `adjustment` (±), `expire` (−) — **append-only defter**, bakiye = hareket toplamı |

## Adım adım

1. **Paket tanımı:** `Finans → Paket Tanımları → yeni`: ad, **program (zorunlu bağlayın)**, toplam seans hakkı, fiyat, geçerlilik. Program bağı yoksa seans tamamlanınca kredi düşmez (Akış 4).
2. **Paket ata/sat:** `Öğrenci Paketleri → Paket Ata` (`student_package:purchase`): öğrenci + paket tanımı; hak sayısı/fiyat/son kullanım tanımdan gelir. Kaydedilince defterde başlangıç `grant` hareketi yazılır. Pasif tanım: "'{0}' paket tanımı pasif olduğu için satın alınamaz." **Paket atamak ödeme almaz.**
3. **Fatura:** `Faturalar → yeni` (`invoice:create`) → **satır ekle / kaldır** (`invoice:update`) → **Faturayı Kes** (onay: "Bu faturayı kesmek istediğinize emin misiniz?"). Numara kesilirken atanır; yalnızca taslak fatura kesilebilir/numara alır.
4. **Ödeme kaydı:** `Ödemeler → Yeni Ödeme` (`finance_payment:record`): öğrenci, (opsiyonel) fatura, yöntem, tutar → `pending` doğar. **Onayla** (`:capture`) → `captured`. Faturaya bağlıysa onayda otomatik: toplam tahsilat ≥ fatura toplamı ise `paid`, değilse `partial`. (`transactions/{id}/fail` ucu API'de; ekranda yok — ödeme sağlayıcısı bağlanınca anlamlı, [ekransız-endpointler](../ekransiz-endpointler.md) B grubu.)
5. **Kredi düşümü:** normalde otomatik — seans tamamlanınca (Akış 4). Elle: `Kredi Defteri → Kredi Düzelt` (adjustment, sıfır olamaz) / **Kredi Ver** (bonus grant). Öğrenci başına özet `GET credits/summary/{studentId}`. `credits/consume` ve `credits/refund` uçları API'de; ekran yalnızca *Ver* ve *Düzelt*'i sunuyor.
6. **İptal/iade senaryoları**
   - *Yanlış fatura:* **İptal Et** + gerekçe → `void`. Ödenmiş fatura doğrudan iptal edilemez ("Ödenmiş faturalar doğrudan geçersiz kılınamaz. Önce iade işlemi yapın.").
   - *Para iadesi:* ödeme detayı → **İade Et** (`refund:request`; tutar + gerekçe). Yalnızca `captured` ödeme için; kalan iade edilebilir tutarı aşamaz. İadeyi **işleme almak** (`POST refunds/{id}/process`, `refund:process`) ve **başarısız işaretlemek** (`…/fail`) `Finans → İade Talepleri` (`RefundListView`, `/finance/refunds`) ekranındadır; yalnızca `pending` iadede düğme görünür. Tam tutar işlenince ödeme `refunded` olur.
   - *Paket iptali:* **Paketi İptal Et** yalnızca durumu `cancelled` yapar; **kalan krediyi otomatik iade etmez/sıfırlamaz** (defterde hareket yazılmaz). Paket aktif olmadığı için artık düşüm yapmaz. Süresi dolan pakette kalan kredi için `expire` hareketi yazan komut var (`ExpireStudentPackageCommand`); onu tetikleyen uç/zamanlayıcı [doğrulanacak].
   - *Yanlış kredi:* silinmez; **Kredi Düzelt** ile ters hareket.
7. **Burs / promosyon / indirim:** `Burslar` (`scholarship:grant|update`; yüzde **veya** sabit tutar, ikisi birden değil), `Promosyonlar` (kod, yüzde/tutar, tarih aralığı; aktif/pasif; `validate` ucu). `discounts` (indirim uygula) API'de.
8. **Raporlar:** `GET /api/payments/reports/revenue|packages|credit-usage` (`finance_report:read`); menü pasif olduğundan [doğrulanacak: Finans panosu bu verileri gösteriyor mu — bkz. Akış 10].

## Dikkat edilecek kurallar ve hata mesajları

| Durum | Mesaj |
|---|---|
| Taslak olmayanı kes | "Yalnızca taslak faturalar düzenlenebilir." |
| Ödenmiş faturayı iptal | "Ödenmiş faturalar doğrudan geçersiz kılınamaz. Önce iade işlemi yapın." |
| İptal gerekçesiz | "{0} zorunludur." (alan: gerekçe; V46 `invoice_void_reason`) |
| Zaten iptal / iptal edilmiş fatura değişimi | "Fatura zaten geçersiz kılınmış." / "Geçersiz kılınmış bir faturanın durumu değiştirilemez." |
| Tam ödenmiş faturayı değiştir | "Tamamen ödenmiş bir faturanın durumu yeniden değiştirilemez." |
| Gecikme | "Yalnızca düzenlenmiş veya kısmen ödenmiş faturalar gecikmiş olabilir." |
| Ödeme durumları | "Yalnızca bekleyen veya yetkilendirilmiş ödemeler tahsil edilebilir." / "'{0}' durumundaki bir ödeme başarısız olarak işaretlenemez." / "Yalnızca tahsil edilmiş ödemeler iade edildi olarak işaretlenebilir." |
| İade | "İade yalnızca tahsil edilmiş ödemeler için talep edilebilir." / "İade tutarı ({0}), kalan iade edilebilir tutarı ({1}) aşıyor." / "Yalnızca bekleyen iadeler işleme alınabilir / başarısız işaretlenebilir." |
| Paket | "'{0}' durumundaki bir paket iptal edilemez." / "Paket '{0}' durumunda. Yalnızca aktif paketlerde kredi işlemi yapılabilir." |
| Sayısal | "{0} pozitif olmalıdır." / "{0} negatif olamaz." / "{0} sıfır olamaz." / "{0} %100'ü aşamaz." / "Yüzde veya sabit tutardan yalnızca biri belirtilmelidir." |

- **Finansal kayıtlar silinmez** (ödeme, fatura, kredi, iade); düzeltme = ters/ayar kaydı. Faturaya silme ucu yok; `void` kullanılır.
- Kredi bakiyesi bir sütun değil, `credit_ledger.delta` toplamıdır.
- Fatura iptal gerekçesi eksikse API kodlu istisna döner (`InvalidArgumentException`; son commit `eacfd1c`).

## Koddaki yeri

- Vue: `views/finance/*` (`FinanceDashboardView`, `PackageDefinition*`, `StudentPackage*`, `Invoice*`, `Payment*`, `CreditLedgerView`, `ScholarshipListView`, `PromotionListView`)
- Store/servis: `stores/package.store.ts`, `invoice.store.ts`, `payment.store.ts`, `creditLedger.store.ts`; `services/finance.service.ts`
- Controller: `PaymentsController.cs` (rota `api/payments/*`; dikkat: ekran adı Finans, API adı payments)
- Command/Query: `Application/Features/Finance/Commands/` (`PackageDefinition`, `StudentPackage`, `Invoice`, `Payment`, `Refund`, `CreditLedger`, `Discount`, `Scholarship`, `Promotion`), `Features/Scheduling/SessionCreditConsumer.cs`
- Domain: `Domain/Modules/Finance/Entities/`
- DB: `finance.package_definition`, `student_package`, `credit_ledger`, `invoice`, `invoice_line`, `payment`, `refund`, `discount`, `scholarship`, `promotion`

## Kendin dene (demo)

`demo_a`: 3 paket tanımı, 8 öğrenci paketi, 8 fatura (taslak/kısmi/ödenmiş/iptal/kesilmiş), 6 ödeme (1 bekleyen), 31 kredi hareketi, 1 burs, 1 promosyon.
1. `muhasebe@demo-a.test` → `Faturalar`: taslak faturada **Faturayı Kes**; ödenmişte **İptal Et** → engel mesajı; kesilmişte iptal gerekçesiz deneyip zorunluluğu görün.
2. `Ödemeler`: bekleyen ödemede **Onayla** → bağlı faturanın `partial`/`paid` olmasını izleyin. Onaylı ödemede **İade Et** (tutarı fazla girip hatayı görün).
3. `Öğrenci Paketleri → Paket Ata` → `Kredi Defteri`'nde yeni `grant`; **Kredi Düzelt** ile −1.
4. Akış 4'ün 1. denemesi (seans tamamla) ile `consume` satırının doğduğunu doğrulayın.
5. `koordinator@demo-a.test` ile girip Finans menüsünün **görünmediğini** kontrol edin. Veli tarafı: `veli1@demo-a.test` → `Portal → (çocuk) → Paketler` (izin: Finans anahtarı açık olmalı).
