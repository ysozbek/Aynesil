# AyNesil — Kurulum Kılavuzu

Bu kılavuz iki bölümden oluşur:

| Bölüm | Kim için | Ne zaman |
|---|---|---|
| **A. Sunucu kurulumu** | Sistem yöneticisi | Bir kez (ve her sürüm güncellemesinde) |
| **B. Kurum kurulumu** | Kurulumu yapan kişi + kurum yöneticisi | Her yeni müşteri (kurum) için |

Koddan doğrulanamayan noktalar **[doğrulanacak]** ile işaretlidir; tam liste en sondadır.

---

# A. Sunucu kurulumu

## A.1 İki senaryo

| | (a) Tek sunucu, tüm müşteriler (SaaS) | (b) Bir kuruma ayrı sunucu |
|---|---|---|
| Kurulum | Bu bölüm bir kez yapılır | Bu bölüm her kurumun sunucusunda ayrıca yapılır |
| Yeni müşteri | `devops/new_tenant.py` ile aynı sunucuda yeni kurum açılır | Sunucuda tek kurum açılır |
| Alan adı | Tek adres (ör. `app.aynesil.com`); giriş kullanıcı adıyla yapıldığı için kuruma özel adres gerekmez | Kuruma özel adres (ör. `aynesil.ornek.com.tr`) |
| Veri ayrımı | Aynı veritabanı; satır düzeyi güvenlik (RLS) ile ayrılır | Fiziksel olarak ayrı |
| Gizli anahtarlar | Bir set | **Her sunucuda farklı** set (JWT anahtarı ve DB şifreleri asla sunucular arasında paylaşılmaz) |
| Yedekleme | Tek yedek tüm kurumları kapsar; tek kurumu geri yüklemek mümkün değildir (bkz. A.7) | Kurum başına yedek |
| Güncelleme | Tüm kurumlar aynı anda güncellenir | Her sunucu ayrı güncellenir |

İki senaryoda da yazılım ve adımlar aynıdır. Fark yalnızca kaç kurum açıldığı ve gizli anahtarların kimde olduğudur.

## A.2 Gereksinimler

| Bileşen | Gereksinim | Not |
|---|---|---|
| İşletim sistemi | 64 bit Linux (Ubuntu 22.04/24.04 LTS önerilir) | Flyway imajı yalnızca `linux/amd64`; ARM sunucuda emülasyonla çalışır (`docker-compose.yml`'deki `platform: linux/amd64`). x86_64 sunucu önerilir. |
| Docker | Docker Engine + Compose eklentisi (v2.24 veya üstü) | `!override` söz dizimi için v2.24+ gerekir (A.4) [doğrulanacak] |
| PostgreSQL | 15 veya üstü, hedef **17** | Compose kendi `postgres:17-alpine` konteynerini kurar. Harici/yönetilen PostgreSQL kullanılacaksa bkz. A.5.4 |
| Redis | 7 | Compose kurar (menü/referans önbelleği, parola sıfırlama jetonları, hesap kilidi) |
| RAM | En az 4 GB, önerilen 8 GB | API (.NET 9) + PostgreSQL + Redis (256 MB sınırlı) + nginx. Tek sunuculu SaaS'ta kurum sayısı arttıkça PostgreSQL için bellek ekleyin [doğrulanacak: yük testi yapılmadı] |
| CPU | 2 çekirdek, önerilen 4 | İmaj derlemesi (`docker compose build`) en çok CPU isteyen adımdır |
| Disk | En az 40 GB SSD | Veritabanı, yüklenen dosyalar (`aynesil-uploads`), 30 günlük log dosyaları ve yerel yedekler. Yedekleri ayrıca sunucu dışına kopyalayın |
| Ağ | 80 ve 443 dışarıya açık; 5432, 6379, 5000, 5173 **kapalı** | Bkz. A.4 "Portları kapatın" |

## A.3 Repo ve dizin

```bash
sudo mkdir -p /opt/aynesil && sudo chown $USER /opt/aynesil
git clone <repo-adresi> /opt/aynesil
cd /opt/aynesil
git checkout <sürüm-etiketi>      # canlıda her zaman bir etiket/sürüm kullanın, main değil
cp .env.example .env
chmod 600 .env
```

`devops/new_tenant.py` ve `devops/set_modules.py` bu dizinden, `docker exec aynesil-db` üzerinden çalışır. Bu yüzden kurum açma işlemleri sunucuda (ya da veritabanına erişen bir makinede) yapılır.

## A.4 Ortam değişkenleri ve gizli anahtarlar

### Önemli: `.env` her şeyi konteynere taşımaz

`docker-compose.yml`'de API servisinin `env_file` satırı **yoktur**. `.env` yalnızca compose dosyasındaki `${...}` yer tutucularını doldurur. Yani `.env`'e yazılan bir değer **ancak compose dosyasında karşılığı varsa** API'ye ulaşır. Compose dosyasında karşılığı olmayan ayarlar (ForwardedHeaders, CORS'un üretim adresi, Storage BaseUrl, token süreleri, log seviyesi) için sunucuda bir `docker-compose.override.yml` oluşturun. `docker compose up` bu dosyayı otomatik okur.

> Ayrıca compose dosyası `AYNESIL_Cors__AllowedOrigins__0` değerini sabit olarak `http://localhost:${FRONTEND_PORT}` verir; `.env`'deki CORS satırları bu yüzden etkisizdir. Ön yüz API'yi aynı adresten (`/api`) çağırdığı için CORS normalde gerekmez; yine de override ile üretim adresini verin.

### Anahtar tablosu

API, yapılandırmayı `appsettings.json` → `appsettings.{Ortam}.json` → `AYNESIL_` önekli ortam değişkenleri sırasıyla okur (`Program.cs`, `AddEnvironmentVariables("AYNESIL_")`). Ortam değişkeninde `:` yerine `__` kullanılır (`Jwt:Secret` → `AYNESIL_Jwt__Secret`).

**Veritabanı ve compose değişkenleri (`.env`)**

| Anahtar | Ne işe yarar | Zorunlu | Örnek | Nasıl üretilir |
|---|---|---|---|---|
| `POSTGRES_DB` | Veritabanı adı | Hayır (vars. `aynesil`) | `aynesil` | — |
| `POSTGRES_USER` | Sahip rolü (`aynesil_owner`): tabloların sahibi, RLS'yi aşar. Yalnızca Flyway, `new_tenant.py`, `set_modules.py` ve yedekleme kullanır. API **kullanmaz**. | Hayır (vars. `aynesil_owner`) | `aynesil_owner` | Değiştirmemeniz önerilir (`new_tenant.py` adı `.env`'deki `POSTGRES_USER`'dan okur; dokümandaki komutlar `aynesil_owner` varsayar) |
| `POSTGRES_PASSWORD` | Sahip rolünün şifresi | **Evet** (varsayılan `changeme_owner` canlıda kabul edilemez) | `<gizli>` | `openssl rand -base64 32 \| tr -d '/+=' \| cut -c1-32` |
| `AYNESIL_DB_APP_USER` | Uygulama rolü (`aynesil_app`): en az yetkili, RLS'ye tabi; API bununla bağlanır | Hayır (vars. `aynesil_app`) | `aynesil_app` | Adı değiştirmeyin (Flyway `afterMigrate` yetkileri bu ada verir) |
| `AYNESIL_DB_APP_PASSWORD` | Uygulama rolünün şifresi | **Evet** | `<gizli>` | Yukarıdaki komutla, sahip şifresinden **farklı** |
| `AYNESIL_Jwt__Secret` | Oturum jetonlarını (JWT, HS256) imzalayan anahtar. Sızarsa herkes her kurumda yönetici jetonu üretebilir. | **Evet** (compose'daki `CHANGE_ME…` varsayılanı canlıda kabul edilemez) | `<gizli>` | `openssl rand -base64 64 \| tr -d '\n'` (en az 32, önerilen 64+ karakter) |
| `AYNESIL_Jwt__Issuer` | Jetonu üreten (doğrulamada karşılaştırılır) | Hayır (vars. `https://api.aynesil.com`) | `https://app.ornek.com` | Serbest metin; değişirse tüm oturumlar düşer |
| `AYNESIL_Jwt__Audience` | Jetonun hedefi | Hayır (vars. `aynesil-web`) | `aynesil-web` | Serbest metin |
| `API_PORT` | API'nin sunucuda yayınlandığı port | Hayır (vars. `5000`) | `5000` | — |
| `FRONTEND_PORT` | Ön yüz nginx'inin sunucuda yayınlandığı port | Hayır (vars. `5173`) | `5173` | — |

Şifre notları:
- `docker/init-db.sh` uygulama rolünü **yalnızca veritabanı diski boşken ilk açılışta** oluşturur ve şifreyi SQL içine tek tırnakla yazar. Şifrede tek tırnak (`'`) kullanmayın.
- Şifreyi sonradan değiştirmek `.env`'i değiştirmekle olmaz; önce veritabanında değiştirin, sonra `.env`'i güncelleyip API'yi yeniden başlatın:
  ```bash
  docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c "ALTER ROLE aynesil_app PASSWORD '<yeni>'"
  ```

**API ayarları (`docker-compose.override.yml` ile verilir)**

| Anahtar (ortam değişkeni) | Ne işe yarar | Zorunlu | Örnek / varsayılan |
|---|---|---|---|
| `AYNESIL_ConnectionStrings__DefaultConnection` | API'nin veritabanı bağlantısı (uygulama rolüyle). Hangfire da bunu kullanır. | Evet (compose `.env`'den kurar) | `Host=aynesil-db;Port=5432;Database=aynesil;Username=aynesil_app;Password=…` |
| `AYNESIL_ConnectionStrings__Redis` | Redis bağlantısı. Boşsa bellek içi önbelleğe düşer. | Hayır (compose kurar) | `aynesil-redis:6379,abortConnect=false` |
| `AYNESIL_ConnectionStrings__MigrationConnection` | `.env.example`'da var ama **kod okumuyor** | Hayır | Yazmayın |
| `AYNESIL_Jwt__AccessTokenExpiryMinutes` | Erişim jetonu süresi (dk) | Hayır | `60` (Development'ta `120`) |
| `AYNESIL_Jwt__RefreshTokenExpiryDays` | Yenileme jetonu süresi (gün) | Hayır | `30` |
| `Jwt:Algorithm` | `appsettings.json`'da var ama kod her zaman HS256 kullanır | Hayır | Değiştirmeyin |
| `AYNESIL_ForwardedHeaders__KnownProxies__0`, `__1`… | Gerçek istemci IP'sini `X-Forwarded-For`'dan almak için güvenilen vekil IP'leri | **Canlıda evet** (bunu ya da bir sonrakini verin) | `172.18.0.1` |
| `AYNESIL_ForwardedHeaders__KnownNetworks__0`, `__1`… | Güvenilen vekil ağları (CIDR) | **Canlıda evet** | `172.18.0.0/16` |
| `AYNESIL_RateLimiting__Auth__Login__PermitLimit` / `__Window` | Giriş hız sınırı | Hayır | `10` / `00:01:00` |
| `AYNESIL_RateLimiting__Auth__PasswordResetRequest__…` | Şifre sıfırlama isteği sınırı | Hayır | `3` / `01:00:00` |
| `AYNESIL_RateLimiting__Auth__PasswordResetConfirm__…` | Şifre sıfırlama onayı sınırı | Hayır | `10` / `00:01:00` |
| `AYNESIL_Cors__AllowedOrigins__0`, `__1`… | Tarayıcıdan API'yi çağırabilecek adresler | Hayır (aynı adresten çalışıyorsa) | `https://app.ornek.com` |
| `AYNESIL_Storage__Local__RootPath` | Yüklenen dosyaların konteyner içindeki dizini | Hayır (compose `/app/uploads` verir) | `/app/uploads` |
| `AYNESIL_Storage__Local__BaseUrl` | Dosya bağlantılarının kök adresi | Hayır | Varsayılan `http://localhost:5000/files`; canlıda kendi adresinizi verin [doğrulanacak: bu adresi sunan bir uç bulunamadı] |
| `AYNESIL_Serilog__MinimumLevel__Default` | Log seviyesi | Hayır | `Information` (Development'ta `Debug`) |
| `ASPNETCORE_ENVIRONMENT` | Ortam | Compose `Production` verir | **`Production` kalmalı** (bkz. A.9) |
| `ASPNETCORE_URLS` | API'nin konteyner içinde dinlediği adres | Compose verir | `http://+:5000` |
| `AllowedHosts` | Kabul edilen Host başlıkları | Hayır | `*` (ters vekil arkasında yeterli) |

**Ön yüz (derleme zamanı)**

| Anahtar | Ne işe yarar | Değer |
|---|---|---|
| `VITE_API_BASE_URL` | Ön yüzün API'yi çağırdığı adres; imaj derlenirken gömülür | Compose `/api` verir; değiştirmeyin |
| `VITE_APP_TITLE`, `VITE_APP_VERSION` | Başlık ve sürüm metni (`frontend/aynesil-web/.env.example`) | İsteğe bağlı |

### Örnek `docker-compose.override.yml`

Sunucuda repo köküne koyun, repoya **eklemeyin**:

```yaml
services:
  aynesil-api:
    environment:
      AYNESIL_ForwardedHeaders__KnownNetworks__0: "172.18.0.0/16"   # A.6'daki komutla bulduğunuz ağ
      AYNESIL_Cors__AllowedOrigins__0: "https://app.ornek.com"
      AYNESIL_Storage__Local__BaseUrl: "https://app.ornek.com/files"
    ports: !override
      - "127.0.0.1:5000:5000"
  aynesil-web:
    ports: !override
      - "127.0.0.1:5173:80"
  aynesil-db:
    ports: !reset []
  aynesil-redis:
    ports: !reset []
```

### Portları kapatın

Compose dosyası PostgreSQL'i (5432), Redis'i (6379), API'yi (5000) ve ön yüzü (5173) **tüm ağ arabirimlerinde** yayınlar. Redis'in şifresi yoktur. Docker kendi iptables kurallarını yazdığı için **`ufw` bu portları engellemez**. Yukarıdaki override ile portları ya kaldırın ya da `127.0.0.1`'e bağlayın. Ardından dışarıdan kontrol edin:

```bash
# Başka bir makineden; hepsi "kapalı/zaman aşımı" vermeli
nc -zv <sunucu-ip> 5432 6379 5000 5173
```

## A.5 İlk kurulum

### A.5.1 Başlatma

```bash
cd /opt/aynesil
docker compose config > /dev/null && echo "compose dosyası geçerli"
docker compose up -d --build
```

Sıra compose dosyasında tanımlıdır:
1. `aynesil-db` açılır. Disk boşsa `docker/init-db.sh` uygulama rolünü (`aynesil_app`) oluşturur.
2. `aynesil-redis` açılır.
3. `aynesil-flyway` (tek seferlik) `db/migrations/` altındaki tüm `V*.sql` dosyalarını sahip rolüyle sırayla uygular. Her çalışmanın sonunda `afterMigrate__app_role_grants.sql` uygulama rolüne yetkileri yeniden verir. Flyway başarıyla bitmeden API başlamaz.
4. `aynesil-api` açılır (sağlık kontrolü 60 sn sonra başlar).
5. `aynesil-web` (nginx + derlenmiş Vue uygulaması) API sağlıklı olunca açılır.

### A.5.2 Kontroller

```bash
docker compose ps                                   # flyway "exited (0)", diğerleri "healthy"/"running"
docker compose logs aynesil-flyway | tail -20       # "Successfully applied N migrations" veya "up to date"
docker compose run --rm aynesil-flyway info         # tüm sürümler "Success"
curl -fsS http://127.0.0.1:5000/health              # {"status":"healthy",...}
docker compose logs aynesil-api | grep "starting on" # "AyNesil API starting on Production"
```

`/health` yalnızca API sürecinin ayakta olduğunu söyler; **veritabanına ve Redis'e bağlanmayı denemez**. Gerçek bir uçtan uca kontrol için bir kurum açıp (Bölüm B.1) giriş yapın.

`/health` adresi `/api` altında değildir. Ön yüz nginx'i yalnızca `/api/`'yi API'ye ilettiği için dışarıdan `https://app.ornek.com/health` çalışmaz; izleme aracını sunucu içinden `127.0.0.1:5000/health`'e yönlendirin ya da ters vekile ayrı bir `location = /health` ekleyin.

### A.5.3 Demo verisi (isteğe bağlı, canlıda kullanmayın)

Demo/test sunucusunda iki örnek kurum kurmak için `scripts/demo-seed/run.sh`. Hesaplar ve ortak şifre `scripts/demo-seed/README.md`'dedir. Bu şifre herkesçe bilindiği için **canlı sunucuda çalıştırmayın**.

### A.5.4 Harici/yönetilen PostgreSQL

Compose'daki veritabanı yerine harici bir PostgreSQL 15+ kullanılacaksa:
1. Sahip rolünü (`aynesil_owner`) ve uygulama rolünü (`aynesil_app`, `NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE`) elle oluşturun.
2. Migration'lar şu eklentileri oluşturur: `btree_gist`, `citext`, `pg_trgm`, `pgcrypto`, `unaccent`. Sahip rolünün bunları oluşturma yetkisi olmalı (ya da önceden kurun).
3. Flyway'i bu sunucuya yönlendirin (`FLYWAY_URL`, `FLYWAY_USER`, `FLYWAY_PASSWORD`, `FLYWAY_PLACEHOLDERS_APPROLE`) ve API bağlantı dizesini değiştirin.
4. `new_tenant.py` ve `set_modules.py` için `--local` ve `PGHOST/PGUSER/PGDATABASE/PGPASSWORD` kullanın.

Bu senaryo bu repoda test edilmemiştir [doğrulanacak].

## A.6 Alan adı, HTTPS ve ters vekil

### Yapı

```
İstemci ──HTTPS──▶ sunucudaki nginx (443, sertifika)
                     ├── /api/  ──▶ 127.0.0.1:5000  (aynesil-api)
                     └── /      ──▶ 127.0.0.1:5173  (aynesil-web: derlenmiş ön yüz)
```

`/api/` isteklerini **doğrudan API'ye** gönderin, ön yüz konteynerinden geçirmeyin. Nedeni aşağıda.

### Neden önemli: hız sınırı ve gerçek IP

Anonim giriş uçlarının hız sınırı (giriş 10/dk, şifre sıfırlama isteği 3/saat, sıfırlama onayı 10/dk) **istemci IP'si başına** ve hedef kullanıcı adı başına ayrı ayrı uygulanır (`Api/RateLimiting/AuthRateLimiter.cs`). API istemci IP'sini bağlantının kaynağından alır. Ters vekil arkasında bu kaynak vekilin kendisidir. Bu durumda:

- **ForwardedHeaders tanımlı değilse ya da yanlışsa**, bütün kullanıcılar tek bir IP'den (vekilden) geliyor görünür ve **tek bir sayacı paylaşır**. Sabah 9'da on kişi giriş yaptığında on birinci kişi bir dakika boyunca "çok fazla istek" alır; kurumun tamamı kilitlenmiş olur. Etkinlik günlüğü ve oturum kayıtlarındaki IP de anlamsızlaşır.
- **Fazla geniş tanımlanırsa** (ör. `0.0.0.0/0`), saldırgan `X-Forwarded-For` başlığını uydurup her denemede farklı IP'den geliyormuş gibi görünür ve hız sınırını aşar.

API, `X-Forwarded-For`'u yalnızca `KnownProxies`/`KnownNetworks`'teki bir kaynaktan gelirse dikkate alır ve listenin **yalnızca en sağdaki** adresini kullanır (ASP.NET varsayılanı, tek vekil adımı). Bu yüzden:

- İstek iki vekilden geçerse (sunucu nginx'i → ön yüz konteynerindeki nginx → API), API en sağdaki adresi, yani **ilk vekilin konteyner ağı adresini** görür ve yine herkes tek sayaçta toplanır. Yerel kurulumda bu doğrulandı: ön yüz konteynerinden gelen girişlerin hepsi `iam.auth_session.ip_address` sütununda aynı konteyner adresiyle (`::ffff:172.19.0.5`) kayıtlı.
- Bu yüzden `/api/`'yi doğrudan `127.0.0.1:5000`'e yönlendirin.

### nginx örneği (sunucuda)

```nginx
server {
    listen 80;
    server_name app.ornek.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name app.ornek.com;

    ssl_certificate     /etc/letsencrypt/live/app.ornek.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.ornek.com/privkey.pem;

    # JWT tüm izinleri taşıdığı için büyük olabilir (docker/nginx.conf ile aynı değerler)
    large_client_header_buffers 8 64k;
    client_header_buffer_size   32k;
    client_max_body_size        20m;

    add_header Strict-Transport-Security "max-age=31536000" always;

    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        # Ekleme değil, ÜZERİNE YAZMA: istemcinin gönderdiği sahte X-Forwarded-For atılır
        proxy_set_header X-Forwarded-For   $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }

    location / {
        proxy_pass http://127.0.0.1:5173;
        proxy_set_header Host $host;
    }
}
```

Sertifika için örnek: `sudo certbot --nginx -d app.ornek.com`.

### KnownNetworks değerini bulma

Sunucu nginx'i `127.0.0.1:5000`'e bağlandığında Docker bağlantıyı compose ağının ağ geçidi adresinden konteynere iletir [doğrulanacak: Docker'ın `userland-proxy` ayarına bağlıdır]. Ağın adresini bulun:

```bash
docker network inspect aynesil_aynesil-net -f '{{(index .IPAM.Config 0).Subnet}}   gateway: {{(index .IPAM.Config 0).Gateway}}'
# ör. 172.18.0.0/16   gateway: 172.18.0.1
```

Override'a ya ağı (`AYNESIL_ForwardedHeaders__KnownNetworks__0: "172.18.0.0/16"`) ya da yalnızca ağ geçidini (`AYNESIL_ForwardedHeaders__KnownProxies__0: "172.18.0.1"`) yazın. Yalnızca ağ geçidi daha dardır ve önerilir. Ağ `docker compose down` ile silinip yeniden oluşturulursa adres değişebilir; her güncellemeden sonra tekrar kontrol edin.

Yapılandırma geçersizse (ör. CIDR yerine düz IP) API açılışta hata vererek durur (`ForwardedHeadersSetup.cs`).

### Doğrulama

```bash
docker compose up -d aynesil-api
# Kendi bilgisayarınızdan tarayıcıyla giriş yapın, sonra sunucuda:
docker exec aynesil-db psql -U aynesil_owner -d aynesil -c \
  "select ip_address, issued_at from iam.auth_session order by issued_at desc limit 5"
```

Son satırda **kendi genel IP adresiniz** görünmeli. `172.x.x.x` görünüyorsa ayar tutmamıştır; herkes tek sayacı paylaşıyordur. (IPv4 adresleri `::ffff:` önekiyle görünebilir; bu normaldir.)

### HTTPS notları

- TLS sunucu nginx'inde sonlanır; API konteyner içinde HTTP dinler. `X-Forwarded-Proto` güvenilen vekilden geldiği için API isteği HTTPS olarak görür.
- `app.UseHttpsRedirection()` konteynerde HTTPS portu olmadığından yönlendirme yapmaz; açılışta "https port" uyarısı loglanabilir, zararsızdır [doğrulanacak].
- Hız sınırı sayaçları **API süreci belleğindedir**. API'yi birden fazla örnekle (ölçekleme, yük dengeleme) çalıştırırsanız etkin sınır örnek sayısıyla çarpılır. Çok örnekli dağıtımdan önce Redis tabanlı bir sınırlayıcı gerekir. Tek örnekli kurulumda sorun yoktur.

## A.7 Yedekleme ve geri yükleme

### Neyi yedekleyeceksiniz

| Ne | Nerede | Yedek gerekli mi |
|---|---|---|
| Veritabanı | `aynesil-db` konteyneri, `aynesil_aynesil-db-data` diski | **Evet** — tüm iş verisi, kullanıcılar, Hangfire işleri |
| Yüklenen dosyalar | `aynesil_aynesil-uploads` diski | Evet |
| `.env` ve `docker-compose.override.yml` | `/opt/aynesil` | **Evet, ayrı ve şifreli** (gizli anahtarlar) |
| Redis | `aynesil_aynesil-redis-data` | Hayır (önbellek; kaybolursa yalnızca açık şifre sıfırlama bağlantıları ve hesap kilitleri düşer) |
| Loglar | `aynesil_aynesil-logs` | İsteğe bağlı |

### Günlük yedek

```bash
#!/bin/sh
# /opt/aynesil/backup.sh  — cron: 0 3 * * * /opt/aynesil/backup.sh
set -eu
DIR=/var/backups/aynesil; TS=$(date +%Y%m%d-%H%M)
mkdir -p "$DIR"

# Veritabanı (özel biçim: sıkıştırılmış, seçici geri yüklenebilir)
docker exec aynesil-db pg_dump -U aynesil_owner -d aynesil -Fc > "$DIR/db-$TS.dump"

# Yüklenen dosyalar
docker run --rm -v aynesil_aynesil-uploads:/data:ro -v "$DIR":/backup alpine \
  tar czf "/backup/uploads-$TS.tgz" -C /data .

# 14 günden eskileri sil
find "$DIR" -type f -mtime +14 -delete
```

Yedekleri sunucu dışına (başka bir sunucu, nesne depolama) kopyalayın. Sunucuyla birlikte kaybolan yedek, yedek değildir.

`pg_dump` rolleri (kullanıcı ve şifreleri) yedeklemez. Uygulama rolü, geri yüklenecek sunucuda `init-db.sh` ile (ya da elle) zaten oluşturulmuş olmalıdır.

### Geri yükleme provası (her ay yapın)

Canlıya dokunmadan, ayrı bir konteynerde:

```bash
# 1) Geçici PostgreSQL
docker run -d --name aynesil-restore-test \
  -e POSTGRES_USER=aynesil_owner -e POSTGRES_PASSWORD=prova -e POSTGRES_DB=aynesil \
  postgres:17-alpine
sleep 10

# 2) Uygulama rolü (dump içindeki yetkiler bu role verilir)
docker exec aynesil-restore-test psql -U aynesil_owner -d aynesil \
  -c "CREATE ROLE aynesil_app LOGIN PASSWORD 'prova' NOCREATEDB NOCREATEROLE"

# 3) Geri yükle
docker exec -i aynesil-restore-test pg_restore -U aynesil_owner -d aynesil \
  --exit-on-error < /var/backups/aynesil/db-<TS>.dump

# 4) Kontrol: son migration, kurum ve kayıt sayıları canlıyla aynı olmalı
docker exec aynesil-restore-test psql -U aynesil_owner -d aynesil -c \
  "select max(version::int) as son_migration from flyway_schema_history where version ~ '^[0-9]+$';"
docker exec aynesil-restore-test psql -U aynesil_owner -d aynesil -c \
  "select c.code, (select count(*) from students.student s where s.corporation_id=c.id) ogrenci,
          (select count(*) from scheduling.session x where x.corporation_id=c.id) seans
     from core.corporation c order by 1;"

# 5) Temizle
docker rm -f aynesil-restore-test
```

Aynı sorguları canlıda çalıştırıp sayıları karşılaştırın. Prova başarılıysa tarihi bir yere not edin.

### Gerçek geri yükleme (felaket durumu)

```bash
cd /opt/aynesil
docker compose stop aynesil-web aynesil-api
docker exec aynesil-db psql -U aynesil_owner -d postgres -c "DROP DATABASE aynesil WITH (FORCE)"
docker exec aynesil-db psql -U aynesil_owner -d postgres -c "CREATE DATABASE aynesil OWNER aynesil_owner"
docker exec -i aynesil-db pg_restore -U aynesil_owner -d aynesil --exit-on-error < /var/backups/aynesil/db-<TS>.dump
docker run --rm -v aynesil_aynesil-uploads:/data -v /var/backups/aynesil:/backup alpine \
  sh -c "rm -rf /data/* && tar xzf /backup/uploads-<TS>.tgz -C /data"
docker exec aynesil-redis redis-cli FLUSHALL      # eski önbellek geri yüklenen veriyle çelişmesin
docker compose up -d
```

> **Tek sunuculu SaaS'ta dikkat:** yedek tüm kurumları birlikte içerir. Geri yükleme **bütün kurumları** yedek anına döndürür. Tek bir kurumun verisini geri almak için hazır bir araç yoktur.

## A.8 Güncelleme prosedürü

### Adımlar

```bash
cd /opt/aynesil

# 1) Yedek (geri dönüşün tek güvenli yolu budur)
./backup.sh

# 2) Yeni sürümü al ve sürüm notlarını oku (yeni migration'lar, yeni ortam değişkenleri)
git fetch --tags
git log --oneline <eski-etiket>..<yeni-etiket>
ls db/migrations | sort -V | tail
git checkout <yeni-etiket>

# 3) İmajları derle
docker compose build

# 4) Migration'ları uygula (API çalışırken de güvenlidir, ama kısa bir bakım penceresi önerilir)
docker compose run --rm aynesil-flyway migrate
docker compose run --rm aynesil-flyway info      # yeni sürümler "Success"

# 5) Yeni imajlarla başlat
docker compose up -d

# 6) Önbelleği temizle (aşağıya bakın)
docker exec aynesil-redis sh -c \
  "redis-cli --scan --pattern 'aynesil:corp:*:menu:*' | xargs -r redis-cli del;
   redis-cli --scan --pattern 'aynesil:corp:*:ref:*'  | xargs -r redis-cli del;
   redis-cli --scan --pattern 'aynesil:ref:global:*'  | xargs -r redis-cli del"

# 7) Kontrol
curl -fsS http://127.0.0.1:5000/health
docker compose logs --since 10m aynesil-api | grep -E "ERR|FTL" || echo "hata yok"
```

### Neden önbellek temizlenir

Menü ağacı kurum ve dil başına Redis'te **1 saat** saklanır (`GetUserMenuQuery`, anahtar `aynesil:corp:<kurum>:menu:<dil>`). Bir migration yeni menü satırı eklediğinde ya da bir menüyü açıp kapattığında, kullanıcılar önbellek dolana kadar eski menüyü görür. Referans değerleri de önbelleklenir (30 dk). Modül açma/kapama (`set_modules.py`) önbellekten bağımsızdır ve hemen etkili olur.

`FLUSHALL` da işe yarar ama açık şifre sıfırlama bağlantılarını ve hesap kilitlerini de siler; yukarıdaki hedefli temizlik tercih edilir.

### Yeni izinler

Kullanıcının izinleri oturum jetonuna yazılır ve jeton her yenilendiğinde veritabanından yeniden okunur (`JwtTokenService.RefreshAsync`). Bir sürüm yeni izin ekleyip mevcut rollere veriyorsa, kullanıcılar yeni ekranları **en geç erişim jetonu süresi dolup yenilendiğinde** (varsayılan 60 dk) görür; hemen görmeleri için çıkış yapıp yeniden girmeleri yeterlidir. Güncellemeden sonra kullanıcılara bunu duyurun.

### Geri alma

Flyway'in ücretsiz sürümü migration'ları geri almaz. Geri alma yolu:

1. **Tercih edilen:** API ve ön yüzü durdurun, 1. adımdaki yedeği geri yükleyin (A.7 "Gerçek geri yükleme"), eski etikete dönün (`git checkout <eski-etiket>`), `docker compose build && docker compose up -d`. Yedekten sonra girilen veri kaybolur; bu yüzden güncellemeyi kullanımın az olduğu saatte yapın.
2. **Elle geri alma betikleri:** `db/rollback/` altında yalnızca birkaç migration grubu için hazır betik vardır (`V38-V41__tenant_integrity_down.sql`, `V43-V44__corporation_and_child_rls_down.sql`, `V45__split_delete_policy_down.sql`). Diğer migration'lar için betik yoktur. Bu betikleri çalıştırdıktan sonra `flyway_schema_history`'den ilgili satırları silmek gerekir; aksi halde Flyway bir sonraki çalışmada uyuşmazlık bildirir. Yalnızca sorunu bilen biri, prova ortamında denedikten sonra kullanmalıdır.
3. Migration yarıda kaldıysa: `docker compose run --rm aynesil-flyway repair` başarısız satırı temizler. Ardından sorunu giderip `migrate`'i yeniden çalıştırın.

## A.9 Canlıya çıkış kontrol listesi

| # | Kontrol | Nasıl doğrulanır |
|---|---|---|
| 1 | `.env`'deki tüm `changeme…`/`CHANGE_ME…` değerleri değişti; `AYNESIL_Jwt__Secret` en az 64 karakter | `grep -i change /opt/aynesil/.env` boş dönmeli |
| 2 | `.env` izinleri `600`, repoya eklenmemiş | `ls -l .env`, `git status` |
| 3 | 5432, 6379, 5000, 5173 dışarıdan kapalı | Başka makineden `nc -zv` |
| 4 | HTTPS çalışıyor, HTTP 443'e yönleniyor | Tarayıcı |
| 5 | ForwardedHeaders doğru: `iam.auth_session.ip_address` gerçek istemci IP'lerini gösteriyor | A.6 "Doğrulama" |
| 6 | `ASPNETCORE_ENVIRONMENT=Production` | `docker compose logs aynesil-api \| grep "starting on"` → `Production`. Development'ta API belgeleri (`/openapi`, `/scalar`) açılır ve log seviyesi `Debug` olur |
| 7 | **E-posta sağlayıcısı yok.** `INotificationService` olarak `LoggingNotificationService` kayıtlı: hiçbir e-posta/SMS gitmez, yalnızca şablon kodu ve alıcı loglanır. Sonuç: **"Şifremi unuttum" çalışmaz** (ekran var ama bağlantı gitmez), davet e-postası gitmez. Geçici şifreler kişiye elle iletilir. **Kurum yöneticisinin başka bir kullanıcının şifresini sıfırlayacağı bir ekran ya da uç yoktur** (`UsersController`'da yok); şifresini unutan kullanıcı için teknik ekip B.1.2'deki komutu kullanır | Müşteriye yazılı olarak bildirin |
| 8 | Hız sınırı **tek örnek** içindir; API tek örnekle çalışıyor | `docker compose ps` (bir `aynesil-api`) |
| 9 | Loglar: dosya logları `aynesil_aynesil-logs` diskinde `logs/aynesil-YYYYMMDD.log`, 30 gün saklanır; konsol `docker compose logs`. Disk dolmasın diye Docker log sürücüsüne boyut sınırı verin (`/etc/docker/daemon.json` → `"log-opts": {"max-size": "50m", "max-file": "5"}`) | `docker volume inspect`, `df -h` |
| 10 | İzleme: `127.0.0.1:5000/health` dakikada bir kontrol ediliyor; disk doluluğu ve yedek başarısı izleniyor | İzleme aracı |
| 11 | Yedekleme çalışıyor ve bir geri yükleme provası yapıldı | A.7 |
| 12 | **KPI'lar otomatik hesaplanmaz.** Arka plan işi kayıtlı değil; tek yol `KPI & Performans → Performans Anlık Görüntüleri → Dönem Hesapla` düğmesi (içinde bulunulan ay için). KPI modülü açık kurumlara bunu söyleyin | Müşteriye bildirin |
| 13 | Olaylardan bildirim üretilmiyor (ör. "seans yarın"); gelen kutusu bugün kendiliğinden dolmaz [doğrulanacak] | Müşteriye bildirin |
| 14 | Pasifleştirilen kurumun kullanıcıları **hâlâ giriş yapabiliyor**: giriş yalnızca kullanıcının durumuna bakar, kurumun durumuna bakmaz (`LoginCommand`, `iam.authenticate_user`). Kurumu kapatırken kullanıcılarını da askıya alın (B.1.4) | — |
| 15 | Demo verisi canlı sunucuda yok | `select code from core.corporation` içinde `demo_a`/`demo_b` olmamalı |
| 16 | Docker diskleri ve `/opt/aynesil` için sunucu düzeyinde disk şifreleme / erişim kısıtı | — |

## A.10 Bilinen eksikler

Ayrıntı: `docs/ekransiz-endpointler.md`. Özet:

**Yarım modüller (yeni kurumda kapalı gelir; açmadan önce müşteriyle konuşun):**

| Modül (kod) | Durum |
|---|---|
| Kamp (`camps`) | Liste, oluşturma ve kayıt ekranları var. Dönem, etkinlik, katılım, yoklama, eğitmen atama, rapor ve kampı aktif etme/silme işlemleri (26 uç) **ekransız** |
| Okul danışmanlığı (`consultancy`) | Liste/oluşturma var. Ziyaret ayrıntısı, tamamlama/iptal, gözlem ayrıntısı, raporlama ve silme işlemleri (18 uç) ekransız |
| Kamera (`camera`) | Kamera, oda ataması, izleme yetkisi ve izleme geçmişi ekranları var. **Canlı izleme (yayın) yok**; "Canlı İzle" düğmesi bu yüzden gizli |
| Anketler (`surveys`) | **Hiç ekranı yok** (yalnızca API) |
| Bakım ekibi (`care_team`) | **Ekranı yok**; altyapı kısmen kurulu |
| KPI (`kpi`) | Tanımlar, pano ve anlık görüntüler var. Otomatik hesaplama yok (yalnızca "Dönem Hesapla"); kuruma özel KPI tanımının formülü hesaplamada kullanılmıyor [doğrulanacak]; rapor/trend ekranları yok |

**Diğer ekransız işler (B grubu; modül kullanılırsa gerekir):** değerlendirme önerisini güncelleme; e-posta doğrulama (sağlayıcı yok); hedef özet raporu; eğitmen hiyerarşisini bitirme; toplantı çıktısını güncelleme; bildirim tetikleyicileri; elle kredi tüketme/iade (kredi düzeltme ekranı var); taslak faturaya indirim; burs ayrıntı/güncelleme; ödeme başarısız işaretleme (ödeme sağlayıcısı yok); KPI rapor ve trendleri; portalda BEP özeti sayfası; takvim kayıtları (tatil/etkinlik); oda takvimi; toplu iptal/oda değiştirme/yoklama ve tekrar istisnası; rıza kanıt dosyası yükleme.

**Koddan çıkan diğer eksikler:**
- **Toplu aktarma yok.** Öğrenci, veli, eğitmen listeleri ekrandan tek tek girilir.
- **"Eğitimciler" ana menüsü `user:read` izni istiyor.** Bu izni olmayan roller (ör. Koordinatör, Eğitmen) menüde "Eğitimciler"i hiç görmez; eğitmen listesine menüden ulaşamaz. Geçici çözüm B.4'te.
- Pasif kurumun kullanıcıları giriş yapabiliyor (A.9 madde 14).
- Kurum yöneticisi başka bir kullanıcının şifresini sıfırlayamıyor; e-posta da gitmediği için şifre yenileme teknik ekibe kalıyor (B.1.2).
- Kurum logosu için alan/ekran yok.

---

# B. Kurum kurulumu

Bu bölüm yeni bir kurumu sıfırdan canlı kullanıma almak içindir. Önceki "Kurulum Yol Haritası" buraya taşındı ve genişletildi.

| Aşama | Kim | Süre | Sonuç |
|---|---|---|---|
| **Gün 0:** Kurumu aç | Siz (teknik ekip) | 15 dakika | Kurum yöneticisi giriş yapabiliyor |
| **1. hafta:** Kurumun tanımları | Siz + kurum yöneticisi | 2–3 saatlik bir oturum | Roller, personel, eğitmenler, odalar, programlar, paketler, şablonlar hazır |
| **2. hafta:** Öğrenciler ve canlı kullanım | Kurum (siz destekte) | Birkaç gün | Haftalık seanslar sistemden yürüyor, veliler portala giriyor |

Başlamadan önce müşteriden bilgileri toplayın: **B.12'deki tabloyu** müşteriye gönderin.

## B.1 Gün 0 — Kurumu açma ve modüller

### B.1.1 Kurumu açma

Bu iş teknik ekibindir; kurum yöneticisinin ekranında "kurum ekle" yetkisi yoktur.

```bash
cd /opt/aynesil
# Önce deneyin: her şeyi çalıştırır, sonunda geri alır
python3 devops/new_tenant.py --check \
  --code ornek --name "Örnek Özel Eğitim" \
  --legal-name "Örnek Özel Eğitim ve Rehabilitasyon Ltd. Şti." \
  --campus "Merkez Kampüs:İstanbul" --campus "Kadıköy Kampüs:İstanbul" \
  --admin-username mudur@ornek.com --admin-name "Ayşe Yılmaz"

# Sorun yoksa aynı komutu --check olmadan çalıştırın
```

**Parametre kontrol listesi:**

| Parametre | Zorunlu | Ne yazılır | Varsayılan |
|---|---|---|---|
| `--code` | Evet | Kısa kurum kodu: küçük harf, rakam ve alt çizgi (tire olmaz), tüm sistemde benzersiz. Sonradan modül açarken bu kod kullanılır | — |
| `--name` | Evet | Ekranlarda görünen ad | — |
| `--legal-name` | Hayır | Resmi unvan | `--name` |
| `--campus "AD:ŞEHİR"` | Hayır, birden fazla verilebilir | Şubeler. İlk yazılan yöneticinin ana kampüsü olur | `Merkez Kampüs` |
| `--admin-username` | Evet | Yöneticinin kullanıcı adı. **Tüm sistemde benzersiz**; e-posta adresi kullanın | — |
| `--admin-name` | Evet | Yöneticinin adı soyadı | — |
| `--admin-email` | Hayır | Kullanıcı adı e-posta değilse | Kullanıcı adı |
| `--admin-password` | Hayır | Vermeyin; güçlü bir geçici şifre üretilir | Üretilir |
| `--locale` | Hayır | Varsayılan dil | `tr` |
| `--currency` | Hayır | Para birimi | `TRY` |
| `--timezone` | Hayır | Saat dilimi | `Europe/Istanbul` |
| `--enable-modules` | Hayır | Açık başlayacak isteğe bağlı modüller (B.1.3) | Hepsi kapalı |
| `--check` / `--dry-run` | — | Deneme (geri alır) / yalnızca SQL'i yazdırır | — |

Komutun açtıkları (tek işlemde; hata olursa hiçbir şey yazılmaz):
- Kurum ve kampüsler.
- **Yönetici** rolü: platform işlemleri (kurum açma, etkinleştirme/pasifleştirme) dışındaki tüm izinler.
- **Veli (portal)** rolü: yalnızca portal izinleri (`portal:access`, `parent_feedback:create`, `survey:respond`, `notification:read`).
- İlk yönetici kullanıcı. Geçici şifre **bir kez** ekrana yazılır.

### B.1.2 Yöneticiye giriş bilgisini iletme

- Kullanıcı adını ve geçici şifreyi **ayrı kanallardan** iletin (ör. kullanıcı adını e-postayla, şifreyi telefonla ya da SMS'le). İkisini aynı mesajda göndermeyin.
- Yönetici ilk girişte **şifresini değiştirmek zorundadır**: şifre değiştirme sayfasından başka hiçbir sayfa açılmaz. Değiştirince yeniden giriş yapar.
- "Şifremi unuttum" bugün çalışmaz (e-posta gitmez) ve kurum yöneticisinin başkasının şifresini sıfırlayacağı bir ekran yoktur. Bir kullanıcı (yönetici dahil) şifresini kaybederse teknik ekip yeni bir geçici şifre verir. Hazır bir betik yoktur; komut:
  ```bash
  GECICI=$(openssl rand -base64 18 | tr -d '/+=')
  docker exec aynesil-db psql -U aynesil_owner -d aynesil -c \
    "update iam.user_account
        set password_hash = crypt('$GECICI', gen_salt('bf', 12)), must_change_password = true
      where username = 'mudur@ornek.com'"
  echo "Geçici şifre: $GECICI"
  ```
  `new_tenant.py` de şifreyi aynı yöntemle (`crypt(…, gen_salt('bf', 12))`) yazar. Kullanıcı ilk girişte şifresini değiştirmeye zorlanır. Çok sayıda hatalı deneme sonrası hesap kilitliyse kilit bir süre sonra kendiliğinden kalkar [doğrulanacak: süre].

### B.1.3 Modülleri açma ve kapama

Şu altı modül **yeni kurumda kapalı** gelir: **kamp** (`camps`), **okul danışmanlığı** (`consultancy`), **kamera** (`camera`), **anketler** (`surveys`), **bakım ekibi** (`care_team`), **eğitmen KPI** (`kpi`). Kapalı modülün menüsü görünmez; ekranları da açılmaz.

Diğer her şey (aday takibi, öğrenciler, değerlendirme, BEP, hedefler, seanslar, finans, sözleşme ve rızalar, toplantılar, izinler, bildirimler, veli portalı) her kurumda açıktır, kapatılamaz.

```bash
python3 devops/set_modules.py --code ornek --list                  # durumu gösterir
python3 devops/set_modules.py --code ornek --enable kpi --check    # dener, geri alır
python3 devops/set_modules.py --code ornek --enable kpi            # açar
python3 devops/set_modules.py --code ornek --disable kpi           # kapatır
# Kurulumda açmak için: new_tenant.py ... --enable-modules kpi,camera   (ya da all)
```

- Değişiklik **hemen** etkili olur; API'yi yeniden başlatmak gerekmez. Kullanıcı sayfayı yenileyince menü değişir.
- Kurum yöneticisi bu ayarı değiştiremez; `Kurumlar → kurum → Ayarlar` sayfasında yalnızca **görür**.
- **Ne zaman açmalı?** Yarım modüllerin bazı işlemleri henüz ekrandan yapılamıyor (A.10). Açmadan önce müşteriye neyin çalışıp neyin çalışmadığını gösterin:
  - **KPI:** açılabilir; ama değerler yalnızca "Dönem Hesapla" ile hesaplanır.
  - **Kamera:** kamera, oda ataması, izleme yetkisi tanımlanabilir; **canlı izleme yok**. Müşteri canlı izleme bekliyorsa açmayın.
  - **Kamp, okul danışmanlığı:** yalnızca liste/kayıt düzeyinde; günlük işletim (yoklama, etkinlik, ziyaret tamamlama) ekrandan yapılamaz.
  - **Anketler, bakım ekibi:** ekranı yok; **açmayın**.

### B.1.4 Kurumu pasifleştirme (platform işi)

Kurumu etkinleştirme/pasifleştirme platform izni (`corporation:delete`) gerektirir ve bu izin hiçbir kurum rolüne verilemez. Kurum yöneticisi kendi kurumunu kapatamaz. Teknik ekip veritabanından yapar [doğrulanacak: hazır bir betik yok]:

```bash
docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c \
  "update core.corporation set status='suspended' where code='ornek'"
# Giriş kurumun durumuna bakmadığı için kullanıcıları da askıya alın:
docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c \
  "update iam.user_account set status='suspended'
     where corporation_id=(select id from core.corporation where code='ornek')"
```

## B.2 Kurum ayarları

Ekran: **Platform Yönetimi → Kurumlar → kurum → Ayarlar** (kurum yöneticisi yapar).

| Ayar | Seçenekler | Not |
|---|---|---|
| Varsayılan dil | Türkçe, English | Arayüz bugün yalnızca Türkçe çevrilmiştir |
| Para birimi | TRY, USD, EUR | |
| Saat dilimi | Europe/Istanbul, UTC, Europe/London, America/New_York | Türkiye için Europe/Istanbul |
| Vergi dairesi, vergi numarası | Serbest metin | Faturalarda kullanılması beklenir [doğrulanacak] |
| Ek ayarlar (JSON) | Serbest | Bugün hiçbir ekran bu alanı okumuyor; boş bırakın (`{}`) [doğrulanacak] |
| Modüller | Salt okunur | B.1.3 |

**Kurum logosu:** bugün logo yükleme alanı yoktur.

Aynı yerde: **Platform Yönetimi → Kampüsler** ile kampüslerin adres ve telefonunu tamamlayın, gerekirse yeni kampüs ekleyin.

## B.3 Referans veriler (listeler)

Ekran: **Platform Yönetimi → Referans Veriler**. "Seans tipi", "ödeme yöntemi", "aday kaynağı" gibi açılır listelerin içeriği buradan gelir.

**Kural:**
- Sistemle gelen (platform) değerler **tüm kurumlarda ortaktır**. Kurum bunları **düzenleyemez** ve silemez; yalnızca **kendi kurumu için pasifleştirebilir** (listelerde görünmez olur).
- Kurum, çoğu listeye **kendi değerini ekleyebilir** ve kendi eklediğini düzenleyip pasifleştirebilir.
- Hiçbir değer silinmez; kullanılmış bir değer yalnızca pasifleştirilir.
- Şu üç liste tamamen sabittir, kurum değer ekleyemez: **cinsiyet**, **entegrasyon türü**, **bildirim kanalı**.

**Kurulumda gözden geçirilecek listeler** (müşteriye "sizde bunlar nasıl?" diye sorun):

| Konu | Liste | Neden önemli |
|---|---|---|
| Aday takibi | Aday kaynakları, aday durumları, pipeline aşamaları, aktivite türleri | Satış hunisi bu aşamalarla çalışır |
| Öğrenci | Öğrenci statüleri, tanı kategorileri, veli yakınlık türleri, gelişim alanları | Öğrenci kartı ve raporlar |
| Eğitim | Program türleri, hizmet türleri, terapi türleri, seans türleri, hedef kategorileri, akademik dönem türleri | Program ve BEP ekranları |
| Planlama | Oda türleri, devamsızlık nedenleri, kaçırma nedenleri | Yoklama ve telafi |
| Finans | Ödeme yöntemleri, paket türleri, burs türleri, indirim türleri | Fatura ve tahsilat |
| Hukuk | Sözleşme türleri, rıza türleri | Şablonlar ve rızalar. **Kamera izleme** rıza türünü pasifleştirmeyin |
| Personel | Eğitmen unvanları, uzmanlık alanları, çalışma türleri, sertifika türleri, izin türleri | Eğitmen kartı ve izinler |
| Diğer | Toplantı türleri, bildirim kategorileri ve türleri | |

Modül kapalıysa ilgili listeler (kamp türleri, kamera türleri, kurum türleri vb.) gözden geçirilmez.

**Menüler** de aynı kuralı izler: **Platform Yönetimi → Menü Yönetimi**'nde sistemle gelen menü öğeleri salt okunurdur (düzenlenemez, gizlenemez, silinemez); kurum yalnızca kendi menü öğesini ekleyebilir. Kurulumda menüye dokunmanız gerekmez.

## B.4 Roller ve izinler

Ekran: **Platform Yönetimi → Roller → Rol Ekle**, sonra rolün **İzinler** sekmesinden izinleri seçin. Sistem kişinin unvanına değil, rolündeki izinlere bakar. Bir kişiye birden fazla rol verilebilir; izinler toplanır.

Hazır gelen iki rol vardır: **Yönetici** ve **Veli (portal)**. Aşağıdaki öneriler demo kurumların rollerinden türetilmiştir (`scripts/demo-seed/phases/identity.mjs`). İzin kodları ekranda kodla birlikte görünür.

### Yönetici (hazır gelir)
Platform işlemleri dışında **her şey**. Kullanıcı açma, rol verme ve eğitmen/veli hesabı bağlama yalnızca bu roldedir. Kurumda en az iki kişiye verin (biri tatildeyken kilitlenmemek için).

### Koordinatör
**Yönetici rolündeki her şey, şu gruplar hariç:** kullanıcı (`user:*`), rol ve izin (`role:*`, `permission:*`), kurum (`corporation:*`), ayarlar (`settings:*`), entegrasyon, denetim kaydı (`audit:*`), menü (`menu:*`), referans veri (`ref_data:*`), finansın tamamı (`finance`, `invoice`, `refund`, `credit_ledger`, `package_definition`, `student_package`, `scholarship`, `promotion`, `discount`), raporlar (`report:*`), KPI hesaplama ve yönetimi (`kpi_snapshot:*`, `kpi:manage`).

Günlük operasyonu yürütür: aday, öğrenci, veli, değerlendirme, BEP (onay dahil), program ve kayıt, seans planlama, telafi onayı, toplantı, izin onayı, sözleşme ve rıza.

> **Önemli:** "Eğitimciler" menüsü `user:read` izni istiyor (A.10). Koordinatörün eğitmen listesini menüden açabilmesi için role **`user:read`** iznini de ekleyin. Bu izin kullanıcı listesini **yalnızca görüntülemeyi** sağlar; kullanıcı açma ve hesap bağlama yetkisi vermez.

### Eğitmen

| Alan | İzinler |
|---|---|
| Öğrenci, veli (okuma) | `student:read`, `guardian:read`, `enrollment:read`, `program:read`, `campus:read`, `room:read`, `educator:read` |
| Seans | `session:read`, **`session:complete`** (eğitmen seansı kendisi tamamlayacaksa; demo rolünde yok), **`session:manage_goals`** (seansa hedef ekleyip çıkaracaksa; demo rolünde yok) |
| Yoklama | `attendance:read`, `attendance:record` |
| Seans notu | `session_note:write`. **`session_note:delete` vermeyin**: bu izin başkalarının notlarını düzenleme ve silme yetkisidir (yönetici yetkisi) |
| Hedefler | `student_goal:read`, `student_goal:create`, `student_goal:update`, `student_goal:change_status`, `goal_progress:read`, `goal_progress:record`, `goal_library:read`, `goal_template:read` |
| BEP | `education_plan:read`, `education_plan:add_review`. BEP onaylayacak eğitmenlere ayrıca `education_plan:approve` |
| Vaka notu | `case_note:create`, `case_note:read`, `case_note:update` (gizli notları okuyacaksa `case_note:read_confidential`) |
| Telafi | `makeup_request:read`, `makeup_request:request` |
| İzin | `leave_request:submit`, `leave_request:read`, `leave_request:cancel`, `leave_balance:read` |
| Diğer | `meeting:read`, `assessment_session:read`, `notification:read`, `kpi_dashboard:read` (KPI açıksa), `parent_feedback:read` |
| Kamp (modül açıksa) | `camp:read`, `camp_attendance:read/record`, `camp_participation:read/record` |
| Menü kusuru | Eğitmen listesini görecekse `user:read` (A.10) |

### Muhasebe

| Alan | İzinler |
|---|---|
| Finans | `finance:read`, `finance:write`, `finance_report:read` |
| Paket | `package_definition:create/read/update/delete`, `student_package:purchase/read/cancel` |
| Fatura | `invoice:create/read/update/void` |
| Tahsilat | `finance_payment:capture/read/record` |
| İade | `refund:request/read/process` |
| Kredi defteri | `credit_ledger:read/grant/consume/adjust` |
| İndirim | `scholarship:grant/read/update`, `promotion:create/read/update`, `discount:apply/read` |
| Rapor | `report:read/run/export/manage` |
| Okuma | `student:read`, `guardian:read`, `enrollment:read`, `program:read`, `campus:read`, `notification:read` |

### Sekreter / Resepsiyon (öneri, demoda yok)
Koordinatörden şunları çıkararak başlayın: BEP onayı (`education_plan:approve`), değerlendirme ve hedef yönetimi, sözleşme imzalama/fesih, izin onayı. Aday, öğrenci/veli kaydı, seans planlama, oda ve telafi talepleri kalsın. Kurumun iş bölümüne göre ayarlayın.

### Veli (portal) (hazır gelir)
`portal:access`, `parent_feedback:create`, `survey:respond`, `notification:read`. Veli hesapları bu rolle açılır. Veli hesabına verilen rol **mutlaka `portal:access` içermelidir**; içermiyorsa sistem hesabı açmaz. Bu role personel izni eklemeyin.

**Rol değişikliği ne zaman etkili olur?** İzinler oturuma yazılır ve oturum her yenilendiğinde yeniden okunur. Rolü değişen kişi değişikliği en geç bir saat içinde görür; hemen görmesi için çıkış yapıp yeniden girmesi yeterlidir.

## B.5 Personel hesapları, eğitmen ve veli bağlantısı

Hesap açma ve bağlama **kullanıcı yönetimi izni** gerektirdiği için bu işleri **kurum yöneticisi** yapar. Koordinatör bu düğmeleri görmez.

### B.5.1 Sekreter, muhasebe gibi personel (eğitmen olmayanlar)

1. **Platform Yönetimi → Kullanıcılar → Kullanıcı Ekle**.
2. Kullanıcı adı (e-posta önerilir; tüm sistemde benzersiz olmalı), ad soyad, e-posta, telefon, **şifre** (siz belirlersiniz; en az 8 karakter), dil, ana kampüs. Şifreyi boş bırakırsanız kişi giriş yapamaz.
3. Açılan kullanıcının sayfasında **Rol Ata** ile rolünü verin (isterseniz geçerlilik tarihleriyle).
4. Kişi ilk girişte şifresini değiştirmek zorundadır.

### B.5.2 Eğitmenler

1. Önce eğitmen kaydı: **Eğitimciler → Yeni Eğitmen** (ad, soyad, unvan, e-posta, telefon, çalışma türü, işe başlama tarihi, ana kampüs). Kartta uzmanlık, kampüs ve sertifikaları ekleyin.
2. Eğitmen kartı → **Genel Bakış** → **Kullanıcı hesabı** bölümü:
   - **Yeni kullanıcı oluştur:** kullanıcı adı (e-posta), ad soyad ve **rol** (ör. Eğitmen). Hesap açılır, karta bağlanır ve **geçici şifre bir kez** gösterilir.
   - **Mevcut kullanıcıyı bağla:** kişinin hesabı Kullanıcılar'da zaten açıldıysa onu seçin.
   - **Bağlantıyı kaldır:** yalnızca bağ kalkar; hesap ve rolleri silinmez.
3. Bağlı hesap, **Kullanıcılar → kullanıcı** sayfasında "Eğitmen kaydı" olarak görünür.

**Neden şart?** Seans notunu kimin yazdığını ve BEP'i kimin onayladığını sistem **giriş yapan kişinin bağlı olduğu eğitmen kaydından** alır; formda seçtirmez. Bu yüzden:
- **Hesabı bağlanmamış eğitmen seans notu yazamaz.** Denediğinde "bir yönetici bu kullanıcıyı eğitmen kaydına bağlamalıdır" uyarısı alır.
- **BEP onaylayacak ya da reddedecek herkesin** (koordinatör, klinik sorumlu, müdür dahil) bir **eğitmen kaydı olmalı ve hesabına bağlı olmalıdır**. Koordinatör ders vermese bile onun için bir eğitmen kaydı açın ve hesabını bağlayın. Bağlı değilse "Onayla" uyarı verir.
- Bir hesap en fazla **bir** eğitmen kaydına bağlanabilir.

İzin onayı ise eğitmen kaydı gerektirmez: onaylayan, giriş yapan kullanıcıdır.

### B.5.3 Veliler (portal hesabı)

Portal iki adımdır; **ikisi de** gerekir:
1. **Hesap:** veli kartı → **Genel Bakış** → **Kullanıcı hesabı** → **Yeni kullanıcı oluştur**. Kullanıcı adı olarak velinin e-postası; rol olarak **Veli (portal)** önceden seçilidir. Geçici şifre bir kez gösterilir.
2. **Çocuk bazında erişim:** veli kartı → **Portal Erişimi** → çocuk için **Erişim Aç** ve görünecek bölümleri seçin (B.10).

"Erişim Aç" tek başına hesap açmaz; hesap yoksa Portal Erişimi sekmesinde sarı uyarı çıkar.

### B.5.4 Geçici şifre akışı

1. Hesap açılınca geçici şifre **yalnızca bir kez** gösterilir (**Kopyala** düğmesi var). Pencereyi kapatırsanız bir daha göremezsiniz.
2. Şifreyi kişiye **güvenli bir yoldan** iletin:
   - Yüz yüze ya da telefonla söyleyin, veya SMS ile gönderin.
   - Kullanıcı adını ve şifreyi aynı e-postada/mesajda göndermeyin.
   - WhatsApp grubu gibi birden çok kişinin gördüğü yerlere yazmayın.
3. Kişi ilk girişte **şifre değiştirme sayfasına** yönlenir; başka sayfa açılmaz. Yeni şifreyi belirleyince yeniden giriş yapar.
4. Sonradan şifresini değiştirmek isteyen herkes sağ üstteki baş harf rozetinden **Şifre Değiştir**'e gider.
5. "Şifremi unuttum" ekranı vardır ama e-posta gitmediği için **bugün çalışmaz**. Kurum yöneticisinin başkasının şifresini sıfırlayacağı bir ekran da yoktur. Şifresini unutan kişi için teknik ekibe başvurun (B.1.2'deki komut).

## B.6 Programlar, hizmetler ve paketler

| # | Ekran | Ne girilir | Kontrol |
|---|---|---|---|
| 1 | **Seans Planlama → Odalar** | Her kampüsün odaları, kapasite, oda türü | Aynı odaya aynı saatte iki seans konamaz; liste eksiksiz olmalı |
| 2 | **Programlar → Yeni Program** | Kod, ad, program türü, açıklama | |
| 3 | Program sayfası → **Hizmetler** sekmesi → **Hizmet Ekle** | Her hizmetin adı, varsayılan süresi (dk), haftalık sıklığı | Tekrarlayan planda seçilecek hizmet, öğrencinin programının hizmetlerinden biri olmak zorundadır |
| 4 | **Finans → Paket Kataloğu** → yeni paket | Kod, ad, liste fiyatı, para birimi, paket türü, **program**, seans hakkı sayısı, geçerlilik süresi (gün) | **Paketi mutlaka bir programa bağlayın.** Seans tamamlanınca hak, seansın programına bağlı paketten düşer; programsız paketten hiç düşmez |
| 5 | **Finans → Burslar / Kampanyalar** (isteğe bağlı) | İndirim tanımları | |

## B.7 Akademik dönem ve "güncel dönem"

Ekran: **BEP / IEP → Akademik Dönemler**.

1. **Dönem Ekle**: ad, başlangıç ve bitiş tarihi (bitiş başlangıçtan sonra olmalı).
2. İçinde bulunulan dönemi **Güncel dönem yap** ile işaretleyin. Tek bir güncel dönem olur; yenisini işaretleyince eskisinin işareti kalkar.
3. BEP oluştururken dönem seçilir; dönem tanımlı değilse BEP açılamaz.
4. BEP bağlı dönem ve güncel dönem silinemez. Yeni yıla geçerken önce yeni dönemi ekleyip güncel yapın.

## B.8 Sözleşme ve onam şablonları, rızalar

| # | Ekran | Ne yapılır |
|---|---|---|
| 1 | **Sözleşme ve Rıza Yönetimi → Sözleşme Şablonları** → Yeni şablon | Kod, tür, başlık, metin. Metin değişirse **Yeni sürüm** kullanın; eski sözleşmeler eski sürümde kalır. Şablonlar silinmez |
| 2 | **Sözleşme ve Rıza Yönetimi → Rıza Şablonları** → Yeni şablon | KVKK, kamera izleme, foto-video, iletişim izni metinleri |
| 3 | Öğrenci kartı → **Rızalar** sekmesi → **Rıza ver** | Öğrenci bazında, **veli seçilerek** rıza kaydı. Geri çekilen rıza için yeni kayıt açılır |
| 4 | **Sözleşme ve Rıza Yönetimi → Öğrenci Sözleşmeleri** → Yeni Sözleşme | Öğrenci, güncel şablon, imzalayan veli, tarihler. Taslak → Gönderildi → İmzalandı → Aktif |

**Kamera kuralı:** veliye kamera izleme yetkisi vermeden önce öğrencinin **o veli adına verilmiş, süresi geçmemiş "Kamera İzleme" rızası** olmalıdır; yoksa yetki verilemez. Rıza geri alınırsa o velinin açık izleme yetkileri otomatik iptal edilir.

İmzalanmış sözleşme ve verilmiş rıza değiştirilemez ve silinemez. Yalnızca **taslak** sözleşme düzenlenebilir ve silinebilir.

## B.9 Bildirim şablonları

Ekran: **Bildirimler → Şablonlar → Yeni Şablon** (kod, kategori, tür, her dil için konu ve içerik).

Kurulumda şablon hazırlayabilirsiniz, ama bugün **e-posta ve SMS gönderilmez** ve sistem olaylarından (ör. "seans yarın") otomatik bildirim üretilmez [doğrulanacak]. Müşteriye bunu açıkça söyleyin; şablonlar sağlayıcı bağlandığında kullanılacaktır.

## B.10 Portal erişimi ve görünürlük ayarları

Veli kartı → **Portal Erişimi** → çocuk için **Erişim Aç**. Her çocuk için ayrı ayrı şu kutular seçilir:

| Kutu | Açıksa veli ne görür |
|---|---|
| **Oturumlar** | Seanslar ve eğitmenin **Veliye Görünür** işaretlediği seans notları |
| **Devam** | Yoklama bilgisi |
| **Raporlar** | Belgeler ve **kesinleşmiş** gelişim raporları (taslaklar görünmez) |
| **BEP/IEP** | Onaylanmış ve "Veli Görüntülüyor" açılmış plan, hedefler ve ilerleme |
| **Finans** | Paketler ve kalan seans hakkı |
| **Kamera** | Kamera izleme (modül açık ve izleme yetkisi verilmişse) |

Toplantılar her veliye açıktır. Kuruma önerilen başlangıç: Oturumlar, Devam, Raporlar ve BEP/IEP açık; Finans ve Kamera müşterinin kararına göre.

Bu ayarlar ayrıca şu işaretlerle birlikte çalışır:
- Seans notunda **Veliye Görünür** (grup seansında öğrenci seçimi zorunlu).
- BEP'te **Veli Görüntülüyor** (yalnızca onaylı/aktif planda açılabilir).
- Gelişim raporunda **Kesinleştir**.

## B.11 İzin bakiyeleri ve KPI tanımları

**İzin bakiyeleri:** **İzin Yönetimi → İzin Bakiyeleri → Bakiye Tanımla** ile her eğitmen için, her izin türü ve yıl için yıllık hakkı girin. Bakiye kaydı olmayan eğitmende izin onayında bakiye kontrolü **yapılmaz**; hak tanımlamazsanız sınırsız izin onaylanabilir. Yıl sonunda **Devir İşlemi** yapılır.

**KPI tanımları (KPI modülü açıksa):** **KPI & Performans → KPI Tanımları**. Sistemle gelen ölçütler değiştirilemez, gerekirse pasifleştirilir. Kuruma özel ölçüt **Yeni KPI tanımı** ile eklenebilir, ancak bugün hesaplamaya katılmaz [doğrulanacak]. Değerler her ay **Performans Anlık Görüntüleri → Dönem Hesapla** ile hesaplanır; otomatik hesaplanmaz.

## B.12 Müşteriden istenecek bilgiler (doldurulabilir şablon)

Bu tabloları müşteriye gönderin (Excel'e kopyalanabilir).

**1. Kurum**

| Bilgi | Değer |
|---|---|
| Kurumun ekranda görünecek adı | |
| Resmi unvan | |
| Vergi dairesi / vergi numarası | |
| Saat dilimi (varsayılan Europe/Istanbul) | |
| Para birimi (varsayılan TRY) | |
| Kullanılacak ek modüller (KPI / kamera / kamp / okul danışmanlığı) | |

**2. Kampüsler**

| Kampüs adı | Şehir | Adres | Telefon |
|---|---|---|---|
| | | | |

**3. Odalar**

| Kampüs | Oda adı | Kapasite | Türü (bireysel / grup / sanal) |
|---|---|---|---|
| | | | |

**4. Personel ve hesaplar**

| Ad soyad | E-posta (kullanıcı adı olacak) | Telefon | Görevi | Rol (Yönetici / Koordinatör / Eğitmen / Muhasebe / Sekreter) | Ders veriyor mu? | BEP onaylayacak mı? |
|---|---|---|---|---|---|---|
| | | | | | | |

> "Ders veriyor" ya da "BEP onaylayacak" sütunlarından biri "evet" olan herkes için eğitmen kaydı açılır ve hesabına bağlanır.

**5. Eğitmen ayrıntıları**

| Ad soyad | Unvan | Uzmanlık alanları | Çalışma türü (tam / yarı zamanlı) | İşe başlama | Ana kampüs | Yıllık izin hakkı (gün) |
|---|---|---|---|---|---|---|
| | | | | | | |

**6. Programlar ve hizmetler**

| Program | Hizmet | Süre (dk) | Haftada kaç kez |
|---|---|---|---|
| | | | |

**7. Paketler ve fiyatlar**

| Paket adı | Bağlı program | Seans sayısı | Geçerlilik (gün) | Fiyat |
|---|---|---|---|---|
| | | | | |

**8. Akademik dönem**

| Dönem adı | Başlangıç | Bitiş | Güncel mi? |
|---|---|---|---|
| | | | |

**9. Şablonlar**

| Belge | Metni gönderildi mi? |
|---|---|
| Hizmet sözleşmesi | |
| KVKK aydınlatma / açık rıza | |
| Kamera izleme rızası (kamera kullanılacaksa) | |
| Foto-video kullanım izni | |

**10. Listeler** (sistemdekilerden farklıysa)

| Liste | Sizin kullandığınız değerler |
|---|---|
| Aday kaynakları (ör. Instagram, tavsiye, doktor) | |
| Ödeme yöntemleri | |
| Seans/terapi türleri | |
| İzin türleri | |

**11. Öğrenciler (2. hafta)**

| Öğrenci ad soyad | Doğum tarihi | Kampüs | Aldığı program(lar) | Veli ad soyad | Yakınlık | Veli telefonu | Veli e-postası | Portal hesabı istiyor mu? | Kalan seans hakkı |
|---|---|---|---|---|---|---|---|---|---|
| | | | | | | | | | |

**12. Haftalık düzen (2. hafta)**

| Öğrenci | Program / hizmet | Eğitmen | Oda | Gün(ler) | Saat | Süre |
|---|---|---|---|---|---|---|
| | | | | | | |

**13. Kullandıkları kelimeler**

| Sistemdeki | Sizdeki |
|---|---|
| Seans hakkı | |
| Eğitimci / eğitmen | |
| Seans | |

## B.13 Önerilen sıra (özet)

**Gün 0 (teknik ekip):** kurumu aç → modülleri ayarla → yöneticiye giriş bilgisini ilet.

**1. hafta (yöneticiyle birlikte, bu sırayla):**
1. Kurum ayarları ve kampüsler (B.2)
2. Referans verileri gözden geçir (B.3)
3. Roller (B.4)
4. Odalar, programlar, hizmetler, paketler (B.6)
5. Akademik dönem (B.7)
6. Eğitmen kayıtları ve hesapları; BEP onaylayacak koordinatörün eğitmen kaydı (B.5.2)
7. Diğer personel hesapları (B.5.1)
8. Sözleşme ve rıza şablonları (B.8)
9. İzin bakiyeleri, KPI (B.11), bildirim şablonları (B.9)

**2. hafta:**
1. Veliler ve öğrenciler, veli–öğrenci bağı
2. **Programlar → Kayıtlar → Kayıt Oluştur** ile her öğrenciyi programına ekleme
3. **Finans → Öğrenci Paketleri → Paket Ata** ile satılmış paketler ve kalan haklar
4. Sözleşmeler ve rızalar
5. **Seans Planlama → Tekrarlayan Programlar** ile haftalık düzen → **Seansları Oluştur**
6. BEP'ler ve hedefler
7. Veli portal hesapları ve erişimleri (B.5.3, B.10)

**Canlıya geçiş testi:** bir öğrenci için uçtan uca deneyin: eğitmen hesabıyla girip takvimde seansı görün, yoklama alın, veliye görünür not yazın, seansı tamamlayın; seans hakkının düştüğünü ve velinin portalda notu gördüğünü kontrol edin. Koordinatör hesabıyla bir BEP'i onaylayın.

---

## [doğrulanacak] listesi

1. Docker Compose v2.24+ gereksinimi (`!override` / `!reset` söz dizimi) — kullanılan sürümde denenmeli.
2. RAM/CPU önerileri yük testiyle doğrulanmadı.
3. `Storage:Local:BaseUrl` adresini sunan bir uç bulunamadı; dosya bağlantılarının nasıl açıldığı.
4. Harici/yönetilen PostgreSQL senaryosu (A.5.4) test edilmedi.
5. Sunucu nginx'i `127.0.0.1:5000`'e bağlandığında API'nin gördüğü kaynak adres (Docker `userland-proxy` ayarına bağlı); KnownProxies değeri buna göre seçilmeli ve A.6'daki doğrulamayla kontrol edilmeli.
6. `UseHttpsRedirection`'ın konteynerde yalnızca uyarı loglaması.
7. Hesap kilidinin (çok sayıda hatalı giriş) ne kadar sürdüğü.
8. Olaylardan otomatik bildirim üretilmediği (tetikleyici işleyicisi bulunamadı).
9. Kuruma özel KPI tanımlarının hesaplamaya katılmadığı.
10. Kurum pasifleştirme için hazır betik olmadığı; SQL ile yapılması.
11. Vergi dairesi/numarasının faturada kullanılıp kullanılmadığı; kurum ayarlarındaki serbest JSON alanını okuyan bir ekran olup olmadığı.
