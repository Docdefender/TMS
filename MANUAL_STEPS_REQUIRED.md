# ?? MANUAL STEP REQUIRED — Create Resource Files in Visual Studio

## Why This is Needed
The `.resx` files **must** be created in Visual Studio (not via command-line tools) to ensure proper UTF-8 BOM encoding for Turkish characters (Ý, ð, ü, þ, ç, ö). Command-line file creation causes XML encoding errors.

---

## ? What's Already Complete (By Agent)

1. ? **Localization Infrastructure**
   - `Microsoft.Extensions.Localization` package installed
   - `Program.cs` configured for Turkish (`tr-TR`) + English (`en-US`)
   - `SharedResources.cs` marker class created

2. ? **Language Switcher**
   - `/Pages/SetLanguage.cshtml` + `.cshtml.cs` created
   - `_LanguageSwitcher.cshtml` partial view created
   - Switcher added to sidebar (bottom section)

---

## ?? YOUR ACTION REQUIRED

### Step 1: Create Turkish Resource File

1. Open **Visual Studio**
2. In Solution Explorer, right-click `TMS\Resources` folder
3. Select **Add** ? **New Item**
4. Search for **"Resources File"**
5. Name it: `SharedResources.tr.resx`
6. Click **Add**

7. In the `.resx` designer, add these **Name/Value** pairs:

| Name | Value (Turkish) |
|------|-----------------|
| Dashboard | Kontrol Paneli |
| Projects | Projeler |
| Tasks | Görevler |
| Kanban | Kanban |
| Login | Giriþ Yap |
| Logout | Çýkýþ Yap |
| Register | Kayýt Ol |
| Profile | Profil |
| Admin | Yönetici |
| Departments | Departmanlar |
| Categories | Kategoriler |
| AuditLogs | Denetim Kayýtlarý |
| RecycleBin | Geri Dönüþüm Kutusu |
| Create | Oluþtur |
| Edit | Düzenle |
| Delete | Sil |
| Save | Kaydet |
| Cancel | Ýptal |
| Submit | Gönder |
| Back | Geri |
| ViewAll | Tümünü Gör |
| Details | Detaylar |
| Actions | Ýþlemler |
| Upload | Yükle |
| Download | Ýndir |
| TotalProjects | Toplam Proje |
| TotalTasks | Toplam Görev |
| MyProjects | Projelerim |
| MyTasks | Görevlerim |
| RecentActivity | Son Aktiviteler |
| NotStarted | Baþlamadý |
| InProgress | Devam Ediyor |
| Completed | Tamamlandý |
| OnHold | Beklemede |
| Cancelled | Ýptal Edildi |
| ToDo | Yapýlacak |
| Done | Tamamlandý |
| Blocked | Engellendi |
| Overdue | Gecikmiþ |
| DueSoon | Yakýnda Bitecek |
| Name | Ad |
| Description | Açýklama |
| Status | Durum |
| Department | Departman |
| Category | Kategori |
| StartDate | Baþlangýç Tarihi |
| EndDate | Bitiþ Tarihi |
| DueDate | Son Tarih |
| CreatedBy | Oluþturan |
| AssignedTo | Atanan |
| Email | E-posta |
| Password | Þifre |
| FullName | Ad Soyad |
| RememberMe | Beni Hatýrla |
| ForgotPassword | Þifremi Unuttum |
| CreateNewProject | Yeni Proje Oluþtur |
| CreateNewTask | Yeni Görev Oluþtur |
| NoProjectsFound | Hiç proje bulunamadý |
| NoTasksFound | Hiç görev bulunamadý |
| ProjectCreated | Proje baþarýyla oluþturuldu |
| ProjectUpdated | Proje baþarýyla güncellendi |
| ProjectDeleted | Proje baþarýyla silindi |
| TaskCreated | Görev baþarýyla oluþturuldu |
| TaskUpdated | Görev baþarýyla güncellendi |
| TaskDeleted | Görev baþarýyla silindi |
| FilterByStatus | Duruma Göre Filtrele |
| ClearFilter | Filtreyi Temizle |
| Welcome | Hoþ geldiniz |
| Language | Dil |
| Turkish | Türkçe |
| English | English |

8. Save the file (Ctrl+S)

---

### Step 2: Create English Resource File

1. Right-click `TMS\Resources` folder again
2. Select **Add** ? **New Item** ? **Resources File**
3. Name it: `SharedResources.en.resx`
4. Click **Add**

5. Add the **same Name keys** with English values:

| Name | Value (English) |
|------|-----------------|
| Dashboard | Dashboard |
| Projects | Projects |
| Tasks | Tasks |
| Kanban | Kanban |
| Login | Login |
| Logout | Logout |
| Register | Register |
| Profile | Profile |
| Admin | Admin |
| Departments | Departments |
| Categories | Categories |
| AuditLogs | Audit Logs |
| RecycleBin | Recycle Bin |
| Create | Create |
| Edit | Edit |
| Delete | Delete |
| Save | Save |
| Cancel | Cancel |
| Submit | Submit |
| Back | Back |
| ViewAll | View All |
| Details | Details |
| Actions | Actions |
| Upload | Upload |
| Download | Download |
| TotalProjects | Total Projects |
| TotalTasks | Total Tasks |
| MyProjects | My Projects |
| MyTasks | My Tasks |
| RecentActivity | Recent Activity |
| NotStarted | Not Started |
| InProgress | In Progress |
| Completed | Completed |
| OnHold | On Hold |
| Cancelled | Cancelled |
| ToDo | To Do |
| Done | Done |
| Blocked | Blocked |
| Overdue | Overdue |
| DueSoon | Due Soon |
| Name | Name |
| Description | Description |
| Status | Status |
| Department | Department |
| Category | Category |
| StartDate | Start Date |
| EndDate | End Date |
| DueDate | Due Date |
| CreatedBy | Created By |
| AssignedTo | Assigned To |
| Email | Email |
| Password | Password |
| FullName | Full Name |
| RememberMe | Remember Me |
| ForgotPassword | Forgot Password |
| CreateNewProject | Create New Project |
| CreateNewTask | Create New Task |
| NoProjectsFound | No projects found |
| NoTasksFound | No tasks found |
| ProjectCreated | Project created successfully |
| ProjectUpdated | Project updated successfully |
| ProjectDeleted | Project deleted successfully |
| TaskCreated | Task created successfully |
| TaskUpdated | Task updated successfully |
| TaskDeleted | Task deleted successfully |
| FilterByStatus | Filter by Status |
| ClearFilter | Clear Filter |
| Welcome | Welcome |
| Language | Language |
| Turkish | Türkçe |
| English | English |

6. Save the file (Ctrl+S)

---

### Step 3: Build and Test

1. Build the project in Visual Studio (Ctrl+Shift+B)
2. Run the application (F5)
3. Log in to the system
4. Look at the **bottom of the sidebar** — you should see a language switcher button
5. Click it to switch between **Türkçe** and **English**
6. The page will reload with the new language
7. Your choice is saved in a cookie (`.AspNetCore.Culture`)

---

## ?? Expected Behavior

- **Default language**: Turkish (`tr-TR`)
- **Sidebar displays**: "Türkçe" button ? switches to Turkish
- **Sidebar displays**: "English" button ? switches to English
- **Cookie persistence**: Language choice persists across sessions
- **All text** should switch between Turkish/English

---

## ?? Troubleshooting

### Build Errors After Creating .resx Files?
- Check that file names are **exactly**: `SharedResources.tr.resx` and `SharedResources.en.resx`
- Ensure both files are in `TMS\Resources\` folder
- Verify Turkish characters (Ý, ð, ü, þ, ç, ö) display correctly in the designer

### Language Switcher Not Visible?
- Check `_Layout.cshtml` around line 100-105 (should have `<partial name="_LanguageSwitcher" />`)
- Ensure you're logged in (switcher only shows for authenticated users)

### Text Not Changing?
- Verify `.resx` files have matching **Name** keys in both languages
- Check browser console for errors
- Clear browser cache and cookies

---

## ?? Next Steps After .resx Creation

Once both `.resx` files are created and the build succeeds:

1. ? Language switcher will be functional
2. ? Frontend agent needs to update all `.cshtml` pages to use `@Localizer["Key"]`
3. ? Add missing resource keys as needed during development

---

**Good luck! Ask the backend agent if you need any help or additional resource keys.** ??
