# smoke-api — AyNesil API duman testi

Gerçek API'ye karşı otomatik duman testi. **Hata düzeltmez, yalnızca raporlar.**

```bash
scripts/smoke-api/run.sh                 # DB + Flyway + API + demo kurumlar (demo-seed) + test
scripts/smoke-api/run.sh --only camp     # yalnızca 'camp' ile başlayan GET kimlikleri
node scripts/smoke-api/run.mjs --strict  # API zaten ayaktaysa; 4xx/5xx/tip hatasında çıkış kodu 1
```

Çıktı: konsol tablosu + `out/report.md` + `out/report.json`.

## Ne yapar
1. `extract-endpoints.mjs` — TypeScript derleyici API'siyle `frontend/aynesil-web/src` altındaki tüm
   `apiService.get|post|put|patch|delete<T>()` çağrılarını ve `T` tipinin (zorunlu/opsiyonel/null alanlar) şemasını çıkarır → `endpoints.json`.
2. `run.mjs` — `fixtures.json`'daki demo_a yöneticisi ile giriş yapar (kurumlar `scripts/demo-seed` ile kurulur); hazırlık verisi (öğrenci, eğitmen, derslik, program, aday)
   oluşturur; yazma akışlarını dener (kamp + dönem + etkinlik, toplantı oluştur/güncelle, seans oluştur);
   sonra tüm GET'leri çağırır. Detay çağrıları için id, ilgili listenin ilk öğesinden alınır.
3. Sonuç sınıfları: `ok`, `4xx`, `5xx`, `type` (2xx ama frontend tipiyle uyuşmuyor), `skip` (veri/kimlik yok ya da beklenen durum).
   Atlanan uç noktalarda sahte GUID ile **rota yoklaması** yapılır; backend'de rota yoksa `4xx / ROTA YOK` olarak raporlanır.

## Yazma akışları (yönetici turunda, `newFlows`)
Ekransız uçları kapatan işlerin API düzeyi kanıtı (her çalıştırmada `Smoke …` kayıtları ekler):
- **must_change_password:** `/users` ile açılan hesap girişte `mustChangePassword=true` alır; `/students`, `/menus/me`, `/campuses`, `/settings` → **403 `Auth.PasswordChangeRequired`**; `change-password` izinli; yeni şifreyle girişte bayrak kapalı ve uçlar açık. Geçersiz sıfırlama token'ı → 422 `Auth.InvalidResetToken`.
- **Hukuk:** sözleşme/onam şablonu oluştur → güncelle → yeni sürüm → sürümler; sözleşme oluştur → gönder → imzala → etkinleştir → süresini bitir; rıza ver → geri al (ikinci geri alma reddedilir).
- **Telafi:** talep → onay → telafi seansı → ata → tamamla; başka öğrencinin seansı atanamaz (reddedilir).
- **İade:** fatura → satır → kes → ödeme → onay → iade → işleme al; ikinci iade → başarısız işaretle (tekrar işlenemez).
- **İzin bakiyesi:** tanımla (aynı eğitmen/tür/yıl ikinci kez reddedilir). **KPI tanımı:** oluştur → güncelle (geçersiz `spec` reddedilir).

## Turlar
1. **Yönetici turu** (demo_a `admin@demo-a.test`): hazırlık + yazma akışları + tüm GET'ler (portal hariç).
2. **İzolasyon**: demo_a yöneticisiyle demo_b kayıtlarının id'lerine (öğrenci, seans, fatura, toplantı) detay isteği → **404** beklenir; demo_b öğrencisine PUT → 404. Kurumlar: demo_a yöneticisi kurum listesinde yalnızca kendini görür; demo_b kurumuna GET (detay, ayarlar) ve PUT → 404. Beklenmeyen 2xx `leak` (izolasyon ihlali) olarak raporlanır.
3. **Veli turu** (demo_a `veli1@demo-a.test`): `portal.*` uçları, `studentId` `/portal/my-students` listesinden. Ek olarak veli1 → aynı kurumda başka velinin çocuğu (GK-2603) ve demo_b öğrencisi için portal uçları → **403/404** beklenir.

> Not: yazma akışları ("hazırlık", "yazma") her çalıştırmada demo_a'ya `Smoke …` adlı kayıtlar ekler; temizlemek için `scripts/demo-seed/run.sh --reset`.

## Fixture
`fixtures.json` — demo kurum (`demo_a`), yönetici ve veli kullanıcı adı/şifresi (yalnızca yerel DB; ortak şifre `scripts/demo-seed/README.md`'de), izolasyon sabitleri, yol parametresi kaynakları,
zorunlu sorgu parametresi override'ları (`{{today-30}}` jetonu desteklenir), beklenen durumlar (`expectedStatus`).
Kurumlar yoksa `run.sh`, demo-seed ile oluşturur; mevcut kurumlara (ör. `akran`) dokunmaz.

## Bilinen sınırlar
- Boş listelerin öğe alanları doğrulanamaz (raporda sayısı verilir).
- Yazma yanıtları yalnızca ilgili akışlarda doğrulanır; tüm POST/PUT/DELETE'ler denenmez.
- API, üretim imajıyla (`docker/api.Dockerfile`, ICU dahil) çalışır; ayrı bir sarmalayıcı yoktur.
