import { apiFetch, showToast } from './core.js';

let lastShiftReports = [];
let lastAdminReportHistory = [];

export async function loadShiftReports() {
    const data = await apiFetch('/api/raporty/zmiany');
    if (data) {
        lastShiftReports = data;
        document.getElementById('reports-shift-tbody').innerHTML = data.map(r => `
            <tr>
                <td>${r.id}</td>
                <td style="font-weight:600;">${r.cashierLogin}</td>
                <td style="font-size:12px;">${new Date(r.startTime).toLocaleString()}</td>
                <td style="font-size:12px;">${r.endTime ? new Date(r.endTime).toLocaleString() : '<span class="badge badge-active">OTWARTA</span>'}</td>
                <td style="font-weight:700;">${r.totalRevenue?.toLocaleString() || 0} zl</td>
                <td>${r.cardsIssuedCount || 0}</td>
            </tr>
        `).join('');
        const exportButton = document.getElementById('btn-export-shifts');
        if (exportButton) exportButton.onclick = exportShiftReportsCsv;
    }
}

export async function loadAdminReportHistory() {
    const data = await apiFetch('/api/raporty/historia-admin');
    if (data) {
        lastAdminReportHistory = data;
        document.getElementById('reports-history-tbody').innerHTML = data.map(r => `
            <tr>
                <td>${r.id}</td>
                <td>${escapeHtml(r.adminLogin)}</td>
                <td><span class="badge badge-info">${formatReportType(r.reportType)}</span></td>
                <td style="font-size:12px; color:var(--text-muted);">${escapeHtml(formatReportParams(r.reportParameters))}</td>
                <td style="font-size:12px;">${new Date(r.generatedAt).toLocaleString()}</td>
            </tr>
        `).join('');
        const exportButton = document.getElementById('btn-export-admin-history');
        if (exportButton) exportButton.onclick = exportAdminReportHistoryCsv;
    }
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function formatMoney(value) {
    return `${Number(value || 0).toLocaleString()} zl`;
}

function formatReportType(type) {
    const labels = {
        sprzedaz_ogolna: 'SPRZEDAZ',
        przepustowosc_wyciagow: 'PRZEPUSTOWOSC',
        zarzadczy: 'ZARZADCZY',
        zmiana: 'ZMIANA'
    };
    return labels[type] || String(type || 'NIEZNANY').toUpperCase();
}

function formatReportParams(params) {
    if (!params) return '-';
    return params
        .split(';')
        .filter(p => p && !p.startsWith('type='))
        .join(', ') || '-';
}

function csvCell(value) {
    const text = String(value ?? '');
    return /[",\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function printThroughputReport() {
    document.body.classList.add('print-throughput');
    window.addEventListener('afterprint', () => {
        document.body.classList.remove('print-throughput');
    }, { once: true });
    window.print();
}

export function downloadCsv(filename, data) {
    const csvContent = "data:text/csv;charset=utf-8,\uFEFF" + data.map(row => row.map(csvCell).join(",")).join("\n");
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function exportShiftReportsCsv() {
    if (!lastShiftReports.length) {
        showToast('Brak danych do eksportu', 'error');
        return;
    }

    const rows = [
        ['ID', 'Kasjer', 'Otwarcie', 'Zamkniecie', 'Przychod', 'Zwroty kaucji', 'Wydane karty']
    ];
    lastShiftReports.forEach(r => rows.push([
        r.id,
        r.cashierLogin,
        r.startTime ? new Date(r.startTime).toLocaleString('pl-PL') : '',
        r.endTime ? new Date(r.endTime).toLocaleString('pl-PL') : 'OTWARTA',
        r.totalRevenue ?? 0,
        r.totalDepositReturns ?? 0,
        r.cardsIssuedCount ?? 0
    ]));
    downloadCsv('raport_zmian_kasowych.csv', rows);
}

function exportAdminReportHistoryCsv() {
    if (!lastAdminReportHistory.length) {
        showToast('Brak danych do eksportu', 'error');
        return;
    }

    const rows = [
        ['ID', 'Administrator', 'Typ', 'Parametry', 'Data wygenerowania']
    ];
    lastAdminReportHistory.forEach(r => rows.push([
        r.id,
        r.adminLogin,
        formatReportType(r.reportType),
        formatReportParams(r.reportParameters),
        r.generatedAt ? new Date(r.generatedAt).toLocaleString('pl-PL') : ''
    ]));
    downloadCsv('historia_raportow_admin.csv', rows);
}

export function setSalesRange(range) {
    const fromInput = document.getElementById('report-sales-from');
    const toInput = document.getElementById('report-sales-to');
    if (!fromInput || !toInput) return;

    const today = new Date();
    const from = new Date(today);
    if (range === 'week') from.setDate(today.getDate() - 6);
    if (range === 'month') from.setDate(today.getDate() - 29);

    const toDateInput = value => value.toISOString().slice(0, 10);
    fromInput.value = toDateInput(from);
    toInput.value = toDateInput(today);
}

export async function generateGeneralReport() {
    const from = document.getElementById('report-sales-from').value;
    const to = document.getElementById('report-sales-to').value;
    if (!from || !to) { showToast('Wybierz zakres dat', 'error'); return; }

    const r = await apiFetch(`/api/raporty/sprzedaz-ogolna?from=${from}T00:00:00Z&to=${to}T23:59:59Z`);
    if (r) {
        const resultEl = document.getElementById('sales-report-result');
        resultEl.innerHTML = `
            <div style="display:flex; justify-content:flex-end; gap:12px; margin-bottom:16px;">
                <button class="btn btn-outline btn-sm" onclick="window.print()">Drukuj PDF</button>
                <button class="btn btn-outline btn-sm" id="btn-export-sales">Pobierz CSV</button>
            </div>
            <div class="stats-grid">
                <div class="stat-card"><div class="stat-label">Wynik netto</div><div class="stat-value">${formatMoney(r.totalRevenue)}</div><div style="font-size:12px; color:var(--text-muted);">${r.transactionCount} transakcji</div></div>
                <div class="stat-card"><div class="stat-label">Sprzedaz brutto</div><div class="stat-value">${formatMoney(r.grossSalesAmount)}</div><div style="font-size:12px; color:var(--text-muted);">${r.grossSalesCount} sprzedazy</div></div>
                <div class="stat-card"><div class="stat-label">Zwroty</div><div class="stat-value">${formatMoney(r.returnsAmount)}</div><div style="font-size:12px; color:var(--text-muted);">${r.returnsCount} zwrotow</div></div>
                <div class="stat-card"><div class="stat-label">Kasy</div><div class="stat-value">${formatMoney(r.onsite.amount)}</div><div style="font-size:12px; color:var(--text-muted);">${r.onsite.count} transakcji</div></div>
                <div class="stat-card"><div class="stat-label">Online</div><div class="stat-value">${formatMoney(r.online.amount)}</div><div style="font-size:12px; color:var(--text-muted);">${r.online.count} transakcji</div></div>
            </div>
            <div class="card" style="margin-top:24px;">
                <div class="card-header"><h3>Rozbicie operacji wedlug kanalow</h3></div>
                <div class="card-body">
                    <table>
                        <thead><tr><th>Kanal</th><th>Operacja</th><th>Liczba</th><th>Suma</th></tr></thead>
                        <tbody>
                            ${[
                                ...r.onsite.byOperation.map(o => ({ channel: 'Kasa', ...o })),
                                ...r.online.byOperation.map(o => ({ channel: 'Online', ...o }))
                            ].map(o => `
                                <tr>
                                    <td><span class="badge badge-info">${o.channel}</span></td>
                                    <td>${escapeHtml(o.operation)}</td>
                                    <td>${o.count}</td>
                                    <td style="font-weight:700;">${formatMoney(o.amount)}</td>
                                </tr>
                            `).join('') || '<tr><td colspan="4" style="color:var(--text-muted);">Brak operacji w tym zakresie.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        document.getElementById('btn-export-sales').onclick = () => {
            const rows = [
                ["Kategoria", "Wartosc"],
                ["Wynik netto", r.totalRevenue],
                ["Liczba transakcji", r.transactionCount],
                ["Sprzedaz brutto", r.grossSalesAmount],
                ["Liczba sprzedazy", r.grossSalesCount],
                ["Zwroty", r.returnsAmount],
                ["Liczba zwrotow", r.returnsCount],
                ["Kasy netto", r.onsite.amount],
                ["Kasy transakcje", r.onsite.count],
                ["Online netto", r.online.amount],
                ["Online transakcje", r.online.count]
            ];
            r.onsite.byOperation.forEach(o => rows.push([`Kasa operacja: ${o.operation}`, o.amount]));
            r.online.byOperation.forEach(o => rows.push([`Online operacja: ${o.operation}`, o.amount]));
            downloadCsv(`raport_sprzedazy_${from}_${to}.csv`, rows);
        };
    }
}

export async function loadThroughputReport() {
    const date = document.getElementById('report-infra-date').value;
    if (!date) return;

    const data = await apiFetch(`/api/raporty/przepustowosc-wyciagow?date=${date}`);
    if (data) {
        const resultEl = document.getElementById('infra-report-result');
        const totalScans = data.reduce((sum, l) => sum + (l.totalScans || 0), 0);
        resultEl.innerHTML = `
            <div class="report-actions" style="display:flex; justify-content:flex-end; margin-bottom:16px;">
                <button class="btn btn-outline btn-sm" id="btn-export-throughput">Pobierz CSV</button>
                <button class="btn btn-outline btn-sm" id="btn-print-throughput">Drukuj zestawienie</button>
            </div>
            <div class="stats-grid">
                <div class="stat-card"><div class="stat-label">Laczna liczba przejsc</div><div class="stat-value">${totalScans}</div></div>
                <div class="stat-card"><div class="stat-label">Wyciagi w raporcie</div><div class="stat-value">${data.length}</div></div>
            </div>
        ` + data.map(l => {
            const max = Math.max(...l.hourlyStats.map(s => s.count), 1);
            return `
            <div class="card throughput-report-card" style="margin-bottom:24px;">
                <div class="card-header">
                    <span style="font-weight:700;">Wyci\u0105g: ${escapeHtml(l.liftName)}</span>
                    <span style="color:var(--text-muted); font-size:12px;">Razem: ${l.totalScans}, szczyt: ${l.peakHour}:00 (${l.peakCount})</span>
                </div>
                <div class="card-body">
                    <div class="throughput-chart" style="display:flex; align-items:flex-end; gap:4px; height:100px; padding-top:20px;">
                        ${Array.from({length: 24}, (_, h) => {
                            const hourStat = l.hourlyStats.find(s => s.hour === h);
                            const count = hourStat ? hourStat.count : 0;
                            return `
                                <div style="flex:1; display:flex; flex-direction:column; align-items:center; gap:2px;">
                                    <div title="${h}:00 - ${count} osob" style="width:100%; background:var(--primary); height:${(count/max)*80}px; min-height:${count>0?2:0}px;"></div>
                                    <span style="font-size:8px; color:var(--text-muted);">${h}</span>
                                </div>
                            `;
                        }).join('')}
                    </div>
                    <table class="throughput-print-table">
                        <thead>
                            <tr><th>Godzina</th><th>Liczba przej\u015b\u0107</th></tr>
                        </thead>
                        <tbody>
                            ${l.hourlyStats
                                .filter(s => s.count > 0)
                                .map(s => `<tr><td>${s.hour}:00</td><td>${s.count}</td></tr>`)
                                .join('') || '<tr><td colspan="2">Brak przej\u015b\u0107 w tym dniu</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>
        `}).join('');

        document.getElementById('btn-export-throughput').onclick = () => {
            const rows = [["Wyci\u0105g", "Godzina", "Liczba przej\u015b\u0107"]];
            data.forEach(l => l.hourlyStats.forEach(h => rows.push([l.liftName, `${h.hour}:00`, h.count])));
            downloadCsv(`raport_przepustowosci_${date}.csv`, rows);
        };
        document.getElementById('btn-print-throughput').onclick = printThroughputReport;
    }
}

