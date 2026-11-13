# Akış 11 — Anketler ve veli geri bildirimi (ekranı yok)

> **Bu modülün ekranı yok.** Sistem tarafı hazır; ama menüde ya da ekranlarda anket oluşturacak, doldurulacak ya da sonuçlarını gösterecek bir sayfa bulunmuyor. Aşağıdakiler, ekran eklendiğinde (ya da arka plandan kullanıldığında) nasıl işleyeceğini anlatır.

## Bu akış ne anlatıyor?
Kurumun velilere, eğitmenlere ya da öğrencilere yönelik anket hazırlaması, cevapların toplanması ve velilerin bir eğitmene ya da seansa puan (1–5) ve yorum bırakması. Velilerin verdiği puanlar eğitmenin KPI'ındaki "Veli Memnuniyeti" değerini besler (Akış 10).

## Kim yapar?
Anketi yönetici hazırlar; veli yanıtlar. Demo'da Veli rolü anket yanıtlama ve geri bildirim gönderme iznine sahip; Eğitmen geri bildirimleri okuyabilir.

## Adımlar
1. **Anket hazırla:** başlık, kime yönelik (veli, eğitmen ya da öğrenci), sorular ve soruların seçenekleri. En az bir soru olmalı.
2. **Yanıtla:** anketin hedefi olan kişi cevapları gönderir. Her soru için bir cevap verilir; ankete ait olmayan bir soruya cevap verilemez. Aynı anket aynı kişi tarafından ikinci kez gönderilemez.
3. **Veli geri bildirimi:** veli bir eğitmen ya da seans için 1–5 puan ve/veya yorum bırakır. İkisinden en az biri dolu olmalı.
4. **Sonuçlara bak:** yanıtlar ve geri bildirimler listelenebilir; puanlar KPI hesaplamasında kullanılır.

## Dikkat
- Ekran olmadığı için bugün bu verileri yalnızca demo veri doldurur: demo'da her kurumda 2 anket, bir yanıt ve bir veli geri bildirimi var.
- Geri bildirim puanı 1 ile 5 arasında olmalı. Aynı anketin ikinci kez gönderilmesi "Anket yanıtı zaten gönderilmiş." uyarısını verir.
- Menüde "Anketler" ve "Geri Bildirim" satırları seed'de tanımlıydı ama pasifleştirildi.
- Veli portalında anket doldurma ekranı da yok.

## Kendin dene
Ekran olmadığı için tarayıcıdan denenemez. KPI'daki etkisini görmek için `admin@demo-a.test` ile `KPI & Performans`'a bakın: veli memnuniyeti bu geri bildirimlerden gelir. Denemek için teknik destek gerekir.

Teknik ayrıntılar: [teknik/akis_11_anketler.md](teknik/akis_11_anketler.md)
