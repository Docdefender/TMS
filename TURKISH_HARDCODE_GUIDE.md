# TMS — Convert All UI to Turkish (Hardcoded)

## 🎯 Goal
Convert **every visible text** in the TMS application to Turkish by directly editing the `.cshtml` files. No localization framework, no resource files, just replace English text with Turkish text.

---

## 🚫 STRICT RULES

### ✅ DO:
1. **ONLY modify `.cshtml` files** (HTML/Razor markup)
2. **Replace English text with Turkish** directly in the markup
3. **Keep all C# code unchanged** (variables, property names, method names)
4. **Keep all CSS classes unchanged** (Tailwind classes, Bootstrap classes)
5. **Keep all `asp-` attributes unchanged** (they are for routing, not display)
6. **Keep icons (`<i class="bi bi-...">`) unchanged**

### ❌ DON'T:
1. **DON'T touch `.cshtml.cs` files** (PageModel code-behind)
2. **DON'T touch `Models/`, `Services/`, `Data/` folders**
3. **DON'T touch `Program.cs`**
4. **DON'T use `@Localizer` or resource files**
5. **DON'T change C# variable names or property names**
6. **DON'T change database column names or enum values**

---

## 📋 Translation Reference

### Navigation & Pages
| English | Turkish |
|---------|---------|
| Dashboard | Kontrol Paneli |
| Projects | Projeler |
| Tasks | Görevler |
| Kanban | Kanban Tahtası |
| Login | Giriş Yap |
| Logout | Çıkış Yap |
| Register | Kayıt Ol |
| Profile | Profil |
| Admin | Yönetici |
| Departments | Departmanlar |
| Categories | Kategoriler |
| Audit Logs | Denetim Kayıtları |

### Actions & Buttons
| English | Turkish |
|---------|---------|
| Create | Oluştur |
| Create New Project | Yeni Proje Oluştur |
| Create New Task | Yeni Görev Oluştur |
| Edit | Düzenle |
| Delete | Sil |
| Save | Kaydet |
| Cancel | İptal |
| Submit | Gönder |
| Back | Geri |
| View All | Tümünü Gör |
| Details | Detaylar |
| Actions | İşlemler |
| Close | Kapat |
| Update | Güncelle |
| Add | Ekle |
| Add Task | Görev Ekle |
| Remove | Kaldır |
| Filter by Status | Duruma Göre Filtrele |
| Clear Filter | Filtreyi Temizle |
| Back to List | Listeye Dön |

### Table Headers / Form Labels
| English | Turkish |
|---------|---------|
| Name | İsim |
| Title | Başlık |
| Description | Açıklama |
| Status | Durum |
| Department | Departman |
| Category | Kategori |
| Start Date | Başlangıç Tarihi |
| End Date | Bitiş Tarihi |
| Due Date | Son Tarih |
| Created By | Oluşturan |
| Assigned To | Atanan |
| Created Date | Oluşturulma Tarihi |
| Email | E-posta |
| Password | Şifre |
| Full Name | Ad Soyad |
| Role | Rol |
| Project | Proje |
| Task | Görev |
| Comment | Yorum |
| Comments | Yorumlar |

### Status Values
| English | Turkish |
|---------|---------|
| Not Started | Başlamadı |
| In Progress | Devam Ediyor |
| Completed | Tamamlandı |
| On Hold | Beklemede |
| Cancelled | İptal Edildi |
| To Do | Yapılacak |
| Done | Tamamlandı |
| Blocked | Engellendi |
| Overdue | Gecikmiş |
| Due Soon | Yakında Bitecek |
| On Track | Zamanında |

### Messages
| English | Turkish |
|---------|---------|
| Welcome | Hoş geldiniz |
| Welcome back | Tekrar hoş geldiniz |
| No projects found | Hiç proje bulunamadı |
| No tasks found | Hiç görev bulunamadı |
| Create your first project to get started! | İlk projenizi oluşturarak başlayın! |
| Project created successfully | Proje başarıyla oluşturuldu |
| Task created successfully | Görev başarıyla oluşturuldu |
| All on track! | Her şey yolunda! |
| Needs attention | Dikkat gerektirir |
| Analytics & Insights | Analizler ve İçgörüler |

### Dashboard Specific
| English | Turkish |
|---------|---------|
| Total Projects | Toplam Proje |
| Total Tasks | Toplam Görev |
| My Projects | Projelerim |
| My Tasks | Görevlerim |
| Project Completion Rate | Proje Tamamlanma Oranı |
| Project Task Ratio | Proje Görev Oranı |
| Tasks per Project | Proje Başına Görev |
| Task Distribution | Görev Dağılımı |
| Overdue Analysis | Gecikme Analizi |
| Completion Trend | Tamamlanma Eğilimi |
| System Stats | Sistem İstatistikleri |
| Total Users | Toplam Kullanıcı |
| Total Departments | Toplam Departman |
| Total Categories | Toplam Kategori |
| projects completed | proje tamamlandı |
| tasks per project | görev/proje |
| average | ortalama |

### Authentication
| English | Turkish |
|---------|---------|
| Remember Me | Beni Hatırla |
| Forgot Password | Şifremi Unuttum |
| Don't have an account? | Hesabınız yok mu? |
| Already have an account? | Zaten hesabınız var mı? |
| Sign In | Giriş Yap |
| Sign Up | Kayıt Ol |
| Invalid login attempt | Geçersiz giriş denemesi |

### Misc
| English | Turkish |
|---------|---------|
| Filtered | Filtrele |
| of | / |
| Ratio | Oran |
| Most Active | En Aktif |
| Unassigned | Atanmamış |
| Select Department | Departman Seçin |
| Select Category | Kategori Seçin |

---

## 📄 File-by-File Conversion Instructions

### Priority 1: Layout & Navigation

#### `TMS/Pages/Shared/_Layout.cshtml`

**Find and Replace:**

Dashboard → Kontrol Paneli Projects → Projeler Tasks → Görevler Kanban → Kanban Tahtası Departments → Departmanlar Categories → Kategoriler Audit Logs → Denetim Kayıtları Logout → Çıkış Yap Profile → Profil Admin → Yönetici


**Example:**

<!-- BEFORE --> <span class="sidebar-text">Dashboard</span> <span class="sidebar-text">Projects</span> <button>Logout</button>
<!-- AFTER --> <span class="sidebar-text">Kontrol Paneli</span> <span class="sidebar-text">Projeler</span> <button>Çıkış Yap</button>

---

### Priority 2: Dashboard

#### `TMS/Pages/Index.cshtml`

**Replace ALL visible text:**

<!-- Page Title --> <h1>Dashboard</h1> → <h1>Kontrol Paneli</h1>
<!-- Section Headers --> Projects → Projeler Tasks → Görevler Analytics & Insights → Analizler ve İçgörüler
<!-- KPI Labels --> Total → Toplam Not Started → Başlamadı In Progress → Devam Ediyor Completed → Tamamlandı On Hold → Beklemede Cancelled → İptal Edildi Overdue → Gecikmiş To Do → Yapılacak Done → Tamamlandı Blocked → Engellendi
<!-- Analytics Cards --> Project Completion Rate → Proje Tamamlanma Oranı of → / projects completed → proje tamamlandı
Project Task Ratio → Proje Görev Oranı Ratio → Oran
Tasks per Project → Proje Başına Görev tasks per project → görev/proje average → ortalama
Task Distribution → Görev Dağılımı
Overdue Analysis → Gecikme Analizi Needs attention → Dikkat gerektirir All on track! → Her şey yolunda!
Completion Trend → Tamamlanma Eğilimi This Week → Bu Hafta tasks completed → görev tamamlandı
System Stats → Sistem İstatistikleri Total Users → Toplam Kullanıcı Total Departments → Toplam Departman Total Categories → Toplam Kategori Most Active → En Aktif


---

### Priority 3: Projects Pages

#### `TMS/Pages/Projects/Index.cshtml`

<!-- Page Title --> <h1>Projects</h1> → <h1>Projeler</h1>
<!-- Button --> Create New Project → Yeni Proje Oluştur Clear Filter → Filtreyi Temizle Filtered → Filtrelenmiş
<!-- Filter Buttons --> Filter by Status → Duruma Göre Filtrele Not Started → Başlamadı In Progress → Devam Ediyor Completed → Tamamlandı On Hold → Beklemede Cancelled → İptal Edildi Overdue → Gecikmiş
<!-- Table Headers --> <th>Name</th> → <th>İsim</th> <th>Status</th> → <th>Durum</th> <th>Department</th> → <th>Departman</th> <th>Category</th> → <th>Kategori</th> <th>Start Date</th> → <th>Başlangıç Tarihi</th> <th>End Date</th> → <th>Bitiş Tarihi</th> <th>Created By</th> → <th>Oluşturan</th> <th>Assigned To</th> → <th>Atanan</th> <th>Actions</th> → <th>İşlemler</th>
<!-- Action Button --> Details → Detaylar
<!-- Empty State --> No projects found → Hiç proje bulunamadı Create your first project to get started! → İlk projenizi oluşturarak başlayın!


#### `TMS/Pages/Projects/Create.cshtml`

<h1>Create Project</h1> → <h1>Proje Oluştur</h1>
<!-- Form Labels --> Name → İsim Description → Açıklama Start Date → Başlangıç Tarihi End Date → Bitiş Tarihi Status → Durum Department → Departman Category → Kategori Assigned To → Atanan
<!-- Dropdown Placeholders --> Select Department → Departman Seçin Select Category → Kategori Seçin Select User → Kullanıcı Seçin
<!-- Buttons --> Create → Oluştur Cancel → İptal


#### `TMS/Pages/Projects/Details.cshtml`

<h1>Project Details</h1> → <h1>Proje Detayları</h1>
<!-- Info Section --> Project Information → Proje Bilgileri
<!-- Task Section --> Tasks → Görevler Add Task → Görev Ekle No tasks yet → Henüz görev yok Add your first task using the form above! → Yukarıdaki formu kullanarak ilk görevinizi ekleyin!
<!-- Table Headers --> Title → Başlık Status → Durum Category → Kategori Assigned To → Atanan Created → Oluşturulma Due Date → Son Tarih
<!-- Comment Section (if visible) --> Comments → Yorumlar Add Comment → Yorum Ekle No comments yet → Henüz yorum yok
<!-- Buttons --> Back to Projects → Projelere Dön Edit → Düzenle Delete → Sil


---

### Priority 4: Tasks Page

#### `TMS/Pages/Tasks/Index.cshtml`

<h1>Tasks</h1> → <h1>Görevler</h1>
Clear Filter → Filtreyi Temizle Filtered → Filtrelenmiş
<!-- Filter Buttons --> Filter by Status → Duruma Göre Filtrele To Do → Yapılacak In Progress → Devam Ediyor Done → Tamamlandı Blocked → Engellendi Overdue → Gecikmiş
<!-- Table Headers --> <th>Title</th> → <th>Başlık</th> <th>Project</th> → <th>Proje</th> <th>Status</th> → <th>Durum</th> <th>Category</th> → <th>Kategori</th> <th>Assigned To</th> → <th>Atanan</th> <th>Due Date</th> → <th>Son Tarih</th> <th>Actions</th> → <th>İşlemler</th>
<!-- Empty State --> No tasks found → Hiç görev bulunamadı Tasks will appear here once they are created in projects. → Görevler projelerde oluşturulduktan sonra burada görünecektir.


---

### Priority 5: Kanban Board

#### `TMS/Pages/Kanban/Index.cshtml`

<h1>Kanban Board</h1> → <h1>Kanban Tahtası</h1>
<!-- Column Headers --> Not Started → Başlamadı In Progress → Devam Ediyor Completed → Tamamlandı On Hold → Beklemede Cancelled → İptal Edildi
<!-- Card Content Labels --> tasks → görev task(s) → görev


---

### Priority 6: Admin Pages

#### `TMS/Pages/Admin/Departments.cshtml`

<h1>Departments</h1> → <h1>Departmanlar</h1> Create New Department → Yeni Departman Oluştur Name → İsim Description → Açıklama Actions → İşlemler Edit → Düzenle Delete → Sil Save → Kaydet Cancel → İptal

#### `TMS/Pages/Admin/Categories.cshtml`

<h1>Categories</h1> → <h1>Kategoriler</h1> Create New Category → Yeni Kategori Oluştur Name → İsim Description → Açıklama Actions → İşlemler Edit → Düzenle Delete → Sil Save → Kaydet Cancel → İptal

#### `TMS/Pages/Admin/AuditLogs.cshtml`

<h1>Audit Logs</h1> → <h1>Denetim Kayıtları</h1> User → Kullanıcı Action → İşlem Entity Type → Varlık Türü Timestamp → Zaman Details → Detaylar


---

### Priority 7: Authentication Pages

#### `TMS/Pages/Account/Login.cshtml`

<h1>Login</h1> → <h1>Giriş Yap</h1>
Email → E-posta Password → Şifre Remember Me → Beni Hatırla Login → Giriş Yap Don't have an account? Register → Hesabınız yok mu? Kayıt Ol Invalid login attempt. → Geçersiz giriş denemesi.

#### `TMS/Pages/Account/Register.cshtml`

<h1>Register</h1> → <h1>Kayıt Ol</h1>
Full Name → Ad Soyad Email → E-posta Password → Şifre Confirm Password → Şifre Tekrar Department → Departman Select Department → Departman Seçin Register → Kayıt Ol Already have an account? Login → Zaten hesabınız var mı? Giriş Yap


#### `TMS/Pages/Account/Profile.cshtml` (if exists)
<h1>My Profile</h1> → <h1>Profilim</h1>
Profile Information → Profil Bilgileri Full Name → Ad Soyad Email → E-posta Role → Rol Department → Departman Back to Dashboard → Kontrol Paneline Dön Created Date → Kayıt Tarihi
Edit Profile → Profili Düzenle Update Profile → Profili Güncelle
Change Password → Şifre Değiştir Current Password → Mevcut Şifre New Password → Yeni Şifre Confirm Password → Şifre Tekrar
Save → Kaydet Cancel → İptal


---

## 🎯 Step-by-Step Process

### Step 1: Layout First (Most Important)
1. Open `TMS/Pages/Shared/_Layout.cshtml`
2. Replace all navigation text with Turkish
3. Replace "Logout" button text
4. Save and test - navigation should be in Turkish

### Step 2: Dashboard
1. Open `TMS/Pages/Index.cshtml`
2. Replace page title: `Dashboard` → `Kontrol Paneli`
3. Replace all section headers
4. Replace all KPI labels
5. Replace analytics card titles and text
6. Save and test

### Step 3: Projects Pages
1. `Projects/Index.cshtml` - list page
2. `Projects/Create.cshtml` - create form
3. `Projects/Details.cshtml` - details page
4. Replace all visible text with Turkish

### Step 4: Tasks Page
1. `Tasks/Index.cshtml`
2. Replace all visible text with Turkish

### Step 5: Kanban Board
1. `Kanban/Index.cshtml`
2. Replace column headers

### Step 6: Admin Pages
1. `Admin/Departments.cshtml`
2. `Admin/Categories.cshtml`
3. `Admin/AuditLogs.cshtml`

### Step 7: Authentication Pages
1. `Account/Login.cshtml`
2. `Account/Register.cshtml`
3. `Account/Profile.cshtml` (if exists)

---

## ✅ Testing Checklist

After conversion, test each page:
- [ ] Layout/Navigation is Turkish
- [ ] Dashboard is Turkish
- [ ] Projects list is Turkish
- [ ] Project create form is Turkish
- [ ] Project details is Turkish
- [ ] Tasks list is Turkish
- [ ] Kanban board is Turkish
- [ ] Admin pages are Turkish
- [ ] Login page is Turkish
- [ ] Register page is Turkish
- [ ] All buttons work (functionality unchanged)
- [ ] All forms submit correctly
- [ ] No broken links

---

## 🚨 Common Mistakes to Avoid

1. ❌ **DON'T change C# code:**

// DON'T change this: @Model.Dashboard.TotalProjects asp-page="/Projects/Index" class="sidebar-text"

2. ❌ **DON'T change enum values in @functions:**

// DON'T change "InProgress", "Completed" etc. in switch statements TMS.Models.ProjectStatus.InProgress  // Keep as is


3. ❌ **DON'T translate variable names:**

@foreach (var project in Model.Projects)  // Keep "project"


4. ✅ **DO change only visible text:**

<h1>Projects</h1>  →  <h1>Projeler</h1> <small>Total</small>  →  <small>Toplam</small>


---

## 📝 Example: Before & After

### Dashboard Example

**BEFORE:**

<h1 class="text-3xl font-bold mb-6"> <i class="bi bi-speedometer2 mr-2"></i>Dashboard </h1>
<h5 class="text-sm font-semibold mb-3"> <i class="bi bi-folder2-open me-1"></i>Projects </h5>
<div class="text-4xl font-bold">@Model.Dashboard.TotalProjects</div> <div class="text-sm">Total</div>
<div class="text-4xl font-bold">@Model.Dashboard.InProgressProjects</div> <div class="text-sm">In Progress</div>


**AFTER:**

<h1 class="text-3xl font-bold mb-6"> <i class="bi bi-speedometer2 mr-2"></i>Kontrol Paneli </h1>
<h5 class="text-sm font-semibold mb-3"> <i class="bi bi-folder2-open me-1"></i>Projeler </h5>
<div class="text-4xl font-bold">@Model.Dashboard.TotalProjects</div> <div class="text-sm">Toplam</div>
<div class="text-4xl font-bold">@Model.Dashboard.InProgressProjects</div> <div class="text-sm">Devam Ediyor</div>


---

Save this file as `TURKISH_HARDCODE_GUIDE.md` in your repository root (`C:\Users\ahmeta\source\repos\TMS\`)