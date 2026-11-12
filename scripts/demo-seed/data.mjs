// Uydurma ama gerçekçi Türkçe isim havuzları. Kurumlar arasında KESİŞMEZ (izolasyon elle ayırt edilebilsin).
// Gerçek kişi, telefon ya da geçerli TC no kullanılmaz; e-postalar .test alan adıyla.
export const POOLS = {
  'demo_a': {
    child: [['Ada', 'F'], ['Eymen', 'M'], ['Defne', 'F'], ['Kerem', 'M'], ['Zeynep', 'F'], ['Alp', 'M'], ['Elif', 'F'], ['Mert', 'M'],
            ['Nehir', 'F'], ['Yiğit', 'M'], ['Asya', 'F'], ['Ege', 'M'], ['İpek', 'F'], ['Arda', 'M'], ['Lina', 'F']],
    surname: ['Sezer', 'Uysal', 'Karahan', 'Tekin', 'Aslan', 'Güneş', 'Demirci', 'Yalçın', 'Özkan', 'Bulut', 'Erdem', 'Kaplan', 'Şahin', 'Çetin', 'Koç'],
    mother: ['Gülay', 'Sevgi', 'Ayten', 'Deniz', 'Pınar', 'Hülya', 'Nilgün', 'Ceren', 'Burcu', 'Aslı', 'Tuba', 'Songül', 'Meltem', 'Filiz', 'Derya'],
    father: ['Fikret', 'Murat', 'Selim', 'Ercan', 'Volkan', 'Orhan', 'Kaan', 'Levent', 'Barış', 'Taner', 'Cem', 'Ahmet', 'Serkan', 'Yusuf', 'Emre'],
    educator: [['Merve', 'Çalışkan', 'therapist', 'aba'], ['Berk', 'Altın', 'educator', 'special_education'], ['Seda', 'Korkmaz', 'therapist', 'speech_language'],
               ['Utku', 'Işık', 'psychologist', 'psychological'], ['Gizem', 'Arslan', 'therapist', 'occupational'], ['Okan', 'Turan', 'educator', 'special_education']],
    lead: ['Hande Yıldız', 'Sinan Çakır', 'Ebru Kılıç', 'Tamer Aydın', 'Naz Erkan', 'Ozan Sönmez', 'Pelin Duman', 'Rıza Kurt', 'Aylin Gök', 'Doruk Esen',
           'Leyla Tuncer', 'Bora Sarı', 'Gamze Özer', 'Tarık Yurt', 'Sibel Ay', 'Erkan Bilgin', 'Nazlı Korur', 'Cihan Ateş', 'Müge Polat', 'Anıl Taş'],
    leadChild: ['Bade', 'Poyraz', 'Duru', 'Umut', 'Ela', 'Çınar', 'Mira', 'Atlas', 'Selin', 'Kuzey', 'Nil', 'Rüzgar', 'Beren', 'Çağan', 'Azra', 'Toprak', 'Ecrin', 'Göktuğ', 'Mislina', 'Barlas'],
    rooms: ['Bulut', 'Güneş', 'Online'], school: 'Demo Ay Işığı İlkokulu', campName: 'Demo Gökkuşağı Yaz Kampı',
    rooms2: ['Yıldız', 'Ay', 'Online'],
  },
  'demo_b': {
    child: [['Mina', 'F'], ['Efe', 'M'], ['Azra', 'F'], ['Çınar', 'M'], ['Nil', 'F'], ['Barış', 'M'], ['Ela', 'F'], ['Hamza', 'M'],
            ['Su', 'F'], ['Tuna', 'M'], ['Melis', 'F'], ['Aras', 'M'], ['Derin', 'F'], ['Kağan', 'M'], ['Yaren', 'F']],
    surname: ['Aydemir', 'Polat', 'Başaran', 'Kurtuluş', 'Eren', 'Yüksel', 'Tunç', 'Akgül', 'Soylu', 'Çelik', 'Karataş', 'Önal', 'Acar', 'Sevim', 'Gürsoy'],
    mother: ['Nurcan', 'Ayşe', 'Sibel', 'Özlem', 'Gönül', 'Ferda', 'Çiğdem', 'Rabia', 'Betül', 'Neslihan', 'Kübra', 'Sema', 'Dilek', 'Yasemin', 'Emine'],
    father: ['Cengiz', 'Hasan', 'Mustafa', 'Erdal', 'Ramazan', 'Bülent', 'Süleyman', 'Metin', 'Kadir', 'İsmail', 'Recep', 'Hüseyin', 'Zafer', 'Doğan', 'Tuncay'],
    educator: [['Tolga', 'Bayram', 'therapist', 'aba'], ['Naciye', 'Özdemir', 'educator', 'special_education'], ['Furkan', 'Yazıcı', 'therapist', 'speech_language'],
               ['Ceyda', 'Mutlu', 'psychologist', 'psychological'], ['Buğra', 'Kalkan', 'therapist', 'physiotherapy'], ['Reyhan', 'Toprak', 'educator', 'special_education']],
    lead: ['Gülsüm Ak', 'Taylan Er', 'Nuray Can', 'Volkan Işın', 'Dilan Şen', 'Eray Pala', 'Songül Turgut', 'Kutay Oral', 'Şule Bozkurt', 'Alper Yaka',
           'İrem Sağlam', 'Batuhan Ünal', 'Zehra Kocabaş', 'Oğuz Tezcan', 'Hatice Mert', 'Soner Çınar', 'Berna Aksu', 'Yavuz Dinç', 'Tülin Ekin', 'Cüneyt Say'],
    leadChild: ['Defne', 'Efe', 'Elif', 'Ravza', 'Mehmet', 'Ayaz', 'Zehra', 'Yusuf', 'Hira', 'Eymen', 'Asel', 'Miraç', 'Nehir', 'Emir', 'Alya', 'Kerem', 'Ecem', 'Buğra', 'Sare', 'Taha'],
    rooms: ['Papatya', 'Lale', 'Online'], school: 'Demo Çınar Ortaokulu', campName: 'Demo Papatya Yaz Kampı',
    rooms2: ['Menekşe', 'Gül', 'Online'],
  },
}
export const BEP_GOALS = [
  ['Göz teması kurarak ismine yanıt verme', 'communication', 'social_emotional'],
  ['İki sözcüklü istek cümleleri kurma', 'communication', 'language'],
  ['Basit yönergeleri (otur, gel, ver) yerine getirme', 'behavior', 'cognitive'],
  ['Kalemi uygun tutuş ile çizgi çizme', 'motor', 'motor'],
  ['Akranlarıyla sırayla oyun oynama', 'social', 'social_emotional'],
  ['Tuvalet becerisini bağımsız yürütme', 'behavior', 'self_care'],
]
