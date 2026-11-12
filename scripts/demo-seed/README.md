# demo-seed — AyNesil demo veri seti

Yerel geliştirme ve duman testi için **iki demo kurum**. Üretim seed'ine (`db/`, Flyway) dokunmaz; yalnızca `demo_a` ve `demo_b` kurumlarını yazar. İsteğe bağlıdır.

```bash
scripts/demo-seed/run.sh                 # kur / eksikleri tamamla
scripts/demo-seed/run.sh --reset         # demo kurumlarını silip baştan kur
scripts/demo-seed/run.sh --only demo_a   # tek kurum
scripts/demo-seed/run.sh --build         # API imajını yeniden derle
node scripts/demo-seed/seed.mjs          # API zaten ayaktaysa (DEMO_API ile adres değiştirilebilir)
```

Çıkış kodu: `0` tamam · `2` beklenmeyen adım hatası. Bilinen backend hataları (aşağıda) hata sayılmaz, özette ayrıca listelenir.

## Giriş bilgileri
Ortak yerel şifre: **`DemoSifre!2026`** (`fixtures.json`; yalnızca yerel/geliştirme DB'si için).

| Rol | demo_a (Demo Gökkuşağı Merkezi) | demo_b (Demo Papatya Merkezi) |
|---|---|---|
| Kurum yöneticisi | `admin@demo-a.test` | `admin@demo-b.test` |
| Koordinatör | `koordinator@demo-a.test` | `koordinator@demo-b.test` |
| Eğitmen | `egitmen@demo-a.test` | `egitmen@demo-b.test` |
| Muhasebe | `muhasebe@demo-a.test` | `muhasebe@demo-b.test` |
| Veli portalı 1 (kamera yetkili, 2 kardeş) | `veli1@demo-a.test` | `veli1@demo-b.test` |
| Veli portalı 2 (tek öğrenci) | `veli2@demo-a.test` | `veli2@demo-b.test` |

Kullanıcı adı **globalde benzersiz** olduğundan alan adı kuruma özeldir. Roller: `admin` (kurulumla gelir), `coordinator`, `educator`, `finance`, `guardian` (izin alt kümeleri `phases/identity.mjs` içinde).
Kurum kodları `demo_a` / `demo_b` (alt çizgili; `new_tenant.py` tire kabul etmez).

## Ayırt edilebilirlik
Kurumlar arası isim havuzları kesişmez: öğrenci no `GK-26xx` / `PP-26xx`, farklı öğrenci/veli/eğitmen/aday adları, farklı kampüs/okul/kamp adları, İstanbul / Ankara. Veriler deterministiktir. İsimler uydurmadır; gerçek kişi, telefon ya da geçerli TC no yoktur (TC ve telefon boş), e-postalar `.test`.

## Her kurumda ne var
2 kampüs · 6 derslik (kampüs başına 2 fiziksel + 1 sanal) · 6 eğitmen · 15 öğrenci · 23 veli · 6 kullanıcı · 20 aday ·
~225 seans (son 8 hafta + 2 hafta ileri; karışık yoklama, iptal, gelmedi, 2 ertelenen, tekrarlayan program, telafi talepleri, 8 seans notu) ·
3 program + kayıtlar · 8 BEP planı (taslak/incelemede/onaylı/aktif) + 24 hedef + 96 ilerleme kaydı + gözden geçirmeler · 1 değerlendirme şablonu + 4 oturum ·
CRM (aşamalara dağılmış 20 aday, ~42 aktivite, 10 görüşme, 4 dönüşmüş) · finans (3 paket tanımı, 8 öğrenci paketi, 8 fatura [taslak/kısmi/ödenmiş/iptal/kesilmiş], 6 ödeme [1 bekleyen], 31 kredi hareketi, 1 burs, 1 promosyon) ·
hukuk (2 sözleşme + 3 rıza şablonu, 8 sözleşme [taslak/gönderilmiş/imzalı/aktif/feshedilmiş], rızalar [kamera dahil, 1 geri çekilmiş]) ·
4 toplantı + takip görevleri (tamamlanmış/gecikmiş/açık) · izin (8 bakiye, 4 talep: bekleyen/onaylı/reddedilmiş/iptal) ·
danışmanlık (1 okul, plan, 2 ziyaret, gözlem, takip görevi, sözleşme) · 2 kamera + derslik ataması + veli1 izleme yetkisi ·
1 kamp (2 dönem, 4 etkinlik, 8 kayıt [1 bekleme listesi], 2 eğitmen, yoklama) · KPI (önceki 2 ay, API'nin `compute/bulk` ucu) · bildirim şablonları + 7 örnek bildirim · 2 anket + yanıt + veli geri bildirimi.

## Yöntem
Her şey mümkün olduğunca **gerçek API** üzerinden oluşturulur (iş kuralları ve doğrulamalar sınanır). Kurum açma `devops/new_tenant.py` ile yapılır.

**İdempotentlik:** *var olanı atla* yöntemi seçildi (kopya üretmez, silmez). Varlıklar doğal anahtarla bulunur (öğrenci no, e-posta, kod…); olay verisi (seans, fatura, aday…) bölümler halinde "bu kurumda zaten var mı?" diye kontrol edilir. Temiz kurulum için `--reset`: owner rolüyle SQL üzerinden **yalnızca** `demo_a`/`demo_b` kurumunun tüm satırlarını (RLS'yi aşarak, tetikleyiciler kapalı) siler ve baştan kurar. Finansal kayıtlar bu yüzden `--reset` ile bile ancak demo kurumlar için silinir.

## SQL ile yapılanlar (API'nin yapamadığı)
| Ne | Neden |
|---|---|
| `educators.educator.user_id`, `students.guardian.user_id` | Eğitmen/veli kaydı ↔ kullanıcı bağını kuran API yok (veli portalı girişi için şart) |
| `crm.interview.scheduled_at` (geçmişe çekme) | API geçmiş tarihli görüşme planlamayı reddediyor; sonra normal `complete`/`no-show` API'si ile kapatılır |
| `scheduling.session_note` (8 not) | **Backend hatası:** not API'si `authored_by`'a kullanıcı id'si yazıyor, FK `educators.educator`'a bakıyor → HTTP 422 |
| `finance.invoice_line` + `finance.invoice` (toplam/durum/no) | **Backend hatası:** satır ekleme/kesme/iptal HTTP 500 (`Invoice.CreatedBy` EF modelinde yok) |
| `finance.payment.status = captured` | **Backend hatası:** `capture` HTTP 500 (`Payment.CreatedBy`) |
| `consultancy.consultancy_plan.status = active` | **Backend hatası:** `activate` HTTP 500 (`ConsultancyPlan.CreatedBy`) |
| `core.notification` (7 örnek) | Bildirimi yalnızca sistem olayları üretiyor, oluşturma ucu yok |

Geçmiş seanslar, yoklamalar, toplantılar, BEP ölçümleri ve KPI değerleri API ile üretilir (API geçmiş tarihi kabul ediyor); KPI `compute/bulk` ile hesaplanır.

## Bilinen backend hataları (seed atlar, düzeltilmedi)
- `POST /assessment-sessions/{id}/report/finalize` → 500 (`AssessmentReport.CreatedBy`); rapor taslak kalır.
- `POST /payments/discounts` (faturaya indirim) → 500 (`Invoice.CreatedBy`).
- Aynı kök neden: salt-ekleme (CreatedBy sütunu olmayan) varlıkların **güncelleme** yolu EF modelinde `CreatedBy` arıyor (Invoice, Payment, AssessmentReport, ConsultancyPlan).
- Sanal derslik kampüse bağlanamıyor (API kuralı) → kampüs başına sanal oda `campusId = null` ile oluşturulur.

## Dosyalar
`run.sh` (tek komut) · `seed.mjs` (orkestrasyon, `--reset`, `--phase a,b`) · `lib.mjs` (istemci, SQL, yardımcılar) · `data.mjs` (isim havuzları) · `fixtures.json` · `phases/*.mjs` (identity, people, sessions, learning, crm, finance, legal, ops, camp, extras).
