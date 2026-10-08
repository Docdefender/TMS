# Organizasyon ve yetkilendirme — ilk paket

Tarih: 2026-09-09. Durum: kodlandı ve izole ortamda doğrulandı; yerel Tms veritabanına uygulandı; harici ortama yayınlanmadı.

## Uygulananlar

- ManagerDepartment ile çoklu departman sorumluluğu; departman başına filtreli benzersiz indeksle tek varsayılan Manager.
- /Admin/Organization ekranında Manager/departman bağlantıları, varsayılan Manager, kullanıcı ana departmanı ve devirden kalan erişimin kaldırılması. Departmanlar ekranından bağlantı var.
- AccessService/AccessRules üzerinden departman, proje üyeliği, atama, oluşturucu ve devirden kalan erişim kuralları. Rol değişiklikleri mevcut veritabanı rolüne göre kontrol edilir; eski çerez Admin işlemlerini açmaz.
- Proje oluşturma/düzenlemede ana departman, yalnızca Manager sorumlu seçimi, departmana bağlı çoklu ekip seçimi. Admin atama seçeneklerinde sunulmaz.
- Member yalnızca katıldığı projede görev oluşturur ve projedeki Member'lara atar. Proje seçimi değişince atama listesi güncellenir.
- Departman dışı görev devri sonrası salt okunur erişim; Manager oluşturucunun yönetim hakkının korunması. Atama olayları ve geçmiş erişim iptali ayrı tutulur.
- Proje detayında katılımcı Manager/Admin durum değişikliği; Kanban taşıma ve sunucu handler'ı yalnızca Admin.
- Yalnızca görev erişimi için /Projects/Basic: ad, açıklama, departman, sorumlu ve durum. Diğer görevler, ekip, proje ekleri ve yorumları yok.
- Dashboard/listeler ortak erişim kapsamını kullanır; dashboard durum bağlantıları Statuses parametresini gönderir.
- Yorum ve dosya kontrolleri kaynak yetkisine bağlı. Eski /uploads bağlantıları korunur; oturum/erişim doğrulanarak indirme yapılır. Statik dosya sağlayıcısı uploads içeriğini hiçbir alternatif yol üzerinden sunmaz.
- Proje silme işlem kimliğiyle bağlı görevlerin geri yüklenmesi; daha önce ayrı silinen görevler kapalı kalır.
- Üç eski kodlamalı C# dosyası UTF-8'e çevrildi; aynı değeri içeren yinelenmiş Türkçe RecycleBin kaynak anahtarı kaldırıldı.

## Doğrulama

Test komutu:

```powershell
dotnet run --project Tests/AccessChecks/AccessChecks.csproj --no-launch-profile -- --sql
```

- 16 bağımsız yetki senaryosu geçti.
- 27 SQL Server/EF senaryosu geçti: eski şemadan migration, Manager departman aktarımı, snapshot uyumu, sorgu kapsamı, atama/devir, geri yükleme ve ana departman değişikliği.
- 20 HTTP senaryosu geçti: gerçek giriş, Member görev oluşturma, Manager proje oluşturma, yetkisiz doğrudan POST, yalnızca temel proje görünümü, Admin organizasyon ekranı, dosya indirme yetkisi, alternatif URL ayırıcıları, antiforgery ve geçersiz durum.
- team-picker.js sözdizimi node --check ile doğrulandı.
- Testler uygulamanın connection string'ini kullanmaz. Rastgele DizgeAccessTests_* LocalDB veritabanı oluşturur; sonunda yalnızca bu veritabanını kaldırır. HTTP test süreci ve kendi oluşturduğu geçici dosya temizlenir.

Bu testler canlı ortam doğrulaması veya tarayıcıda etkileşimli/görsel kabul testi değildir. Özellikle çoklu ekip seçiminin mobil ve koyu tema turu görsel fazda yapılmalıdır.

## Veritabanı geçişi

Migration: 20260909090121_OrganizationAndAccess.

[İncelenebilir SQL geçiş dosyası](organization-and-access.sql), 20260710130019_RemoveHasDataSeeds seviyesindeki şemadan bu migration'a geçiş içindir. Mevcut veritabanının migration geçmişi doğrulanmadan uygulanmamalıdır. Yerel veritabanında farklı tarihli eski migration geçmişi bulundu. 115 sütun ve 67 anahtar/indeks adı mevcut temel şemayla karşılaştırıldı. Eski sekiz geçmiş kaydı korundu; iki güncel temel migration yalnızca geçmişe kaydedildi, başlangıç oluşturma ve seed silme SQL komutları yeniden çalıştırılmadı. Yeni migration aynı transaction içinde uygulandı; bu uyarlama denetim kaydına yazıldı.

Geçiş davranışı:

1. Mevcut Manager'ın açıkça kayıtlı ana departmanı sorumluluk ilişkisine aktarılır. Admin rolü de olanlar atama listelerine aktarılmaz.
2. Departmanda yalnızca bir Manager varsa varsayılan yapılır. Birden fazla varsa varsayılanı Admin /Admin/Organization ekranından seçer; rastgele kişi seçilmez.
3. Eski görevlerin ilk atayanı ve devir geçmişi tahmin edilmez. Bu kayıtlar null/boş kalır; yeni olaylar kaydedilir. İlk atayan bilgisi eksik eski kayıtlarda bu gerekçeden yetki üretilmez.
4. Eski bağımsız silme işlemleri için ilişki tahmin edilmez. Otomatik ilişkili geri yükleme, bu sürümün kaydettiği DeletionBatchId bulunan işlemler için çalışır.
5. Yeni kod başlatılmadan şema güncellenmeli; uygulama başlangıcı otomatik migration çalıştırmaz. Geçiş öncesinde veritabanı ve uploads yedeği, ardından Admin ile departman varsayılanlarının kontrolü gerekir.
6. Reverse proxy/web sunucusu uploads klasörünü uygulamadan bağımsız statik olarak sunmamalıdır; indirme uygulamanın yetki kontrolünden geçmelidir.
7. Migration Down yeni atama/geçmiş tablolarını kaldırır. Kullanım başladıktan sonra geri dönüş kararı veri kaybı dikkate alınarak yedek ve uygulama sürümüyle birlikte planlanır.

## Sonraki işler

Organizasyon/yetkilendirme kabul turu tamamlandı. Pipeline ayrı faz olarak başladı. Ticket ve e-posta geliştirmesi bu pakette yoktur.

## Yerel geçiş sonucu

2026-09-09 tarihinde localhost\SQLEXPRESS üzerindeki Tms veritabanına geçiş tamamlandı. Önce transaction geri alınarak prova yapıldı, ardından geçiş commit edildi. Her iki kontrolde 9 proje, 17 görev, 3 kullanıcı, 4 ek ve 10 yorum sayıları korundu.

- scripts/Backup-OrganizationUpgrade.ps1 veritabanı ve uploads yedeğini alır.
- scripts/Test-OrganizationBaseline.ps1 temel şemayı karşılaştırır.
- scripts/Apply-OrganizationUpgrade.ps1 varsayılan olarak geri alınan prova yapar; -Commit geçişi uygular. Bu script doğrulanan yerel eski geçmiş içindir, başka ortamlara genel geçiş aracı değildir.
- Özel yedek ve sonuç kayıtları: .local-backups/organization-20260909-163427/. Bu klasör Git dışında tutulur.
- SQL yedeği COPY_ONLY ve CHECKSUM ile tamamlandı; msdb tamamlanma/checksum/hasar bilgisi ve yerel kopyanın SHA256 değeri kontrol edildi. DBCC CHECKDB hata bildirmedi. RESTORE VERIFYONLY sunucudaki CREATE DATABASE yetkisi eksik olduğundan çalışmadı; geri yükleme provası yapılmadı.
- Geçiş sonrası uygulama başlatma denemesi erişim kısıtıyla başarısız oldu. Ek izin isteği reddedildiğinden tarayıcı kabul kontrolü yapılmadı. Admin organizasyon ekranında varsayılan Manager kontrolü ve mevcut kayıtlarla görsel kabul turu bekliyor.
## 2026-09-10 yerel ekran kontrolü

Ek başlatma izni alındı ve uygulama http://127.0.0.1:5088 adresinde başarıyla başladı. Admin girişi, kontrol paneli, organizasyon, proje oluşturma ve Kanban ile mevcut proje detayı tarayıcıda açıldı. Proje oluşturma formunda Engineering seçimi katılımcıları güncelledi; başka departman seçeneği Evet yapılınca ek departman/katılımcı alanları açıldı. Form kaydedilmedi.

Engineering için bb varsayılan Manager olarak görünüyor; diğer dört departmanda varsayılan seçilmemiş. İncelenen eski test4 projesinde departman ve sorumlu yok; tahmini atama yapılmadı. Kontrol paneli 3 aktif (silinmemiş) proje ve 6 görev gösteriyor. Tam mobil/tema kabul turu ve gerçek Manager/Member hesaplarıyla tarayıcı kontrolü henüz yapılmadı; önceki izole yetki testleri bunlardan ayrıdır.

### 2026-09-10 ikinci kontrol turu

Mevcut derlenmiş paketle 16 yetki, 27 SQL ve 20 HTTP senaryosu tekrar geçti. İlk derleme denemesi açık TMS.exe dosya kilidine takıldı; kod değiştirilmediği için --no-build ile mevcut test paketi çalıştırıldı. Gerçek Tms yerine ayrı geçici test veritabanı kullanıldı. Manager/Member giriş ve yazma yetkileri HTTP ile doğrulandı; bu hesaplarla gerçek veritabanında tarayıcı kabul testi yapılmış sayılmaz.

Görsel bulgular: Proje durum formu detay başlığından önce büyük bir kart olarak duruyor; başlık/işlem alanına uyarlanmalı. Organizasyon ekranındaki yerel çoklu seçim kutuları, özellikle koyu temada açık renk seçim ve kaydırma çubuklarıyla ortak tasarımdan ayrılıyor. Mobil önizlemede anormal ölçekleme görüldü; DOM ölçümünde clientWidth/scrollWidth 375/375, innerWidth 390 bulundu. Yatay taşma kanıtlanmadı; mobil kabul tamamlanmış sayılmamalı. Geçici viewport kaldırıldı ve açık tema geri yüklendi. Bu turda ürün kodu ve gerçek organizasyon atamaları değiştirilmedi.

### Görsel düzenleme — 2026-09-10

Proje durum formu başlığın işlem alanına taşındı; dar ekranlarda tam genişlik kullanır. Organizasyon Manager çoklu seçimi aynı managerIds alan adını gönderen, etiketli checkbox listesine çevrildi. Seçili/fokus durumları ortak tema renklerini kullanır; Manager bulunmadığında boş durum metni gösterilir. Yetki koşulları ve POST handler'ları korundu. dotnet build --no-restore: 0 hata, 0 uyarı.

### Mobil ve Kanban — 2026-09-10

390x844 tarayıcı ölçümünde Kanban ana içerik genişliği ve sayfa scrollWidth 390px; menü kapalıyken -240px öteleniyor. Menü açılışı ve perdeye tıklayarak kapanışı kontrol edildi. Proje detayında daha sonra gelen 1200px kuralı mobil tek sütunu eziyordu; mobil override ile tarih/kategori kesilmesi giderildi ve screenshot ile doğrulandı. Durum formu mobilde ayrı tam satır kullanıyor. Geçici viewport kaldırıldı.

Kanban istemci kodu wwwroot/js/kanban.js dosyasına ayrıldı. Sunucu onayında durum, tarih uyarısı, kart rengi, sütun/aktif sayaçları ve boş durumlar güncellenir. İstek sırasında tekrar taşıma kilitlenir; hata/yanıt belirsizliğinde kart eski sırasına alınır ve kullanıcıya yenileme mesajı gösterilir. Tamamlanmış/iptal edilmiş projelerde Yaklaşıyor etiketi kaldırıldı; ilk sayfa görünümünde doğrulandı. Derleme 0 hata/uyarı; node --check geçti. Başarılı/başarısız sürükleme uçtan uca senaryoları henüz çalıştırılmadı; tam mobil form turu da bekliyor. Gerçek proje durumları değiştirilmedi.

### Görsel demo verileri ve Kanban kabul — 2026-09-10

Önce .local-backups/organization-20260910-095936 yedeği alındı (önceki RESTORE VERIFYONLY yetki sınırlaması devam ediyor). scripts/Add-VisualDemoData.ps1 yerel hedef/fresh-backup/tekrar-kayıt kontrolleri ve transaction ile çalıştırıldı. İlk denemedeki tablo adı hatası transaction rollback ile geri alındı; TaskItems adı düzeltildikten sonra başarıyla tamamlandı.

[TEST] Görsel Demo önekli projeler 11–15: Müşteri Portalı, Mobil Saha Uygulaması, Raporlama Merkezi, Tedarikçi Entegrasyonu, Eski Sistem Geçişi. Beş durum, 20 görev, 25 yorum, 10 gerçek metin eki ve 20 ilk görev atama olayı eklendi. Mevcut bb Manager ve a Member ekipleri kullanıldı; roller/ana departmanlar değiştirilmedi. Projeler Engineering departmanında; çok departmanlı örnek kullanıcılar oluşturulmadı.

Gerçek tarayıcıda proje 12 InProgress → Completed taşındı: aktif sayı 3→2, sütun sayıları 3→2 ve 2→3, gecikme etiketi/kırmızı tarih kaldırılması ve sayfa yenilenmeden başarı mesajı doğrulandı. İkinci sekmeden çıkış yapılarak stale Kanban ekranında taşıma denendi: istek reddedildi, kart önceki sütun/sırasına alındı ve hata mesajı gösterildi. Yeniden giriş/yükleme ile durumun değişmediği doğrulandı. Proje 12 InProgress durumuna başarıyla geri taşındı. Ek test sekmesi kapatıldı, Admin oturumu geri açıldı. Örnek kayıtlar kullanıcı incelemesi için bırakıldı.

### Örnek kullanıcılar ve yerel erişim kontrolü — 2026-09-10

.local-backups/organization-20260910-100851 yedeğinden sonra scripts/Add-DemoUsers.ps1 ile altı [TEST] kullanıcı oluşturuldu: Deniz (Marketing Manager), Selin (Finance Manager), Ece/Mert (Engineering Member), İpek (Marketing Member), Can (Finance Member). Mevcut varsayılan Manager atamaları korundu. Deniz, Ece, Mert ve İpek örnek proje 11 ekibine eklendi; Finance hesapları dışarıda bırakıldı. Kimlik bilgileri yalnızca Git dışında .local-backups/demo-users.txt dosyasında tutulur.

scripts/Test-DemoUserAccess.ps1 ile gerçek yerel uygulamaya dört ayrı oturum açılarak doğrulandı: Ece projeyi görür, proje düzenleme/silme/durum kontrolü yok; Deniz dış departman katılımcısı olarak görür ve durum kontrolü vardır, proje düzenleme/silme yok; Selin ve Can proje detayını göremez. Member atama seçeneklerinde Manager/Admin ve dış ekip kullanıcıları bulunmaz. İlk kontrol HTML entity çözümlemediği için yanlış negatif verdi; HtmlDecode eklenerek dört hesap kontrolü geçti. Bu kontroller HTTP seviyesindedir; tarayıcı etkileşim turu zaman aşımı nedeniyle tamamlanmadı.

Yerel süreç yeniden başlatıldı. Kontrol sırasında bir EF sorgusu yaklaşık 25 saniye sürdü; gecikmenin nedeni henüz ayrıştırılmadı, performans incelemesi takip maddesidir.

### Profil ve proje detay performansı — 2026-09-10

Profile handler'ları ilgisiz formun ModelState alanlarını hem önekli hem öneksiz adlarıyla kaldırır. İlk yalnızca önekli düzeltme HTTP testinde başarısız oldu; tamamen gönderilmeyen modelin öneksiz bağlanması da kapsandı. İzole hesapta ad güncelleme, boş ad reddi ve profil alanları gönderilmeden şifre değiştirme testleri eklendi. Gerçek kullanıcı şifreleri değiştirilmedi. 16 yetki + 27 SQL + 23 HTTP = 66 test geçti.

Ayrıntılı SQL terminal günlükleri azaltıldığı halde proje 11 yanıt süresi Ece'de 25633ms, Deniz'de 25171ms olarak tekrarlandı. GetProjectByIdAsync içindeki görev ve ekip koleksiyonlarının tek sorguda yüklenmesi AsSplitQuery ile ayrıldı. Aynı yerel verilerle yeniden ölçüm: Ece 717ms, Deniz 123ms; erişimi olmayan Selin 97ms ve Can 33ms. Dört gerçek demo hesabının giriş/görünürlük/izin kontrolü tekrar geçti. Bu sonuçlar tek yerel ölçüm turudur, yük testi değildir. Bölünmüş sorgular eşzamanlı değişikliklerde koleksiyonlar arasında küçük zaman farkları gösterebilir; yetki ve yazma kontrolleri korunur. Uygulama 127.0.0.1:5088 üzerinde yeniden başlatıldı.

### Yönetim ekranı metinleri ve doğrulama — 2026-09-10

Geri dönüşüm kutusunda proje/görev/yorum silen kullanıcıların kimlikleri yerine adları, tek toplu kullanıcı sorgusuyla gösterilir; bulunamayan hesap için Silinmiş kullanıcı yazılır. AuditLogs ChangePassword ve UserProfile metin/ikon eşlemeleri eklendi. Kullanıcı oluşturma giriş modeli Required, ad uzunluğu ve e-posta doğrulamasıyla güçlendirildi; handler ModelState hatalarını kayıttan önce gösterir.

16 yetki + 27 SQL + 25 HTTP = 68 test geçti. Yeni HTTP kontrolleri geçersiz ad/e-posta reddini ve geri dönüşüm sayfasının açılmasını kapsar. Bunlar tüm yönetim ekranlarının görsel kabulü veya tüm Identity hata mesajlarının Türkçeleştirildiği anlamına gelmez. Yerel uygulama yeniden açıldı; Login 200 kontrol edildi. Gerçek kullanıcı/rol kayıtları değiştirilmedi.

### Kullanıcı oluşturma bütünlüğü — 2026-09-11

Users.CreateUser kullanıcı ve rol kaydını tek transaction içinde yapar; rol ataması başarısızsa kullanıcı kaydı geri alınır. ChangeRole security stamp güncellemesinin sonucunu kontrol eder, başarısızsa transaction tamamlanmaz. Gerçek kullanıcılarda rol değişikliği yapılmadı. Geçici veritabanında başarılı kullanıcı oluşturma ve istenen rolün kalıcı kaydı kontrol edildi; 16 yetki + 27 SQL + 27 HTTP = 70 test geçti. Rol atama başarısızlığı ayrıca hata enjeksiyonuyla sınanmadı. Yerel uygulama tekrar açıldı ve giriş sayfası 200 döndü.

### Kabul turunun kapanışı ve pipeline başlangıcı — 2026-09-11

Manager ve Member proje detayları gerçek tarayıcıda yeniden kontrol edildi. Member proje bilgilerini ve katıldığı projenin görevlerini görür; görev atama listesi proje ekibindeki Member kullanıcılarla sınırlıdır. Katılımcı Manager proje durumunu değiştirebilir ancak projeyi düzenleyip silemez. Kanban Manager/Member için açıkça salt okunur metin kullanır ve sürükleme kodu bu rollerde yüklenmez. Oturumu açık kullanıcı `/Account/Login` adresinden kontrol paneline yönlendirilir.

`ProjectPipelineFoundation` migration'ı eklendi ve yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260911-152935` yedeği alındı. Genel pipeline, proje pipeline'ı, aşama/checkpoint oluşturma, otomatik aşama bekleyen görevler, görev yerleştirme, eşit ağırlıklı ilerleme, isteğe bağlı onay ve görev yeniden açılınca onayı kaldırma çalışıyor. `[TEST] Görsel Demo 1` projesine üç aşama, üç checkpoint ve dört görev yerleşimi tarayıcı üzerinden eklendi. Member salt okunur görünümü koyu temada kontrol edildi.

Doğrulama sonucu: 16 bağımsız yetki, 32 SQL ve 32 HTTP olmak üzere 80 senaryo geçti; derleme 0 hata ve 0 uyarı verdi. İncelenebilir migration SQL'i `docs/project-pipeline-foundation.sql` dosyasındadır.

### Pipeline zaman çizelgesi — 2026-09-14

Proje pipeline ekranına akış görünümünden ayrı bir zaman çizelgesi eklendi. Görevler planlanan başlangıç ve bitiş tarihleriyle gün, hafta veya ay ölçeğinde gösterilir; aşama grupları, checkpoint işaretleri, bugün çizgisi ve görev durumundan türeyen ilerleme çubukları aynı tabloda izlenir. Manager ve Admin tarihleri düzenleyebilir; Member görünümü salt okunurdur. Görev ilk kez başlatıldığında gerçek başlangıç, tamamlandığında tamamlanma tarihi kaydedilir. Tamamlanan görev yeniden açılırsa tamamlanma tarihi ve checkpoint onayı temizlenir, gerçek başlangıç korunur.

`TimelineScheduling` migration'ı yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260911-160509` yedeği alındı. İncelenebilir geçiş SQL'i `docs/timeline-scheduling.sql`, örnek tarihleri tekrar uygulanabilir biçimde ekleyen script `scripts/Add-TimelineDemoData.ps1` dosyasındadır. Proje 11'de dört örnek görev farklı tarihler ve durumlarla zaman çizelgesine yerleştirildi.

Ece Member hesabıyla salt okunur görünüm, Deniz Manager hesabıyla tarih düzenleme alanı tarayıcıda kontrol edildi. Gün/hafta/ay ölçekleri, görev çubukları, checkpoint'ler ve durum ilerlemeleri görüldü. Doğrulama sonucu 16 bağımsız yetki, 35 SQL ve 35 HTTP olmak üzere 86 senaryo geçti; derleme 0 hata ve 0 uyarı verdi.

### Proje ilişkileri ve bağımlılıklar — 2026-09-14

Pipeline ekranına proje bağlantıları bölümü eklendi. Manager/Admin, erişebildiği başka bir projeyi “İlişkili” veya “Bu proje seçilen projeye bağlı” türünde bağlayabilir ve bağlantıyı kaldırabilir. Tamamlanmamış ön koşullar bekliyor olarak gösterilir. Member bağlantıları salt okunur görür. İlişki başka bir projeye erişim kazandırmaz; erişilemeyen proje seçimi ve döngü oluşturan bağımlılık sunucuda reddedilir.

`ProjectRelations` migration'ı yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260914-160120` yedeği alındı; checksum metadata kontrolü geçti, SQL hesabının CREATE DATABASE yetkisi olmadığı için RESTORE VERIFYONLY yine kullanılamadı. İncelenebilir geçiş SQL'i `docs/project-relations.sql` dosyasındadır. `scripts/Add-ProjectRelationDemoData.ps1`, proje 11'e bir ilişkili proje ve bir tamamlanma bağımlılığı ekledi; Deniz test Manager'ına bağlantı hedeflerinde ekip erişimi verdi.

Doğrulama sonucu 16 bağımsız yetki, 41 SQL ve 40 HTTP olmak üzere 97 senaryo geçti. Derleme 0 hata ve 0 uyarı verdi. Deniz Manager görünümünde iki bağlantı, tamamlanmamış bağımlılığın “Bekliyor” durumu, kaldırma düğmeleri ve yeni bağlantı alanı açık temada görsel olarak kontrol edildi.

### Aşama ve checkpoint yönetimi — 2026-09-14

Manager/Admin için pipeline aşaması ve checkpoint düzenleme, silme ve yukarı/aşağı sıralama işlemleri eklendi. Checkpoint açıklaması oluşturma ve düzenleme formlarına dahil edildi. Aşama silme bağlı görevlerin hem aşama hem checkpoint konumunu temizleyip görevleri “Aşama bekleyenler” alanına taşır. Checkpoint silme yalnızca checkpoint konumunu temizler ve görevleri aynı aşamada tutar. Silme sonrasında sıra numaraları boşluk bırakmadan yeniden düzenlenir; sıralama değişimleri transaction içinde yapılır. Member tarafında yazma kontrolleri gösterilmez ve doğrudan istekler reddedilir.

Yeni pipeline işlem türleri Admin denetim kayıtlarında Türkçe metin ve uygun simgelerle gösterilir. Şema değişikliği olmadığı için yeni migration gerekmedi. Doğrulama sonucu 16 bağımsız yetki, 49 SQL ve 45 HTTP olmak üzere 110 senaryo geçti; derleme 0 hata ve 0 uyarı verdi. Manager görünümünde sıra düğmeleri, aşama/checkpoint düzenleme panelleri ve dar ekran yerleşimi tarayıcıda kontrol edildi.

### Checkpoint'e bağlı proje bağımlılıkları — 2026-09-15

Yönlü proje bağımlılığı artık genel proje seviyesinde bırakılabilir veya kaynak projenin belirli bir checkpoint'ine bağlanabilir. Ön koşul proje tamamlanmadığında bağlı checkpoint bekleme uyarısı gösterir, tamamlanmış sayılmaz ve yönetici onayı alamaz. Ön koşul tamamlandığında görev ve varsa onay kuralları normal biçimde değerlendirilir. Manager/Admin bağlantıyı oluştururken checkpoint seçebilir ve mevcut bağımlılığın checkpoint'ini değiştirebilir; Member salt okunur kalır. Checkpoint veya aşama silinirse bağımlılık kaybolmaz, genel proje bağımlılığına döner.

`CheckpointProjectDependencies` migration'ı yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260915-143716` yedeği alındı; checksum metadata kontrolü geçti, SQL hesabının CREATE DATABASE yetkisi olmadığı için RESTORE VERIFYONLY kullanılamadı. İncelenebilir geçiş SQL'i `docs/checkpoint-project-dependencies.sql` dosyasındadır. Proje 11'in Tedarikçi Entegrasyonu bağımlılığı “Geliştirme tamamlandı” checkpoint'ine bağlandı.

Doğrulama sonucu 16 bağımsız yetki, 52 SQL ve 48 HTTP olmak üzere 116 senaryo geçti; derleme 0 hata ve 0 uyarı verdi. Manager görünümünde bağımlılık kartındaki checkpoint seçimi ve Geliştirme checkpoint'indeki bekleme uyarısı açık temada kontrol edildi.

### Proje bazlı pipeline geçmişi — 2026-09-15

Proje pipeline ekranına “Geçmiş” sekmesi eklendi. Aşama ve checkpoint oluşturma, düzenleme, sıralama, silme ve onaylama; görevlerin pipeline konumu, takvimi ve durum değişimleri; proje ilişkisi ve bağımlılık hareketleri kullanıcı ve zaman bilgisiyle en yeniden eskiye gösterilir. Kayıtlar tümü, aşama/checkpoint, görev ve bağlantı gruplarıyla filtrelenebilir. Projeyi görebilen Member geçmişi salt okunur izleyebilir; yalnızca harici görev ataması proje geçmişine erişim kazandırmaz.

`PipelineHistoryProjectScope` migration'ı işlem kayıtlarına kalıcı proje bağı ekledi. Mevcut aşama, checkpoint, görev ve proje ilişkisi kayıtlarının eşleştirilebilen geçmişi yeni alana aktarıldı; gelecekte kaynak kayıt silinse bile proje kapsamı korunur. Migration yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260915-145507` yedeği alındı; checksum metadata kontrolü geçti, SQL hesabının CREATE DATABASE yetkisi olmadığı için RESTORE VERIFYONLY kullanılamadı. İncelenebilir geçiş SQL'i `docs/pipeline-history-project-scope.sql` dosyasındadır.

Doğrulama sonucu 16 bağımsız yetki, 56 SQL ve 53 HTTP olmak üzere 125 senaryo geçti; derleme 0 hata ve 0 uyarı verdi. Yerel veritabanında migration kaydı, 40 proje kapsamlı işlem ve proje 11 için 10 geçmiş kaydı doğrulandı.

### Görev ön koşulları — 2026-09-15

Aynı proje içindeki görevler yönlü ön koşullarla bağlanabilir. Manager/Admin bağlantı ekleyip kaldırabilir; Member bağlantıları salt okunur görür. Kendi kendine bağlantı, tekrar kayıt ve döngü reddedilir. Tamamlanmamış bir ön koşulu bulunan görev, doğrudan durum değişiminden veya yorum formundan Tamamlandı yapılamaz. Bekleyen görevler pipeline akışında uyarılı kartla, zaman çizelgesinde çerçeveli çubukla ve görev detayında önce/sonra ilişkileriyle gösterilir. Yalnızca harici görev erişimi diğer görev adlarını açmaz.

`TaskDependencies` migration'ı yerel Tms veritabanına uygulandı. Öncesinde `.local-backups/organization-20260915-161144` yedeği alındı; checksum metadata kontrolü geçti, SQL hesabının CREATE DATABASE yetkisi olmadığı için RESTORE VERIFYONLY kullanılamadı. İncelenebilir geçiş SQL'i `docs/task-dependencies.sql`, tekrar çalıştırılabilen örnek veri scripti `scripts/Add-TaskDependencyDemoData.ps1` dosyasındadır. Proje 11'e analiz → geliştirme → kalite kontrol → teslim zinciri eklendi ve üç oluşturma olayı geçmişe kaydedildi.

Doğrulama sonucu 16 bağımsız yetki, 65 SQL ve 59 HTTP olmak üzere 140 senaryo geçti; derleme 0 hata ve 0 uyarı verdi. Deniz Manager görünümünde ön koşul ekleme/kaldırma alanı, iki bekleyen görev kartı, zaman çizelgesindeki bekleme işaretleri, görev detayındaki önce/sonra özeti ve görev geçmişi filtresi tarayıcıda kontrol edildi.

### Zaman çizelgesinde sürükleyerek tarih değiştirme — 2026-09-16

Manager/Admin, planlı görev çubuğunu gün, hafta veya ay görünümünde tam gün adımlarıyla sürükleyebilir. Başlangıç ve bitiş aynı miktarda kaydırılır; görev süresi korunur. Odaklanabilen çubuklarda ok tuşları bir gün, Shift+ok yedi gün kaydırır. Mevcut tarih formu korunur ve tüm yollar aynı sunucu yetkisi, tarih doğrulaması ve `TaskScheduleChanged` geçmişini kullanır. Member için sürükleme işaretleri ve istemci kodu üretilmez. Bu değişiklik şema gerektirmez.

Derleme 0 hata/uyarı, JavaScript sözdizimi kontrolü ve izole veritabanında 16 yetki + 65 SQL + 61 HTTP = 142 senaryo geçti. Deniz Manager ile gerçek tarayıcıda geliştirme görevi bir gün klavyeyle, iki gün sürüklemeyle kaydırıldı; iki tarihin de birlikte kaydedildiği görüldü. Örnek görev 03.09.2026–17.09.2026 tarih aralığına geri alındı.

### Sürüm doğrulaması başlangıcı — 2026-09-16

İlk yönetici hesabının kodda duran varsayılan parolası kaldırıldı. Yeni kurulumda yönetici bulunmuyorsa `TMS_BOOTSTRAP_ADMIN_PASSWORD` ortam değişkeni gereklidir; kullanıcı oluşturma ve rol ataması tek transaction içindedir. Yinelenen pipeline servis kaydı kaldırıldı. Release derlemesi 0 hata/uyarı ile tamamlandı. İzole veritabanında 16 yetki + 65 SQL + 61 HTTP = 142 senaryo yeniden geçti. Çalışan yerel uygulama yeniden başlatılmadı.

Son yedeğin SHA-256 özeti manifest ile eşleşti; 14 ek dosyasının toplam boyutu canlı dosya diziniyle eşleşti. SQL Server 17 yedeği kurulu LocalDB 15'e geri yüklenemiyor. SQLExpress hesabı yeni veritabanı oluşturamadığı için `RESTORE VERIFYONLY` ve tam geri yükleme testi açık kaldı. Yerel bağlantı bilgisi Git dışında tutulan ve yayımlanmayan `appsettings.Local.json` dosyasına taşındı; hedef ortamda `ConnectionStrings__DefaultConnection` değişkeni kullanılacak. Daha önce izlenen bağlantı parolası döndürülmeli. Adımlar `docs/release-checklist.md` içindedir; faz 5 henüz tamamlanmış sayılmaz.

Temiz Release yayın paketi kontrolünde yerel yapılandırma dosyası bulunmadı, paketlenen temel bağlantı boştu ve `wwwroot/uploads` içinden dosya kopyalanmadı. Yapılandırma değişikliğinden sonra 142 senaryo yeniden geçti.
