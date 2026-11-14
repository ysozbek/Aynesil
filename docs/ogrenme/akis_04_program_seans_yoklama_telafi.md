# Akış 4 — Program kaydı → seans planlama → yoklama → seans notu → telafi

## Bu akış ne anlatıyor?
Öğrencinin bir programa yazılmasını, haftalık derslerin (seansların) planlanmasını, ders günü yoklama alınıp not yazılmasını ve gelmeyen öğrenciye telafi hakkı verilmesini anlatır. Seans tamamlandığında öğrencinin paketinden **otomatik olarak 1 seans hakkı düşer**; bu, finans tarafıyla buluştuğu yerdir (Akış 5).

## Kim yapar?
Koordinatör/planlamacı: program, kayıt, seans planı, telafi onayı. Eğitmen: yoklama, seans notu, telafi talebi. Eğitmen seans oluşturamaz.

## Adımlar
1. **Programı hazırla (bir kez):** `Programlar → Yeni Program`; programa hizmetleri ekleyin. Paketler ve seanslar bu programa bağlanacağı için önce bu gerekir.
2. **Öğrenciyi programa yaz:** `Programlar → Kayıtlar → Kayıt Oluştur`; öğrenciyi ve programı seçin. Öğrencinin zaten açık bir kaydı varsa yeni kayıt açılmaz, program mevcut kayda eklenir; ekran bunu size söyler. Sonradan **Program Ata**, **Programı Sonlandır** ve **Kaydı Sonlandır** da buradadır.
3. **Odaları kontrol et:** `Seans Planlama → Odalar`. Sistem aynı odayı ve aynı eğitmeni iki seansa birden vermez; odalar güncel olmalı.
4. **Seans planla:**
   - *Tek seferlik:* `Seanslar → Seans Oluştur`; kampüs, oda, saat, öğrenci ve eğitmen seçin.
   - *Her hafta tekrarlayan:* `Tekrar Eden Planlar → Plan Oluştur` (öğrenci, program, eğitmen, oda, günler, saat), sonra listede **Seansları Oluştur** ile tarih aralığı verin. Sistem kaç seans oluşturduğunu, oda ya da eğitmen dolu olduğu için kaç tarihi atladığını yazar.
5. **Seansa hedef bağla:** seans sayfası → **Hedefler** sekmesi → **Hedef Ekle**. Önce seansa öğrenci eklenmiş olmalı. Böylece seansta çalışılan BEP hedefi kayda geçer. Yanlış bağlanan hedef, kartındaki **✕** ile seanstan çıkarılır (onay sorulur); öğrencinin hedefi silinmez, yalnızca bu seanstaki ilerleme notu ve ölçüm kalkar.
6. **Ders günü yoklama al:** seans sayfası → **Yoklama** sekmesi: her öğrenci için Katıldı, Gelmedi veya Geç Geldi seçin; gelmeyene mazeret yazabilirsiniz. Kredi düşümü yoklamaya bakar, bu yüzden tamamlamadan önce yapın.
7. **Not yaz:** **Notlar** sekmesi → **Not Ekle**. Eğitmenin görüşü ve gözlemi burada kalır. Notu **kalem simgesiyle düzenleyebilirsiniz**: yalnızca notu yazan eğitmen düzenler; başkasının notunu notları silebilen yönetici düzeltebilir, diğerleri "yalnızca yazan eğitmen ya da yönetici düzenleyebilir" uyarısı alır. Düzenlenen notta **Düzenlendi** rozeti çıkar (üzerine gelince son düzenleme zamanı).
8. **Seansı tamamla:** **Tamamla**. Yoklamada "geldi" olan her öğrenci için, seansın programına bağlı paketinden 1 seans hakkı düşer. Gerekirse **İptal Et** (gerekçe yazılır) veya **Yeniden Planla**.
9. **Telafi iste:** gelmeyen öğrenci için `Seans Planlama → Telafi Talepleri → Talep Oluştur`; kaçırılan seansı seçin.
10. **Telafiyi karara bağla:** koordinatör bekleyen talepte **Onayla** ya da **Reddet** (gerekçeyle). Onaylanan talepte **Seans ata**: öğrencinin önümüzdeki 90 gündeki planlı seansları listelenir (telafi seansını önce Seanslar'dan öğrenciyle oluşturup telafi olarak işaretlersiniz); seçince talep "Planlandı" olur. Telafi gerçekleşince **Tamamla** ile "Tamamlandı" yapılır. Seansın katılımcıları arasında talebin öğrencisi yoksa sistem atamayı reddeder.

## Dikkat
- **Seans hakkı hiç düşmedi mi?** Seans sayfası "Seans hakkı düşülmedi" yazar. Nedeni genelde şunlardan biri: yoklamada "geldi" yok, öğrencinin bu programa bağlı hakkı kalan aktif paketi yok ya da paket tanımı bir programa bağlı değil. Gerekirse Kredi Defteri'nden elle düşülür.
- Tamamlanmış, iptal edilmiş ya da "Gelmedi" yapılmış seansın durumu bir daha değişmez.
- Oda veya eğitmen o saatte doluysa kayıt reddedilir: "… odası istenen zaman diliminde zaten dolu." ya da "… eğitimcisi … zaten meşgul."
- Yoklamada ekranda yalnızca Katıldı / Gelmedi / Geç Geldi görünüyor; Mazeretli ve Erken Ayrıldı sistemde var ama bu ekrandan seçilemiyor gibi [doğrulanacak].
- Notu yazan kişi, giriş yapan kullanıcıya bağlı eğitmen kaydıdır; bağlı değilse not yazılamaz ("Oturumdaki kullanıcı hiçbir eğitmen kaydına bağlı değil…"). Düzenlenmiş notun eski hâli ekranda tutulmaz; değişiklik denetim kaydına (`audit_log`) düşer. Veli portalı bugün seans notlarını göstermiyor, bu yüzden "veliye görünür not düzenlendi" bilgisi velide görünmez.

## Kendin dene
`demo_a`'da yaklaşık 225 seans, tekrarlayan plan ve telafi talebi var.
1. `koordinator@demo-a.test` → `Seanslar`: ileri tarihli bir seansı açın, **Katılımcılar**'a öğrenci ekleyin, **Yoklama**'da Katıldı seçip **Tamamla**; "seans hakkı düşüldü" yazısını ve Finans'taki Kredi Defteri'ni (Akış 5) görün. Yoklamasız bir seansı tamamlayıp "düşülmedi" uyarısını da görün.
2. `Tekrar Eden Planlar`'dan bir planla **Seansları Oluştur** (iki haftalık aralık) ve atlanan tarihleri izleyin.
3. `Telafi Talepleri`: bekleyen talepte **Onayla** veya **Reddet**; onaylı talepte **Seans ata**, planlı talepte **Tamamla** (demo'da bir talep tamamlanmış durumda).
4. `egitmen@demo-a.test`: kendi seansında yoklama ve not; "Seans Oluştur" düğmesinin olmadığına dikkat edin.
5. Seansa hedef ekleyip **✕** ile çıkarın. Bir not yazıp kalemle düzenleyin ve **Düzenlendi** rozetini görün; sonra `admin@demo-a.test` ile başka eğitmenin notunu düzeltmeyi deneyin (yönetici yapabilir).

Teknik ayrıntılar: [teknik/akis_04_program_seans_yoklama_telafi.md](teknik/akis_04_program_seans_yoklama_telafi.md)
