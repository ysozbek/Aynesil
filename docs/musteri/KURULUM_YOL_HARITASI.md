# AyNesil — Yeni Müşteri Kurulum Yol Haritası

Bu doküman yeni bir kurumu sıfırdan canlı kullanıma almak içindir. Üç aşama var. Her aşamada kimin ne yapacağı ve müşteriden ne isteneceği yazıyor.

| Aşama | Kim | Süre | Sonuç |
|---|---|---|---|
| **Gün 0:** Kurumu aç | Siz | 5 dakika | Müşteri giriş yapabiliyor |
| **1. hafta:** Kurumun tanımları | Siz + kurum yöneticisi | ~2 saatlik bir oturum | Eğitimciler, odalar, programlar, paketler hazır |
| **2. hafta:** Öğrenciler ve canlı kullanım | Kurum (siz destekte) | Birkaç gün | Haftalık seanslar sistemden yürüyor |

---

## Gün 0: Kurumu aç (siz)

Sunucu ayaktaysa (`docker compose up`) tek komut yeterli:

```bash
# Önce deneyin: her şeyi çalıştırır, sonunda geri alır
python3 devops/new_tenant.py --check \
  --code ornek --name "Örnek Özel Eğitim" \
  --campus "Merkez Kampüs:İstanbul" \
  --admin-username mudur@ornek.com --admin-name "Ayşe Yılmaz"

# Sorun yoksa aynı komutu --check olmadan çalıştırın
```

Komut kurumu, kampüsleri, platform işlemleri (kurum açma, etkinleştirme/pasifleştirme) dışındaki tüm izinlere sahip **Yönetici** rolünü ve ilk yönetici kullanıcıyı açar. Geçici şifreyi bir kez ekrana yazar; müşteriye bu kullanıcı adı ve şifreyi iletin.

**Bilmeniz gerekenler:**
- Seans türleri, ödeme yöntemleri, aday kaynakları gibi listeler ve menüler platform genelidir; kurum ilk girişten itibaren görür. Kurum isterse kendine özel değer ekler.
- Kullanıcı adları **tüm sistemde** benzersizdir; giriş yalnızca kullanıcı adıyla yapılır. Başka bir kurumda kullanılan ad kabul edilmez. Bu yüzden yönetici ve personel hesaplarında e-posta adresi kullanın (ör. `mudur@ornek.com`). "ayse" gibi kısa adlar kurumlar arasında çakışır.
- Kurum, kampüs ve kullanıcı açma tek işlemdir. Hata olursa hiçbir şey yazılmaz.

---

## 1. hafta: Kurumun tanımları (birlikte)

**Oturumdan önce müşteriden isteyin** (Excel veya düz liste yeterli):

1. **Eğitimciler:** ad soyad, branş, e-posta, telefon
2. **Odalar:** her kampüsteki oda adları ve kapasite
3. **Programlar ve hizmetler:** hangi programları sunuyorlar, her programda hangi hizmetler var, süresi kaç dakika, haftada kaç kez (ör. "Erken Çocukluk: Dil ve Konuşma 45 dk haftada 2, Duyu Bütünleme 60 dk haftada 1")
4. **Paketler ve fiyatlar:** ör. "20 seanslık Dil ve Konuşma paketi, 3 ay geçerli, X TL"
5. **Personel ve görevleri:** sekreter, koordinatör, muhasebe gibi kimler sisteme girecek
6. **Kullandıkları kelimeler:** "seans hakkı" mı "ders hakkı" mı, "eğitimci" mi "öğretmen" mi (bkz. `docs/calisma/EKRAN_IYILESTIRME_LISTESI.md` Bölüm 3)

**Oturumda, bu sırayla:**

| # | Ekran | Ne girilir |
|---|---|---|
| 1 | Kampüsler | Adres, telefon (kampüsler Gün 0'da açıldı) |
| 2 | Planlama → Odalar | Oda listesi |
| 3 | Eğitimciler | Eğitimci kayıtları |
| 4 | Programlar | Programlar ve Hizmetler sekmesinden hizmetleri |
| 5 | Finans → Paket Tanımları | Paketler ve fiyatlar. Her paketi bir programa bağlayın; seans hakkı bu bağlantıyla otomatik düşer |
| 6 | Roller | Gerekirse "Sekreter", "Eğitimci" gibi roller ve izinleri |
| 7 | Kullanıcılar | Personel hesapları, rol ataması |

Kamera, kamp, danışmanlık, KPI gibi ek modüller bu aşamada açılmaz. Kurum ihtiyaç duyduğunda ayrıca ele alınır.

---

## 2. hafta: Öğrenciler ve canlı kullanım

**Müşteriden isteyin:** öğrenci listesi (ad soyad, doğum tarihi, veli adı ve telefonu, aldığı program) ve mevcut haftalık ders programı.

| # | Ekran | Ne yapılır |
|---|---|---|
| 1 | Veliler, Öğrenciler | Kayıtlar açılır, veli öğrenciye bağlanır |
| 2 | Programlar → Kayıtlar | Her öğrenci aldığı programa eklenir |
| 3 | Öğrenci Paketleri → Paket Ata | Satılmış paketler ve kalan seans hakları girilir |
| 4 | Planlama → Tekrar Eden Planlar | Haftalık düzen girilir, "Seansları Oluştur" ile takvim üretilir |
| 5 | Veli → Portal Erişimi | İsteyen velilere portal hesabı açılır |

**Canlıya geçiş kontrolü:** Bir öğrenci için uçtan uca deneyin: takvimde seansı görün, yoklama alın, not yazın, seansı tamamlayın, seans hakkının düştüğünü ve velinin portalda notu gördüğünü kontrol edin.

---

## Henüz elle yapılanlar

- **Toplu aktarma yok.** Excel'deki eğitimci ve öğrenci listeleri ekranlardan tek tek giriliyor. Kurum büyükse bu, kurulumun en uzun adımı olur.
- **Hazır rol seti yok.** Gün 0'da yalnızca Yönetici rolü açılıyor. Sekreter, eğitimci gibi roller her kurum için Roller ekranından tanımlanıyor.
- **Demo verisi yok.** Demo öncesi hazırlık `docs/musteri/DEMO_SENARYOSU.md` bölüm 1'deki listeyle elle yapılıyor.
