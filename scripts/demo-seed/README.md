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

> **İlk giriş zorunluluğu (V50):** yönetici/API ile açılan hesaplar varsayılan olarak `must_change_password = true` olur. Demo kullanıcılarında takılmamak için seed bayrağı SQL ile kapatır (`lib.mjs → clearMustChange`; `new_tenant.py` yöneticisi dahil, oturum açılmadan önce). Tek istisna bilerek açık bırakılan **`ilkgiris@demo-a.test` / `ilkgiris@demo-b.test`** (koordinatör rolü): ilk girişte şifre değiştirme akışını denemek için.

| Rol | demo_a (Demo Gökkuşağı Merkezi) | demo_b (Demo Papatya Merkezi) |
|---|---|---|
| Kurum yöneticisi | `admin@demo-a.test` | `admin@demo-b.test` |
| Koordinatör | `koordinator@demo-a.test` | `koordinator@demo-b.test` |
| Eğitmen | `egitmen@demo-a.test` | `egitmen@demo-b.test` |
| Muhasebe | `muhasebe@demo-a.test` | `muhasebe@demo-b.test` |
| Veli portalı 1 (kamera yetkili, 2 kardeş) | `veli1@demo-a.test` | `veli1@demo-b.test` |
| Veli portalı 2 (tek öğrenci) | `veli2@demo-a.test` | `veli2@demo-b.test` |
| İlk giriş örneği (`must_change_password` açık) | `ilkgiris@demo-a.test` | `ilkgiris@demo-b.test` |

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

## Yeni akış örnekleri (ekransız uçları kapatan işler)
`phases/extras.mjs → newFlowExamples` (her biri kendi idempotency kontrolüyle) ve ilgili fazlar:
sözleşme şablonu **yeni sürüm** (KVKK şablonu v1→v2) · telafi talebi **seans ata → tamamla** · ikinci iade talebi **başarısız işaretle** (ilki finans fazında işleme alınır) · kuruma özel **KPI tanımı** (`demo.veli_geri_bildirim_orani`) · CRM'de gelecek görüşmelerden biri **ertelendi**, geçmişte **gelmedi** işaretli · ilk giriş örneği kullanıcısı. Zaten seed'de olanlar: rıza verme/geri alma, sözleşme oluştur/gönder/imzala/etkinleştir/fesih, fatura oluştur/kes/iptal, izin bakiyesi tanımlama, iade işleme.

## SQL ile yapılanlar (API'nin yapamadığı)
| Ne | Neden |
|---|---|
| `crm.interview.scheduled_at` (geçmişe çekme) | API geçmiş tarihli görüşme planlamayı reddediyor; sonra normal `complete`/`no-show` API'si ile kapatılır |
| `core.notification` (7 örnek) | Bildirimi yalnızca sistem olayları üretiyor, oluşturma ucu yok |
| `iam.user_account.must_change_password = false` (demo kullanıcıları) | API'den açılan hesaplar ilk girişte şifre değiştirmeye zorlanır (V50); seed'in kendi oturumları ve elle gezme takılmasın |

Geçmiş seanslar, yoklamalar, toplantılar, BEP ölçümleri ve KPI değerleri API ile üretilir (API geçmiş tarihi kabul ediyor); KPI `compute/bulk` ile hesaplanır.

## Bilinen sınırlar
- Sanal derslik kampüse bağlanamıyor (API kuralı) → kampüs başına sanal oda `campusId = null` ile oluşturulur.
- Seans notları ve BEP onayları eğitmen kullanıcısı (`egitmen@…`, `educators[0]` ile bağlı) adına API ile yapılır: yazar/onaylayan oturumdaki kullanıcıya bağlı eğitmen kaydıdır. Bu yüzden demo `educator` rolü `education_plan:approve` içerir.
- Eğitmen/veli kaydı ↔ kullanıcı bağı API ile kurulur (`PUT /educators/{id}/user-account`, `PUT /guardians/{id}/user-account`). Kullanıcılar sabit demo şifresiyle `/users` üzerinden açılır; ekrandaki "Yeni kullanıcı oluştur" akışı ise geçici şifre üretir (girişler tablodaki şifreyle kalsın diye burada kullanılmaz).
- Fatura satırı/kesme/iptal/indirim, ödeme onayı, rapor kesinleştirme ve danışmanlık planı aktifleştirme artık API ile yapılır (eski `CreatedBy` EF eşleme hatası giderildi).

## Dosyalar
`run.sh` (tek komut) · `seed.mjs` (orkestrasyon, `--reset`, `--phase a,b`) · `lib.mjs` (istemci, SQL, yardımcılar) · `data.mjs` (isim havuzları) · `fixtures.json` · `phases/*.mjs` (identity, people, sessions, learning, crm, finance, legal, ops, camp, extras).
