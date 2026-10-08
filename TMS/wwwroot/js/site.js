(function () {
    // ── Sidebar ──────────────────────────────────────
    var sidebar    = document.getElementById('sidebar');
    var toggle     = document.getElementById('sidebarToggle');
    var mobileBtn  = document.getElementById('mobileMenuBtn');
    var overlay    = document.getElementById('sidebarOverlay');

    function isMobile() { return window.innerWidth <= 768; }

    function openMobile() {
        if (sidebar) sidebar.classList.add('mobile-open');
        if (overlay) overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }
    function closeMobile() {
        if (sidebar) sidebar.classList.remove('mobile-open');
        if (overlay) overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    if (sidebar && !isMobile() && localStorage.getItem('sidebar-collapsed') === 'true') {
        sidebar.classList.add('collapsed');
    }

    function syncSidebarToggle() {
        if (!toggle || !sidebar) return;
        var collapsed = !isMobile() && sidebar.classList.contains('collapsed');
        var label = isMobile() ? 'Menüyü kapat' : (collapsed ? 'Menüyü genişlet' : 'Menüyü daralt');
        toggle.setAttribute('aria-label', label);
        toggle.setAttribute('title', label);
        var icon = toggle.querySelector('i');
        if (icon) icon.className = 'bi ' + (collapsed ? 'bi-chevron-right' : 'bi-chevron-left');
        sidebar.querySelectorAll('.sidebar-link').forEach(function (link) {
            var text = link.querySelector('.sidebar-text');
            var linkLabel = text ? text.textContent.trim() : '';
            if (!linkLabel) return;
            if (collapsed) {
                link.setAttribute('title', linkLabel);
                link.setAttribute('aria-label', linkLabel);
            } else {
                link.removeAttribute('title');
                link.removeAttribute('aria-label');
            }
        });
    }
    syncSidebarToggle();

    if (toggle) {
        toggle.addEventListener('click', function () {
            if (isMobile()) { closeMobile(); }
            else {
                sidebar.classList.toggle('collapsed');
                localStorage.setItem('sidebar-collapsed', sidebar.classList.contains('collapsed'));
            }
            syncSidebarToggle();
        });
    }
    if (mobileBtn) {
        mobileBtn.addEventListener('click', function () {
            sidebar && sidebar.classList.contains('mobile-open') ? closeMobile() : openMobile();
        });
    }
    if (overlay) overlay.addEventListener('click', closeMobile);
    window.addEventListener('resize', function () { if (!isMobile()) closeMobile(); syncSidebarToggle(); });

    document.querySelectorAll('.sidebar-link').forEach(function (link) {
        link.addEventListener('click', function () { if (isMobile()) closeMobile(); });
    });

    // ── Aktif link vurgula ────────────────────────────
    var currentPath = window.location.pathname.toLowerCase().replace(/\/+$/, '') || '/';
    document.querySelectorAll('.sidebar-link').forEach(function (link) {
        var href = link.getAttribute('href');
        if (!href) return;
        var linkPath = href.toLowerCase().replace(/\/+$/, '') || '/';
        if (currentPath === linkPath || (linkPath !== '/' && currentPath.startsWith(linkPath))) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }
    });

    var sectionName = currentPath === '/kanban/projects' || currentPath.startsWith('/pipeline')
        ? 'projects'
        : (currentPath === '/kanban' || currentPath === '/kanban/index' ? 'tasks' : '');
    if (sectionName) {
        document.querySelectorAll('.sidebar-link.active').forEach(function (link) {
            link.classList.remove('active');
            link.removeAttribute('aria-current');
        });
        var sectionLink = document.querySelector('[data-sidebar-section="' + sectionName + '"]');
        if (sectionLink) {
            sectionLink.classList.add('active');
            sectionLink.setAttribute('aria-current', 'page');
        }
    }

    // ── Tema Toggle ───────────────────────────────────
    var themeToggle = document.getElementById('themeToggle');
    var themeLabel  = document.getElementById('themeLabel');
    var themeThumb  = document.getElementById('themeThumb');
    var html        = document.documentElement;

    function applyTheme(theme) {
        html.setAttribute('data-theme', theme);
        localStorage.setItem('dizge-theme', theme);
        if (themeLabel) themeLabel.textContent = theme === 'dark' ? 'Koyu Tema' : 'Açık Tema';
        if (themeThumb) themeThumb.textContent = theme === 'dark' ? '🌙' : '☀️';
    }

    applyTheme(localStorage.getItem('dizge-theme') || 'light');

    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            applyTheme(html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
        });
    }

    // ── Profile menu ──────────────────────────────────
    var profileMenu = document.querySelector('[data-profile-menu]');
    var profileToggle = profileMenu && profileMenu.querySelector('[data-profile-menu-toggle]');
    var profilePanel = profileMenu && profileMenu.querySelector('[data-profile-menu-panel]');

    function closeProfileMenu() {
        if (!profileToggle || !profilePanel) return;
        profilePanel.hidden = true;
        profileToggle.setAttribute('aria-expanded', 'false');
    }

    var notificationMenu = document.querySelector('[data-notification-menu]');
    var notificationToggle = notificationMenu && notificationMenu.querySelector('[data-notification-menu-toggle]');
    var notificationPanel = notificationMenu && notificationMenu.querySelector('[data-notification-menu-panel]');

    function closeNotificationMenu() {
        if (!notificationToggle || !notificationPanel) return;
        notificationPanel.hidden = true;
        notificationToggle.setAttribute('aria-expanded', 'false');
    }

    if (notificationToggle && notificationPanel) {
        notificationToggle.addEventListener('click', function () {
            var willOpen = notificationPanel.hidden;
            closeProfileMenu();
            notificationPanel.hidden = !willOpen;
            notificationToggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        });
    }

    if (profileToggle && profilePanel) {
        profileToggle.addEventListener('click', function () {
            var willOpen = profilePanel.hidden;
            closeNotificationMenu();
            profilePanel.hidden = !willOpen;
            profileToggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        });
        document.addEventListener('click', function (event) {
            if (profileMenu && !profileMenu.contains(event.target)) closeProfileMenu();
            if (notificationMenu && !notificationMenu.contains(event.target)) closeNotificationMenu();
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeProfileMenu();
                closeNotificationMenu();
            }
        });
    }

    // ── Detail tabs ───────────────────────────────────
    document.querySelectorAll('[data-detail-tabs]').forEach(function (tabs) {
        var buttons = Array.from(tabs.querySelectorAll('[data-detail-tab]'));
        var panels = Array.from(document.querySelectorAll('[data-detail-panel]'));
        if (!buttons.length || !panels.length) return;

        var storageKey = 'dizge-detail-tab:' + (tabs.dataset.detailTabsKey || window.location.pathname);
        var available = buttons.map(function (button) { return button.dataset.detailTab; });

        function activate(name, remember) {
            if (!available.includes(name)) name = available[0];
            buttons.forEach(function (button) {
                var active = button.dataset.detailTab === name;
                button.classList.toggle('is-active', active);
                button.setAttribute('aria-selected', active ? 'true' : 'false');
                button.tabIndex = active ? 0 : -1;
            });
            panels.forEach(function (panel) {
                panel.hidden = panel.dataset.detailPanel !== name;
            });
            if (remember) sessionStorage.setItem(storageKey, name);
        }

        buttons.forEach(function (button, index) {
            button.setAttribute('role', 'tab');
            button.addEventListener('click', function () { activate(button.dataset.detailTab, true); });
            button.addEventListener('keydown', function (event) {
                if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
                event.preventDefault();
                var next = event.key === 'ArrowRight' ? (index + 1) % buttons.length : (index - 1 + buttons.length) % buttons.length;
                buttons[next].focus();
                activate(buttons[next].dataset.detailTab, true);
            });
        });
        document.querySelectorAll('[data-open-detail-tab]').forEach(function (trigger) {
            trigger.addEventListener('click', function () {
                activate(trigger.dataset.openDetailTab, true);
                tabs.scrollIntoView({ behavior: 'smooth', block: 'start' });
            });
        });
        tabs.setAttribute('role', 'tablist');
        activate(sessionStorage.getItem(storageKey) || available[0], false);
    });
})();
