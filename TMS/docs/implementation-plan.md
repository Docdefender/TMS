# İlk geliştirme paketi

2026-09-09. İlk paket uygulandı; izole LocalDB ve geçici uygulama sürecinde 63 kontrol geçti. Mevcut uygulama veritabanı ve canlı yayın değiştirilmedi. Kullanıcının önceki görsel yenilemesi korundu; ilgili ekranlara işlevsel eklemeler yapıldı. Ayrıntılı rapor: [organization-release.md](organization-release.md).

## Başlangıçta doğrulanan farklar (uygulama öncesi kayıt)

| Kod alanı | Mevcut yapı | Gerekli değişiklik |
|---|---|---|
| Models/ApplicationUser.cs | Tek DepartmentId | Kullanıcı ana departmanından ayrı çoklu Manager sorumlulukları |
| Models/Department.cs | Varsayılan Manager yok | Departmana bağlı geçerli varsayılan Manager |
| Models/Project.cs, ProjectMember.cs | Ana departman, sorumlu ve ekip var | Departmanlı seçim, rol kısıtı ve geçiş kuralları |
| Models/TaskItem.cs | Oluşturan ve mevcut atanan var | İlk atayan, devir geçmişi ve kaldırılabilir geçmiş erişimi |
| Services/ProjectService.cs, TaskService.cs | Rol/üyelik tabanlı parçalı kapsam | Departman ve erişim gerekçelerine bağlı merkezi sorgu/yazma kontrolleri |
| Pages/Projects, Pages/Tasks | Bazı işlemlerde genel Manager izni; oluşturma yalnızca Authorize | Her işlem için ayrı kaynak yetkisi; sınırlı proje bilgi sunumu |
| Pages/Kanban/Index.cshtml.cs | Admin, Manager veya proje sorumlusu taşıyabiliyor | Yalnızca Admin; proje detay durum yetkisinden ayrı |
| Services/DashboardService.cs | Eski proje AssignedToUserId kullanılıyor | Merkezi görünürlük kapsamı ve doğru toplamlar |
| Services/CommentService.cs, detay PageModel'leri | Eski atama kontrolü; yorum görünümü Admin/Manager ile sınırlı | Yeni proje/görev ve salt okunur erişim kuralları |
| Program.cs | UseStaticFiles ile statik sunum | Özel ekler için yetkili indirme ve mevcut bağlantıların geçiş planı |

## Uygulama sırası

1. Veri modeli: Manager-departman ilişkisi, varsayılan Manager, atama/devir geçmişi, geçmiş erişim iptali ve silme işlem ilişkisi tasarımı.
2. Migration ve eski veri geçişi: mevcut kullanıcı departmanları ve proje sorumluları korunur. Bilinmeyen ilk atayan/devir geçmişi uydurulmaz. Eski veride çıkarılamayan yetki bilgileri açıkça raporlanır; toplu tahminle erişim verilmez.
3. Merkezi kaynak yetkilendirmesi: proje tam/temel görünüm, görev görünüm, oluşturma, düzenleme, atama, silme, durum, yorum/dosya, Kanban ve pipeline ayrı işlemler olarak değerlendirilir.
4. Admin organizasyon ekranları ve proje oluşturma/düzenleme seçimleri.
5. Görev oluşturma ve devir akışları; Member kısıtları ve Manager oluşturucu yetkisinin korunması.
6. Bütün GET/POST ve dosya yollarına kuralların uygulanması; dashboard ve Kanban kapsamı. Proje genel düzenleme formundan durum kuralı aşılamamalı.
7. Kritik yetki ve veri bütünlüğü testleri; güncel belgelerle iş teslimi.

Uygulanan modeller: ManagerDepartment, TaskAssignment, TaskHistoryAccess; Project/TaskItem üzerindeki FirstAssignedByUserId ve DeletionBatchId. Pipeline, Ticket ve e-posta bu ilk pakete dahil değildir. Departman değişikliğinde varsayılan Manager, ekip/görev korunması ve rol değişikliğinde sorumluluk kontrolü eklendi.

## Kabul senaryoları

- Admin bütün işlemleri yapar; kullanıcı atama listeleri Admin'i sunmaz.
- Çok departmanlı Manager yalnızca sorumlu departmanlarda departman yetkilerini kullanır; diğer erişimler atama/üyelik gerekçelidir.
- Departmansız Manager proje oluşturamaz. Member doğrudan POST ile de proje oluşturamaz.
- Yalnızca görev atanmış kişi diğer görevleri, proje eklerini/yorumlarını ve tam pipeline'ı göremez; yalnızca temel proje bilgisi döner.
- Departman dışı Manager aldığı görevi düzenler/devreder fakat silemez. Devir sonrası yazma kapanır; okuma kalır.
- Kendi görevini oluşturan Manager devretse de yönetir; Member başkasına atadığı görevde yönetim hakkı kazanmaz.
- Member yalnızca üyesi olduğu projede görev oluşturur ve yalnızca o ekipteki Member'lara atar; sahte kullanıcı kimliği reddedilir.
- Ekipten çıkarma atamaları silmez; departman değişikliği üyeleri/görevleri korur ve varsayılan sorumluyu atar.
- Proje katılımcısı olmayan departman Manager'ı genel düzenleme üzerinden durum değiştiremez. Projedeki Manager detaydan değiştirebilir ama Kanban POST'u reddedilir.
- Yorum/dosya yazma yetkisi kayıt kapsamını izler; dosya URL'si oturumsuz veya yetkisiz erişime açık değildir.
- Admin'in geçmiş erişimi kaldırması yalnızca o gerekçeyi kaldırır; bağımsız aktif üyelik/atama yetkileri kaybolmaz.
- Proje geri yükleme yalnızca aynı silme işlemindeki görevleri açar; önceden silinenler kapalı kalır.
- Yetkili kullanıcılar için başarılı akışlar ve yetkisiz doğrudan HTTP istekleri birlikte test edilir.

## Sonraki bağımsız işler

Profil form doğrulaması, dashboard bağlantı parametreleri, dosya silme/geri saklama davranışı ve geri yükleme mevcut kodda ayrıntılı yeniden incelenecek. Görsel yenilemenin tamamı tarayıcıda doğrulanacak. Eski inceleme bulguları test edilmeden düzeltilmiş veya kesin hata kabul edilmeyecek.
