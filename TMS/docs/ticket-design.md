# Ticket çekirdeği — tasarım taslağı

> **8 Ekim 2026 kapsam güncellemesi:** Manuel Ticket çekirdeği kullanılmaya devam ediyor. Postadan Ticket alma, eşleşmeyen gönderen ekranı ve dış e-posta gönderimi kapatıldı; aşağıdaki e-posta maddeleri alınmış eski tasarım kararlarını ve ileride yeniden kullanılabilecek altyapıyı belgeliyor.

Son güncelleme: 2026-09-17. Bu belge görüşmede netleşen davranışları, ilk uygulama dilimini ve sonraki işleri ayırır.

2026-09-18 güncellemesi: Gelen adresin bir dağıtım grubu olduğu netleşti. Bu adres uygulamaya bağlı değildir ve açık kaynak ayarlarında kurum adresi tutulmaz. Sistem Geliştirme departmanı artık adından tahmin edilmez; tekil Ticket destek departmanı olarak işaretlenir. Ticket çalışma alanının tüm kayıtlar, görüntüleyebildiklerim, izlediklerim, çözüldü/kapatıldı, arama ve sayfalama görünümleri uygulandı. Kullanıcı hesapları silinmek yerine pasifleştirilir; tarihî Ticket ve görev bilgileri korunur.

## Uygulama durumu

Ticket, görüntüleyici, mesaj ve olay kayıtlarının şeması; merkezi Ticket erişim servisi; talep sahibi listesi, ekip havuzu ve Ticket detayındaki atama/durum/mesaj/iç not/görüntüleyici/takip işlemleri eklendi. `TicketCoreFoundation` migration'ı önce izole test veritabanında, ardından `.local-backups/organization-20260917-114730` yedeği alındıktan sonra yerel `Tms` veritabanında uygulandı. Dört yeni tablo ve migration kaydı doğrulandı; Ticket verisi henüz yok. Kullanıcı onayıyla yerel uygulama `127.0.0.1:5088` adresinde yeniden başlatıldı; giriş sayfası 200, Ticket ekranı oturumsuz istekte beklenen 302 yanıtını verdi. Release derlemesi 0 hata/uyarı ile tamamlandı; 16 yetki + 81 SQL + 69 HTTP = 166 senaryo geçti.

Mevcut projeye özel erişimli görev oluşturma, Ticket ve görev arasında çift yönlü bağlantı, görev tamamlanınca Ticket'ı çözme ve otomatik çözülmüş görevin yeniden açılması halinde Ticket'ı İşlemde'ye alma eklendi. Proje ayrıntısı, Kanban, dashboard ve pipeline görünümleri görev erişimine göre süzülüyor. Ticket düzeyinde dosya yükleme/indirme; ekip içi dosyaları yalnızca destek ekibine gösterme; eşleşmeyen gönderenleri elle kaydetme, inceleme ve kullanıcı hesabı açılınca Ticket'a dönüştürme eklendi. Gerçek posta bağlantısı ve e-posta eklerini yazışma mesajlarına bağlama, kullanıcının sunumundan sonraya bırakıldı.

2026-09-17'de posta altyapısının Microsoft 365 / Exchange olduğu ve mevcut adresin dağıtım grubu olduğu netleşti. Dağıtım grubu üyelerine posta dağıtır; uygulamanın doğrudan okuyacağı ortak gelen kutusu değildir. Mevcut grup adresi korunarak ayrı bir paylaşılan posta kutusunun gruba üye eklenmesi önerisi BT ile kontrol edilecek. Bu karar netleşmeden gerçek posta alma bağlantısı kurulmayacak. Kaynak: [Exchange alıcı türleri](https://learn.microsoft.com/en-us/exchange/recipients-in-exchange-online/recipients-in-exchange-online), [dağıtım gruplarının yönetimi](https://learn.microsoft.com/en-us/exchange/recipients-in-exchange-online/manage-distribution-groups/manage-distribution-groups).

Yerel kurulumda Sistem Geliştirme departmanı (ID 6) mevcut, ancak 2026-09-17 itibarıyla bu departmana bağlı kullanıcı yok. Ekip havuzu ve atama için ilgili kullanıcılar bu departmana bağlanmalı; Admin genel erişimi ayrıca sürer.

İncelenebilir yerel SQL değişiklikleri `docs/ticket-core-foundation.sql`, `docs/ticket-task-resolution.sql` ve `docs/ticket-files-unmatched.sql` dosyalarındadır. `TicketFilesAndUnmatchedIntake` migration'ı, `.local-backups/organization-20260917-153418` yedeği alındıktan sonra yerel `Tms` veritabanına uygulandı; alanlar, tablo ve migration kaydı doğrulandı. Henüz inceleme kaydı yok. İzole test veritabanında 16 yetki + 97 SQL + 80 HTTP senaryosu geçti. Yeni Release derlemesi kullanıcı onayıyla `127.0.0.1:5088` adresinde yeniden başlatıldı; giriş sayfası 200, Ticket ve eşleşmeyen gönderen ekranları oturumsuz istekte beklenen 302 yönlendirmesini verdi.

## Netleşen davranış

- Kaynak, Sistem Geliştirme grubunun tek e-posta adresidir. Bu adres dışına gönderilen postalar otomatik yakalanmaz.
- Kullanıcılar kişi ve e-posta bilgileriyle önceden açılır. Gelen posta gönderen adresine göre eşleştirilir; eşleşmeyen gönderiler inceleme havuzunda bekler. Kişi kaydı açılıp eşleştirilince işleme alınır.
- Her yeni talep Ticket olur. Aynı konudaki gelen ve gönderilen yanıtlar yeni Ticket açmaz; Ticket'ın yazışma akışına eklenir.
- Ticket'ın talep sahibi ile talep sahibinin departmanı kaydedilir. Talep sahibinin departmanındaki herkes otomatik erişim kazanmaz.
- Sistem Geliştirme grubu uygulamadaki Sistem Geliştirme departmanının kullanıcılarıdır. Grubun tamamı Ticket atama ve proje görevine dönüştürme yetkisine sahip olacaktır. Bu kullanıcıların Admin rolüyle çalışması muhtemeldir; kesin rol kararı verilmedi.
- Talep sahibi ve Sistem Geliştirme grubu ek görüntüleyiciler ekleyip çıkarabilir. Grup üyeleri ilgilenmedikleri Ticket'ları kendi çalışma/takip listesinden çıkarabilir; bu işlem Admin'in genel erişimini kaldırmaz. Atanmamış havuzdaki kayıt kaybolmaz.
- Eşleşen yeni Ticket, Sistem Geliştirme'nin atanmamış açık Ticket havuzuna girer. Kişiye atanınca bu havuzdan çıkar; Ticket açık kalır.
- Her Ticket'ın en fazla bir birincil sorumlusu vardır. Diğer ekip üyeleri ilgili kişi olarak takip edebilir. Sorumlu kaldırılırsa kapanmamış Ticket yeniden atanmamış havuza döner.
- Ek görüntüleyicileri talep sahibi veya Sistem Geliştirme mevcut kullanıcılar arasından ekleyip çıkarabilir. Talep sahibinin ve atanmış sorumlunun temel erişimi kaldırılamaz; Sistem Geliştirme kullanıcılarının Admin erişimi devam eder.
- Talep hemen çözülebilir veya geliştirme işine alınabilir. Projeye aktarma mevcut projede yeni bir görev oluşturur. Görevde kaynak Ticket, gönderen ve gönderim zamanı gibi bilgiler görünür; Ticket ile görev bağlantısı korunur.
- Ticket durumları Açık, İşlemde, Bilgi Bekleniyor, Çözüldü ve Kapatıldı olarak tasarlanır. Atanmamış olmak ayrı durum değil havuz filtresidir. Durumu Sistem Geliştirme yönetir; talep sahibi yazışmaya katılabilir ve ek görüntüleyicileri yönetebilir.
- Projede görev oluşturulduğunda Ticket üzerinde otomatik “Geliştirmeye alındı” işareti ve görev bağlantısı görünür. Bu işaret bir durum değildir: Ticket aynı anda Bilgi Bekleniyor olabilir. Görev oluşturma Açık Ticket'ı İşlemde'ye taşıyabilir; diğer durumlarda gereksiz durum değişimi yapılmamalı.
- E-posta yanıtı tek başına Ticket durumunu Çözüldü/Kapatıldı yapmaz; bilgi istemek için de yanıt verilebilir ve sonradan yeniden yanıt gelebilir. Ticket kapatma elle yapılır. Bağlı görev tamamlandığında Ticket Çözüldü durumuna otomatik geçebilir; ardından Sistem Geliştirme tarafından elle Kapatıldı yapılır.
- Çözüldü veya Kapatıldı Ticket'a yeni yanıt gelirse durumu otomatik değişmez. “Yeni yanıt var” işaretiyle inceleme listesine girer; Sistem Geliştirme gerekirse elle yeniden açar. Bu, teşekkür gibi yanıtların gereksiz açık iş oluşturmasını önler.
- İnceleme alanı “Gönderen eşleşmedi” ve “Mevcut Ticket'a yeni yanıt” nedenlerini ayrı gösterir. Yeni yanıt uyarısı yalnızca Sistem Geliştirme'den biri “İncelendi” dediğinde temizlenir; talep sahibinin kaydı açması ekip uyarısını kaldırmaz.
- Ticket'tan doğan görev özel erişimlidir: Sistem Geliştirme, talep sahibi ve Ticket'a eklenen kişiler görebilir; talep sahibi ve ek görüntüleyiciler görevde salt okunurdur, ek bilgi vermek için e-postayı/Ticket yazışmasını kullanır. Admin'in genel yetkisi korunur. Proje listeleri, Kanban, pipeline, arama ve dosya erişimi bu istisnayı tutarlı uygulamalıdır. Sınırlı görev erişimi tüm projenin içeriğini açmaz.
- Ticket'a yeni yanıt gelince bağlantılı görevde “Ticket'ta yeni yanıt” işareti görünür. Proje ekranında yalnızca özel görevi görmeye yetkili kişilere yansır. Bu işaret genel bildirim altyapısı gerektirmez; yanıt görev durumunu veya proje ilerlemesini otomatik değiştirmez. Gerekirse ekip Ticket'ı ve görevi elle yeniden açar.
- Göreve aktarılırken gönderen, gönderim zamanı ve Ticket içeriği görevde de görünebilir. Kopyalanacak ayrıntılar ve sonraki yanıtların görevde nasıl sunulacağı ayrıca netleştirilecek.
- E-posta alma/gönderme ve aynı yazışmayı güvenilir biçimde eşleştirme Ticket çekirdeğinden sonraki entegrasyon dilimidir. Çekirdek, bunların bağlanabileceği kayıt ve yetki yapısını kurar.

## Açık kararlar

1. Görev oluşturma Açık Ticket'ı İşlemde'ye geçirir. Diğer Ticket durumları korunur; “Geliştirmeye alındı” işareti görev bağlantısından gelir.
2. Silinen görev Ticket'a bağlı kalır; Ticket otomatik çözülmüşse İşlemde'ye döner, görev bağlantısı geçmişte görünür. Yeni görev açmak yerine önce görev geri yüklenir. Bir Ticket'ın aynı anda yalnızca bir bağlı görevi olur. Görevi başka projeye taşıma bu dilimde desteklenmez.
3. Yeni Ticket ile aynı konuya gelen yanıtı nasıl ayıracağız? E-posta `Message-ID`/`In-Reply-To`/`References` ve gerekiyorsa Ticket numarası kullanımı entegrasyon aşamasında tasarlanacak.

## Ekran akışı — ilk dilim

1. Talep sahibinin **Ticket'larım** ekranı: kendi talepleri, durum, son yanıt, varsa “Geliştirmeye alındı” ve “Yeni yanıt” bilgisi. Departmanındaki başkalarının Ticket'ları görünmez. Ek görüntüleyici olduğu Ticket'lar ayrı filtrede bulunur.
2. Sistem Geliştirme'nin **Ticket çalışma alanı**: Atanmamış, Bana atananlar, İzlediklerim, İnceleme, Çözüldü ve Kapatıldı filtreleri. İnceleme içinde “Gönderen eşleşmedi” ve “Yeni yanıt” nedenleri ayrılır. Bir kullanıcı kendi izleme listesinden çıkabilir; bu işlem havuzdaki Ticket'ı gizlemez.
3. **Ticket detayı**: gönderen/kullanıcı, departman, ilk ve son e-posta zamanı, durum, birincil sorumlu, görüntüleyiciler, yazışma/ekler, iç notlar, işlem geçmişi ve varsa proje görevi bağlantısı. İç not talep sahibine veya e-postaya gösterilmez.
4. **Projeye aktar** işlemi: mevcut proje seçilir, yeni görev başlığı ve içeriği düzenlenir; kaynak Ticket ve gönderim bilgileri görünür. Bağlantılı görev özel erişimlidir. Görev kartı ve detayında Ticket'a dönüş bağlantısı bulunur; yeni yanıt işareti gerektiğinde gösterilir.

## İlk geliştirme dilimi için kabul ölçütleri

- Yetkili kullanıcı yalnızca erişebildiği Ticket'ları listeler, açar, dosyalarını indirir ve değiştirebilir; arama, sayım ve doğrudan bağlantı aynı kuralı uygular.
- Tanınan ve tanınmayan gönderenler ayrı işlenir; kullanıcı eşleştirilmeden Ticket normal iş akışına girmez. Tekrarlanan işleme aynı e-postadan ikinci Ticket oluşturmaz.
- Atanmamış havuz, birincil atama ve izleme listesi birbirinden bağımsız çalışır; atama kaldırılınca açık Ticket havuza geri döner.
- Yeni yanıt, mevcut Ticket'ın yazışmasına eklenir; Çözüldü/Kapatıldı durumunu otomatik değiştirmez. İnceleme işareti ekip tarafından temizlenir ve bağlı görevde yetkili kişilere görünür.
- Görev bağlantısı oluşturulduğunda özel erişim proje, Kanban, pipeline, arama ve eklerde tutarlı uygulanır. Görev Tamamlandı olunca Ticket Çözüldü olur; Kapatıldı durumu elle verilir.

## İlk uygulama sırası önerisi

1. Ticket kaydı, talep sahibi eşleştirme, atanmamış ve eşleşmeyen gönderi havuzları, merkezi görüntüleme/işlem yetkileri.
2. Atama, durum ve geçmiş; yazışma, iç not ve dosya kayıtları.
3. Mevcut projede görev oluşturma ve çift yönlü bağlantı; görev tamamlanma olayının Ticket'a yansıması.
4. Grup posta kutusuyla gelen/giden e-posta entegrasyonu, yanıt eşleştirme ve tekrar işleme koruması.
