# Akış 12 — Bakım ekibi: öğrenciye bakan eğitmenlerin belirlenmesi (ekranı yok)

> **Modül anahtarı: `care_team` (Bakım ekibi).** Bu modül kurum bazında açılıp kapatılır; yeni kurumda **kapalı** başlar (menüsü görünmez, uçları 403 `Error.ModuleDisabled` döner). Demo kurumlarda açıktır. Açmak için teknik ekip: `python3 devops/set_modules.py --code <kurum> --enable care_team` (Akış 16). Bayrak yalnızca `/api/care-team` ve `/api/students/{id}/care-team` uçlarını kapatır; öğrenci erişimini sınırlayan veritabanı politikası (ABAC/RLS) bundan bağımsız çalışmaya devam eder.

> **Bu modülün ekranı yok.** Menüde "Bakım Ekibi" satırı tanımlı ama açılacak bir sayfa yok. Hazırlanmış altyapı olarak çalışıyor.

## Bu akış ne anlatıyor?
Bir öğrenciye hangi eğitmenlerin "bakım ekibi" olarak atandığını anlatır. Amaç, hassas klinik bilgilerin (vaka notları, raporlar, BEP gibi) yalnızca öğrenciyle ilgilenen kişiler tarafından görülmesidir. Kurum isterse bu kısıtı açar; kapalıyken bilgilere izni olan herkes erişir. Bakım ekibine atanmamış ama yönetici gibi "özel geçiş izni" olan kişiler her zaman görür.

## Kim yapar?
Atamayı bakım ekibi atama izni olan yönetici/koordinatör yapar. Atanan eğitmen, kendi öğrencilerini görür.

## Adımlar
1. **Eğitmeni ata:** öğrenci, eğitmen, ekip rolü (birincil terapist, ikincil terapist, koordinatör, psikolog, danışman, gözlemci, süpervizör), başlangıç ve isteğe bağlı bitiş tarihi, kampüs.
2. **Birincil olarak işaretle:** her öğrencide aynı anda yalnızca bir birincil atama olabilir.
3. **Geçici yetki ver (gerekirse):** atamaya "kalıcı", "acil" ya da "devredilmiş" gibi bir yetki türü seçilir. Acil ve devredilmiş yetki için **gerekçe yazmak zorunludur**.
4. **Güncelle ya da bitir:** tarihler ve rol değiştirilebilir; atama kaldırılınca sonlandırılır (silinmez, geçmiş kalır).
5. **Eğitmen kendi öğrencilerine bakar:** "benim öğrencilerim" sorgusuyla eğitmenin ekibinde olduğu öğrenciler listelenir.

## Dikkat
- Kısıt kurum bazında bir ayarla açılıp kapanır ve varsayılan olarak **kapalıdır**. Açıkken, ekibe atanmamış bir eğitmen (özel geçiş izni yoksa) öğrencinin klinik kayıtlarını göremez ve veli portalı da buna uyar (kesinleşmiş gelişim raporu gibi).
- Aynı öğrenci için ikinci birincil atama yapılamaz: önce mevcut birincil atamayı kaldırın.
- Zaten sonlanmış bir atama tekrar sonlandırılamaz.
- Eğitmenin sisteme giriş yapabilmesi için eğitmen kaydının kullanıcı hesabına bağlı olması gerekir; bunu yapan ekran yok (Akış 15).
- Tasarım belgesi bu özelliği "kısmen devreye alınmış" olarak tarif eder; bu akış mevcut sınırlar içinde kalır.

## Kendin dene
Ekran olmadığı için tarayıcıdan denenemez. Demo'da bakım ekibi verisi ya da bu kısıtın açık olduğu bir ayar yok [doğrulanacak]; etkisini görmek için veli portalındaki kesinleşmiş rapor kuralına (Akış 2 ve 14) bakabilirsiniz.

Teknik ayrıntılar: [teknik/akis_12_bakim_ekibi.md](teknik/akis_12_bakim_ekibi.md)
