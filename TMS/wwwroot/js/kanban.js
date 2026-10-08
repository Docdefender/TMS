(() => {
    const statusLabels = {
        ToDo: 'Yapılacak',
        InProgress: 'Devam Ediyor',
        InReview: 'Gözden Geçiriliyor',
        Done: 'Tamamlandı'
    };

    document.querySelectorAll('[data-task-board]').forEach(initBoard);

    function initBoard(shell) {
        const board = shell.querySelector('.task-board');
        const lists = [...shell.querySelectorAll('[data-task-status]')];
        const feedback = shell.querySelector('[data-board-feedback]');
        const filters = [...shell.querySelectorAll('[data-board-filter]')];
        const clearButton = shell.querySelector('[data-board-clear]');
        const result = shell.querySelector('[data-board-result]');
        const drawerLayer = shell.querySelector('[data-board-drawer-layer]');
        const drawer = shell.querySelector('[data-board-drawer]');
        const storageKey = `dizge-board:${shell.dataset.taskBoard}`;
        const instances = [];
        let activeCard = null;
        let dragging = false;

        function getFilters() {
            return Object.fromEntries(filters.map(input => [input.dataset.boardFilter, input.value.trim()]));
        }

        function isDueMatch(card, due) {
            if (!due) return true;
            const date = card.dataset.due;
            if (due === 'nodate') return !date;
            if (!date) return false;
            const today = shell.dataset.today;
            if (due === 'overdue') return card.dataset.status !== 'Done' && date < today;
            if (due === 'week') {
                const limit = new Date(`${today}T00:00:00Z`);
                limit.setUTCDate(limit.getUTCDate() + 7);
                return card.dataset.status !== 'Done' && date >= today && date <= limit.toISOString().slice(0, 10);
            }
            return true;
        }

        function applyFilters() {
            const values = getFilters();
            const term = (values.search || '').toLocaleLowerCase('tr-TR');
            let visible = 0;
            shell.querySelectorAll('.task-board-card').forEach(card => {
                const matches = (!term || card.dataset.search.toLocaleLowerCase('tr-TR').includes(term))
                    && (!values.project || card.dataset.project === values.project)
                    && (!values.assignee || card.dataset.assignee === values.assignee)
                    && (!values.priority || card.dataset.priority === values.priority)
                    && (!values.label || card.dataset.labels.includes(`|${values.label}|`))
                    && isDueMatch(card, values.due);
                card.hidden = !matches;
                if (matches) visible++;
            });

            lists.forEach(list => {
                const visibleCards = [...list.querySelectorAll('.task-board-card')].filter(card => !card.hidden);
                const counter = shell.querySelector(`[data-board-counter="${list.dataset.taskStatus}"]`);
                if (counter) counter.textContent = visibleCards.length;
                const empty = list.querySelector('[data-board-empty]');
                if (empty) empty.hidden = visibleCards.length > 0;
            });

            if (result) result.textContent = `${visible} görev`;
            if (clearButton) clearButton.hidden = !Object.values(values).some(Boolean);
            const activeSummary = document.querySelector('[data-board-active-summary]');
            if (activeSummary) {
                activeSummary.textContent = [...shell.querySelectorAll('.task-board-card')]
                    .filter(card => !card.hidden && card.dataset.status !== 'Done').length;
            }
            sessionStorage.setItem(storageKey, JSON.stringify(values));
        }

        try {
            const saved = JSON.parse(sessionStorage.getItem(storageKey) || '{}');
            filters.forEach(input => {
                if (typeof saved[input.dataset.boardFilter] === 'string')
                    input.value = saved[input.dataset.boardFilter];
            });
        } catch { /* Ignore stale filter state. */ }

        filters.forEach(input => input.addEventListener(input.type === 'search' ? 'input' : 'change', applyFilters));
        clearButton?.addEventListener('click', () => {
            filters.forEach(input => { input.value = ''; });
            applyFilters();
        });

        function getDateState(card) {
            const due = card.dataset.due;
            if (!due || card.dataset.status === 'Done') return '';
            if (due < shell.dataset.today) return 'overdue';
            const limit = new Date(`${shell.dataset.today}T00:00:00Z`);
            limit.setUTCDate(limit.getUTCDate() + 7);
            return due <= limit.toISOString().slice(0, 10) ? 'soon' : '';
        }

        function renderDrawer(card) {
            if (!drawer) return;
            const setText = (selector, value) => {
                const element = drawer.querySelector(selector);
                if (element) element.textContent = value;
            };
            setText('[data-drawer-key]', `TSK-${card.dataset.taskId}`);
            const drawerStatus = drawer.querySelector('[data-drawer-status]');
            if (drawerStatus) {
                drawerStatus.textContent = card.dataset.statusLabel || statusLabels[card.dataset.status] || '';
                drawerStatus.className = `task-board-drawer-status is-${(card.dataset.status || 'ToDo').toLowerCase()}`;
            }
            setText('[data-drawer-title]', card.dataset.title || 'Görev');
            setText('[data-drawer-project]', card.dataset.projectName || 'Projesiz');
            setText('[data-drawer-assignee]', card.dataset.assigneeName || 'Atanmamış');
            setText('[data-drawer-due]', card.dataset.dueLabel || 'Tarih yok');
            setText('[data-drawer-category]', card.dataset.category || 'Kategori yok');
            setText('[data-drawer-labels]', card.dataset.labelList || 'Etiket yok');
            setText('[data-drawer-description]', card.dataset.description || 'Açıklama eklenmemiş.');
            setText('[data-drawer-comments]', card.dataset.comments || '0');
            setText('[data-drawer-attachments]', card.dataset.attachments || '0');

            const priority = drawer.querySelector('[data-drawer-priority]');
            if (priority) {
                priority.className = `task-priority task-priority-${(card.dataset.priority || 'Normal').toLowerCase()}`;
                priority.textContent = card.dataset.priorityLabel || 'Normal';
            }

            const dateState = drawer.querySelector('[data-drawer-date-state]');
            const state = getDateState(card);
            if (dateState) {
                dateState.hidden = !state;
                dateState.className = `task-board-date-state ${state === 'overdue' ? 'is-overdue' : 'is-warning'}`;
                dateState.textContent = state === 'overdue' ? 'Gecikmiş' : 'Yaklaşıyor';
            }

            const detailsLink = drawer.querySelector('[data-drawer-details-link]');
            if (detailsLink) detailsLink.href = card.dataset.detailsUrl || '#';
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
            drawer.querySelector('[data-board-drawer-close]')?.focus();
        }

        function closeDrawer() {
            if (!drawerLayer || !drawer) return;
            drawerLayer.classList.remove('is-open');
            drawer.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('task-board-drawer-open');
            const cardToFocus = activeCard;
            activeCard?.classList.remove('is-selected');
            activeCard = null;
            window.setTimeout(() => {
                drawerLayer.hidden = true;
                cardToFocus?.focus();
            }, 180);
        }

        shell.querySelectorAll('.task-board-card').forEach(card => {
            card.addEventListener('click', event => {
                if (dragging) return;
                const interactive = event.target.closest('a, button');
                if (interactive && !interactive.matches('[data-board-open-detail]')) return;
                event.preventDefault();
                openDrawer(card);
            });
        });
        shell.querySelectorAll('[data-board-drawer-close]').forEach(button => button.addEventListener('click', closeDrawer));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && drawerLayer && !drawerLayer.hidden) closeDrawer();
        });

        async function saveStatus(card, newStatus, previousStatus) {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
            board.setAttribute('aria-busy', 'true');
            instances.forEach(instance => instance.option('disabled', true));
            feedback.textContent = 'Görev durumu kaydediliyor…';

            try {
                const response = await fetch(shell.dataset.updateUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
                    body: JSON.stringify({ taskId: Number(card.dataset.taskId), newStatus })
                });
                const payload = await response.json().catch(() => ({}));
                if (!response.ok || !payload.success)
                    throw new Error(payload.message || 'Görev durumu güncellenemedi.');

                card.dataset.status = newStatus;
                card.dataset.statusLabel = statusLabels[newStatus];
                renderDueState(card);
                if (activeCard === card) renderDrawer(card);
                feedback.textContent = `Görev “${statusLabels[newStatus]}” durumuna taşındı.`;
                applyFilters();
                return true;
            } catch (error) {
                feedback.textContent = error.message || 'İşlem doğrulanamadı. Kart eski durumuna alındı.';
                card.dataset.status = previousStatus;
                card.dataset.statusLabel = statusLabels[previousStatus];
                renderDueState(card);
                if (activeCard === card) renderDrawer(card);
                return false;
            } finally {
                board.removeAttribute('aria-busy');
                instances.forEach(instance => instance.option('disabled', false));
            }
        }

        function renderDueState(card) {
            const due = card.dataset.due;
            const open = card.dataset.status !== 'Done';
            const today = shell.dataset.today;
            const limit = new Date(`${today}T00:00:00Z`);
            limit.setUTCDate(limit.getUTCDate() + 7);
            const overdue = Boolean(open && due && due < today);
            const soon = Boolean(open && due && !overdue && due <= limit.toISOString().slice(0, 10));
            card.classList.toggle('is-overdue', overdue);
            card.querySelector('.task-board-card-counts [class~="is-overdue"]')?.classList.remove('is-overdue');
            const dueCount = [...card.querySelectorAll('.task-board-card-counts span')]
                .find(item => item.querySelector('.bi-calendar3'));
            dueCount?.classList.toggle('is-overdue', overdue);
            card.querySelector('.task-board-date-state')?.remove();
            if (overdue || soon) {
                const badge = document.createElement('span');
                badge.className = `task-board-date-state ${overdue ? 'is-overdue' : 'is-warning'}`;
                badge.textContent = overdue ? 'Gecikmiş' : 'Yaklaşıyor';
                card.querySelector('.task-board-card-topline')?.append(badge);
            }
        }

        if (typeof Sortable === 'undefined') {
            if (shell.querySelector('[data-can-move="true"]'))
                feedback.textContent = 'Kart taşıma aracı yüklenemedi. Sayfayı yenileyip tekrar deneyin.';
            applyFilters();
            return;
        }

        lists.forEach(list => instances.push(new Sortable(list, {
            group: `task-board-${shell.dataset.taskBoard}`,
            draggable: '.task-board-card[data-can-move="true"]',
            animation: 160,
            ghostClass: 'task-board-card-ghost',
            chosenClass: 'task-board-card-chosen',
            dragClass: 'task-board-card-dragging',
            onStart() {
                dragging = true;
            },
            async onEnd(event) {
                const newStatus = event.to.dataset.taskStatus;
                const previousStatus = event.from.dataset.taskStatus;
                if (newStatus === previousStatus) {
                    dragging = false;
                    return;
                }

                const saved = await saveStatus(event.item, newStatus, previousStatus);
                if (!saved) {
                    const siblings = [...event.from.children].filter(item => item !== event.item);
                    event.from.insertBefore(event.item, siblings[event.oldIndex] ?? null);
                }
                applyFilters();
                window.setTimeout(() => { dragging = false; }, 0);
            }
        })));

        applyFilters();
    }
})();
