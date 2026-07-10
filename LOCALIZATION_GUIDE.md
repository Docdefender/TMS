# TMS — Bilingual Application Guide (Turkish & English)

## 🎯 Goal
Add full bilingual support to TMS with:
- **Turkish (tr-TR)** as default language
- **English (en-US)** as secondary language
- Language switcher buttons in the UI
- User preference saved in cookie/session
- All UI elements translated

---

## 📋 Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Setup Instructions](#setup-instructions)
3. [Translation Files Structure](#translation-files-structure)
4. [Implementation Steps](#implementation-steps)
5. [Language Switcher Component](#language-switcher)
6. [Usage in Pages](#usage-in-pages)
7. [Translation Keys Reference](#translation-keys)

---

## How It Works
1. User clicks language button (Türkçe/English)
2. Request sent to `/Language/SetLanguage?culture=tr-TR`
3. Cookie `.AspNetCore.Culture` is set
4. Page reloads with new language
5. `IStringLocalizer` reads from correct `.resx` file

---

## 🚀 Setup Instructions

### Step 1: Install NuGet Package

### Step 2: Create Resources Folder Structure

### Step 3: Create SharedResources Marker Class

### Step 4: Update Program.cs

### Step 5: Create Language Controller

---

## 📝 Translation Files Structure

### SharedResources.tr.resx (Turkish - Default)

Create this file in Visual Studio:
1. Right-click `Resources` folder → **Add** → **New Item**
2. Search for "Resources File"
3. Name: `SharedResources.tr.resx`
4. Add Name/Value pairs:

| Name | Value (Turkish) |
|------|-----------------|
| Dashboard | Kontrol Paneli |
| Projects | Projeler |
| Tasks | Görevler |
| Kanban | Kanban |
| Login | Giriş Yap |
| Logout | Çıkış Yap |
| Register | Kayıt Ol |
| Profile | Profil |
| Admin | Yönetici |
| Departments | Departmanlar |
| Categories | Kategoriler |
| AuditLogs | Denetim Kayıtları |
| Create | Oluştur |
| Edit | Düzenle |
| Delete | Sil |
| Save | Kaydet |
| Cancel | İptal |
| Submit | Gönder |
| Back | Geri |
| ViewAll | Tümünü Gör |
| Details | Detaylar |
| Actions | İşlemler |
| TotalProjects | Toplam Proje |
| TotalTasks | Toplam Görev |
| MyProjects | Projelerim |
| MyTasks | Görevlerim |
| RecentActivity | Son Aktiviteler |
| NotStarted | Başlamadı |
| InProgress | Devam Ediyor |
| Completed | Tamamlandı |
| OnHold | Beklemede |
| Cancelled | İptal Edildi |
| ToDo | Yapılacak |
| Done | Tamamlandı |
| Blocked | Engellendi |
| Overdue | Gecikmiş |
| DueSoon | Yakında Bitecek |
| Name | Ad |
| Description | Açıklama |
| Status | Durum |
| Department | Departman |
| Category | Kategori |
| StartDate | Başlangıç Tarihi |
| EndDate | Bitiş Tarihi |
| DueDate | Son Tarih |
| CreatedBy | Oluşturan |
| AssignedTo | Atanan |
| Email | E-posta |
| Password | Şifre |
| FullName | Ad Soyad |
| RememberMe | Beni Hatırla |
| ForgotPassword | Şifremi Unuttum |
| CreateNewProject | Yeni Proje Oluştur |
| CreateNewTask | Yeni Görev Oluştur |
| NoProjectsFound | Hiç proje bulunamadı |
| NoTasksFound | Hiç görev bulunamadı |
| ProjectCreated | Proje başarıyla oluşturuldu |
| ProjectUpdated | Proje başarıyla güncellendi |
| ProjectDeleted | Proje başarıyla silindi |
| TaskCreated | Görev başarıyla oluşturuldu |
| TaskUpdated | Görev başarıyla güncellendi |
| TaskDeleted | Görev başarıyla silindi |
| FilterByStatus | Duruma Göre Filtrele |
| ClearFilter | Filtreyi Temizle |
| Welcome | Hoş geldiniz |
| Language | Dil |
| Turkish | Türkçe |
| English | English |

### SharedResources.en.resx (English)

Same structure, English values:

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

---

## 🔄 Language Switcher Component

### Create Partial View

### Add to Layout


