# Akış 11 — Anketler ve veli geri bildirimi

> **Modül anahtarı: `surveys` (Anketler).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, sayfaları `/403`'e, uçları 403 `Error.ModuleDisabled`'a gider). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable surveys` (Akış 16). Kapalı modülde yöneticinin ya da velinin rolünde izin olsa bile ekranlar görünmez.

## Bu akış ne anlatıyor?
Kurumun velilere anket hazırlaması, velilerin portaldan doldurması, yönetimin sonuçları soru bazında görmesi; ayrıca velilerin bir seans ya da eğitmen için 1–5 puan ve yorum bırakması. Velilerin verdiği **puanlar eğitmenin KPI'ındaki "Veli Memnuniyeti" değerini besler** (Akış 10).

## Kim yapar?
- **Yönetici** (`survey:*` izinleri): anket hazırlar, sonuçlara bakar, geri bildirimleri okur (`parent_feedback:read`).
- **Veli** (Veli (portal) rolü: `survey:respond`, `parent_feedback:create`): bekleyen anketleri doldurur, geri bildirim gönderir. Yalnızca **kendi çocuğuyla** ilgili anketi yanıtlar ve yalnızca kendi çocuğunun seansını/eğitmenini puanlar.
- **Eğitmen** geri bildirimleri okuyabilir (demo'da `parent_feedback:read`).

## Adımlar — yönetici
1. **Anket hazırla:** `Öğrenciler → Anketler → Yeni Anket`. Başlık, tür (liste: genel geri bildirim, seans geri bildirimi…), **kime yönelik** (veli, eğitmen, öğrenci), açıklama. Sorular: **puan (1–5)**, **evet/hayır**, **çoktan seçmeli** (en az iki seçenek), **ölçek**, **serbest metin**; her soru zorunlu yapılabilir, ↑/↓ ile sıralanır. En az bir soru olmalı. **Velinin portalında yalnızca "Veli" hedefli anketler görünür.**
2. **Düzenle:** listeden **Düzenle**. Başlık, açıklama, tür, hedef, aktif/pasif ve sorular/seçenekler değişir. **Ankete yanıt gelmişse sorular kilitlenir** (cevaplar bu sorulara bağlı); başlık, açıklama ve durum yine düzenlenir. Soruları değiştirmek için yeni anket açın, eskisini pasife alın.
3. **Sonuçlar:** listede satıra tıklayın ya da **Sonuçlar**. Yanıt sayısı, son yanıt tarihi; her soru için ortalama, dağılım çubukları (puan ve seçenek başına kaç kişi) ya da yazılı yanıtlar; altta yanıtlayan veli ve öğrenci listesi.
4. **Sil:** yanıtı olmayan anket silinir; **yanıt alınmışsa silinmez, pasife alınır** (yanıtlar korunur; ekran bunu önceden söyler). Pasif anket velide görünmez.
5. **Veli geri bildirimi:** `Öğrenciler → Veli Geri Bildirimi` (ya da Anketler sayfasındaki düğme): tarih, veli, eğitmen, yıldız puanı, yorum.

## Adımlar — veli (portal)
1. `Veli Portalı → Anketler`: kurumun size gönderdiği ve henüz yanıtlamadığınız anketler. Kartı açın; birden fazla çocuğunuz varsa **çocuğu seçin** (her çocuk için ayrı yanıtlanır). Zorunlu (*) sorular boş bırakılamaz. **Yanıtı Gönder**'den sonra anket listeden düşer.
2. `Veli Portalı → Geri Bildirim`: çocuğu seçin, istersen **yapılmış bir seansı** seçin, yıldız verin ve/veya yorum yazın. Seans seçerseniz puan o seansın eğitmenine yazılır (KPI'a böyle yansır). Seans seçmezseniz genel geri bildirim olur. Sayfanın altında gönderdiğiniz geri bildirimler listelenir.

## Dikkat
- Aynı anketi **aynı çocuk için ikinci kez** göndermek "Anket yanıtı zaten gönderilmiş." uyarısını verir; kardeş için ayrı yanıt verilir.
- Puan 1 ile 5 arasındadır; geri bildirimde puan ya da yorumdan en az biri dolu olmalıdır.
- Başka bir velinin çocuğu için anket ya da geri bildirim gönderilemez; çocuğunuzla ilgisi olmayan eğitmen/seans için geri bildirim de reddedilir (403).
- **KPI etkisi:** yalnızca **geri bildirim puanları** eğitmenin KPI veli memnuniyetine girer (eğitmen + dönem ortalaması; puansız yorumlar sayılmaz). Anket yanıtlarındaki puanlar KPI'a **girmez** (ankette eğitmen bağı yoktur); anket sonuçları yalnızca Sonuçlar sayfasında görünür.
- Yeni kurumda modül kapalıdır; menü satırları yalnızca modül açıksa görünür.

## Kendin dene
- `admin@demo-a.test` → **Öğrenciler → Anketler**: iki demo anket. "Veli Memnuniyet Anketi (Demo)" için **Sonuçlar**'ı açın (3 yanıt: puanlar, seçenek dağılımı, yazılı öneri); **Düzenle**'de kilit uyarısını görün. "Yeni Anket" ile yanıtsız bir anket açıp düzenleyip silin.
- `veli1@demo-a.test` (iki çocuk) → **Veli Portalı → Anketler**: "Seans Geri Bildirimi (Demo)" iki çocuk için bekliyor; birini yanıtlayın, öteki çocuk için hâlâ bekler. **Geri Bildirim** sayfasından bir seans seçip puan verin.
- `admin@demo-a.test` → **Veli Geri Bildirimi**: az önceki puanı görün; **KPI & Performans**'ta ilgili eğitmenin veli memnuniyeti puandan etkilenir (snapshot yeniden hesaplanınca).

Teknik ayrıntılar: [teknik/akis_11_anketler.md](teknik/akis_11_anketler.md)
