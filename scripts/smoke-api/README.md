# smoke-api — AyNesil API duman testi

Gerçek API'ye karşı otomatik duman testi. **Hata düzeltmez, yalnızca raporlar.**

```bash
scripts/smoke-api/run.sh                 # DB + Flyway + API + test kurumu + test
scripts/smoke-api/run.sh --only camp     # yalnızca 'camp' ile başlayan GET kimlikleri
node scripts/smoke-api/run.mjs --strict  # API zaten ayaktaysa; 4xx/5xx/tip hatasında çıkış kodu 1
```

Çıktı: konsol tablosu + `out/report.md` + `out/report.json`.

## Ne yapar
1. `extract-endpoints.mjs` — TypeScript derleyici API'siyle `frontend/aynesil-web/src` altındaki tüm
   `apiService.get|post|put|patch|delete<T>()` çağrılarını ve `T` tipinin (zorunlu/opsiyonel/null alanlar) şemasını çıkarır → `endpoints.json`.
2. `run.mjs` — `fixtures.json`'daki yönetici ile giriş yapar; hazırlık verisi (öğrenci, eğitmen, derslik, program, aday)
   oluşturur; yazma akışlarını dener (kamp + dönem + etkinlik, toplantı oluştur/güncelle, seans oluştur);
   sonra tüm GET'leri çağırır. Detay çağrıları için id, ilgili listenin ilk öğesinden alınır.
3. Sonuç sınıfları: `ok`, `4xx`, `5xx`, `type` (2xx ama frontend tipiyle uyuşmuyor), `skip` (veri/kimlik yok ya da beklenen durum).
   Atlanan uç noktalarda sahte GUID ile **rota yoklaması** yapılır; backend'de rota yoksa `4xx / ROTA YOK` olarak raporlanır.

## Fixture
`fixtures.json` — test kurumu (`smoke`), yönetici kullanıcı adı/şifresi (yalnızca yerel DB), yol parametresi kaynakları,
zorunlu sorgu parametresi override'ları (`{{today-30}}` jetonu desteklenir), beklenen durumlar (`expectedStatus`).
Kurum yoksa `run.sh`, `devops/new_tenant.py` ile oluşturur; mevcut kurumlara (ör. `akran`) dokunmaz.

## Bilinen sınırlar
- Boş listelerin öğe alanları doğrulanamaz (raporda sayısı verilir).
- Veli portalı uç noktaları veli kullanıcısı gerektirir; `expectedStatus` ile `skip` sayılır.
- Yazma yanıtları yalnızca ilgili akışlarda doğrulanır; tüm POST/PUT/DELETE'ler denenmez.
- API, üretim imajıyla (`docker/api.Dockerfile`, ICU dahil) çalışır; ayrı bir sarmalayıcı yoktur.
