# Akış 12 — Bakım ekibi: öğrenciye bakan eğitmenlerin belirlenmesi

> **Modül anahtarı: `care_team` (Bakım ekibi).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü ve öğrenci detayındaki sekmesi görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable care_team` (Akış 16). Bayrak yalnızca bakım ekibi ekranlarını ve uçlarını kapatır; öğrenci erişimini sınırlayan veritabanı politikası (ABAC/RLS) bundan bağımsız çalışır.

## Bu akış ne anlatıyor?
Bir öğrenciye hangi eğitmenlerin "bakım ekibi" olarak atandığını anlatır. Amaç, hassas klinik bilgilerin (vaka notları, raporlar, BEP, hedefler gibi) yalnızca öğrenciyle ilgilenen kişiler tarafından görülmesidir. Kurum isterse bu kısıtı açar; kapalıyken bilgilere izni olan herkes erişir. Bakım ekibine atanmamış ama yönetici gibi "özel geçiş izni" (`care_team:bypass`) olan kişiler her zaman görür.

## Kim yapar?
Atamayı bakım ekibi atama izni (`care_team:assign`) olan yönetici/koordinatör yapar. Atanan eğitmen, `care_team:read` izniyle kendi öğrencilerini görür (eğitmen rolüne bu izin ayrıca verilmelidir; demo eğitmeninde vardır).

## Adımlar
1. **Ekibi aç:** Öğrenciler → öğrenci → **Bakım Ekibi** sekmesi. Geçerli ekip listelenir (birincil üye rozetle); "Sona erenleri de göster" geçmişi getirir.
2. **Eğitmen ekle:** "Eğitmen ekle" → eğitmen, ekip rolü (birincil terapist, ikincil terapist, koordinatör, psikolog, danışman, gözlemci, süpervizör), başlangıç ve isteğe bağlı bitiş tarihi, isteğe bağlı gerekçe.
3. **Birincil işaretle:** her öğrencide aynı anda yalnızca bir birincil atama olabilir. İkincisi eklenirse ekranda uyarı çıkar: önce mevcut birincili düzenleyin ya da çıkarın.
4. **Düzenle:** rol, tarihler, birincil durumu ve gerekçe değiştirilebilir. Başkası aynı anda değiştirdiyse "değişmiş" uyarısı alırsınız; sayfayı yenileyip tekrar deneyin.
5. **Ekipten çıkar:** atama bugün itibarıyla sona erer; kayıt silinmez, "Sona erenleri de göster" ile geçmişte görünür.
6. **Eğitmen kendi öğrencilerine bakar:** menüde **Benim Öğrencilerim** (`/care-team/my-students`): ekibinde olduğunuz öğrenciler, rolünüz ve tarihlerle. Hesabınız bir eğitmen kaydına bağlı değilse liste boş kalır (Akış 15).
7. **Öğrenci panosu:** "Atanmış eğitmenler" alanı bakım ekibini gösterir. Ekip boşsa, öğrencinin seanslarında görev almış eğitmenler **"seanslardan"** etiketiyle listelenir ve her birinin yanında **Ekibe ekle** düğmesi çıkar (rol varsayılan olarak ilk rol, başlangıç bugün). Seanslardan gelen liste kimseye erişim vermez; yalnızca bilgidir.
8. **Tekrar eden plan:** Planlama → Tekrar eden planlar → Oluştur'da öğrenci ve sorumlu eğitimci seçildiğinde, eğitimci öğrencinin ekibinde değilse sarı bir öneri çıkar: "Ekibe ekle". Zorunlu değildir; plan eklemeden de kaydedilir.

## Dikkat
- **Erişim kısıtı varsayılan olarak kapalıdır** ve arayüzden açılmaz. Kurum için açıldığında (teknik ekip, bkz. Kurulum Kılavuzu B.1.3) ekibe atanmamış bir eğitmen (özel geçiş izni yoksa) öğrencinin **klinik kayıtlarını** (hedef, vaka notu, rapor, BEP, değerlendirme) göremez ve veli portalı da buna uyar. Öğrenci kartı listesi bu kısıta bağlı değildir. Açmadan önce her eğitmenin kullanıcı hesabına bağlı olması ve her öğrencinin ekibinin kurulmuş olması gerekir; aksi halde eğitmenler kayıtları aniden göremez.
- Aynı öğrenci için ikinci birincil atama yapılamaz.
- Zaten sonlanmış bir atama tekrar sonlandırılamaz.
- Acil ve devredilmiş yetki türleri için gerekçe zorunludur; bu iki yetki türü ve kampüs kapsamı henüz ekranda yok (uçta var).
- Eğitmenin sisteme giriş yapabilmesi için eğitmen kaydının kullanıcı hesabına bağlı olması gerekir (Akış 15).

## Kendin dene
Demo kurumda (`demo_a`, `demo_b`) ilk üç öğrencinin ekibi kuruludur: `egitmen@demo-a.test` hepsinde birincil terapisttir (ilk öğrencide ayrıca ikincil terapist ve psikolog üyeler var). `egitmen@…` ile girip **Benim Öğrencilerim**'e bakın; yönetici ile öğrenci detayında **Bakım Ekibi** sekmesini deneyin. Ekibi boş öğrencilerin panosunda "seanslardan" etiketli eğitmenleri ve "Ekibe ekle" düğmesini görürsünüz. Demoda erişim kısıtı **kapalıdır**.

Teknik ayrıntılar: [teknik/akis_12_bakim_ekibi.md](teknik/akis_12_bakim_ekibi.md)
