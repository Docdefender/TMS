# Kamdizge — Modern Dual-Theme UI Redesign Guide

## 🎯 Mission
Redesign Kamdizge with a **turquoise + crimson** color identity, featuring a **light and dark theme** toggled from the sidebar. Remove the top navbar entirely. Make every page visually distinct and modern.

---

## 🚫 Strict Rules
1. **ONLY modify `.cshtml` files, `tailwind.css`, `site.js`**
2. **NEVER touch `.cs` files** — no PageModels, Services, Models, Program.cs
3. **Keep ALL `asp-` tag helpers, `@Model.*`, C# blocks UNCHANGED**
4. **Keep all form submissions, links, and routing UNCHANGED**
5. **Preserve all functionality** — only appearance changes

---

## 🎨 Color System

### Brand Colors

Primary Turquoise : #0D9488 Primary Dark Teal : #0a6b62 Crimson Red       : #A91101 Crimson Dark      : #7f0d01

### Supporting Palette (for harmony)

Sky Blue    : #0ea5e9   → InProgress badges, info Emerald     : #10b981   → Completed/Done badges Amber       : #f59e0b   → OnHold/Warning badges Purple      : #8b5cf6   → ToDo badges, accents Rose        : #f43f5e   → Blocked badges Slate       : #64748b   → NotStarted badges


## 🎨 How to Apply Classes in Pages

### Replace Bootstrap/old classes with these:

| Old Class | New Class |
|-----------|-----------|
| `bg-white rounded-lg shadow-sm border` | `card` |
| `card-header bg-primary text-white` | `card-header` |
| `card-body p-4` | `card-body` |
| `<table class="data-table ...">` | `<table class="data-table">` (wrap in `.table-container`) |
| `btn btn-primary` | `btn-primary` |
| `btn btn-danger` | `btn-danger` |
| `btn btn-outline-*` | `btn-secondary` |
| `badge bg-primary` | `badge badge-inprogress` |
| `badge bg-success` | `badge badge-completed` |
| `badge bg-danger` | `badge badge-cancelled` |
| `badge bg-warning` | `badge badge-onhold` |
| `badge bg-secondary` | `badge badge-notstarted` |
| `bg-gray-100` (page bg) | Remove — handled by CSS var |
| Filter buttons | `filter-chip filter-chip-default` or `filter-chip-active` |
| Status filter danger | `filter-chip filter-chip-danger` |
| Empty state div | `empty-state` |
| Form inputs | `form-input` or just use `input` (global style applies) |

### Dashboard KPI Cards — Use These Gradient Classes:


Total Projects  → kpi-teal Not Started     → kpi-slate In Progress     → kpi-sky Completed       → kpi-emerald On Hold         → kpi-amber Cancelled       → kpi-crimson Total Tasks     → kpi-purple To Do           → kpi-purple Done            → kpi-emerald Blocked         → kpi-rose Overdue         → kpi-crimson


**Example KPI card structure:**

<a asp-page="/Projects/Index" asp-route-status="InProgress" class="kpi-card kpi-sky"> <span class="kpi-number">@Model.Dashboard.InProgressProjects</span> <span class="kpi-label">Devam Ediyor</span> <i class="bi bi-arrow-repeat absolute bottom-3 right-3 text-white/20 text-4xl"></i> </a>


---

## 📋 Page Conversion Checklist

Apply the patterns to these files in order:

1. **`tailwind.css`** — Replace entirely with full file above ✅
2. **`_Layout.cshtml`** — Replace entirely with full file above ✅
3. **`site.js`** — Replace with new JS above ✅
4. **`Index.cshtml`** — KPI cards → `.kpi-card kpi-*`, section titles → `.section-heading`
5. **`Projects/Index.cshtml`** — `.table-container` + `.data-table`, filter buttons → `.filter-chip`
6. **`Tasks/Index.cshtml`** — Same as Projects/Index
7. **`Kanban/Index.cshtml`** — `.kanban-column`, `.kanban-card`, colored left borders
8. **`Projects/Create.cshtml`** — `.card` + `.card-header` + `.card-body`, `.btn-primary`
9. **`Projects/Details.cshtml`** — `.card`, `.badge-*`, `.btn-primary`, `.btn-danger`
10. **`Account/Login.cshtml`** — `.auth-card` (already applied)
11. **`Account/Register.cshtml`** — `.auth-card`
12. **`Account/Profile.cshtml`** — `.card` + `.card-header` + `.card-body`
13. **`Admin/Departments.cshtml`** — `.card`, `.table-container`, `.btn-primary`, `.btn-danger`
14. **`Admin/Categories.cshtml`** — Same as Departments
15. **`Admin/AuditLogs.cshtml`** — `.card`, `.table-container`
16. **`Admin/RecycleBin.cshtml`** — `.card`, `.table-container`, `.btn-primary`

---

## ✅ Testing After Each Page

- [ ] Light theme looks correct
- [ ] Dark theme looks correct  
- [ ] Theme toggle switches instantly
- [ ] Theme saved after page refresh
- [ ] Sidebar collapses correctly
- [ ] Active link highlighted
- [ ] All buttons work
- [ ] All forms submit
- [ ] Tables readable in both themes
- [ ] Badges colored correctly
- [ ] KPI cards show gradients

