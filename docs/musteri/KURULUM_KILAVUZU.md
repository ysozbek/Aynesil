# AyNesil — Kurulum Kılavuzu

Bu kılavuz iki bölümden oluşur:

| Bölüm | Kim için | Ne zaman |
|---|---|---|
| **A.0 Yerel/geliştirme ortamı** | Geliştirici, demo hazırlayan | Kendi makinenizde, istediğiniz zaman sıfırdan |
| **A. Sunucu kurulumu** | Sistem yöneticisi | Bir kez (ve her sürüm güncellemesinde) |
| **B. Kurum kurulumu** | Kurulumu yapan kişi + kurum yöneticisi | Her yeni müşteri (kurum) için |

Koddan doğrulanamayan noktalar **[doğrulanacak]** ile işaretlidir; tam liste en sondadır.

---

# A. Sunucu kurulumu

## A.0 Yerel/geliştirme ortamını sıfırdan kurma

Bu bölüm geliştirici makinesi ve demo hazırlığı içindir. Canlı sunucu için A.1'den devam edin.

### Verinin üç katmanı

Veritabanındaki veri üç ayrı yoldan gelir. Hangisinin nereden geldiğini bilmek, neyi silip neyi yeniden kurabileceğinizi belirler.

| Katman | Ne içerir | Nereden gelir | Ne zaman çalışır |
|---|---|---|---|
| **1. Platform** | Şema, izin kataloğu, platform menüleri ve çevirileri, referans verileri (seans türleri, ödeme yöntemleri vb.), mesaj çevirileri | `db/migrations/V*.sql` (Flyway) | Her `docker compose up`'ta kendiliğinden (`aynesil-flyway`); elle bir şey yapılmaz |
| **2. Demo** | İki örnek kurum (`demo_a`, `demo_b`), kullanıcıları, öğrenciler, seanslar, BEP'ler, finans vb. | `scripts/demo-seed` (gerçek API üzerinden) | Yalnızca siz çalıştırınca. **Canlıda asla** |
| **3. Gerçek kurum** | Müşterinin kurumu, kampüsleri, yönetici hesabı | `devops/new_tenant.py` (Bölüm B.1) | Her yeni müşteri için bir kez |

> **Not:** Platform katmanı geçmişten kalan bir örnek kurum da açar: `akran` kurumu ve içinde `admin` / `Admin123!` hesabı (V2, V28; "yalnızca geliştirme" diye işaretli). Bu hesap her yeni kurulumda, canlıda da oluşur. Canlıda ne yapılacağı için A.9 madde 17'ye bakın.

### Adımlar

Gerekenler: Docker (Compose v2.24+), Node.js 20+, Python 3. API'yi Docker dışında `dotnet run` ile çalıştıracaksanız .NET 9 SDK.

```bash
cd ~/Projects/Aynesil
cp -n .env.example .env          # yoksa oluşturur; varsa dokunmaz
```

`.env`'e şu satırı ekleyin (yalnızca geliştirici makinesinde). Bu satırla her `docker compose` komutu `docker-compose.dev.yml`'i de okur; PostgreSQL (5432), Redis (6379), API ve ön yüz portları ana makineye açılır (`dotnet run`, veritabanı istemcisi için):

```bash
COMPOSE_FILE=docker-compose.yml:docker-compose.dev.yml
```

**1) Eski yığını silin (volume'larla birlikte):**

```bash
docker compose down -v --remove-orphans
```

`-v` şu diskleri **kalıcı olarak siler**: veritabanı (`aynesil-db-data`), yüklenen dosyalar (`aynesil-uploads`), Redis ve loglar.

> **Gerçek veri varsa önce yedek alın.** Bu makinede gerçek bir kurumun verisi ya da saklamak istediğiniz bir test verisi varsa silmeden önce:
> ```bash
> docker exec aynesil-db pg_dump -U aynesil_owner -d aynesil -Fc > ~/aynesil-yedek-$(date +%Y%m%d-%H%M).dump
> ```
> Geri yükleme A.7'dedir. Canlı sunucuda `down -v` komutunu **asla** çalıştırmayın.

**2) Yığını yeniden kurun:**

```bash
docker compose up -d --build
docker compose logs aynesil-flyway | tail -5      # "Successfully applied N migrations"
docker compose ps                                 # flyway "exited (0)", diğerleri healthy
```

Flyway boş veritabanına bütün migration'ları uygular; **platform katmanı** (izinler, menüler, referans veriler) bu adımda kendiliğinden gelir. Ayrı bir seed komutu yoktur.

**3) Demo verisini kurun:**

```bash
scripts/demo-seed/run.sh              # iki demo kurum + tüm veri (birkaç dakika)
```

Sonunda `hata: 0` görmelisiniz. Demo verisini daha sonra baştan kurmak için `scripts/demo-seed/run.sh --reset` yeterlidir: yalnızca `demo_a`/`demo_b`'yi siler ve yeniden kurar, başka kuruma ve platform verisine dokunmaz. API kodu değiştiyse `--build` ekleyin.

**4) Giriş:** `http://localhost:5173`. Ortak demo şifresi `DemoSifre!2026` (`scripts/demo-seed/fixtures.json`).

| Rol | demo_a (Demo Gökkuşağı Merkezi) | demo_b (Demo Papatya Merkezi) |
|---|---|---|
| Kurum yöneticisi (eğitmen kaydına bağlı) | `admin@demo-a.test` | `admin@demo-b.test` |
| Koordinatör (eğitmen kaydına bağlı) | `koordinator@demo-a.test` | `koordinator@demo-b.test` |
| Eğitmen | `egitmen@demo-a.test` | `egitmen@demo-b.test` |
| Muhasebe | `muhasebe@demo-a.test` | `muhasebe@demo-b.test` |
| Veli (iki çocuk, kamera yetkili) | `veli1@demo-a.test` | `veli1@demo-b.test` |
| Veli (tek çocuk) | `veli2@demo-a.test` | `veli2@demo-b.test` |
| İlk girişte şifre değiştirme örneği | `ilkgiris@demo-a.test` | `ilkgiris@demo-b.test` |

Ayrıntı (her kurumda ne var, bugünün seansları): `scripts/demo-seed/README.md`.

**5) İsteğe bağlı kontroller:**

```bash
scripts/smoke-api/run.sh        # ön yüzün kullandığı uçların duman testi
scripts/smoke-proxy/run.sh      # gerçek istemci IP'si zinciri (A.6)
```

Giriş hız sınırı (dakikada 10) yerelde bütün istekleri aynı adresten görür. Betikleri art arda çalıştırırken `429` alırsanız bir dakika bekleyin.

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
| Docker | Docker Engine + Compose eklentisi (v2.24 veya üstü) | `env_file` içindeki `required: false` ve geliştirme ekindeki `!override` için v2.24+ gerekir [doğrulanacak] |
| PostgreSQL | 15 veya üstü, hedef **17** | Compose kendi `postgres:17-alpine` konteynerini kurar. Harici/yönetilen PostgreSQL kullanılacaksa bkz. A.5.4 |
| Redis | 7 | Compose kurar (menü/referans önbelleği, parola sıfırlama jetonları, hesap kilidi) |
| RAM | En az 4 GB, önerilen 8 GB | API (.NET 9) + PostgreSQL + Redis (256 MB sınırlı) + nginx. Tek sunuculu SaaS'ta kurum sayısı arttıkça PostgreSQL için bellek ekleyin [doğrulanacak: yük testi yapılmadı] |
| CPU | 2 çekirdek, önerilen 4 | İmaj derlemesi (`docker compose build`) en çok CPU isteyen adımdır |
| Disk | En az 40 GB SSD | Veritabanı, yüklenen dosyalar (`aynesil-uploads`), 30 günlük log dosyaları ve yerel yedekler. Yedekleri ayrıca sunucu dışına kopyalayın |
| Ağ | 80 ve 443 dışarıya açık; 5432, 6379, 5000, 5173 **kapalı** | Compose dosyası varsayılan olarak kapatır; bkz. A.4 "Portlar ve güvenlik duvarı" |

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

### `.env` nasıl okunur

`docker-compose.yml`, `.env`'i iki yoldan kullanır:

1. **Yer tutucular:** compose dosyasındaki `${...}` değerleri (veritabanı şifreleri, Redis şifresi, portlar, ağ) `.env`'den doldurulur.
2. **API'nin ortamı (`env_file`):** `.env`'deki **her** değişken API konteynerine geçer. `AYNESIL_` ile başlayanları API okur (ör. `AYNESIL_Cors__AllowedOrigins__0`, `AYNESIL_Jwt__AccessTokenExpiryMinutes`, `AYNESIL_ForwardedHeaders__KnownProxies__0`). Override dosyası gerekmez.

Compose dosyasının `environment` bloğundaki değerler `env_file`'dan önceliklidir. Bu yüzden oradaki ayarlar `${AD:-varsayılan}` biçimindedir: `.env`'e aynı adla yazılan değer kullanılır. Bağlantı dizeleri (`AYNESIL_ConnectionStrings__*`) ise compose dosyasında şifrelerden kurulur; `.env`'e yazmayın.

> `.env`'deki her şey API konteynerinin ortamında görünür (sahip rolünün şifresi `POSTGRES_PASSWORD` dahil). `.env`'e işi olmayan gizli bilgi koymayın.

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
| `AYNESIL_REDIS_PASSWORD` | Redis şifresi. Redis yalnızca iç ağdadır ama şifresiz bırakılmaz; API bağlantısı compose'da buna göre kurulur | **Evet** (varsayılan `changeme_redis` canlıda kabul edilemez) | `<gizli>` | `openssl rand -hex 24` (yalnızca harf ve rakam: bağlantı dizesinde virgül ve eşittir ayırıcıdır) |
| `AYNESIL_NETWORK_SUBNET` | Compose iç ağının adres aralığı. API ve ön yüz nginx'i `X-Forwarded-For`'u yalnızca bu ağdan kabul eder (A.6) | Hayır (vars. `172.30.0.0/24`) | `172.30.0.0/24` | Sunucudaki başka bir ağla çakışırsa değiştirin; sonra `docker compose down && docker compose up -d` |
| `API_PORT` | API'nin **127.0.0.1** üzerinde yayınlandığı port (dışarıya açık değil) | Hayır (vars. `5000`) | `5000` | — |
| `FRONTEND_PORT` | Ön yüz nginx'inin yayınlandığı port | Hayır (vars. `5173`) | `5173` | — |
| `FRONTEND_BIND` | Ön yüzün yayınlandığı adres | Hayır (vars. `127.0.0.1`) | `127.0.0.1` | Ters vekil kullanılıyorsa değiştirmeyin. Yalnızca ters vekilsiz (TLS'siz) kurulumda `0.0.0.0` |
| `COMPOSE_FILE` | Ek compose dosyaları | **Canlıda yazmayın** | — | Yalnızca geliştirici makinesinde `docker-compose.yml:docker-compose.dev.yml` (A.0) |

Şifre notları:
- `docker/init-db.sh` uygulama rolünü **yalnızca veritabanı diski boşken ilk açılışta** oluşturur ve şifreyi SQL içine tek tırnakla yazar. Şifrede tek tırnak (`'`) kullanmayın.
- Şifreyi sonradan değiştirmek `.env`'i değiştirmekle olmaz; önce veritabanında değiştirin, sonra `.env`'i güncelleyip API'yi yeniden başlatın:
  ```bash
  docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c "ALTER ROLE aynesil_app PASSWORD '<yeni>'"
  ```

**API ayarları (`.env`'e yazılır)**

| Anahtar (ortam değişkeni) | Ne işe yarar | Zorunlu | Örnek / varsayılan |
|---|---|---|---|
| `AYNESIL_ConnectionStrings__DefaultConnection` | API'nin veritabanı bağlantısı (uygulama rolüyle). Hangfire da bunu kullanır. | Compose kurar | Yazmayın (şifreler `POSTGRES_*`/`AYNESIL_DB_APP_*`'den gelir) |
| `AYNESIL_ConnectionStrings__Redis` | Redis bağlantısı. Boşsa bellek içi önbelleğe düşer. | Compose kurar | Yazmayın (`aynesil-redis:6379,password=<AYNESIL_REDIS_PASSWORD>,abortConnect=false`) |
| `AYNESIL_Jwt__AccessTokenExpiryMinutes` | Erişim jetonu süresi (dk) | Hayır | `60` (Development'ta `120`) |
| `AYNESIL_Jwt__RefreshTokenExpiryDays` | Yenileme jetonu süresi (gün) | Hayır | `30` |
| `Jwt:Algorithm` | `appsettings.json`'da var ama kod her zaman HS256 kullanır | Hayır | Değiştirmeyin |
| `AYNESIL_ForwardedHeaders__KnownNetworks__0` | Güvenilen vekil ağı (CIDR). Gerçek istemci IP'si `X-Forwarded-For`'dan yalnızca bu ağdan gelen bağlantılarda alınır | Hayır: compose iç ağını verir (`AYNESIL_NETWORK_SUBNET`) | `172.30.0.0/24` |
| `AYNESIL_ForwardedHeaders__KnownNetworks__1`…, `AYNESIL_ForwardedHeaders__KnownProxies__0`… | Ek güvenilen vekiller (ters vekil başka bir makinedeyse) | Hayır | `10.0.0.5` |
| `AYNESIL_RateLimiting__Auth__Login__PermitLimit` / `__Window` | Giriş hız sınırı | Hayır | `10` / `00:01:00` |
| `AYNESIL_RateLimiting__Auth__PasswordResetRequest__…` | Şifre sıfırlama isteği sınırı | Hayır | `3` / `01:00:00` |
| `AYNESIL_RateLimiting__Auth__PasswordResetConfirm__…` | Şifre sıfırlama onayı sınırı | Hayır | `10` / `00:01:00` |
| `AYNESIL_Cors__AllowedOrigins__0`, `__1`… | Tarayıcıdan API'yi çağırabilecek adresler | Hayır (ön yüz API'yi aynı adresten, `/api` ile çağırır) | Vars. `http://localhost:<FRONTEND_PORT>`; canlıda `https://app.ornek.com` |
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

### Canlı `.env` örneği

```bash
POSTGRES_PASSWORD=<openssl rand -base64 32 | tr -d '/+=' | cut -c1-32>
AYNESIL_DB_APP_PASSWORD=<farklı bir değer>
AYNESIL_REDIS_PASSWORD=<openssl rand -hex 24>
AYNESIL_Jwt__Secret=<openssl rand -base64 64 | tr -d '\n'>
AYNESIL_Jwt__Issuer=https://app.ornek.com
AYNESIL_Cors__AllowedOrigins__0=https://app.ornek.com
AYNESIL_Storage__Local__BaseUrl=https://app.ornek.com/files
# COMPOSE_FILE satırı YOK (geliştirme eki portları dışarı açar)
```

### Portlar ve güvenlik duvarı

`docker-compose.yml` üretim için yazılmıştır:

| Servis | Yayın | Kim erişir |
|---|---|---|
| PostgreSQL (5432) | **Yok** | Yalnızca compose iç ağı (API, Flyway); betikler `docker exec` ile |
| Redis (6379) | **Yok**, ayrıca şifreli | Yalnızca API |
| API (5000) | `127.0.0.1:${API_PORT}` | Sunucunun kendisi (ters vekil, izleme, betikler) |
| Ön yüz (5173) | `${FRONTEND_BIND:-127.0.0.1}:${FRONTEND_PORT}` | Sunucudaki ters vekil |

**Neden `ufw` yetmez:** Docker, yayınladığı portlar için iptables'a kendi kurallarını yazar ve bu kurallar `ufw`'nin kurallarından **önce** işlenir. Yani `ufw deny 5432` dese bile `0.0.0.0:5432`'ye yayınlanmış bir konteyner portu dışarıdan erişilebilir kalır. Port kapatma bu yüzden güvenlik duvarında değil, compose dosyasındaki yayın adresinde yapılır: hiç yayınlamamak ya da `127.0.0.1`'e bağlamak. `ufw` ise 22, 80, 443 gibi ana makine servisleri için kullanılmaya devam eder.

Geliştirme eki (`docker-compose.dev.yml`) PostgreSQL, Redis, API ve ön yüzü tüm arabirimlere açar. **Canlı sunucuda kullanmayın** (`.env`'de `COMPOSE_FILE` olmamalı).

Kontrol:

```bash
# Sunucuda: yalnızca 127.0.0.1 görünmeli
docker compose ps --format '{{.Name}} {{.Ports}}'
# Başka bir makineden: hepsi "kapalı/zaman aşımı" vermeli
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

Redis şifreli olduğu için `redis-cli`'yi konteyner içinden çalıştırın; şifre konteynerde tanımlıdır (`REDISCLI_AUTH`): `docker exec aynesil-redis redis-cli ping` → `PONG`.

### A.5.3 Demo verisi (isteğe bağlı, canlıda kullanmayın)

Demo/test sunucusunda iki örnek kurum kurmak için `scripts/demo-seed/run.sh` (A.0 adım 3). Hesaplar ve ortak şifre A.0'da ve `scripts/demo-seed/README.md`'dedir. Bu şifre herkesçe bilindiği için **canlı sunucuda çalıştırmayın**.

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
                     └── /  ──▶ 127.0.0.1:5173  aynesil-web (nginx: derlenmiş ön yüz)
                                                  └── /api/ ──▶ aynesil-api:5000 (compose iç ağı)
```

Her şeyi ön yüz konteynerine (`127.0.0.1:5173`) gönderin; o `/api/`'yi API'ye iletir. İsterseniz `/api/`'yi doğrudan `127.0.0.1:5000`'e de gönderebilirsiniz; iki yol da gerçek istemci IP'sini korur.

### Neden önemli: hız sınırı ve gerçek IP

Anonim giriş uçlarının hız sınırı (giriş 10/dk, şifre sıfırlama isteği 3/saat, sıfırlama onayı 10/dk) **istemci IP'si başına** ve hedef kullanıcı adı başına ayrı ayrı uygulanır (`Api/RateLimiting/AuthRateLimiter.cs`). Vekil arkasında bağlantının kaynağı vekilin kendisidir. IP zinciri kurulmamışsa bütün kullanıcılar tek bir IP'den geliyor görünür ve tek sayacı paylaşır: sabah on kişi giriş yaptığında on birinci kişi bir dakika boyunca "çok fazla istek" alır. Giriş kayıtlarındaki ve log'lardaki IP de anlamsızlaşır.

Zincir varsayılan kurulumda hazırdır:

1. **Sunucudaki nginx** istemcinin adresini `X-Forwarded-For` olarak yazar (aşağıdaki örnekte: ekleme değil, üzerine yazma).
2. **Ön yüz nginx'i** (`docker/nginx.conf.template`) bu başlığı yalnızca compose ağından gelen bağlantıda kabul eder. Sunucudaki nginx'in `127.0.0.1:5173` bağlantısı Docker ağ geçidi üzerinden bu ağdan gelir. Başka bir yerden gelen başlık yok sayılır. Ardından API'ye giden `X-Forwarded-For`'u tek değerle, belirlediği istemci adresiyle **üzerine yazar**.
3. **API** `X-Forwarded-For`'u yalnızca compose ağından (`AYNESIL_NETWORK_SUBNET`, vars. `172.30.0.0/24`) gelen bağlantıda kabul eder (`AYNESIL_ForwardedHeaders__KnownNetworks__0`, compose varsayılanı).

Bu yüzden istemcinin kendisinin gönderdiği sahte bir `X-Forwarded-For` hız sınırını aşmaya yaramaz. Ayar gerekmez; ters vekil başka bir makinedeyse `.env`'e `AYNESIL_ForwardedHeaders__KnownProxies__0=<vekil-ip>` ekleyin ve ön yüz nginx'inin o adresten de başlık kabul etmesi için şablondaki `set_real_ip_from` satırını çoğaltın.

> **Docker Desktop (macOS/Windows):** ana makineden gelen bağlantılar VM adresinden (ör. `192.168.65.1`) gelir ve güvenilmez. Bu yalnızca geliştirme makinesini etkiler; Linux sunucuda ana makine bağlantısı compose ağının ağ geçidinden gelir [doğrulanacak: Docker'ın `userland-proxy` ayarına bağlıdır; `scripts/smoke-proxy/run.sh` 4. adımda görülen adresi yazar].

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

    # JWT tüm izinleri taşıdığı için büyük olabilir (docker/nginx.conf.template ile aynı değerler)
    large_client_header_buffers 8 64k;
    client_header_buffer_size   32k;
    client_max_body_size        20m;

    add_header Strict-Transport-Security "max-age=31536000" always;

    location / {
        proxy_pass http://127.0.0.1:5173;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        # Ekleme değil, ÜZERİNE YAZMA: istemcinin gönderdiği sahte X-Forwarded-For atılır
        proxy_set_header X-Forwarded-For   $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
}
```

Sertifika için örnek: `sudo certbot --nginx -d app.ornek.com`.

### Doğrulama

```bash
# Kendi bilgisayarınızdan tarayıcıyla giriş yapın, sonra sunucuda:
docker exec aynesil-db psql -U aynesil_owner -d aynesil -c \
  "select ip_address, issued_at from iam.auth_session order by issued_at desc limit 5"
docker compose logs --since 10m aynesil-api | grep "Login succeeded"
```

Son satırda ve log'da **kendi genel IP adresiniz** görünmeli. `172.x.x.x` görünüyorsa zincir kopuktur (sunucudaki nginx `X-Forwarded-For` yazmıyor ya da başka bir adresten bağlanıyor). İstek log'larında da her satırda `ClientIp` vardır.

Otomatik kontrol: `scripts/smoke-proxy/run.sh` (demo verisi gerekir; bu yüzden demo/test sunucusunda). Aynı vekil arkasındaki iki istemcinin ayrı sayaç kullandığını, compose ağı dışından gelen sahte başlığın yok sayıldığını ve giriş kaydında gerçek IP'nin göründüğünü sınar.

### HTTPS notları

- TLS sunucu nginx'inde sonlanır; API konteyner içinde HTTP dinler. `X-Forwarded-Proto` yalnızca güvenilen vekilden geldiğinde aktarılır; API isteği HTTPS olarak görür.
- `app.UseHttpsRedirection()` konteynerde HTTPS portu olmadığından yönlendirme yapmaz; açılıştan sonra bir kez "Failed to determine the https port for redirect" uyarısı loglanır, zararsızdır.
- Hız sınırı sayaçları **API süreci belleğindedir**. API'yi birden fazla örnekle (ölçekleme, yük dengeleme) çalıştırırsanız etkin sınır örnek sayısıyla çarpılır. Çok örnekli dağıtımdan önce Redis tabanlı bir sınırlayıcı gerekir. Tek örnekli kurulumda sorun yoktur.

## A.7 Yedekleme ve geri yükleme

### Neyi yedekleyeceksiniz

| Ne | Nerede | Yedek gerekli mi |
|---|---|---|
| Veritabanı | `aynesil-db` konteyneri, `aynesil_aynesil-db-data` diski | **Evet** — tüm iş verisi, kullanıcılar, Hangfire işleri |
| Yüklenen dosyalar | `aynesil_aynesil-uploads` diski | Evet |
| `.env` (ve varsa `docker-compose.override.yml`) | `/opt/aynesil` | **Evet, ayrı ve şifreli** (gizli anahtarlar) |
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
docker exec aynesil-redis redis-cli FLUSHALL      # eski önbellek geri yüklenen veriyle çelişmesin (şifre konteynerde tanımlı)
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

### Ağ ve Redis ayarlarının değiştiği sürüme geçiş

Compose iç ağına sabit alt ağ (`AYNESIL_NETWORK_SUBNET`) ve Redis'e şifre (`AYNESIL_REDIS_PASSWORD`) gelen sürüme ilk geçişte:

1. `.env`'e `AYNESIL_REDIS_PASSWORD` ekleyin (A.4).
2. Eski `docker-compose.override.yml` yalnızca port kapatma ve ForwardedHeaders içinse silin. Bunlar artık varsayılan; diğer ayarlarını `.env`'e taşıyın.
3. Ağın yeniden oluşturulması gerekir: `docker compose down && docker compose up -d --build`. **`-v` kullanmayın** (veriler silinir).
4. A.6 "Doğrulama" ile gerçek IP'yi kontrol edin.

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
| 1 | `.env`'deki tüm `changeme…`/`CHANGE_ME…` değerleri değişti (Redis şifresi dahil); `AYNESIL_Jwt__Secret` en az 64 karakter; `COMPOSE_FILE` satırı yok | `grep -iE "change\|COMPOSE_FILE" /opt/aynesil/.env` boş dönmeli |
| 2 | `.env` izinleri `600`, repoya eklenmemiş | `ls -l .env`, `git status` |
| 3 | 5432, 6379, 5000, 5173 dışarıdan kapalı (compose varsayılanı; `ufw` bunu sağlamaz, A.4) | Başka makineden `nc -zv` |
| 4 | HTTPS çalışıyor, HTTP 443'e yönleniyor | Tarayıcı |
| 5 | Gerçek istemci IP'si: `iam.auth_session.ip_address` ve "Login succeeded" log satırı gerçek istemci IP'lerini gösteriyor | A.6 "Doğrulama" |
| 6 | `ASPNETCORE_ENVIRONMENT=Production` | `docker compose logs aynesil-api \| grep "starting on"` → `Production`. Development'ta API belgeleri (`/openapi`, `/scalar`) açılır ve log seviyesi `Debug` olur |
| 7 | **E-posta sağlayıcısı yok.** `INotificationService` olarak `LoggingNotificationService` kayıtlı: hiçbir e-posta/SMS gitmez, yalnızca şablon kodu ve alıcı loglanır. Sonuç: **"Şifremi unuttum" çalışmaz** (ekran var ama bağlantı gitmez), davet e-postası gitmez. Geçici şifreler kişiye elle iletilir. Şifresini unutan kullanıcının şifresini **kurum yöneticisi** sıfırlar (Kullanıcılar → kullanıcı → **Şifreyi Sıfırla**, B.1.2) | Müşteriye yazılı olarak bildirin |
| 8 | Hız sınırı **tek örnek** içindir; API tek örnekle çalışıyor | `docker compose ps` (bir `aynesil-api`) |
| 9 | Loglar: dosya logları `aynesil_aynesil-logs` diskinde `logs/aynesil-YYYYMMDD.log`, 30 gün saklanır; konsol `docker compose logs`. Disk dolmasın diye Docker log sürücüsüne boyut sınırı verin (`/etc/docker/daemon.json` → `"log-opts": {"max-size": "50m", "max-file": "5"}`) | `docker volume inspect`, `df -h` |
| 10 | İzleme: `127.0.0.1:5000/health` dakikada bir kontrol ediliyor; disk doluluğu ve yedek başarısı izleniyor | İzleme aracı |
| 11 | Yedekleme çalışıyor ve bir geri yükleme provası yapıldı | A.7 |
| 12 | **KPI'lar otomatik hesaplanmaz.** Arka plan işi kayıtlı değil; tek yol `KPI & Performans → Performans Anlık Görüntüleri → Dönem Hesapla` düğmesi (içinde bulunulan ay için). KPI modülü açık kurumlara bunu söyleyin | Müşteriye bildirin |
| 13 | Olaylardan bildirim üretilmiyor (ör. "seans yarın"); gelen kutusu bugün kendiliğinden dolmaz [doğrulanacak] | Müşteriye bildirin |
| 14 | Demo verisi canlı sunucuda yok | `select code from core.corporation` içinde `demo_a`/`demo_b` olmamalı |
| 15 | Docker diskleri ve `/opt/aynesil` için sunucu düzeyinde disk şifreleme / erişim kısıtı | — |
| 16 | Kurum askıya alındığında girişler ve açık oturumlar kesiliyor | B.1.4'teki komuttan sonra o kurumun kullanıcısıyla giriş "Kurumunuzun hesabı etkin değil" vermeli |
| 17 | **Platform migration'larının açtığı `admin` hesabı** (`akran` kurumu, şifre `Admin123!`, A.0 not) canlıda bu şifreyle açık kalmamalı. `akran` müşteriniz değilse hesabı askıya alın; müşterinizse kurum yöneticisinin ilk iş şifreyi değiştirmesini sağlayın | `docker exec aynesil-db psql -U aynesil_owner -d aynesil -Atc "select username, status, crypt('Admin123!', password_hash) = password_hash from iam.user_account where username = 'admin'"` → `t` ise açık. Askıya almak: `update iam.user_account set status = 'suspended' where username = 'admin'` |

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
- **Şifresini unutan kullanıcı:** "Şifremi unuttum" bugün çalışmaz (e-posta gitmez). Bunun yerine kurum yöneticisi **Platform Yönetimi → Kullanıcılar → kullanıcı → Şifreyi Sıfırla**'ya basar (izin: `user:reset_password`, Yönetici rolünde var). Sistem:
  - yeni bir geçici şifre üretir ve **bir kez** gösterir (Kopyala düğmesi; pencere kapanınca bir daha görülmez),
  - kullanıcının eski şifresini geçersiz kılar ve **açık oturumlarını kapatır**,
  - kullanıcıyı ilk girişte şifresini değiştirmeye zorlar,
  - çok sayıda hatalı deneme nedeniyle kilitlenmiş hesabın kilidini hemen kaldırır.

  Yönetici kendi şifresini buradan sıfırlayamaz; kendi şifresini **Şifre Değiştir**'den değiştirir. Başka kurumun kullanıcısı bu ekranda görünmez.
- **Hesap kilidi:** aynı hesaba 5 hatalı şifre denemesinden sonra hesap 15 dakika kilitlenir; süre dolunca kendiliğinden açılır ya da yönetici şifreyi sıfırlayınca hemen açılır.
- **Kurumun tek yöneticisi şifresini kaybederse** (bu yüzden en az iki yönetici önerilir, B.4) şifreyi sıfırlayacak kimse kalmaz. Bu durumda teknik ekip veritabanından geçici şifre verir:
  ```bash
  GECICI=$(openssl rand -base64 18 | tr -d '/+=')
  docker exec aynesil-db psql -U aynesil_owner -d aynesil -c \
    "update iam.user_account
        set password_hash = crypt('$GECICI', gen_salt('bf', 12)), must_change_password = true
      where username = 'mudur@ornek.com'"
  echo "Geçici şifre: $GECICI"
  ```

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
# Askıya al (geçici)          → status='suspended'
# Kapat (sözleşme bitti)       → status='closed'
docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c \
  "update core.corporation set status='suspended' where code='ornek'"
# Yeniden aç
docker exec -it aynesil-db psql -U aynesil_owner -d aynesil -c \
  "update core.corporation set status='active' where code='ornek'"
```

Kurum `active` değilken:
- **Giriş** reddedilir: kullanıcı doğru şifreyle girse de "Kurumunuzun hesabı etkin değil. Lütfen kurum yöneticinizle ya da destek ekibiyle iletişime geçin." mesajını alır (İngilizce arayüzde İngilizce). Şifre yanlışsa yalnızca "Kullanıcı adı veya parola hatalı" der; kurumun durumu açık edilmez.
- **Oturum yenileme** reddedilir ve oturum kapatılır.
- **Açık oturumlar** en geç **1 dakika** içinde (API'nin kurum durumu önbelleği) kesilir; kullanıcı giriş sayfasına düşer.

Kullanıcıları ayrıca askıya almak gerekmez. Kurum yeniden açılınca kullanıcılar eski şifreleriyle normal giriş yapar.

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

Hazır gelen iki rol vardır: **Yönetici** ve **Veli (portal)**. Aşağıdaki öneriler demo kurumların rolleriyle aynıdır (`scripts/demo-seed/phases/identity.mjs`; demo seed rolleri her çalıştırmada bu kümeye eşitler). Demo hesaplarıyla her rolün günlük işini deneyebilirsiniz (A.0). İzin kodları ekranda kodla birlikte görünür.

### Yönetici (hazır gelir)
Platform işlemleri dışında **her şey**. Kullanıcı açma, rol verme ve eğitmen/veli hesabı bağlama yalnızca bu roldedir. Kurumda en az iki kişiye verin (biri tatildeyken kilitlenmemek için).

### Koordinatör
**Yönetici rolündeki her şey, şunlar hariç:**

| Hariç | İzinler | Neden |
|---|---|---|
| Kurum yönetimi | kullanıcı (`user:*`, `user:read` hariç), rol ve izin (`role:*`, `permission:*`), kurum (`corporation:*`), ayarlar (`settings:*`), entegrasyon, denetim kaydı (`audit:*`), menü (`menu:*`), referans veri (`ref_data:*`), kampüs ekleme/düzenleme/silme (`campus:create/update/delete`), bildirim tetikleyicileri (`notification_trigger:manage`) | Yönetici işi |
| Finans | `finance`, `finance_payment`, `finance_report`, `invoice`, `refund`, `credit_ledger`, `package_definition`, `student_package`, `scholarship`, `promotion`, `discount` | Muhasebe işi |
| Rapor ve KPI yönetimi | `report:*`, `kpi_snapshot:*`, `kpi:manage` | |
| Başkasının kaydını değiştirme, ana kayıt silme | `session_note:delete`, `case_note:delete` (başkalarının notunu düzenleme/silme), `student:delete`, `guardian:delete`, `educator:delete` | Klinik kayıt ve ana kayıt güvenliği; yönetici yapar |
| Veli tarafı | `portal:access`, `parent_feedback:create/submit`, `survey:respond` | Personel rolüne ait değil (verilirse menüde Veli Portalı çıkar) |

**Ek olarak `user:read`:** aday formunda (atanan kişi) ve değerlendirme oturumu formunda (değerlendiren) kişi seçmek için kullanıcı listesi gerekir. Bu izin kullanıcı listesini **yalnızca görüntülemeyi** sağlar; kullanıcı açma, şifre sıfırlama ve hesap bağlama yetkisi vermez.

Günlük operasyonu yürütür: aday, öğrenci, veli, değerlendirme, BEP (onay dahil), program ve kayıt, seans planlama, telafi onayı, toplantı, izin onayı, sözleşme ve rıza. BEP onaylayacağı ve not yazabileceği için koordinatörün **bir eğitmen kaydı olmalı ve hesabına bağlı olmalıdır** (B.5.2).

### Eğitmen

| Alan | İzinler |
|---|---|
| Öğrenci, veli (okuma) | `student:read`, `guardian:read`, `enrollment:read`, `program:read`, `campus:read`, `room:read`, `educator:read` |
| Seans | `session:read`, `session:complete` (seansı tamamlar; paketten hak düşer), `session:manage_goals` (seansa hedef ekler/çıkarır) |
| Yoklama | `attendance:read`, `attendance:record` |
| Seans notu | `session_note:write`. **`session_note:delete` vermeyin**: bu izin başkalarının notlarını düzenleme ve silme yetkisidir (yönetici yetkisi) |
| Hedefler | `student_goal:read`, `student_goal:create`, `student_goal:update`, `student_goal:change_status`, `goal_progress:read`, `goal_progress:record`, `goal_library:read`, `goal_template:read` (hedef silme `student_goal:delete` yok) |
| BEP | `education_plan:read`, `education_plan:add_review`. BEP'i koordinatör onaylar; onay yetkisi verilecek kıdemli eğitmenlere ayrıca `education_plan:approve` |
| Vaka notu | `case_note:create`, `case_note:read`, `case_note:update` (gizli notları okuyacaksa `case_note:read_confidential`) |
| Telafi | `makeup_request:read`, `makeup_request:request` |
| İzin | `leave_request:submit`, `leave_request:read`, `leave_request:cancel`, `leave_balance:read` |
| Diğer | `meeting:read`, `assessment_session:read`, `notification:read`, `kpi_dashboard:read` (KPI açıksa), `parent_feedback:read` |
| Kamp (modül açıksa) | `camp:read`, `camp_attendance:read/record`, `camp_participation:read/record` |

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
5. "Şifremi unuttum" ekranı vardır ama e-posta gitmediği için **bugün çalışmaz**. Şifresini unutan kişinin şifresini kurum yöneticisi **Kullanıcılar → kullanıcı → Şifreyi Sıfırla** ile sıfırlar; yeni geçici şifre yine bir kez gösterilir ve bu adımlar tekrarlanır (B.1.2).

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

1. Docker Compose v2.24+ gereksinimi (`env_file` `required: false`, geliştirme ekindeki `!override`): Compose v5.1 ile denendi; sunucudaki sürümde `docker compose config` ile kontrol edilmeli.
2. RAM/CPU önerileri yük testiyle doğrulanmadı.
3. `Storage:Local:BaseUrl` adresini sunan bir uç bulunamadı; dosya bağlantılarının nasıl açıldığı.
4. Harici/yönetilen PostgreSQL senaryosu (A.5.4) test edilmedi.
5. Linux sunucuda, sunucu nginx'inin `127.0.0.1:5173`/`127.0.0.1:5000` bağlantısının konteynerde compose ağının ağ geçidinden geldiği (Docker `userland-proxy` ayarı). Docker Desktop'ta denendi (orada VM adresi görünür); Linux'ta `scripts/smoke-proxy/run.sh` 4. adımı ve A.6 "Doğrulama" ile kontrol edilmeli.
6. Olaylardan otomatik bildirim üretilmediği (tetikleyici işleyicisi bulunamadı).
7. Kuruma özel KPI tanımlarının hesaplamaya katılmadığı.
8. Kurum pasifleştirme için hazır betik olmadığı; SQL ile yapılması.
9. Vergi dairesi/numarasının faturada kullanılıp kullanılmadığı; kurum ayarlarındaki serbest JSON alanını okuyan bir ekran olup olmadığı.
