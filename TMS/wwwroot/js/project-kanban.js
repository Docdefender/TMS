(() => {
    const statusLabels = {
        NotStarted: 'Başlamadı',
        InProgress: 'Devam Ediyor',
        OnHold: 'Beklemede',
        Completed: 'Tamamlandı',
        Cancelled: 'İptal'
    };

    document.querySelectorAll('[data-project-kanban]').forEach(shell => {
        const lists = [...shell.querySelectorAll('[data-project-status]')];
        const feedback = document.querySelector('[data-project-kanban-feedback]');
        const drawerLayer = shell.querySelector('[data-project-drawer-layer]');
        const drawer = shell.querySelector('[data-project-drawer]');
        const sortableInstances = [];
        let activeCard = null;
        let dragging = false;

        const setDrawerText = (selector, value) => {
            const element = drawer?.querySelector(selector);
            if (element) element.textContent = value;
        };

        function renderDrawer(card) {
            if (!drawer) return;
            setDrawerText('[data-project-drawer-key]', `PRJ-${card.dataset.projectId}`);
            setDrawerText('[data-project-drawer-title]', card.dataset.title || 'Proje');
            setDrawerText('[data-project-drawer-department]', card.dataset.department || 'Departman yok');
            setDrawerText('[data-project-drawer-manager]', card.dataset.manager || 'Sorumlu atanmamış');
            setDrawerText('[data-project-drawer-dates]', `${card.dataset.start} — ${card.dataset.end}`);
            setDrawerText('[data-project-drawer-tasks]', card.dataset.taskProgress || '0 / 0');
            setDrawerText('[data-project-drawer-members]', card.dataset.members || 'Ekip üyesi yok');
            setDrawerText('[data-project-drawer-description]', card.dataset.description || 'Açıklama eklenmemiş.');
            setDrawerText('[data-project-drawer-progress]', `${card.dataset.progress || 0}%`);

            const status = drawer.querySelector('[data-project-drawer-status]');
            if (status) {
                status.textContent = card.dataset.statusLabel || statusLabels[card.dataset.status] || '';
                status.className = `is-${(card.dataset.status || 'NotStarted').toLowerCase()}`;
            }
            const bar = drawer.querySelector('[data-project-drawer-progress-bar]');
            if (bar) bar.style.width = `${card.dataset.progress || 0}%`;
            const details = drawer.querySelector('[data-project-drawer-details]');
            if (details) details.href = card.dataset.detailsUrl || '#';
            const pipeline = drawer.querySelector('[data-project-drawer-pipeline]');
            if (pipeline) pipeline.href = card.dataset.pipelineUrl || '#';
        }

        function openDrawer(card) {
            if (!drawerLayer || !drawer || dragging) return;
            activeCard?.classList.remove('is-selected');
            activeCard = card;
            activeCard.classList.add('is-selected');
            renderDrawer(card);
            drawerLayer.hidden = false;
            drawer.setAttribute('aria-hidden', 'false');
            document.body.classList.add('task-board-drawer-open');
            requestAnimationFrame(() => drawerLayer.classList.add('is-open'));
            drawer.querySelector('[data-project-drawer-close]')?.focus();
        }

        function closeDrawer() {
            if (!drawerLayer || !drawer) return;
            drawerLayer.classList.remove('is-open');
            drawer.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('task-board-drawer-open');
            activeCard?.classList.remove('is-selected');
            activeCard = null;
            window.setTimeout(() => { drawerLayer.hidden = true; }, 180);
        }

        shell.querySelectorAll('.project-kanban-card').forEach(card => {
            card.addEventListener('click', event => {
                if (dragging) return;
                const interactive = event.target.closest('a,button');
                if (interactive && !interactive.matches('[data-project-open-detail]')) return;
                event.preventDefault();
                openDrawer(card);
            });
        });
        shell.querySelectorAll('[data-project-drawer-close]').forEach(button => button.addEventListener('click', closeDrawer));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && drawerLayer && !drawerLayer.hidden) closeDrawer();
        });

        function refreshColumns() {
            lists.forEach(list => {
                const count = list.querySelectorAll('.project-kanban-card').length;
                const counter = shell.querySelector(`[data-project-counter="${list.dataset.projectStatus}"]`);
                if (counter) counter.textContent = count;
                const empty = list.querySelector('[data-project-empty]');
                if (empty) empty.hidden = count > 0;
            });
        }

        async function saveStatus(card, newStatus, oldStatus) {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            feedback.textContent = 'Proje durumu kaydediliyor…';
            sortableInstances.forEach(instance => instance.option('disabled', true));
            try {
                const response = await fetch(shell.dataset.updateUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
                    body: JSON.stringify({ projectId: Number(card.dataset.projectId), newStatus })
                });
                const payload = await response.json().catch(() => ({}));
                if (!response.ok || !payload.success) throw new Error(payload.message || 'Proje durumu güncellenemedi.');
                card.dataset.status = newStatus;
                card.dataset.statusLabel = statusLabels[newStatus];
                feedback.textContent = `Proje “${statusLabels[newStatus]}” durumuna taşındı.`;
                if (activeCard === card) renderDrawer(card);
                return true;
            } catch (error) {
                card.dataset.status = oldStatus;
                card.dataset.statusLabel = statusLabels[oldStatus];
                feedback.textContent = error.message || 'İşlem doğrulanamadı.';
                return false;
            } finally {
                sortableInstances.forEach(instance => instance.option('disabled', false));
            }
        }

        if (typeof Sortable !== 'undefined' && shell.querySelector('[data-can-move="true"]')) {
            lists.forEach(list => sortableInstances.push(new Sortable(list, {
                group: 'project-kanban',
                draggable: '.project-kanban-card[data-can-move="true"]',
                animation: 160,
                ghostClass: 'task-board-card-ghost',
                chosenClass: 'task-board-card-chosen',
                dragClass: 'task-board-card-dragging',
                onStart() { dragging = true; },
                async onEnd(event) {
                    const newStatus = event.to.dataset.projectStatus;
                    const oldStatus = event.from.dataset.projectStatus;
                    if (newStatus === oldStatus) {
                        dragging = false;
                        return;
                    }
                    const saved = await saveStatus(event.item, newStatus, oldStatus);
                    if (!saved) {
                        const siblings = [...event.from.children].filter(item => item !== event.item && item.classList.contains('project-kanban-card'));
                        event.from.insertBefore(event.item, siblings[event.oldIndex] || null);
                    }
                    refreshColumns();
                    window.setTimeout(() => { dragging = false; }, 0);
                }
            })));
        }

        refreshColumns();
    });
})();
