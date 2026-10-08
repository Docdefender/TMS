# Dizge ürün kuralları

2026-09-14 — Görüşmede onaylanan kurallar. Organizasyon, proje/görev yetkileri, devir geçmişi, yorum/dosya, ilişkili geri yükleme ve pipeline'ın ilk dilimleri yerel veritabanında çalışıyor. Test ve geçiş kapsamı organization-release.md belgesindedir. Sonraki açık kararlar önceki önerilerin yerine geçer.

## Organizasyon

- Admin tüm kayıt ve işlemlerde tam yetkilidir. Kullanıcıların atama seçimlerinde Admin sunulmaz; proje sorumlusu yalnızca Manager olur.
- Bir Manager birden çok departmandan sorumlu olabilir. Bir departmanda birden çok Manager ve bir varsayılan Manager bulunabilir.
- Departman, kullanıcı departmanı, Manager sorumlulukları ve varsayılan Manager yönetimi yalnızca Admin'dedir.
- Departmansız Manager yalnızca açıkça erişim verilmiş kayıtlara erişir; proje oluşturamaz.
- Görevlerin departman kapsamı bağlı projenin ana departmanından gelir; kişiye atama bu departmanı değiştirmez.

## Proje ve ekip

- Manager proje oluştururken sorumlu departmanları arasından ana departmanı seçer. Member proje oluşturamaz.
- Başka departmandan katılımcı sorusu Evet ise departman ve o departmanın çoklu kişi seçimi açılır. “Başka departman ekle” ile gruplar çoğaltılır/kaldırılır; kişiler tekrarlanmaz.
- Departman seçimi o departmanın tamamına erişim vermez; yalnızca seçilen katılımcılar erişir.
- Düzenleme yetkili Manager ekip ve katılımcı departmanlarını sonradan değiştirebilir. Departman dışından katılan Manager proje ekibini değiştiremez.
- Başka departmandan Manager proje sorumlusu seçilebilir; ana departman değişmez.
- Ana departmanı yalnızca Admin değiştirir. Hedefin varsayılan Manager'ı otomatik sorumlu olur; geçerli varsayılan Manager olmadan değişiklik tamamlanmaz.
- Departman değişince ekip ve görev atamaları korunur; departmana bağlı erişim yeniden hesaplanır. Önceki departman tek başına erişim sağlamaz.
- Ekipten çıkarılan kişinin görev atamaları korunur; üyelik erişimi kalkar. Diğer geçerli yetki gerekçeleri korunur.

## Yetki matrisi

Admin her satırın istisnasıdır. “Departman içi”, Manager'ın sorumlu olduğu departmanlardan biridir. Proje durumu ve Kanban ayrı yetkilerdir; genel düzenleme bunları otomatik vermez.

| Durum | Görüntüleme | Oluşturma / düzenleme / atama | Silme |
|---|---|---|---|
| Manager, departman içi | Tüm proje ve görevler | Proje/görev yönetimi; tüm departmanlardaki kullanıcılara atama (Admin seçim dışı) | Proje ve görevler |
| Manager, departman dışı proje sorumlusu/üyesi | Proje ve görevleri | Proje bilgilerini düzenleyemez; görev oluşturup atayabilir | Projeyi silemez; kendi oluşturduğu görevleri silebilir |
| Manager, yalnızca departman dışı görev atanmış | Görev ve temel proje bilgileri | Atanan görevi düzenleyip yeniden atayabilir | Aldığı görevi silemez |
| Manager, aldığı departman dışı görevi devretmiş | Salt okunur görev ve temel proje bilgileri | Yok | Yok |
| Manager, kendi oluşturduğu görev | Görev | Başkasına atadıktan sonra da düzenleme/yeniden atama sürer | Yetki sürer |
| Member, proje üyesi | Proje ve görevleri | Projede görev oluşturabilir; yalnızca proje ekibindeki Member'lara atar | Yok |
| Member, kendisine atanmış görev | Görev; proje üyeliği yoksa yalnızca temel proje bilgileri | Durum, yorum ve dosya ekleme; görev bilgisi/atama düzenleme yok | Görevi silemez |
| Member, oluşturup başkasına atadığı görev | Görüntüleme sürer | Görev düzenleme/yeniden atama yok | Yok |

Temel proje bilgileri: ad, açıklama, departman, sorumlu ve durum. Yalnızca görev erişimi, diğer görevleri, proje yorumlarını/eklerini veya ekip listesini açmaz.

## Durum, Kanban ve pipeline

- Proje durumunu yalnızca o projede sorumlu/ekip üyesi olan Manager ve Admin değiştirir.
- Departman dışı proje Manager'ı için bu, proje düzenleme yasağının açık istisnasıdır.
- Sadece görev ataması proje durumu değiştirme hakkı vermez. Departman yetkisi olup proje katılımı olmayan Manager da değiştiremez.
- Kanban üzerinde elle taşıma ve durum değişikliği yalnızca Admin'dedir; görüntüleme erişim kapsamına bağlıdır.
- Pipeline aşamaları, checkpoint ve görev yerleşimini projedeki Manager'lar ve Admin yönetir. Ayrıntılar roadmap'tedir.
- Manager/Admin aşama ve checkpoint adını, açıklamasını ve sırasını değiştirebilir. Aşama silinirse bağlı görevler aşama bekleyen alana döner; checkpoint silinirse görevler mevcut aşamada kalır.
- Projeler “İlişkili” veya yönlü “Bağımlı” olarak bağlanabilir. Bağımlı proje, ön koşul proje tamamlanmadığında bekliyor görünür. Bağlantı yeni erişim hakkı doğurmaz; kullanıcı yalnızca zaten görebildiği projeyi seçebilir ve bağımlılık döngüleri engellenir.
- Proje pipeline geçmişi; aşama/checkpoint yapısı, görev durumu ve takvimi ile proje bağlantısı hareketlerini kullanıcı ve zaman bilgisiyle saklar. Projeyi görebilen kullanıcı geçmişi okuyabilir; yalnızca görev ataması tüm proje geçmişini açmaz. Silinen pipeline kayıtlarının geçmişteki proje bağı korunur.
- Proje bağımlılığı tüm projeye veya kaynak projenin belirli bir checkpoint'ine bağlanabilir. Ön koşul proje tamamlanmadan bağlı checkpoint tamamlanmış veya yönetici tarafından onaylanabilir sayılmaz. Checkpoint silinirse bağımlılık korunur ve genel proje bağımlılığına döner.
- Görev ön koşulları yalnızca aynı proje içindeki görevler arasında kurulabilir. Bir görev birden fazla ön koşula sahip olabilir; kendi kendine bağlantı, tekrar kayıt ve yönlü döngü kabul edilmez.
- Görev ön koşullarını projedeki Manager'lar ve Admin yönetir. Member bağlantıları salt okunur görür. Yalnızca görev erişimi, bağımlılık üzerinden başka görevlerin adını veya detayını açmaz.
- Tamamlanmamış bir ön koşul varsa bağlı görev Tamamlandı durumuna geçirilemez. Bekleme durumu akışta, zaman çizelgesinde ve tam proje erişimi olan kullanıcıların görev detayında görünür; görev durumu değişiklikleri ve bağımlılık hareketleri pipeline geçmişine kaydedilir.
- Planlanan görevin zaman çizelgesi çubuğunu yalnızca projedeki Manager ve Admin sürükleyebilir; klavyede ok tuşu bir gün, Shift+ok yedi gün kaydırır. Başlangıç ve bitiş birlikte taşınır, görev süresi korunur. Sunucudaki tarih doğrulaması ve işlem geçmişi bu değişimlerde de geçerlidir; Member görünümü salt okunurdur.

## Atama geçmişi ve yetki gerekçeleri

- Oluşturan kişi, ilk atayan kişi, mevcut görev sahibi ve sonraki devirler ayrı tutulur.
- Departman dışından görev alan kişinin yeniden ataması silme yetkisi kazandırmaz. İlk atayanın yetkisi devir yapan kişiye aktarılmaz.
- Departman dışından alınan kayıtları alıcı silemez; ilk atayan ve Admin yetkilidir. Manager'ın kendi oluşturduğu görevleri ve departman yönetim yetkisi ayrıca korunur. Member'a atama yapması silme hakkı kazandırmaz.
- Devir sonrası eski görev sahipleri salt okunur erişimi korur. Admin geçmişten gelen bu erişimi kaldırabilir; diğer geçerli yetki gerekçeleri ayrı değerlendirilir.
- Salt okunur erişim durum değiştirme, yorum, dosya ekleme, düzenleme ve yeniden atama içermez.

## Yorumlar ve dosyalar

- Tam proje erişimi olanlar proje yorumlarını/eklerini görebilir, yorum ve dosya ekleyebilir.
- Sadece görev erişimi olanlar bu işlemleri ilgili görevde yapabilir; salt okunur erişimde ekleme yoktur.
- Kullanıcı yazma yetkisi sürerken kendi yorumunu/dosyasını silebilir. Başkasının yorumunu/dosyasını yalnızca Admin silebilir.
- Görüntüleme, yükleme ve indirme sunucuda aynı kayıt erişimine bağlıdır.

## Silme ve geri yükleme

- Proje/görev silme geri dönüşüm kutusuna taşımadır. Geri yükleme yalnızca Admin'dedir.
- Proje geri yüklenirken o silme işlemiyle silinen görevler geri gelir; önceden bağımsız silinmiş görevler gelmez.
- Silme işlemlerinin ilişkisini ayırt edebilen bir kayıt gerekir; sadece tarih yakınlığına güvenilmez.

## Ortak uygulama ilkesi

Yetkiler yalnızca buton görünürlüğüyle uygulanmaz. Liste, detay, dashboard, Kanban, pipeline, dosya ve tüm yazma istekleri aynı merkezi kuralları kullanır. Bağlı kayda erişim, başka kayıtlara otomatik erişim vermez. İstemciden gelen kimlik ve departman değerleri doğrulanır.
