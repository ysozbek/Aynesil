# Akış 17 — Sıfırdan kurum kurulumu

## Bu akış ne anlatıyor?
Sisteme yeni bir müşteri kurumun eklenip ilk gün kullanılabilir hale gelmesini anlatır: kurumun açılması, kurum yöneticisinin ilk girişi, ek modüllerin seçilmesi, rollerin ve personel hesaplarının hazırlanması, programların, paketlerin, şablonların girilmesi ve ilk öğrencinin uçtan uca denenmesi. Diğer akışların hepsi bu kurulumun üstüne kurulur. Tam parametre listeleri ve müşteriden istenecek bilgiler tablosu Kurulum Kılavuzu'nun B bölümündedir (`docs/musteri/KURULUM_KILAVUZU.md`).

## Kim yapar?
- **Teknik ekip:** kurumu açar, ek modülleri açar/kapatır, kurumu pasifleştirir, şifresini unutan kullanıcıya yeni geçici şifre verir. Bu işlerin ekranı yoktur; kurum yöneticisi yapamaz.
- **Kurum yöneticisi:** geri kalan her şey. Hesap açma ve eğitmen/veli kaydını hesaba bağlama kullanıcı yönetimi izni istediği için yöneticidedir; koordinatör yapamaz.

## Adımlar
1. **Bilgileri topla:** müşteriden kurum adı, kampüsler, odalar, personel listesi (kim ders veriyor, kim BEP onaylayacak), programlar ve hizmetler, paketler ve fiyatlar, akademik dönem, sözleşme ve rıza metinleri istenir. Doldurulacak tablolar Kurulum Kılavuzu B.12'dedir.
2. **Kurumu aç (teknik ekip):** kurulum aracı önce deneme modunda çalıştırılır, sorun yoksa gerçekten çalıştırılır. Kurum, kampüsler, **Yönetici** rolü, **Veli (portal)** rolü ve ilk yönetici hesabı tek seferde açılır. Yöneticinin geçici şifresi yalnızca bir kez ekrana yazılır. Kullanıcı adı tüm sistemde benzersiz olmalıdır; bu yüzden e-posta adresi kullanılır.
3. **Ek modülleri seç (teknik ekip):** kamp, okul danışmanlığı, kamera, anketler, bakım ekibi ve KPI yeni kurumda **kapalı** gelir. Müşteri kullanacaksa teknik ekip açar; değişiklik hemen etkili olur. Anketler ve bakım ekibinin ekranı olmadığı için açılmaz; kamerada canlı izleme yoktur (Akış 9), kamp ve danışmanlıkta günlük işlemlerin bir kısmı ekransızdır.
4. **Yöneticinin ilk girişi:** teknik ekip kullanıcı adını ve geçici şifreyi ayrı kanallardan (ör. biri e-postayla, diğeri telefonla) iletir. Yönetici girer, sistem onu doğrudan **şifre değiştirme** sayfasına götürür; başka sayfa açılmaz. Yeni şifreyle yeniden girer.
5. **Kurum ayarları:** `Platform Yönetimi → Kurumlar → kurum → Ayarlar`: dil, para birimi, saat dilimi, vergi dairesi ve numarası. **Modüller** listesi burada salt okunur görünür. Logo yükleme alanı yoktur. `Kampüsler`'den adres ve telefonlar tamamlanır.
6. **Listeleri gözden geçir:** `Referans Veriler`'de aday kaynakları, ödeme yöntemleri, seans türleri, izin türleri gibi listelere bakılır. Sistemle gelen değerler düzenlenemez, yalnızca kurum için gizlenir; kurumun kendi değerleri eklenir (Akış 16, adım 3). Menüye dokunmak gerekmez: sistem menüsü salt okunurdur.
7. **Rolleri tanımla:** `Roller → Rol Ekle`, sonra **İzinler** sekmesi. Önerilen set: Koordinatör, Eğitmen, Muhasebe, gerekirse Sekreter (izin listeleri Kurulum Kılavuzu B.4). İki ince nokta:
   - "Eğitimciler" menüsü bugün kullanıcı listesini görme iznini de istiyor; koordinatör ve eğitmen rollerine bu izni ekleyin (Akış 16, Dikkat).
   - Eğitmenin seansı tamamlaması ve seansa hedef eklemesi için ilgili izinleri role ekleyin; demo rolünde yoktur.
8. **Odalar, programlar, paketler:** `Seans Planlama → Odalar`; `Programlar → Yeni Program`, program sayfasında **Hizmetler** sekmesinden hizmetler (süre, haftalık sıklık); `Finans → Paket Kataloğu`'nda paketler. **Her paket bir programa bağlanmalı**, yoksa seans tamamlanınca hak düşmez (Akış 5).
9. **Akademik dönem:** `BEP / IEP → Akademik Dönemler → Dönem Ekle`, sonra **Güncel dönem yap**. Dönem olmadan BEP açılamaz (Akış 3).
10. **Eğitmenler ve hesapları:** `Eğitimciler → Yeni Eğitmen` ile kayıtlar açılır. Her eğitmen kartında **Genel Bakış → Kullanıcı hesabı → Yeni kullanıcı oluştur** ile hesap açılır ve kayda bağlanır; geçici şifre bir kez gösterilir. **Ders vermese bile BEP onaylayacak koordinatör için de eğitmen kaydı açılıp hesabı bağlanır.** Bağlı olmayan kişi seans notu yazamaz, BEP onaylayamaz (Akış 15).
11. **Diğer personel:** sekreter, muhasebe gibi eğitmen olmayanlar `Kullanıcılar → Kullanıcı Ekle` ile açılır (şifreyi yönetici belirler), sonra kullanıcı sayfasında **Rol Ata**. Herkes ilk girişte şifresini değiştirir.
12. **Şablonlar ve bakiyeler:** `Sözleşme Şablonları` ve `Rıza Şablonları`'na metinler girilir (Akış 2); `İzin Bakiyeleri → Bakiye Tanımla` ile yıllık haklar (Akış 7); KPI açıksa ölçütler gözden geçirilir (Akış 10); istenirse bildirim şablonları hazırlanır (e-posta henüz gitmez, Akış 13).
13. **Öğrenciler ve veliler (2. hafta):** veliler ve öğrenciler açılır, veli öğrenciye bağlanır; `Programlar → Kayıtlar → Kayıt Oluştur` ile her öğrenci programına eklenir; `Öğrenci Paketleri → Paket Ata` ile satılmış paketler ve kalan haklar girilir; sözleşme ve rızalar kaydedilir; `Tekrarlayan Programlar`'dan haftalık düzen girilip **Seansları Oluştur** denir (Akış 4).
14. **Veli portalı:** veli kartı → **Kullanıcı hesabı → Yeni kullanıcı oluştur** (rol: Veli (portal)) ve **Portal Erişimi → Erişim Aç** ile çocuk bazında görünecek bölümler seçilir. İkisi de gerekir (Akış 14).
15. **Uçtan uca dene:** bir öğrenci için eğitmen hesabıyla girip seansı açın, yoklama alın, veliye görünür not yazın, seansı tamamlayın; seans hakkının düştüğünü ve velinin portalda notu gördüğünü kontrol edin. Koordinatörle bir BEP onaylayın.

## Dikkat
- Kurum açma, ek modülleri açma/kapama, kurumu pasifleştirme ve şifre yenileme **ekrandan yapılamaz**; teknik ekibe iletin.
- Pasifleştirilen kurumun kullanıcıları bugün hâlâ giriş yapabiliyor; kurum kapatılırken kullanıcılar da askıya alınmalı (teknik ekip).
- **Geçici şifre bir kez gösterilir.** Pencereyi kapatmadan kopyalayın. Kişiye yüz yüze, telefonla ya da SMS ile iletin; kullanıcı adıyla aynı mesajda göndermeyin.
- **E-posta gitmiyor:** "şifremi unuttum" çalışmaz, davet e-postası gitmez. Şifresini unutan için teknik ekibe başvurulur.
- Toplu aktarma yok; listeler ekrandan tek tek girilir. Büyük kurumda en uzun adım budur.
- KPI değerleri kendiliğinden hesaplanmaz; her ay **Dönem Hesapla** gerekir.
- Bir kullanıcı en fazla bir eğitmen ve en fazla bir veli kaydına bağlanır. Veli hesabına verilen rol portal iznini içermelidir.

## Kendin dene
Yerel geliştirme ortamında (Docker ayakta):
1. Deneme modunda bir kurum açın: `python3 devops/new_tenant.py --check --code deneme --name "Deneme Merkezi" --admin-username mudur@deneme.test --admin-name "Deneme Yönetici"`. Hiçbir şey yazılmadığını görün; sonra `--check` olmadan çalıştırıp geçici şifreyi not edin.
2. `mudur@deneme.test` ile girin; şifre değiştirme sayfasından başka bir yere gidemediğinizi görün, şifreyi değiştirip yeniden girin.
3. `Kurumlar → Deneme Merkezi → Ayarlar`'da Modüller'in hepsinin "Kapalı" olduğunu görün; menüde Kamp, Kamera, KPI yok. `python3 devops/set_modules.py --code deneme --enable kpi` çalıştırıp sayfayı yenileyin; KPI menüsü gelir.
4. Bir oda, bir program (bir hizmetle), programa bağlı bir paket ve bir akademik dönem açın; dönemi güncel yapın.
5. "Eğitmen" rolü açın (B.4'teki izinlerle). Bir eğitmen kaydı açıp kartından **Yeni kullanıcı oluştur** ile bu rolde hesap açın; gizli pencerede o hesapla girip şifre değiştirme akışını tekrar görün.
6. Bir öğrenci ve veli açın, öğrenciyi programa ve pakete ekleyin, bugüne bir seans koyun; eğitmen hesabıyla yoklama, not ve **Tamamla** yapın; "1 seans hakkı düşüldü" yazısını görün.
7. Temizlik: deneme kurumu yerel veritabanında kalır. Silmek için hazır bir araç yoktur; yerel veritabanını sıfırlamak gerekir [doğrulanacak].

Ayrıntılı kılavuz: [../musteri/KURULUM_KILAVUZU.md](../musteri/KURULUM_KILAVUZU.md) (Bölüm B)
