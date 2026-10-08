(() => {
    const form = document.querySelector('[data-topbar-search]');
    const input = form?.querySelector('input[type="search"]');
    const scope = form?.dataset.searchScope;
    const endpoint = form?.dataset.suggestionsUrl;
    if (!form || !input || !scope || !endpoint) return;

    const panel = document.createElement('div');
    panel.className = 'topbar-search-results';
    panel.hidden = true;
    panel.setAttribute('role', 'listbox');
    panel.setAttribute('aria-label', 'Hızlı arama sonuçları');
    form.append(panel);

    let timer;
    let request;

    const close = () => {
        panel.hidden = true;
        panel.replaceChildren();
        input.setAttribute('aria-expanded', 'false');
    };

    const setPreviewData = (link, item) => {
        link.dataset.recordPreviewTrigger = '';
        link.dataset.previewType = item.type;
        link.dataset.previewKey = item.key;
        link.dataset.previewTitle = item.title;
        link.dataset.previewStatus = item.status;
        link.dataset.previewSubtitle = item.subtitle;
        link.dataset.previewDescription = item.description;
        link.dataset.previewDetailUrl = item.detailUrl;
        (item.meta || []).slice(0, 4).forEach((meta, index) => {
            link.setAttribute(`data-preview-meta-${index + 1}-label`, meta.label);
            link.setAttribute(`data-preview-meta-${index + 1}-value`, meta.value);
        });
        if (item.secondaryUrl) {
            link.dataset.previewSecondaryUrl = item.secondaryUrl;
            link.dataset.previewSecondaryLabel = item.secondaryLabel || 'İlişkili görünüm';
        }
        if (item.related) {
            link.dataset.previewRelatedUrl = item.related.url;
            link.dataset.previewRelatedKey = item.related.key;
            link.dataset.previewRelatedTitle = item.related.title;
            link.dataset.previewRelatedSubtitle = item.related.subtitle;
        }
    };

    const render = items => {
        panel.replaceChildren();
        const header = document.createElement('div');
        header.className = 'topbar-search-results-head';
        header.innerHTML = '<strong>Hızlı sonuçlar</strong><span>Enter ile tümünü göster</span>';
        panel.append(header);

        if (!items.length) {
            const empty = document.createElement('p');
            empty.className = 'topbar-search-empty';
            empty.textContent = 'Bu aramayla eşleşen kayıt bulunamadı.';
            panel.append(empty);
        } else {
            items.forEach(item => {
                const link = document.createElement('a');
                link.className = 'topbar-search-result';
                link.href = item.detailUrl;
                link.setAttribute('role', 'option');
                setPreviewData(link, item);

                const icon = document.createElement('span');
                icon.className = `topbar-search-result-icon is-${item.type.toLocaleLowerCase('tr-TR')}`;
                icon.innerHTML = `<i class="bi ${item.type === 'Proje' ? 'bi-folder2' : item.type === 'Görev' ? 'bi-check2-square' : 'bi-inbox'}"></i>`;
                const copy = document.createElement('span');
                copy.className = 'topbar-search-result-copy';
                const title = document.createElement('strong');
                title.textContent = item.title;
                const detail = document.createElement('small');
                detail.textContent = `${item.key} · ${item.subtitle}`;
                copy.append(title, detail);
                const status = document.createElement('em');
                status.textContent = item.status;
                link.append(icon, copy, status);
                panel.append(link);
            });
        }
        panel.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    };

    const search = async () => {
        const query = input.value.trim();
        if (query.length < 2) return close();
        request?.abort();
        request = new AbortController();
        panel.classList.add('is-loading');
        try {
            const url = new URL(endpoint, window.location.origin);
            url.searchParams.set('q', query);
            url.searchParams.set('scope', scope);
            const response = await fetch(url, { signal: request.signal, headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('Arama sonuçları alınamadı.');
            render(await response.json());
        } catch (error) {
            if (error.name !== 'AbortError') close();
        } finally {
            panel.classList.remove('is-loading');
        }
    };

    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-expanded', 'false');
    input.addEventListener('input', () => {
        window.clearTimeout(timer);
        timer = window.setTimeout(search, 220);
    });
    input.addEventListener('focus', () => {
        if (input.value.trim().length >= 2 && !panel.childElementCount) search();
        else if (panel.childElementCount) panel.hidden = false;
    });
    panel.addEventListener('click', event => {
        if (event.target.closest('[data-record-preview-trigger]')) close();
    });
    document.addEventListener('click', event => {
        if (!form.contains(event.target)) close();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && !panel.hidden) {
            close();
            input.focus();
        }
    });
})();
