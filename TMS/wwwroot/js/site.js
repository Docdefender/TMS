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

    if (toggle) {
        toggle.addEventListener('click', function () {
            if (isMobile()) { closeMobile(); }
            else {
                sidebar.classList.toggle('collapsed');
                localStorage.setItem('sidebar-collapsed', sidebar.classList.contains('collapsed'));
            }
        });
    }
    if (mobileBtn) {
        mobileBtn.addEventListener('click', function () {
            sidebar && sidebar.classList.contains('mobile-open') ? closeMobile() : openMobile();
        });
    }
    if (overlay) overlay.addEventListener('click', closeMobile);
    window.addEventListener('resize', function () { if (!isMobile()) closeMobile(); });

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
})();
