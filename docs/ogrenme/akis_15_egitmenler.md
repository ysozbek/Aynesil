# Akış 15 — Eğitmenler: kayıt, uzmanlık, kampüs, sertifika, hiyerarşi

## Bu akış ne anlatıyor?
Kurumdaki eğitmenlerin (terapist, öğretmen) kayıtlarının tutulmasını anlatır: kim, hangi alanda uzman, hangi kampüste çalışıyor, hangi sertifikalara sahip ve kime bağlı. Seans, izin ve KPI akışlarının hepsi bu kayıtlara dayanır.

## Kim yapar?
Kurum Yöneticisi ve Koordinatör. Eğitmen diğer eğitmenleri görüntüleyebilir.

## Adımlar
1. **Eğitmen ekle:** `Eğitimciler → Eğitmen Ekle`; ad, soyad, unvan, e-posta, telefon, çalışma türü, işe başlama tarihi, ana kampüs.
2. **Uzmanlık ata:** eğitmen sayfasında uzmanlık alanı ekleyin; seans atamasında doğru kişiyi bulmak kolaylaşır. Yanlışı **kaldırın**.
3. **Kampüs ata:** kampüse görevlendirin; görevi bittiğinde **Bitir** ile tarihini kapatın (kayıt silinmez).
4. **Sertifika ekle:** sertifika bilgisi ekleyin, düzenleyin ya da silin.
5. **Hiyerarşi kur:** eğitmeni bir üst sorumluya bağlayın ya da bağı kaldırın (kim kime rapor veriyor).
6. **Müsaitlik:** `Eğitimciler →` eğitmen → Müsaitlik sayfası, eğitmenin hangi saatlerde çalıştığını gösterir [doğrulanacak: düzenlenebilir mi].
7. **Pasife al:** ayrılan eğitmen **Pasifleştir** ile pasife alınır, gerekirse yeniden **Aktifleştir**. Geçmiş seanslar bozulmaz.
8. **Kullanıcı hesabına bağla:** eğitmen sayfası → Genel Bakış → **Kullanıcı hesabı** bölümü. Üç seçenek var:
   - **Mevcut kullanıcıyı bağla:** Kullanıcılar'da zaten açılmış bir hesabı arayıp seçin.
   - **Yeni kullanıcı oluştur:** kullanıcı adı (e-posta önerilir), ad soyad ve **rol** seçin. Hesap etkin açılır; sistem bir **geçici şifre** üretir ve yalnızca bir kez gösterir (e-posta henüz gönderilmiyor). Şifreyi eğitmene güvenli bir yoldan iletin; eğitmen ilk girişte yalnızca **Şifre Değiştir** sayfasını görür; şifresini değiştirmeden başka sayfaya geçemez, değiştirince yeniden giriş yapar.
   - **Bağlantıyı kaldır:** yalnızca bağ kalkar; kullanıcı hesabı ve rolleri silinmez.
   Bağlı hesap Kullanıcılar → kullanıcı detayında "Eğitmen kaydı" olarak da görünür.

## Dikkat
- **Bağ şart:** seans notu yazma, BEP onayı, izin talebi ve bakım ekibi gibi işler "giriş yapan kullanıcıya bağlı eğitmen kaydı"na dayanır. Bağlı olmayan biri seans notu yazmaya çalışırsa sistem "Bir yönetici Eğitmenler → eğitmen detayı → Kullanıcı hesabı bölümünden bu kullanıcıyı eğitmen kaydına bağlamalıdır" der.
- Bir kullanıcı **en fazla bir** eğitmen kaydına bağlanabilir; başka bir eğitmene bağlıysa sistem reddeder ve hangisine bağlı olduğunu söyler. Bağlı bir eğitmeni başka kullanıcıya bağlamak için önce mevcut bağı kaldırın.
- Kullanıcı ve eğitmen aynı kurumda olmalı; başka kurumun kullanıcısı listede zaten görünmez.
- Bölümü kullanmak için eğitmen düzenleme ve kullanıcı yönetimi yetkisi birlikte gerekir (yeni hesap için kullanıcı oluşturma da). Koordinatör rolünde kullanıcı yetkileri yoksa bu bölümde düğme görmez; yönetici yapar.
- Yeni hesaba vereceğiniz rol eğitmenin işini kapsamalı (seans notu, yoklama…). Kurumda hazır "Eğitmen" rolü yoksa önce Roller ekranından oluşturun (Akış 16).
- Eğitmen sayfasında "hiyerarşi" ve "doluluk" gibi alt menüler seed'de vardı ama pasifleştirildi.

## Kendin dene
`demo_a`'da 6 eğitmen var. `koordinator@demo-a.test` ile `Eğitimciler`'den birini açın; uzmanlık, kampüs, sertifika ve hiyerarşi bölümlerini gezin, yeni eğitmen ekleyip ona bir uzmanlık atayın. `egitmen@demo-a.test` ile girip yalnızca okuyabildiğinizi görün.

Hesap bağı: `admin@demo-a.test` ile Merve Çalışkan'ı açın; Kullanıcı hesabı bölümünde `egitmen@demo-a.test` bağlı görünür. Yeni eklediğiniz eğitmende **Yeni kullanıcı oluştur** ile "Eğitmen" rolünde hesap açın, geçici şifreyi not edin, çıkış yapıp o hesapla girin ve bir seansa not yazın. Aynı kullanıcıyı ikinci bir eğitmene bağlamayı deneyip reddi görün.

Teknik ayrıntılar: [teknik/akis_15_egitmenler.md](teknik/akis_15_egitmenler.md)
