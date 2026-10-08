# Dizge tasarım ilkeleri

Kaynak: kullanıcının paylaştığı görsel tasarım görüşmesi özeti. 2026-09-09. Bu belge hedef ilkeleri kaydeder; bütün ekranların görsel olarak doğrulandığı anlamına gelmez.

## Kimlik

Modern kurumsal yazılım, hafif Türk kültürü katmanı. Kullanılabilirlik görsel süslemeden önce gelir. Yaklaşık %90 kurumsal kullanılabilirlik, %10 kültürel kimlik. Çizgi → düğüm → dallanma → bağlantı → sistem dili kullanılır. Modern Damga yapısal temel, Digital Synthesis karakter, Hayat Ağacı hafif sıcaklık katmanıdır. Yoğun tarihî/fantastik figürler kullanılmaz.

## Renk ve bileşenler

- Forest #073D3A, ana aksiyon teal #0B8C84, kontrollü turquoise #23B4AA, marka crimson #A72E26.
- Marka kırmızısı semantic danger değildir; başarı, uyarı, hata ve bilgi tokenları ayrı yönetilir.
- İnce sınırlar, temiz boşluklar, güçlü tipografi, kontrollü köşe yuvarlaklığı ve hafif gölgeler.
- Sidebar yaklaşık 240px, solid koyu forest/teal. Koyu tema orman/siyaha yakın tonlarda; neon görünüm yok.
- Harici font bağımlılığı eklenmez. Razor Pages + HTML/CSS/JavaScript korunur.
- Ortak tokenların mevcut merkezi wwwroot/css/tailwind.css. Arka plan, yüzey, kenarlık, metin, semantik renk, radius, shadow ve transition tokenları kullanılır.
- Form, tablo, buton, kart, badge, modal, tab, boş durum ve profil bileşenleri ortak dil kullanır. Sayfa bazında rastgele HEX veya tekrar eden varyantlar üretilmez.
- Backend karşılığı olmayan arama, bildirim, yardım veya aksiyon eklenmez.

## Onaylanan yeni yön — 2026-09-24

- Uygulamanın mevcut forest/koyu turkuaz, teal ve kontrollü turquoise kimliği korunacak. Yeni konseptlerdeki lacivert ağırlıklı palet doğrudan alınmayacak; yalnızca mevcut renklerin ton ve kontrast dengesi iyileştirilebilir.
- Boş alanları doldurmak için kullanılan büyük motif, desen, dağ, çiçek, mimari siluet, slogan ve filigranlar kaldırılacak. Türk kültürü katmanı renk disiplini, düzenli geometri ve sade hayat ağacı marka işaretiyle sınırlı kalacak.
- Kontrol panelinin ana bilgi mimarisi `KPI özeti + Aktif Projeler + Son Hareketler + Zaman Çizelgesi` olacak. Mevcut analiz bölümleri ilk dönüşümde bununla değiştirilecek; kullanım deneyimine göre sonradan ekleme veya çıkarma yapılabilir.
- Proje ekipleri, sorumlular ve hareket akışlarında profil fotoğrafı gösterilebilecek. Kullanıcı fotoğrafı yoksa ad ve soyaddan üretilen harfli avatar kullanılacak. Demo kullanıcıları için yerel örnek profil görselleri kullanılabilir; dış avatar servisine çalışma zamanı bağımlılığı kurulmayacak.
- Dashboard ve diğer yoğun çalışma ekranlarında kompakt Jira benzeri bilgi yoğunluğu korunacak. Renk çeşitliliği KPI, durum, ilerleme ve küçük vurgu alanlarında kullanılacak; geniş yüzeyler sakin kalacak.

## Görsel konsept notları — 2026-09-25

- Proje liste satırlarında “ilgili kişiler” küçük, üst üste binen fotoğraflı avatar grubu olarak gösterilecek; kalan kişi sayısı `+N` rozetiyle özetlenebilecek. Fotoğrafı olmayan kullanıcıda harfli avatar kullanılacak.
- Proje ilerlemesi sayısal yüzde ve ince ilerleme çubuğuyla birlikte gösterilecek. Yüzde, renk tek başına anlam taşımayacak şekilde metinle okunabilir kalacak.
- Proje, görev ve Ticket kayıtları liste, Board veya arama sonucundan seçildiğinde ilk ön izleme sağ çekmecede açılabilecek. Ön izleme içinden ikinci bir ilişkili kayıt açılırsa daha önce kararlaştırılan sol çekmece katmanı kullanılabilecek; tam detay sayfasına geçiş ayrıca korunacak.
- Koyu Pipeline konseptindeki kompakt üst proje özeti beğenildi: proje kimliği, sağlık/ilerleme halkası, devam eden, tamamlanan, gecikmiş ve engellenen sayıları ile görünüm sekmeleri tek ince üst bölgede toplanabilir.
- Pipeline çalışma alanında sabit görev hiyerarşisi, görev adını ve yüzdesini taşıyan zaman çubukları, bağımlılık çizgileri ve sağ görev denetçisi sonraki görsel geliştirmeler için referanstır. Bu yoğun düzen özellikle geniş ekran/fokus görünümü olarak değerlendirilecek.
- Boş alan motifleri ve süs amaçlı kültürel çizimler bu konseptlerde kullanılmayacak; mevcut Dizge renkleri ve sade marka işareti korunacak.

### Onaylanan proje detay referansı

2026-09-25 tarihinde üretilen ikinci konseptteki **Proje Detayı** ekranı, proje detay sayfasının sonraki görsel uygulaması için ana referans olarak seçildi.

- Sayfa başında mevcut ortak breadcrumb ve sağ eylemler korunur: `Pipeline`, `Düzenle`, diğer işlemler ve proje durumu.
- Proje kimliği tek kompakt yatay özet alanında gösterilir: `PRJ-0000` kodu, proje adı, departman/kategori bağlamı ve durum.
- Aynı özet satırında proje yöneticisinin fotoğrafı ve adı, üst üste binen fotoğraflı `İlgili Kişiler` grubu, `+N` özeti, dairesel proje ilerlemesi ve görev sayaçları bulunur.
- Görev sayaçları en az `Açık`, `Tamamlanan`, `Gecikmiş` ve `Engellenen` bilgilerini gösterir. Renkler ikincil destek sağlar; sayı ve metin her zaman görünür kalır.
- İçerik sekmeleri `Genel Bakış`, `Görevler`, `Zaman Çizelgesi`, `Dosyalar` ve `Geçmiş` olarak düzenlenir. İlk açılış `Genel Bakış` olur.
- Genel Bakış'ın ana alanında güncel milestone/aşama; tarih aralığı, durum, ilerleme çubuğu ve içindeki görevlerin toplam/tamamlanan/devam eden/gecikmiş sayılarıyla gösterilir.
- Görev durumu dağılımı kompakt halka grafik ve yanında sayısal açıklamalarla gösterilir. Grafik tek başına bilgi taşımaz.
- İlgili kişiler alanı fotoğraf, ad ve proje rolünü birlikte gösterir; fotoğrafı olmayanlarda harfli avatar kullanılır.
- Proje bilgileri tek kartta proje kodu, ad, departman, başlangıç/bitiş tarihi, yönetici, durum ve kısa açıklamayı içerir; üst özet satırındaki bilgileri gereksiz biçimde tekrarlamamak için uygulama sırasında sadeleştirilir.
- Son aktiviteler fotoğraflı kullanıcı, işlem özeti ve göreli zaman bilgisiyle kompakt bir akış olarak gösterilir.
- Alt bölgede `Yaklaşan Checkpoint'ler` tarih, tür ve durumla; `İlişkili Projeler` ise departman, ilerleme yüzdesi/çubuğu ve sağlık durumuyla tablo biçiminde gösterilir.
- Geniş boşluk, büyük kahraman başlığı, dekoratif motif ve bilgi tekrarı kullanılmaz. Ekran mevcut Dizge forest/teal/turquoise renkleriyle, ince sınırlar ve kompakt Jira benzeri yoğunlukla uygulanır.

## Tamamlama turu

İlk yenileme dashboard, proje/görev ekranları, Kanban, yönetim, giriş, profil ve erişim reddi ekranlarına uygulandı. Dashboard görsel referanstır. Register devre dışıdır; Logout ayrı ekran değildir.

- Gerçek masaüstü/tablet/mobil boyutlarında ve iki temada ekran turu.
- Ortak boşluk, yazı boyutu, kart yüksekliği, tablo yoğunluğu ve buton tutarlılığı.
- Kanban taşıma sonrası durum/tarih/border ve başarısız işlem geri dönüşü; geniş ekran özet boyutu.
- Users Türkçe mesajları, Login hata mesajı, Audit Logs ChangePassword eşlemesi, Recycle Bin gerçek kullanıcı adları.
- Türkçe karakter ve encoding kontrolü; form hata/boş/başarılı durumları.
- Görsel kararlar oturduktan sonra kullanılmayan CSS ve tekrarların temizliği; Razor ve JavaScript sınır durumları.

Önce ortak sorunları bileşen seviyesinde çöz, sonra sayfaya özel ayrıntıları düzelt. Kullanıcıya ait mevcut görsel değişiklikleri koru.

## Uygulama durumu — 2026-09-25

- Geyik boynuzu, hayat ağacı ve devre düğümlerini birleştiren sade Dizge işareti sidebar ve giriş ekranına uygulandı.
- Kontrol paneli `KPI + aktif projeler + son hareketler + proje zaman çizelgesi` yapısına geçirildi. Proje satırlarında ilgili kişiler ve görevlerden hesaplanan ilerleme yüzdesi yer alıyor.
- Kullanıcılara yerel profil fotoğrafı yükleme alanı eklendi; üst çubuk, kontrol paneli ve proje detayında fotoğraf, yoksa harfli avatar gösteriliyor.
- Proje detayının üst özeti ekip, sorumlu, ilerleme halkası ve görev sayılarını içeriyor. Proje zaman çizelgesine detay sekmelerinden erişilebiliyor.
- Açık ve koyu temalar gerçek demo verileriyle görsel olarak kontrol edildi.
- Proje, görev ve ticket listelerine ortak sağ ön izleme çekmecesi uygulandı. Görevden bağlı proje açıldığında ikinci çekmece soldan geliyor; tam detay bağlantıları korunuyor.
- Pipeline üst bölümü proje kimliği, ilerleme halkası ve açık/tamamlanan/gecikmiş/engellenen sayaçlarıyla kompaktlaştırıldı. Akış görünümündeki proje ilişkileri merkez proje etrafında bağlantılı kartlar olarak gösteriliyor.
- Proje detayına checkpoint durumu, ilişkili projeler ve son Pipeline aktiviteleri eklendi.
- Pipeline zaman çizelgesindeki görevler tıklandığında sağ görev denetçisi açılıyor. Panel aşama/checkpoint, ilerleme, öncelik, sorumlu, tarihler, açıklama ve ön koşulları gösteriyor; tıklama ile tarih sürükleme birbirinden ayrıldı.
- Dört yerel demo kullanıcıya dış servise ihtiyaç duymayan profil görselleri bağlandı; fotoğrafı olmayan kullanıcılarda harfli avatar davranışı korunuyor.
- Pipeline görev bağımlılıkları zaman çizelgesinde yönlü bağlantılarla gösteriliyor. Tamamlanan ön koşullar yeşil, bekleyen ön koşullar sıcak vurgu renginde; açıklama göstergesi ve görev paneliyle birlikte çalışıyor.
- Gün, hafta ve ay ölçekleri kısa proje aralıklarında çalışma alanını dengeli dolduruyor; uzun projelerde yatay kaydırma korunuyor.
- Kontrol panelindeki projeler arası zaman çizelgesi, hiçbir projenin aktif olmadığı 30 günden uzun aralıkları otomatik olarak sıkıştırıyor. Gerçek proje tarihleri korunuyor; sıkıştırılan bölüm `//` eksen kırığı ve açıklama metniyle belirtiliyor.
- Ortak proje ön izleme çekmecesi kontrol panelindeki proje listesine, proje zaman çizelgesine ve proje detayındaki ilişkili proje satırlarına yayıldı. Tek sayılı bilgi gruplarında son alan tüm satırı kaplayarak boş hücre bırakmıyor.
- Daraltılmış sol menüde açma düğmesi logonun altına yerleşiyor; açık menüde logo yanında kalıyor. Yalnız ikon görünen durumda bağlantılar hover başlığı ve erişilebilir ad taşıyor; düğmenin yönü ile açıklaması menünün durumuna göre değişiyor.
- Proje, görev ve Ticket liste ekranları 390 px mobil görünümde yeniden kontrol edildi. Ticket hızlı görünüm sekmeleri dokunarak yatay kaydırılmaya devam ederken tarayıcının kaba yatay kaydırma çubuğu gizlendi.
- Pipeline Akış görünümü, zaman çizelgesinin üzerinde açılan geniş bir çalışma penceresine taşındı. İlişkiler, aşamalar, checkpoint'ler ve görev yerleşimi aynı kaydırılabilir yüzeyde kalıyor; mobilde panel tam ekran ve başlığı daha kısa kullanıyor.
- Pipeline içindeki `Akışı yönet` eylemi görünüm sekmelerinden ayrıldı. Zaman Çizelgesi ile Geçmiş aynı görünüm grubunda kalırken çalışma penceresini açan eylem sağda ayrı bir düğme olarak gösteriliyor ve seçili zaman ölçeği dönüşte korunuyor.
- Pipeline başlığındaki açık, tamamlanan, gecikmiş ve engellenen sayaçları mobilde dört eşit sütuna yerleşiyor; ilerleme bilgisi ayrı bir tam genişlikli çubuk olarak gösteriliyor. Başlık kartı artık dar ekranda yatay taşmıyor.
- Ticket listesi mobilde konu ve durum sütunlarına odaklanıyor. Sorumlu ile son işlem bilgileri sağ ön izlemede kalıyor; liste ekranındaki yatay kaydırma kaldırıldı.
- Proje ve görev detay ekranları mobil koyu temada doğrulandı. Proje formundaki çoklu ekip seçimleri açık ve koyu temada ortak yüzey ile turkuaz seçili durum kullanıyor.
- Proje ve görev listelerinde aktif filtreleri temizleyen tam genişlikli mobil eylem artık yalnız simge yerine `Filtreleri temizle` metnini de gösteriyor.
- Yönetim ekranlarının başlık eylemleri mobilde alt alta ve tam genişlikte yerleşiyor. Özet kartı ile ikincil eylem artık aynı satırda sıkışarak sayfayı yatay taşırmıyor.
- Üst arama, iki karakterden sonra çalışma alanına uygun ve kullanıcının erişebildiği kayıtları hızlı sonuç panelinde gösteriyor. Sonuçlar ortak sağ ön izleme çekmecesinde açılıyor; Enter ile tam liste araması korunuyor.
- Pipeline listesi, geçmiş, zaman çizelgesi ve görev bağımlılığı bileşenlerinin statik stilleri ortak CSS'e taşındı. Razor üzerindeki satır içi stiller yalnızca çalışma anında hesaplanan genişlik, konum, ilerleme ve etiket rengi değerleri için kullanılıyor.
- Projeler ve Görevler liste ekranları aynı özet kartı, filtre yüzeyi, liste başlığı, tablo başlığı ve boş durum kurallarını kullanıyor. Durum renkleri, sütun ölçüleri ve kayıt içeriğine özgü kurallar ekran bazında ayrı kalıyor.
- Görev Board'u, Proje Kanbanı ve ortak kayıt ön izlemesinin çekmece açılış, kapatma, başlık ve bilgi alanları ortak seçicilerde birleştirildi. Sağdan açılan ana panel ile soldan açılan ilişkili kayıt paneli davranışı korunuyor; dar ekranda paneller tam genişliğe geçiyor.
- Proje durumu, kullanıcı rolü, denetim hareketi, görev Board'u durumu ve görev önceliği renkleri anlamlarına göre bilgi, başarı, uyarı, tehlike ve nötr gruplarında ortak tema değişkenlerini kullanıyor.
- Proje ve görev oluşturma formlarının ana grid yapısı ortaklaştırıldı; masaüstündeki iki sütunlu düzen ve mobildeki tek sütunlu düzen korunuyor.
- Razor ve JavaScript kullanımıyla karşılaştırmalı seçici taraması tamamlandı. Eski dashboard, form, Ticket ve yönetim yerleşimlerinden kalan 150 kullanılmayan sınıf seçicisi kaldırıldı; dinamik rol, Kanban durumu ve öncelik sınıfları korundu.
- Tailwind CDN kaldırıldı. Uygulama kabuğu, mobil menü, gizleme ve ekran okuyucu yardımcıları ile temel `box-sizing`, gövde boşluğu ve sistem yazı tipi kuralları yerel CSS içinde tanımlandı. Uygulama görünümü artık Tailwind ağına bağlı değildir.
