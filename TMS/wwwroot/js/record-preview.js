(() => {
    const layer = document.createElement('div');
    layer.className = 'record-preview-layer';
    layer.hidden = true;
    layer.innerHTML = `
        <button type="button" class="record-preview-backdrop" data-preview-close aria-label="Ön izlemeyi kapat"></button>
        <aside class="record-preview-drawer" data-preview-drawer aria-hidden="true" aria-labelledby="recordPreviewTitle">
            <header><div><span data-preview-type></span><strong data-preview-key></strong></div><button type="button" data-preview-close aria-label="Kapat"><i class="bi bi-x-lg"></i></button></header>
            <div class="record-preview-body">
                <span class="record-preview-status" data-preview-status></span>
                <h2 id="recordPreviewTitle" data-preview-title></h2>
                <p class="record-preview-subtitle" data-preview-subtitle></p>
                <dl class="record-preview-meta" data-preview-meta></dl>
                <section><h3>Açıklama</h3><p data-preview-description></p></section>
                <button type="button" class="record-preview-related" data-preview-related hidden><i class="bi bi-arrow-left-circle"></i><span><small>İlişkili kayıt</small><strong data-preview-related-title></strong></span><i class="bi bi-chevron-left"></i></button>
            </div>
            <footer><a href="#" class="project-action-button project-action-secondary" data-preview-secondary hidden></a><a href="#" class="project-action-button project-action-primary" data-preview-detail>Detay sayfasını aç <i class="bi bi-arrow-right"></i></a></footer>
        </aside>
        <aside class="record-preview-drawer record-preview-drawer-secondary" data-preview-secondary-drawer aria-hidden="true">
            <header><div><span>İLİŞKİLİ KAYIT</span><strong data-related-key></strong></div><button type="button" data-related-close aria-label="Kapat"><i class="bi bi-x-lg"></i></button></header>
            <div class="record-preview-body"><span class="record-preview-status">Proje</span><h2 data-related-title></h2><p class="record-preview-subtitle" data-related-subtitle></p></div>
            <footer><a href="#" class="project-action-button project-action-primary" data-related-detail>Projeyi aç <i class="bi bi-arrow-right"></i></a></footer>
        </aside>`;
    document.body.append(layer);

    const drawer = layer.querySelector('[data-preview-drawer]');
    const secondaryDrawer = layer.querySelector('[data-preview-secondary-drawer]');
    let activeTrigger = null;

    const put = (selector, value) => {
        const target = layer.querySelector(selector);
        if (target) target.textContent = value || '—';
    };

    function render(trigger) {
        const data = trigger.dataset;
        put('[data-preview-type]', (data.previewType || 'Kayıt').toUpperCase());
        put('[data-preview-key]', data.previewKey);
        put('[data-preview-title]', data.previewTitle);
        put('[data-preview-status]', data.previewStatus);
        put('[data-preview-subtitle]', data.previewSubtitle);
        put('[data-preview-description]', data.previewDescription || 'Açıklama eklenmemiş.');
        const meta = layer.querySelector('[data-preview-meta]');
        meta.innerHTML = '';
        for (let index = 1; index <= 4; index++) {
            const label = trigger.getAttribute(`data-preview-meta-${index}-label`);
            if (!label) continue;
            const item = document.createElement('div');
            const dt = document.createElement('dt');
            const dd = document.createElement('dd');
            dt.textContent = label;
            dd.textContent = trigger.getAttribute(`data-preview-meta-${index}-value`) || '—';
            item.append(dt, dd);
            meta.append(item);
        }
        const detail = layer.querySelector('[data-preview-detail]');
        detail.href = data.previewDetailUrl || trigger.href || '#';
        const extra = layer.querySelector('[data-preview-secondary]');
        extra.hidden = !data.previewSecondaryUrl;
        if (data.previewSecondaryUrl) {
            extra.href = data.previewSecondaryUrl;
            extra.textContent = data.previewSecondaryLabel || 'İlişkili görünüm';
        }
        const related = layer.querySelector('[data-preview-related]');
        related.hidden = !data.previewRelatedUrl;
        put('[data-preview-related-title]', data.previewRelatedTitle);
        related.dataset.url = data.previewRelatedUrl || '';
        related.dataset.key = data.previewRelatedKey || '';
        related.dataset.title = data.previewRelatedTitle || '';
        related.dataset.subtitle = data.previewRelatedSubtitle || '';
    }

    function open(trigger) {
        activeTrigger = trigger;
        render(trigger);
        layer.hidden = false;
        drawer.setAttribute('aria-hidden', 'false');
        document.body.classList.add('record-preview-open');
        requestAnimationFrame(() => layer.classList.add('is-open'));
        drawer.querySelector('[data-preview-close]')?.focus();
    }

    function closeSecondary() {
        layer.classList.remove('secondary-open');
        secondaryDrawer.setAttribute('aria-hidden', 'true');
    }

    function close() {
        layer.classList.remove('is-open');
        closeSecondary();
        drawer.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('record-preview-open');
        window.setTimeout(() => { layer.hidden = true; activeTrigger?.focus(); }, 190);
    }

    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-record-preview-trigger]');
        if (!trigger) return;
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.button === 1) return;
        event.preventDefault();
        open(trigger);
    });
    layer.querySelectorAll('[data-preview-close]').forEach(button => button.addEventListener('click', close));
    layer.querySelector('[data-related-close]').addEventListener('click', closeSecondary);
    layer.querySelector('[data-preview-related]').addEventListener('click', event => {
        const source = event.currentTarget;
        put('[data-related-key]', source.dataset.key);
        put('[data-related-title]', source.dataset.title);
        put('[data-related-subtitle]', source.dataset.subtitle);
        layer.querySelector('[data-related-detail]').href = source.dataset.url || '#';
        secondaryDrawer.setAttribute('aria-hidden', 'false');
        layer.classList.add('secondary-open');
        secondaryDrawer.querySelector('[data-related-close]')?.focus();
    });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape' || layer.hidden) return;
        if (layer.classList.contains('secondary-open')) closeSecondary(); else close();
    });
})();
