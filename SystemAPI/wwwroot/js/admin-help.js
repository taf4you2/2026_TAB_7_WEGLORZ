const HELP_URL = './help/admin-help.pl.json';
const ENABLED_KEY = 'admin_help_enabled';
const OVERRIDES_KEY = 'admin_help_overrides_v1';

let helpData = null;
let currentSection = 'dashboard';
let editMode = false;
let tooltipTimer = null;
let tooltipElement = null;
let activeTarget = null;

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

function readOverrides() {
    try {
        return JSON.parse(localStorage.getItem(OVERRIDES_KEY) || '{}');
    } catch {
        return {};
    }
}

function writeOverrides(overrides) {
    localStorage.setItem(OVERRIDES_KEY, JSON.stringify(overrides));
}

function itemWithOverride(item) {
    return { ...item, ...(readOverrides()[item.id] || {}) };
}

function getItem(id) {
    const item = helpData?.items?.find(entry => entry.id === id);
    return item ? itemWithOverride(item) : null;
}

function isEnabled() {
    return localStorage.getItem(ENABLED_KEY) !== '0';
}

function ensureTooltip() {
    if (tooltipElement) return tooltipElement;
    tooltipElement = document.createElement('div');
    tooltipElement.id = 'admin-help-tooltip';
    tooltipElement.className = 'admin-help-tooltip';
    tooltipElement.setAttribute('role', 'tooltip');
    document.body.appendChild(tooltipElement);
    return tooltipElement;
}

function positionTooltip(target) {
    const tooltip = ensureTooltip();
    const rect = target.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    const gap = 10;
    let left = rect.right + gap;
    let top = rect.top;

    if (left + tooltipRect.width > window.innerWidth - gap) {
        left = Math.max(gap, rect.left - tooltipRect.width - gap);
    }
    if (top + tooltipRect.height > window.innerHeight - gap) {
        top = Math.max(gap, window.innerHeight - tooltipRect.height - gap);
    }

    tooltip.style.left = `${left}px`;
    tooltip.style.top = `${top}px`;
}

function showTooltip(target) {
    if (!isEnabled() || !helpData) return;
    const item = getItem(target.dataset.helpId);
    if (!item) return;

    activeTarget = target;
    const tooltip = ensureTooltip();
    tooltip.innerHTML = `
        <strong>${escapeHtml(item.title)}</strong>
        <span>${escapeHtml(item.tooltip)}</span>
        <small>Kliknij „Pomoc”, aby przeczytać pełną instrukcję.</small>
    `;
    target.setAttribute('aria-describedby', tooltip.id);
    positionTooltip(target);
    tooltip.classList.add('visible');
}

function hideTooltip() {
    clearTimeout(tooltipTimer);
    tooltipTimer = null;
    if (activeTarget) activeTarget.removeAttribute('aria-describedby');
    activeTarget = null;
    tooltipElement?.classList.remove('visible');
}

function scheduleTooltip(target, delay = 450) {
    hideTooltip();
    tooltipTimer = setTimeout(() => showTooltip(target), delay);
}

function bindTooltipEvents() {
    document.addEventListener('mouseover', event => {
        const target = event.target.closest('[data-help-id]');
        if (target) scheduleTooltip(target);
    });
    document.addEventListener('mouseout', event => {
        const target = event.target.closest('[data-help-id]');
        if (target && !target.contains(event.relatedTarget)) hideTooltip();
    });
    document.addEventListener('focusin', event => {
        const target = event.target.closest('[data-help-id]');
        if (target) scheduleTooltip(target, 0);
    });
    document.addEventListener('focusout', event => {
        if (event.target.closest('[data-help-id]')) hideTooltip();
    });
    document.addEventListener('click', event => {
        if (event.target.closest('[data-help-id]')) hideTooltip();
    });
    window.addEventListener('scroll', hideTooltip, true);
    window.addEventListener('resize', hideTooltip);
}

function itemsForSection(section) {
    if (!helpData) return [];
    return helpData.items
        .filter(item => item.section === section)
        .map(itemWithOverride)
        .sort((a, b) => (a.pdfOrder || 0) - (b.pdfOrder || 0));
}

function renderReadEntry(item) {
    const steps = item.steps?.length
        ? `<ol>${item.steps.map(step => `<li>${escapeHtml(step)}</li>`).join('')}</ol>`
        : '';
    const warnings = item.warnings?.length
        ? `<div class="admin-help-warning"><strong>Uwaga:</strong><ul>${item.warnings.map(warning => `<li>${escapeHtml(warning)}</li>`).join('')}</ul></div>`
        : '';

    return `
        <article class="admin-help-entry" data-help-entry="${escapeHtml(item.id)}">
            <h3>${escapeHtml(item.title)}</h3>
            <p>${escapeHtml(item.description)}</p>
            ${steps}
            ${warnings}
        </article>
    `;
}

function renderEditEntry(item) {
    return `
        <article class="admin-help-entry" data-help-entry="${escapeHtml(item.id)}">
            <div class="admin-help-edit-grid">
                <label>Tytuł</label>
                <input data-help-field="title" value="${escapeHtml(item.title)}">
                <label>Krótki tooltip</label>
                <textarea data-help-field="tooltip">${escapeHtml(item.tooltip)}</textarea>
                <label>Opis do instrukcji / PDF</label>
                <textarea data-help-field="description">${escapeHtml(item.description)}</textarea>
                <label>Kroki — jeden w wierszu</label>
                <textarea data-help-field="steps">${escapeHtml((item.steps || []).join('\n'))}</textarea>
                <label>Ostrzeżenia — jedno w wierszu</label>
                <textarea data-help-field="warnings">${escapeHtml((item.warnings || []).join('\n'))}</textarea>
            </div>
            <div class="admin-help-edit-actions">
                <button class="btn btn-primary" type="button" data-help-save="${escapeHtml(item.id)}">Zapisz ten wpis</button>
            </div>
        </article>
    `;
}

function renderPanel() {
    if (!helpData) return;
    const content = document.getElementById('admin-help-content');
    const section = helpData.sections[currentSection] || helpData.sections.global;
    const entries = itemsForSection(currentSection);

    document.getElementById('admin-help-panel-title').textContent = section.title;
    document.getElementById('admin-help-section-select').value = currentSection;
    document.getElementById('admin-help-panel-subtitle').textContent = editMode
        ? 'Tryb edycji: zmiany są zapisywane lokalnie w tej przeglądarce.'
        : helpData.intro;
    document.getElementById('admin-help-edit-button').classList.toggle('active', editMode);
    content.innerHTML = `
        <div class="admin-help-section-intro">${escapeHtml(section.description)}</div>
        ${entries.length
            ? entries.map(item => editMode ? renderEditEntry(item) : renderReadEntry(item)).join('')
            : '<div class="admin-help-empty">Brak wpisów dla tej sekcji.</div>'}
    `;
}

function saveEntry(id, article) {
    const lineList = value => value.split('\n').map(line => line.trim()).filter(Boolean);
    const field = name => article.querySelector(`[data-help-field="${name}"]`).value.trim();
    const overrides = readOverrides();
    overrides[id] = {
        title: field('title'),
        tooltip: field('tooltip'),
        description: field('description'),
        steps: lineList(field('steps')),
        warnings: lineList(field('warnings'))
    };
    writeOverrides(overrides);
    renderPanel();
}

function openPanel(section = currentSection) {
    currentSection = section || currentSection;
    renderPanel();
    document.getElementById('admin-help-panel').classList.add('open');
    document.getElementById('admin-help-backdrop').classList.add('open');
    document.getElementById('admin-help-panel').setAttribute('aria-hidden', 'false');
    document.getElementById('admin-help-close').focus();
}

function closePanel() {
    document.getElementById('admin-help-panel').classList.remove('open');
    document.getElementById('admin-help-backdrop').classList.remove('open');
    document.getElementById('admin-help-panel').setAttribute('aria-hidden', 'true');
}

function exportJson() {
    const merged = { ...helpData, items: helpData.items.map(itemWithOverride) };
    const blob = new Blob([JSON.stringify(merged, null, 2)], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'admin-help.pl.edited.json';
    link.click();
    URL.revokeObjectURL(url);
}

function exportText() {
    const mergedItems = helpData.items
        .map(itemWithOverride)
        .sort((a, b) => (a.pdfOrder || 0) - (b.pdfOrder || 0));
    const lines = [`# ${helpData.title}`, '', helpData.intro, ''];

    for (const [sectionId, section] of Object.entries(helpData.sections)) {
        const entries = mergedItems.filter(item => item.section === sectionId);
        if (!entries.length) continue;

        lines.push(`## ${section.title}`, '', section.description, '');
        for (const item of entries) {
            lines.push(`### ${item.title}`, '', item.description, '');
            if (item.steps?.length) {
                item.steps.forEach((step, index) => lines.push(`${index + 1}. ${step}`));
                lines.push('');
            }
            if (item.warnings?.length) {
                lines.push('**Uwagi:**', '');
                item.warnings.forEach(warning => lines.push(`- ${warning}`));
                lines.push('');
            }
        }
    }

    const blob = new Blob([lines.join('\n')], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'Instrukcja_panelu_administratora.md';
    link.click();
    URL.revokeObjectURL(url);
}

function resetOverrides() {
    if (!confirm('Usunąć wszystkie lokalne zmiany treści instrukcji?')) return;
    localStorage.removeItem(OVERRIDES_KEY);
    renderPanel();
}

function setEnabled(enabled) {
    localStorage.setItem(ENABLED_KEY, enabled ? '1' : '0');
    document.getElementById('admin-help-enabled').checked = enabled;
    document.querySelectorAll('[data-help-id]').forEach(element => {
        element.classList.toggle('admin-help-target', enabled);
    });
    if (!enabled) hideTooltip();
}

function refreshTargets() {
    const enabled = isEnabled();
    document.querySelectorAll('[data-help-id]').forEach(element => {
        element.classList.toggle('admin-help-target', enabled);
    });
}

function bindPanelEvents() {
    document.getElementById('admin-help-open').addEventListener('click', () => openPanel(currentSection));
    document.getElementById('admin-help-close').addEventListener('click', closePanel);
    document.getElementById('admin-help-backdrop').addEventListener('click', closePanel);
    document.getElementById('admin-help-enabled').addEventListener('change', event => setEnabled(event.target.checked));
    document.getElementById('admin-help-section-select').addEventListener('change', event => {
        currentSection = event.target.value;
        renderPanel();
    });
    document.getElementById('admin-help-edit-button').addEventListener('click', () => {
        editMode = !editMode;
        renderPanel();
    });
    document.getElementById('admin-help-export-button').addEventListener('click', exportJson);
    document.getElementById('admin-help-export-text-button').addEventListener('click', exportText);
    document.getElementById('admin-help-reset-button').addEventListener('click', resetOverrides);
    document.getElementById('admin-help-content').addEventListener('click', event => {
        const button = event.target.closest('[data-help-save]');
        if (button) saveEntry(button.dataset.helpSave, button.closest('[data-help-entry]'));
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            hideTooltip();
            closePanel();
        }
    });
}

export async function initAdminHelp() {
    try {
        const response = await fetch(HELP_URL, { cache: 'no-store' });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        helpData = await response.json();
        const sectionSelect = document.getElementById('admin-help-section-select');
        sectionSelect.innerHTML = Object.entries(helpData.sections)
            .map(([id, section]) => `<option value="${escapeHtml(id)}">${escapeHtml(section.title)}</option>`)
            .join('');
        bindTooltipEvents();
        bindPanelEvents();
        setEnabled(isEnabled());
        renderPanel();
        new MutationObserver(refreshTargets).observe(document.body, { childList: true, subtree: true });
    } catch (error) {
        console.error('Nie udało się załadować instrukcji administratora.', error);
        document.getElementById('admin-help-open').disabled = true;
    }
}

export function setAdminHelpSection(section) {
    currentSection = section || 'dashboard';
    if (document.getElementById('admin-help-panel')?.classList.contains('open')) renderPanel();
}
