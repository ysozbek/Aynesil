# Genel bakış — AyNesil nedir, kim ne yapar?

## Sistem ne işe yarar?
AyNesil, özel eğitim ve terapi merkezlerinin günlük işlerini tek yerde toplar: başvuran aileyi (aday) takip etmek, çocuğu öğrenci olarak kaydetmek, değerlendirmek, bireysel eğitim planı (BEP) hazırlamak, dersleri (seansları) planlamak ve yoklama almak, paketleri satıp ödemeleri izlemek ve veliye çocuğunu portaldan göstermek. Birden fazla kurum aynı sistemi kullanır ama her kurum yalnızca kendi verisini görür. Kurumun içinde şubeler (kampüsler) vardır.

## Bir çocuğun sistemdeki yolculuğu
1. **Aday** olarak kaydedilir, aileyle görüşülür (Akış 1).
2. **Öğrenci** olur, velisi bağlanır, sözleşme ve izinler tamamlanır, velinin portal erişimi açılır (Akış 2).
3. **Değerlendirilir**, buna göre **BEP** hazırlanıp onaylanır, hedeflerdeki ilerleme izlenir (Akış 3).
4. **Programa yazılır**, seansları planlanır; her seansta yoklama alınır, not yazılır, gelmeyen için telafi yapılır (Akış 4).
5. Aldığı **paket** ödenir ve her seansta paketten bir hak düşer (Akış 5).
6. Gerekirse **kampa** katılır (Akış 6), velisiyle **toplantı** yapılır (Akış 7), kameradan izlenebilir (Akış 9).
7. **Veli** tüm bunları portaldan takip eder (Akış 14).

Arkada çalışan destek işleri: personel izinleri (7), okul danışmanlığı (8), eğitmen kayıtları (15), performans ölçümü (10), bildirimler (13), kullanıcı/rol/liste yönetimi (16). Anketler (11) ve bakım ekibi (12) yalnızca altyapı olarak var, ekranı yok.

## Modüller birbirine nasıl bağlı?
- **Öğrenci** her şeyin merkezidir; plan, seans, paket, sözleşme, kamera izni öğrenciye bağlanır.
- **Program** hem seansı hem paketi birbirine bağlar: paket bir programa aittir, seans da bir programa aittir; seans tamamlanınca aynı programın paketinden hak düşer.
- **BEP hedefleri** seansta işlenir; seanslar da eğitmenin KPI'larını besler.
- **Sözleşme/rıza** kamera izlemeyi açar; **portal erişimi** velinin neyi göreceğini belirler.

## Roller: kim ne yapar? (demo hesaplarına göre)
| Rol | Ne yapar? | Akışlar |
|---|---|---|
| **Kurum Yöneticisi** | Her şeyi görür ve yapar: yönetim, finans, hukuk, kamera, KPI. | Hepsi |
| **Koordinatör** | Günlük operasyon: aday, öğrenci, plan, seans, telafi onayı, toplantı, izin onayı, danışmanlık, kamp. Finans, kullanıcı/rol ve ayar yönetimi **yok**. | 1–4, 6–9, 15 |
| **Eğitmen** | Kendi seanslarında yoklama, not, hedef ilerlemesi; BEP'e inceleme notu; telafi talebi; kendi izni; öğrenci/veli bilgisini okur. Oluşturma/onay yetkisi yok. | 3, 4, 7, 10 (okur) |
| **Muhasebe** | Paket, fatura, ödeme, kredi, burs, promosyon. Öğrenci/veli bilgisini yalnızca okur. | 5 |
| **Veli** | Yalnızca portal: çocuğunun seansları, hedefleri, paketleri, belgeleri, toplantıları, bildirimleri. | 14 |

Bu roller demo kurumlarının rolleridir; kendi kurumunuzda roller **Roller** ekranından istediğiniz gibi tanımlanır. Sistem kişinin unvanına değil, rolüne tanımlı izinlere bakar; izni olmayan menü görünmez, izni olmayan sayfaya girilirse "yetkiniz yok" sayfası açılır.

## Ekranlar nasıl düzenlenmiş?
Sol menü yetkinize göre dolar. Üst barda bildirimler ve profil var. Çoğu modülün bir ana sayfası (panosu), bir listesi, bir ekleme formu ve bir ayrıntı sayfası vardır. Silinen kayıtların çoğu aslında gizlenir; ödeme, fatura, kredi, imzalı sözleşme ve verilmiş rıza ise hiç silinmez, düzeltme kaydı ile düzeltilir.

## Giriş ve hesap
Giriş kullanıcı adı ve şifreyle yapılır; kullanıcı adı tüm kurumlarda benzersizdir. Şifre unutulursa "parola sıfırlama" istenir, ancak e-posta gönderen bir sağlayıcı henüz bağlı olmadığı için sıfırlama e-postası kullanıcıya ulaşmaz (Akış 13).

Okuma sırası ve tüm dosyalar: [index.md](index.md) · Modül/ekran eşlemesi: [teknik/eslesme_tablosu.md](teknik/eslesme_tablosu.md)
