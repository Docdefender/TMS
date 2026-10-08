(() => {
    const dayMs = 24 * 60 * 60 * 1000;
    const taskTriggers = document.querySelectorAll('[data-timeline-task]');
    if (!taskTriggers.length) return;

    const layer = document.querySelector('[data-timeline-inspector-layer]');
    const inspector = layer?.querySelector('.timeline-inspector');
    const closeButtons = layer?.querySelectorAll('[data-timeline-inspector-close]') ?? [];
    let lastTrigger = null;
    let saving = false;

    const text = (selector, value) => {
        const target = layer?.querySelector(selector);
        if (target) target.textContent = value;
    };

    function openInspector(trigger) {
        if (!layer || !inspector) return;
        lastTrigger = trigger;
        const checkpoint = trigger.dataset.checkpoint;
        const location = checkpoint ? `${trigger.dataset.stage} · ${checkpoint}` : trigger.dataset.stage;
        const description = trigger.dataset.description?.trim();
        const dependencies = trigger.dataset.dependencies?.trim();
        const progress = Math.min(100, Math.max(0, Number(trigger.dataset.progress) || 0));

        text('[data-inspector-key]', trigger.dataset.taskKey || 'Görev');
        text('[data-inspector-status]', trigger.dataset.statusLabel || '—');
        text('[data-inspector-title]', trigger.dataset.title || 'Görev');
        text('[data-inspector-location]', location || 'Aşama bekliyor');
        text('[data-inspector-priority]', trigger.dataset.priorityLabel || 'Normal');
        text('[data-inspector-assignee]', trigger.dataset.assignee || 'Atanmamış');
        text('[data-inspector-start]', trigger.dataset.startLabel || 'Planlanmadı');
        text('[data-inspector-end]', trigger.dataset.endLabel || 'Planlanmadı');
        text('[data-inspector-progress-label]', `%${progress}`);
        text('[data-inspector-description]', description || 'Bu görev için açıklama eklenmemiş.');
        text('[data-inspector-dependencies]', dependencies || 'Bekleyen ön koşul yok.');

        const progressBar = layer.querySelector('[data-inspector-progress]');
        if (progressBar) progressBar.style.width = `${progress}%`;
        const dependenciesWrap = layer.querySelector('[data-inspector-dependencies-wrap]');
        if (dependenciesWrap) dependenciesWrap.hidden = !dependencies;
        const detailsLink = layer.querySelector('[data-inspector-link]');
        if (detailsLink) detailsLink.href = trigger.dataset.detailsUrl || '#';

        layer.hidden = false;
        document.body.classList.add('has-open-drawer');
        requestAnimationFrame(() => layer.querySelector('[data-timeline-inspector-close]')?.focus());
    }

    function closeInspector() {
        if (!layer || layer.hidden) return;
        layer.hidden = true;
        document.body.classList.remove('has-open-drawer');
        lastTrigger?.focus();
    }

    for (const button of closeButtons) button.addEventListener('click', closeInspector);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && layer && !layer.hidden) closeInspector();
    });

    function drawDependencies() {
        const sheet = document.querySelector('.timeline-sheet');
        if (!sheet) return;
        sheet.querySelector('.timeline-dependency-layer')?.remove();
        const connections = [];

        for (const target of taskTriggers) {
            if (!target.classList.contains('timeline-task-bar') || !target.dataset.prerequisiteIds) continue;
            for (const dependency of target.dataset.prerequisiteIds.split(',')) {
                const [sourceId, completed] = dependency.split(':');
                const source = sheet.querySelector(`.timeline-task-bar[data-record-id="${sourceId}"]`);
                if (source) connections.push({ source, target, completed: completed === '1' });
            }
        }
        if (!connections.length) return;

        const namespace = 'http://www.w3.org/2000/svg';
        const svg = document.createElementNS(namespace, 'svg');
        svg.classList.add('timeline-dependency-layer');
        svg.setAttribute('width', String(sheet.scrollWidth));
        svg.setAttribute('height', String(sheet.scrollHeight));
        svg.setAttribute('viewBox', `0 0 ${sheet.scrollWidth} ${sheet.scrollHeight}`);
        svg.setAttribute('aria-hidden', 'true');

        const defs = document.createElementNS(namespace, 'defs');
        for (const type of ['pending', 'complete']) {
            const marker = document.createElementNS(namespace, 'marker');
            marker.setAttribute('id', `timeline-arrow-${type}`);
            marker.setAttribute('viewBox', '0 0 8 8');
            marker.setAttribute('refX', '7');
            marker.setAttribute('refY', '4');
            marker.setAttribute('markerWidth', '5');
            marker.setAttribute('markerHeight', '5');
            marker.setAttribute('orient', 'auto-start-reverse');
            const arrow = document.createElementNS(namespace, 'path');
            arrow.setAttribute('d', 'M 0 0 L 8 4 L 0 8 z');
            arrow.setAttribute('fill', type === 'complete' ? 'var(--color-success)' : 'var(--color-warning)');
            marker.appendChild(arrow);
            defs.appendChild(marker);
        }
        svg.appendChild(defs);

        const sheetRect = sheet.getBoundingClientRect();
        for (const connection of connections) {
            const sourceRect = connection.source.getBoundingClientRect();
            const targetRect = connection.target.getBoundingClientRect();
            const x1 = sourceRect.right - sheetRect.left;
            const y1 = sourceRect.top - sheetRect.top + sourceRect.height / 2;
            const x2 = targetRect.left - sheetRect.left - 4;
            const y2 = targetRect.top - sheetRect.top + targetRect.height / 2;
            const bendX = x2 > x1 + 32 ? (x1 + x2) / 2 : Math.max(x1, x2) + 22;

            const path = document.createElementNS(namespace, 'path');
            path.setAttribute('d', `M ${x1} ${y1} C ${bendX} ${y1}, ${bendX} ${y2}, ${x2} ${y2}`);
            path.setAttribute('class', `timeline-dependency-line${connection.completed ? ' is-complete' : ''}`);
            path.setAttribute('marker-end', `url(#timeline-arrow-${connection.completed ? 'complete' : 'pending'})`);
            svg.appendChild(path);

            const origin = document.createElementNS(namespace, 'circle');
            origin.setAttribute('cx', String(x1));
            origin.setAttribute('cy', String(y1));
            origin.setAttribute('r', '2.5');
            origin.setAttribute('class', `timeline-dependency-origin${connection.completed ? ' is-complete' : ''}`);
            svg.appendChild(origin);
        }
        sheet.prepend(svg);
    }

    drawDependencies();
    let dependencyResizeFrame = 0;
    window.addEventListener('resize', () => {
        cancelAnimationFrame(dependencyResizeFrame);
        dependencyResizeFrame = requestAnimationFrame(drawDependencies);
    });

    function shiftDate(value, days) {
        const date = new Date(`${value}T00:00:00Z`);
        return new Date(date.getTime() + days * dayMs).toISOString().slice(0, 10);
    }

    function allowedDelta(bar, delta) {
        const left = Number(bar.dataset.left);
        const canvasWidth = Number(bar.dataset.width);
        const pixelsPerDay = Number(bar.dataset.pixelsPerDay);
        const barWidth = bar.getBoundingClientRect().width;
        const earliest = Math.ceil(-left / pixelsPerDay);
        const latest = Math.floor((canvasWidth - left - barWidth) / pixelsPerDay);
        return Math.min(latest, Math.max(earliest, delta));
    }

    function preview(bar, delta) {
        const pixelsPerDay = Number(bar.dataset.pixelsPerDay);
        bar.style.transform = `translateX(${delta * pixelsPerDay}px)`;
        const start = shiftDate(bar.dataset.start, delta);
        const end = shiftDate(bar.dataset.end, delta);
        bar.title = `${start} – ${end}`;
    }

    function save(bar, delta) {
        if (!delta || saving) {
            bar.style.transform = '';
            return;
        }
        const form = document.querySelector(`.timeline-schedule-form[data-task-id="${bar.dataset.taskId}"]`);
        if (!form) {
            preview(bar, 0);
            return;
        }
        saving = true;
        bar.setAttribute('aria-busy', 'true');
        form.querySelector('[name="plannedStartDate"]').value = shiftDate(bar.dataset.start, delta);
        form.querySelector('[name="plannedEndDate"]').value = shiftDate(bar.dataset.end, delta);
        form.requestSubmit();
    }

    for (const bar of taskTriggers) {
        if (!bar.classList.contains('is-draggable')) {
            bar.addEventListener('click', () => openInspector(bar));
            bar.addEventListener('keydown', event => {
                if (event.key !== 'Enter' && event.key !== ' ') return;
                event.preventDefault();
                openInspector(bar);
            });
            continue;
        }

        let drag = null;
        const originalTitle = bar.title;

        bar.addEventListener('pointerdown', event => {
            if (saving || event.button !== 0) return;
            bar.focus();
            drag = { pointerId: event.pointerId, startX: event.clientX, delta: 0 };
            bar.setPointerCapture(event.pointerId);
            bar.classList.add('is-dragging');
            event.preventDefault();
        });

        bar.addEventListener('pointermove', event => {
            if (!drag || drag.pointerId !== event.pointerId) return;
            const pixelsPerDay = Number(bar.dataset.pixelsPerDay);
            const delta = allowedDelta(bar, Math.round((event.clientX - drag.startX) / pixelsPerDay));
            if (delta !== drag.delta) {
                drag.delta = delta;
                preview(bar, delta);
            }
        });

        bar.addEventListener('pointerup', event => {
            if (!drag || drag.pointerId !== event.pointerId) return;
            const delta = drag.delta;
            drag = null;
            bar.classList.remove('is-dragging');
            if (!delta) {
                bar.title = originalTitle;
                openInspector(bar);
                return;
            }
            save(bar, delta);
        });

        bar.addEventListener('pointercancel', () => {
            drag = null;
            bar.classList.remove('is-dragging');
            preview(bar, 0);
            bar.title = originalTitle;
        });

        bar.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openInspector(bar);
                return;
            }
            if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
            event.preventDefault();
            const step = event.shiftKey ? 7 : 1;
            const delta = allowedDelta(bar, event.key === 'ArrowRight' ? step : -step);
            save(bar, delta);
        });
    }
})();
