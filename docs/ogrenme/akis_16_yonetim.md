# Akış 16 — Yönetim: kullanıcılar, roller, menüler, referans veriler, kampüsler, kurum ayarları

## Bu akış ne anlatıyor?
Sistemi bir kurum için kurup yönetmeyi anlatır: çalışanlara hesap açmak, kimin ne yapabileceğini rollerle belirlemek, listelerin (seans türü, ödeme yöntemi gibi) içeriğini değiştirmek, kampüsleri ve kurumun genel ayarlarını yönetmek. Yeni müşteri kurumun açılışı ise bu ekranlardan değil, teknik ekibin çalıştırdığı bir kurulum aracıyla yapılır.

## Kim yapar?
Kurum Yöneticisi. Koordinatör, kullanıcı/rol/kurum/ayar/menü/referans veri yönetimini yapamaz (demo rol kuralı). Kurum ve kampüs ekranları "platform yönetimi" menüsü altındadır.

## Adımlar
1. **Kurumu ve ayarları gör:** `Ayarlar` ve `Kurumlar` ekranları kurum adı, dil/bölge, para birimi ve saat dilimi gibi bilgileri gösterir. `Kurumlar → kurum → Ayarlar` ile bu değerler ve ek ayarlar (JSON) güncellenir; kurum **aktif/askıya alınabilir**. `Ayarlar` sayfası yalnızca okuma gibi görünüyor [doğrulanacak].
2. **Kampüs ekle:** `Kampüsler → Kampüs Ekle`; şube aktif/pasif yapılabilir. Kampüs bir ayrı müşteri değil, kurumun içinde yetki ve raporlama bölümüdür.
3. **Listeleri düzenle:** `Referans Veriler`; sol taraftan kategori (Seans Tipleri, Ödeme Yöntemleri, Öğrenci Statüleri, Eğitimci Unvanları, …) seçilir, **Değer Ekle** ile yeni değer girilir. Kod küçük harfle ve kelimeler arasında alt çizgiyle yazılır. Değerler **silinmez, yalnızca pasifleştirilir**; böylece eski kayıtlar bozulmaz. Bir değeri kategoride varsayılan yapabilirsiniz.
4. **Rol tanımla:** `Roller → Rol Ekle`; sonra rolün sayfasında izinleri tek tek verin ya da kaldırın. `İzinler` sayfası tüm izin kataloğunu gösterir. Sistem rolleri silinemez. Sistem, kişinin adına değil rolündeki izinlere bakar.
5. **Kullanıcı aç:** `Kullanıcılar → Kullanıcı Ekle`; kullanıcı adı (tüm sistemde benzersiz, e-posta önerilir), ad soyad, e-posta, telefon, şifre, dil, ana kampüs. Sonra kullanıcı sayfasında **Rol Ata**. Gerekirse **Askıya Al** / **Aktif Et**. Kullanıcı sayfasının Bilgiler sekmesinde, hesap bir eğitmen ya da veli kaydına bağlıysa **Eğitmen kaydı** / **Veli kaydı** kutusu bağlı kaydı gösterir (tıklayınca kayda gider).
   - **Eğitmen ve veli hesapları için kısa yol:** hesabı kişinin kendi kartından açmak daha kolaydır: eğitmen ya da veli kartı → Genel Bakış → **Kullanıcı hesabı → Yeni kullanıcı oluştur**. Hesap rolüyle birlikte açılır, kayda bağlanır ve geçici şifre bir kez gösterilir (Akış 14 ve 15).
   - **Kendi şifreni değiştir:** her kullanıcı sağ üstteki baş harf rozetine tıklayıp **Şifre Değiştir** sayfasına gider; değişiklikten sonra yeniden giriş yapılır. Yönetici açtığı (kullanıcı ekle, eğitmen/veli hesabı, `new_tenant.py` yöneticisi) hesaplar **ilk girişte şifre değiştirmeye zorlanır**: API de bu hesaplarda yalnızca şifre değiştirme/çıkış/token yenileme uçlarına izin verir.
   - **Şifremi unuttum:** giriş ekranındaki bağlantı e-posta ister; sıfırlama bağlantısı e-postayla gelir ve `Yeni şifre belirle` ekranını açar. **E-posta sağlayıcısı bağlı olmadığı için bağlantı şimdilik gitmez** (ekranda da yazar); unutan kullanıcının şifresini yönetici sıfırlamalıdır.
6. **Menüyü düzenle:** `Menüler` (Dinamik Menü) ağacı gösterir (platform menüsü sayfalarla hizalıdır: sayfası olmayan öğeler kapalı, Veliler / Sözleşme Raporları / Bildirim Tercihleri / portal alt öğeleri açık); **Menü Öğesi Ekle** / **Alt Öğe Ekle** ile kuruma özel öğeler eklenir. Platformun varsayılan öğeleri silinemez, yalnızca pasife alınır. Bir öğe, tanımlı izne sahip kullanıcıya görünür; yetkisi olmayan menüyü görmez.
7. **Yeni kurum açılışı:** bu ekrandan değil, teknik ekibin kurulum aracıyla yapılır (Kurulum Yol Haritası belgesine bakın). `Kurumlar → Kurum Ekle` düğmesi de vardır ama kullanımı [doğrulanacak].

## Dikkat
- Kullanıcı adı alınmışsa "'…' kullanıcı adı zaten alınmış." çıkar; başka kurumdaki bir ad da sayılır.
- Menüde "Yetkiniz yok" ya da menü eksikse sorun genelde rolde eksik izindir, hata değildir.
- Kullanıcıyı silmek yerine **Askıya Al** kullanın; kendi hesabınızı silemezsiniz.
- Rol ve izin değişikliği, kullanıcının bir sonraki girişinde ya da oturumu yenilendiğinde etkili olur [doğrulanacak].
- Velilerin ve eğitmenlerin giriş yapabilmesi ve kendi adlarına iş yapabilmesi için kullanıcı hesabının kartlarına **bağlı** olması gerekir (Akış 14 ve 15). Bir kullanıcı en fazla bir eğitmen ve en fazla bir veli kaydına bağlanabilir.
- Her kurumda hazır bir **Veli (portal)** rolü gelir (yalnızca portal izinleri); veli hesapları bununla açılır. Bu rol düzenlenebilir ya da silinebilir, ama veli hesabına verilen rol "portal:access" iznini içermek zorundadır.
- E-posta henüz gönderilmiyor: yeni hesapların geçici şifresini kişiye siz iletirsiniz; "şifremi unuttum" e-postası da gitmez (ekranlar hazır, e-posta sağlayıcısı bağlanınca çalışır).

## Kendin dene
`admin@demo-a.test` ile girin. `Roller`'de "Koordinatör" ve "Muhasebe" rollerinin izin sayılarını karşılaştırın; yeni bir test rolü açıp iki izin verin. `Kullanıcılar → Kullanıcı Ekle` ile yeni hesap açıp bu rolü atayın, o hesapla giriş yapıp hangi menülerin göründüğüne bakın. `Referans Veriler → Seans Tipleri`'nde yeni değer ekleyip sonra pasifleştirin. `koordinator@demo-a.test` ile yönetim menülerinin görünmediğini doğrulayın. `Kullanıcılar`'da `egitmen@demo-a.test` ve `veli1@demo-a.test`'i açıp bağlı eğitmen / veli kaydını görün.

Teknik ayrıntılar: [teknik/akis_16_yonetim.md](teknik/akis_16_yonetim.md)
