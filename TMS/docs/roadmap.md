# Dizge geliştirme yol haritası

> **8 Ekim 2026 kararı:** Microsoft 365 üzerinden postadan Ticket alma çalışması durduruldu. Altyapı silinmedi; `Ticketing:Microsoft365:Enabled=false` varsayılanıyla arka plan işlemi ve ilgili kullanıcı arayüzleri kapalı tutuluyor. Manuel Ticket çalışma alanı kullanılmaya devam ediyor. Bu entegrasyon artık GitHub/yayın öncesi yapılacaklar arasında değildir.

Son güncelleme: 2026-10-08. Organizasyon/yetkilendirme paketi, ana kabul turu ve proje pipeline'ının ilk kapsamı tamamlandı. Ticket çekirdeğinde manuel kayıt, havuz, yazışma, atama, özel erişimli proje görevi ve dosyalar çalışıyor. Ticket çalışma alanına tüm kayıtlar, görüntüleyebildiklerim, izlediklerim, çözüldü/kapatıldı, arama ve sayfalama eklendi. Tek Ticket destek departmanı açık olarak işaretlendi; hesap silme yerine pasifleştirme/yeniden etkinleştirme kullanılıyor. Microsoft 365 posta entegrasyonu ürün kapsamından çıkarılarak kapatıldı; sıradaki ana çalışma GitHub ve yayın öncesi doğrulamadır.

Ticket liste, oluşturma, detay ve eşleşmeyen gönderen ekranları ortak görsel dile taşındı. Ortak `tailwind.css` içine Ticket stilleri eklendi; yüklenmeyen eski `site.css` kaldırıldı. Ticket listesinin masaüstü ve mobil görünümü yerel demo hesabıyla kontrol edildi. Oluşturma, detay ve eşleşmeyen gönderen ekranlarının masaüstü görünümü yetkili hesapla doğrulandı; kalan tema ve mobil durumları ayrıntılı ekran turuna dahildir.

Pipeline genel ekranı üç sütunlu kompakt kart düzenine geçirildi. Proje pipeline detayında varsayılan ve ilk sekme Zaman Çizelgesi oldu; Akış ikinci sıraya alındı ve her iki görünümün boşlukları, başlıkları, kartları ile formları sıkılaştırıldı. Geliştirme ortamına açık, işlemde, bilgi bekleyen, çözülmüş ve kapatılmış durumlarını; gelen/giden yanıtı ve iç notu kapsayan beş örnek Ticket eklendi. Ticket listesi destek kuyruğu biçiminde sık kullanılan görünüm sekmeleri, renkli durumlar ve daha taranabilir satırlarla yenilendi. Detay ekranındaki gelen/giden iletiler konuşma düzenine geçirildi, yanıt alanı yazışmanın altına taşındı ve yönetim paneli sıkılaştırıldı.

Referans tasarım incelemesinin ardından uygulama kabuğu daraltıldı; sol menünün aktif görünümü sakinleştirildi, üst arama bağlama duyarlı hale getirildi, hesap/rol alanı eklendi ve çalışma alanı footer'ı kaldırıldı. Referansta bulunan `Ctrl K` etiketi ve kısayol davranışı üründen çıkarıldı. Ticket listesi kompakt özet şeridi, paylaşılabilir `TCK-yıl-sıra` numarası ve tek panel içinde sekme/arama/tablo düzeni kazandı. Atanmamış havuzda en eski Ticket öne çıkarılır ve yetkili kullanıcı tek adımda üstlenebilir. Manuel Ticket oluşturma ekranı canlı özet ve sonraki adımlar paneli bulunan iki sütunlu akışa geçirildi. Ticket detayındaki yönetim işlemleri iş akışı, projeye aktarma, görüntüleyiciler ve geçmiş için açılır bölümlere ayrıldı. Eşleşmeyen gönderen ekranı özet göstergeleri, sıkı kayıt formu, en eskiden yeniye inceleme kuyruğu ve belirgin boş durumla yenilendi.

Ticket ekranındaki başlık hiyerarşisi uygulamanın ortak standardı yapıldı. Kontrol paneli; proje, görev, Board, Kanban ve Pipeline görünümleri; detay ve düzenleme formları; Ticket ekranları; yönetim, profil, gizlilik ve sistem durum sayfaları breadcrumb, başlık, açıklama ve sağ eylem sırasını kullanıyor. Eski büyük harfli sayfa etiketleri kaldırıldı, başlık ölçüleri ve mobil kırılım ortaklaştırıldı. Temsilî liste, detay, yönetim ve Pipeline ekranları masaüstünde; Projeler ekranı mobil genişlikte taşma olmadan doğrulandı.

Projeler çalışma alanı durumlara göre filtrelenebilen kompakt özet şeridi kazandı. Filtre ve liste tek yüzeyde birleştirildi; departman ile kategori proje adının altına taşınarak tablo beş sütuna indirildi ve normal masaüstü genişliğindeki yatay kaydırma kaldırıldı. Liste ile Kanban'da `PRJ-0000` biçimi ortaklaştırıldı; Kanban başlığına yetkiye bağlı Yeni Proje eylemi eklendi. Liste, durum filtresi ve Kanban açık tema masaüstünde doğrulandı.

Görevler çalışma alanı toplam, devam eden, incelemede ve gecikmiş işleri gösteren renkli hızlı filtreler kazandı. Filtre ile liste tek yüzeyde birleştirildi; kategori görev etiketlerine, son tarih sorumlu alanına taşınarak tablo beş sütuna indirildi. Gecikme vurgusu satırı boyamak yerine mor tonlu küçük işaret ve tarih üzerinde tutuldu. Liste ile Board görev kodları `TSK-00000` biçiminde eşitlendi; Board'daki tamamlanmamış görev sayısı “Açık” olarak adlandırıldı. Liste, gecikmiş filtresi ve Board açık tema masaüstünde doğrulandı.

Proje ve görev detay ekranlarındaki yinelenen bilgiler azaltıldı. Proje genel bakış kartı görev durumlarından hesaplanan yüzde, ilerleme çubuğu ve dört durum sayacına dönüştürüldü; ekip listesi sayfayı uzatmaması için sınırlı yüksekliğe alındı ve Pipeline geçişi üst eylemlere eklendi. Görev genel bakışı yalnızca durum ile planlanan/gerçekleşen kayıt tarihlerini gösterir; proje, atanan, kategori, öncelik ve etiket tekrarları kaldırıldı. Ayrıntı başlıklarında `PRJ-0000` ve `TSK-00000` kodları gösteriliyor. Her iki ekran açık tema masaüstünde doğrulandı.

Proje ve görev oluşturma/düzenleme formları iki adımlı, iki kartlı kompakt çalışma alanına geçirildi. İlişkili durum, öncelik, tarih, sorumlu ve kategori alanları masaüstünde yan yana yerleşiyor; açıklama alanları kısaltıldı, proje ekip seçimi sınırlı yükseklikte tutuldu ve kaydet/iptal alanı sayfanın altında görünür kalıyor. Seçim kutularındaki hatalı ikon kodu giderildi. Dört form da veri dolu ve boş durumlarıyla açık tema masaüstünde doğrulandı.

Yönetim ekranları görsel turdan geçirildi. Departman, kategori, kullanıcı ve geri dönüşüm ekranlarının mevcut kompakt düzeni korundu. Denetim günlüğü yüzlerce kaydı tek seferde çizmek yerine sunucu tarafında 25 kayıtlık sayfalara ayrıldı; serbest metin, işlem türü ve varlık türü filtreleri eklendi. Yönetim sayfalarındaki üst arama artık görev aramasına gitmiyor; kullanıcı yönetimine, denetim günlüğünde ise doğrudan kayıt aramasına bağlanıyor. Organizasyon ve yetkiler ekranındaki tekrar eden tam genişlikli departman formları iki sütunlu kompakt kartlara taşındı; sorumlu ve varsayılan Manager bilgileri görünür hale getirildi. Kullanıcı departmanı ile geçmiş erişim işlemleri ayrı bir operasyon bölümünde yan yana düzenlendi; bu ekrana geçiş Departmanlar sayfasının başlık eylemlerine taşındı. Profil ekranının mevcut kompakt yapısı korundu; erişim reddi sayfasındaki bozuk Türkçe karakterler düzeltildi, genel hata ve eski gizlilik şablonu ürün diline uyarlandı, yönlendirme sayfalarındaki artık metinler temizlendi ve üst çubuktaki bildirim zili erişilebilir bir boş durum paneli kazandı. Pipeline bağımsız sol menü öğesi olmaktan çıkarıldı; Projeler içindeki Liste, Kanban ve Pipeline görünüm grubuna alındı, proje pipeline detayından geri dönüş ilgili proje detayına bağlandı.

## Belgeler

- [Ürün kuralları ve yetki matrisi](product-rules.md)
- [Tasarım ilkeleri](design-system.md)
- [İlk geliştirme paketi ve kod farkları](implementation-plan.md)
- [İlk paket uygulama ve test raporu](organization-release.md)
- [Ticket çekirdeği tasarım taslağı](ticket-design.md)

## Mevcut durum

ASP.NET Core 8, Razor Pages, Identity, EF Core ve SQL Server kullanılıyor. Proje/görev, proje Kanban'ı, yorum, ek, yönetim, geri dönüşüm ve pipeline altyapısı mevcut. Organizasyon ve yetki paketi yerel migration ile uygulandı; Admin, Manager ve Member kabul kontrolleri tamamlandı. Pipeline'ın akış ve zaman çizelgesi dilimleri yerel veritabanında çalışıyor. Ticket çekirdeğinin mevcut dilimi yerel uygulamada açıldı. Temiz yayın paketi doğrulandı; canlı ortam ve ikinci bilgisayar kurulumu henüz doğrulanmadı.

## Fazlar

| Faz | İş | Tamamlanma ölçütü |
|---|---|---|
| 0 | Karar belgeleri ve güncel kod farkları | İlk paket için kurallar ve doğrulama senaryoları belgeli. Bu hazırlık tamamlandı; kapsamlı canlı inceleme yapılmadı. |
| 1 | Yetkilendirme ve organizasyon | Çok departmanlı Manager, varsayılan Manager, ekip, atama/devir geçmişi ve merkezi erişim kuralları sunucuda doğrulanıyor. |
| 2 | Veri ve işlem doğruluğu | Yorum/ek erişimi, ilişkili geri yükleme, profil, dashboard ve filtreler tutarlı çalışıyor. |
| 3 | Görsel ve etkileşim düzenlemeleri | Açık/koyu tema, mobil, Türkçe, ortak bileşenler ve yalnızca Admin'in taşıma yaptığı Kanban doğrulandı. Ana kabul turu tamamlandı; yeni ekranlar kendi fazında kontrol edilir. |
| 4 | Proje pipeline | Genel/proje görünümü, aşama/checkpoint yönetimi ve sıralaması, görev yerleştirme, otomatik ilerleme, gün/hafta/ay ölçekli zaman çizelgesi, sürükleyerek tarih değiştirme, proje ilişkileri, checkpoint bağımlılıkları, görev ön koşulları ve filtrelenebilir geçmiş çalışıyor. |
| 5 | Ticket çekirdeği — ilk kapsam tamamlandı | Model, erişim, havuz, atama, durum, yazışma, iç not, görüntüleyici, takip, dosyalar ve özel erişimli proje görevi eklendi. Çalışma alanı filtreleri, arama ve sayfalama eklendi. Bağlı görev bitince Ticket çözülür. |
| 6 | E-posta entegrasyonu — kapalı | Microsoft Graph alma altyapısı kodda korunuyor. Arka plan işlemi, eşleşmeyen gönderen ekranı ve gelen posta adresi varsayılan ayarda kapalı; gerçek bağlantı ve dış yanıt gönderimi mevcut ürün kapsamından çıkarıldı. |
| 7 | Tamamlayıcı ürün özellikleri — tamamlandı | Uygulama içi bildirimler, Ticket önceliği ve SLA hedefi, departman bazlı otomatik atama, rol kapsamlı raporlar ve görev zaman takibi eklendi. |
| 8 | Sürüm doğrulaması — devam ediyor | 16 yetki, 117 SQL ve 91 HTTP senaryosu geçti; Release derlemesi uyarısız. Yerel migration'lar yedek alınarak uygulandı. Temiz bilgisayar kurulumu ve GitHub sürümü hazırlanıyor. |

Güvenlik kontrolleri ve dokümantasyon bütün fazlarda sürer. Bağımsız görsel işler erken yapılabilir; yeni modüller erişim altyapısına dayanmalıdır.

## Genel işlerden kalanlar

- **Görsel tur — mevcut kapsam tamamlandı:** Ticket detay ve eşleşmeyen gönderen ekranları, Projeler liste/filtre/Kanban ile detay ekranları ve Görevler liste/filtre/Board ile detay ekranlarının açık tema masaüstü kontrolü tamamlandı. Pipeline başlığı, Ticket listesi, proje/görev detayları ile düzenleme formları mobil koyu temada doğrulandı; dar ekrandaki sayaç ve Ticket tablosu taşmaları giderildi. Yetkisiz erişim, genel hata, profil, boş ve filtre sonucu olmayan durumlar ile bütün yönetim ekranları mobil koyu temada doğrulandı. Ortak CSS temizliği ve Tailwind CDN kaldırma sonrası temsilî ekranlar yeniden kontrol edildi. Yeni görsel çalışma yalnızca kullanıcı geri bildirimi veya yeni ekran ihtiyacı oluştuğunda açılacak.
- **Son CSS temizliği:** Kullanılmayan `site.css` kaldırıldı. Pipeline listesi, geçmişi, zaman çizelgesi ve görev bağımlılıklarının statik sayfa içi stil blokları ortak `tailwind.css` dosyasına taşındı; yalnızca hesaplanan konum, ilerleme ve etiket rengi gibi dinamik değerler Razor üzerinde kaldı. Projeler ve Görevler liste ekranlarındaki özet kartı, filtre kabuğu, liste başlığı, tablo başlığı ve boş durumlara ait 43 birebir kural çifti ortak seçicilerde birleştirildi. Görev Board'u, Proje Kanbanı ve ortak kayıt ön izlemesinin çekmece davranışları; proje, rol, denetim hareketi ve görev önceliği durum renkleri 15 ortak seçici grubunda toplandı. Proje ve görev formlarının ortak grid yapısı da tek kurala indirildi. Razor ve JavaScript kullanımıyla karşılaştırmalı taramada eski dashboard, form, Ticket ve yönetim yerleşimlerinden kalan 150 kullanılmayan sınıf seçicisi kaldırıldı; ortak CSS yaklaşık 42 KB küçüldü. Tailwind CDN kaldırıldı, gerekli temel sıfırlama, erişilebilirlik ve uygulama kabuğu kuralları yerel CSS'e taşındı. Geriye eşleşmesiz görünen 23 sınıf rol, Kanban durumu ve öncelik için çalışma anında üretilen ve bilerek korunan sınıflardır. Masaüstü açık ve mobil koyu tema karşılaştırmaları tamamlandı.
- **Pipeline akış etkileşimi:** Zaman Çizelgesi ana görünüm olarak korunuyor. Akış; aşama, checkpoint, görev yerleşimi ve proje bağlantılarını aynı yerde tutan geniş, kaydırılabilir bir çalışma penceresinde açılıyor. Pencere kapatma düğmesi ve `Esc` ile zaman çizelgesine döner; masaüstü ve mobil görünümü doğrulandı.
- **Microsoft 365 bağlantısı — kapalı:** Gelen kutusu takibi ve eşleştirme altyapısı kaynak kodda korunuyor fakat çalıştırılmıyor. Yeniden açma kararı verilmedikçe BT yetkisi, `Mail.Read`, `Mail.Send` veya gerçek posta kutusu testi bekleyen iş değildir.
- **Yayın öncesi doğrulama:** Kaynak kod, migration bütünlüğü, tam kabul turu ve temiz yayın paketi tamamlandı. İlk kurulum betiği ile GitHub CI hazırlandı. Eski SQL parolasını döndürme, Git geçmişini temizleme, lisans seçimi, ikinci bilgisayar kabulü ve GitHub sürümü açık kalır.
- **Tamamlayıcı özellikler:** Bildirim merkezi, Ticket SLA, isteğe bağlı otomatik atama, rol bazlı raporlar ve görev zaman takibi tamamlandı. Mail alma varsayılan olarak kapalı kalır.

## Keşif sonrası yüksek öncelikli düzenlemeler — 2026-09-18

- Ticket destek departmanı `IsTicketSupport` alanıyla tekil olarak tanımlandı. Var olan Sistem Geliştirme departmanı migration ile işaretlendi; yeni kurulumda yoksa başlangıçta oluşturulur. Ad değişikliği Ticket yetkisini artık değiştirmez. Diğer departmanlar Ticket destek departmanı olarak seçilemez.
- Gelen posta adresi ve Microsoft Graph bağlantı hazırlığı geçmiş çalışma olarak korunuyor. Postadan Ticket alma özelliği kapalı olduğundan uygulama bu adresi arayüzde göstermez, posta çekmez veya yanıt göndermez.
- Ticket listesinde destek ekibi bütün kayıtları, atanmış işleri, izlediklerini, incelemeyi, çözülmüş ve kapanmış kayıtları filtreleyebilir. Talep sahibi kendi kayıtlarını; ek görüntüleyici erişebildiği kayıtları görebilir. Listeye konu/talep sahibi araması ve sayfalama eklendi.
- Kullanıcı silme, geçmişi koruyan pasifleştirme/yeniden etkinleştirmeye çevrildi. Devam eden sorumlulukları olan hesap önce devredilir; pasif hesap oturum açamaz ve yeni atama listelerinde görünmez.
- Ayrı yerel yedek: `.local-backups/organization-20260918-122456`. İki migration yerel Tms veritabanına uygulandı; kayıtlar, tek destek departmanı ve kullanıcıların etkin durumu salt okunur sorguyla doğrulandı. İzole testlerde 16 yetki + 101 SQL + 88 HTTP senaryosu geçti. İncelenebilir SQL: `ticket-support-account-lifecycle.sql`.

## Pipeline kapsamı — ilk dilim uygulandı

- Genel ekran, erişilebilir projeleri bir arada gösterir; her projenin kendi pipeline'ı vardır. Görev başına pipeline yoktur.
- Pipeline aşaması ve proje durumu ayrıdır. Otomatik görev ilerlemesi proje durumunu kendiliğinden değiştirmez.
- Aşama çalışma bölümüdür; checkpoint geçiş noktasıdır.
- Görevler otomatik eklenir. Aşaması seçilmeyenler “Aşama bekleyen görevler” alanına girer.
- Zorunlu görevler tamamlanınca checkpoint tamamlanır; isteğe bağlı Manager/Admin onayı şartı bulunur.
- İlk sürümde aktif görevler eşit ağırlıklıdır. Zorunlu checkpoint bitmeden pipeline tamamlanmış sayılmaz.
- Yeniden açılan görev ilerlemeyi geriletir; checkpoint yeniden değerlendirilir ve geçmiş tutulur.
- “İlişkili proje” bağlantısı ve checkpoint geçişini engelleyebilen “Bağımlı proje” ilişkisi vardır.
- Pipeline düzenleme: projedeki Manager'lar ve Admin. Kanban taşıma: yalnızca Admin.
- Sınırlı görev erişimi tüm proje pipeline'ını açmaz. İlişki erişim kazandırmaz; bağımlılık döngüleri engellenir.
- Aynı projedeki görevler yönlü ön koşullarla bağlanabilir. Ön koşulları tamamlanmayan görev tamamlanamaz; akış, zaman çizelgesi ve görev detayında bekliyor görünür.
- Yönetici zaman çizelgesindeki görev çubuğunu sürükleyerek veya klavyeyle kaydırarak planlanan başlangıç ve bitiş tarihlerini birlikte değiştirebilir. Tarih formu erişilebilir alternatif olarak kalır.

Uygulanan dilimler: erişilebilir projelerin genel ekranı; proje içi aşama ve checkpoint oluşturma, düzenleme, silme ve sıralama; yeni görevlerin “Aşama bekleyen görevler” alanında otomatik görünmesi; Manager/Admin tarafından görev yerleştirme; görev durumlarına göre eşit ağırlıklı ilerleme; isteğe bağlı yönetici onayı; görev yeniden açılınca checkpoint onayının kaldırılması. Aşama silinince görevler bekleyen alana döner; checkpoint silinince görevler aşamasında kalır. Aynı proje ekranındaki zaman çizelgesi gün, hafta ve ay ölçeklerinde planlanan görev tarihlerini, bugünü, checkpoint'leri ve durumdan türeyen ilerlemeyi gösterir. Yönetici çubukları sürükleyerek veya ok tuşlarıyla tarihleri değiştirebilir; süre korunur ve mevcut sunucu yetkisi ile tarih doğrulaması kullanılır. Projeler “İlişkili” veya yönlü “Bağımlı” olarak bağlanabilir; bağımlılık tüm projeye veya belirli bir checkpoint'e uygulanabilir. Ön koşul proje tamamlanmadıkça ilgili checkpoint tamamlanmış veya onaylanabilir sayılmaz. Aynı proje içindeki görev ön koşulları Manager/Admin tarafından yönetilir; tekrarlar ve döngüler engellenir. Ön koşulları bitmeyen görev tamamlanamaz ve bekleme durumu akış, zaman çizelgesi ile görev detayına yansır. Proje bazlı geçmiş; aşama/checkpoint, görev, görev bağımlılığı ve bağlantı hareketlerini kullanıcı ve zaman bilgisiyle gösterir, kategoriye göre filtreler ve kaynak kayıt silinse de proje bağını korur. Member görünümü salt okunurdur; yalnızca görev erişimi diğer görev adlarını veya pipeline geçmişini açmaz. Pipeline ilk kapsamı tamamlandı; sıradaki faz Ticket çekirdeğidir. Sürüm doğrulaması kullanıcının kararıyla sona ertelendi.

## Ticket için sonraki kapsam

Manuel kayıt, atama, durum, talep sahibi, yanıt/iç not, geçmiş, dosyalar ve projeye özel erişimli görev aktarma eklendi. Microsoft Graph gelen kutusu takibinin kodu korunuyor ancak ürün kararına göre kapalıdır; dış yanıt gönderimi ve hata kuyruğu planlanmıyor. Önceliğe göre otomatik SLA hedefi, hedef tarihi düzenleme, SLA aşımı filtresi ve departman ayarından açılan iş yükü bazlı otomatik atama tamamlandı.

## Ayrı çalışmaların sınırları

Önerilen işler: organizasyon/veri modeli; merkezi yetkilendirme ve dosyalar; proje/görev akışları; ortak tasarım ve Kanban; pipeline; Ticket; e-posta. Her çalışma bu belgeleri okuyup kendi kabul senaryolarını tamamlar. Ortak dosyalara eşzamanlı değişiklik yapılmadan sorumluluk paylaşılır. Uygulama kararı belgeden farklılaşırsa belge güncellenir.
