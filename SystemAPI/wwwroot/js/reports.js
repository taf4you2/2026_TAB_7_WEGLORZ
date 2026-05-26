import { apiFetch, showToast } from './core.js';

export async function loadShiftReports() {
    const data = await apiFetch('/api/raporty/zmiany');
    if (data) {
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
    }
}

export async function loadAdminReportHistory() {
    const data = await apiFetch('/api/raporty/historia-admin');
    if (data) {
        document.getElementById('reports-history-tbody').innerHTML = data.map(r => `
            <tr>
                <td>${r.id}</td>
                <td>${r.adminLogin}</td>
                <td><span class="badge badge-info">${r.reportType.toUpperCase()}</span></td>
                <td style="font-size:12px; color:var(--text-muted);">${r.reportParameters || '-'}</td>
                <td style="font-size:12px;">${new Date(r.generatedAt).toLocaleString()}</td>
            </tr>
        `).join('');
    }
}

export function downloadCsv(filename, data) {
    const csvContent = "data:text/csv;charset=utf-8," + data.map(e => e.join(",")).join("\n");
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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
                <div class="stat-card"><div class="stat-label">Calkowity przychod</div><div class="stat-value">${r.totalRevenue.toLocaleString()} zl</div></div>
                <div class="stat-card"><div class="stat-label">Sprzedaz w kasach</div><div class="stat-value">${r.onsite.amount.toLocaleString()} zl</div><div style="font-size:12px; color:var(--text-muted);">${r.onsite.count} transakcji</div></div>
                <div class="stat-card"><div class="stat-label">Sprzedaz online</div><div class="stat-value">${r.online.amount.toLocaleString()} zl</div><div style="font-size:12px; color:var(--text-muted);">${r.online.count} transakcji</div></div>
            </div>
            <div class="card" style="margin-top:24px;">
                <div class="card-header"><h3>Rozbicie operacji stacjonarnych</h3></div>
                <div class="card-body">
                    ${r.onsite.byOperation.map(o => `
                        <div style="display:flex; justify-content:space-between; margin-bottom:8px; border-bottom:1px solid var(--border); padding-bottom:4px;">
                            <span>${o.operation}</span><span style="font-weight:700;">${o.amount.toLocaleString()} zl</span>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;

        document.getElementById('btn-export-sales').onclick = () => {
            const rows = [
                ["Kategoria", "Wartosc"],
                ["Calkowity przychod", r.totalRevenue],
                ["Sprzedaz w kasach", r.onsite.amount],
                ["Liczba transakcji kasowych", r.onsite.count],
                ["Sprzedaz online", r.online.amount],
                ["Liczba transakcji online", r.online.count]
            ];
            r.onsite.byOperation.forEach(o => rows.push([`Operacja: ${o.operation}`, o.amount]));
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
        resultEl.innerHTML = `
            <div style="display:flex; justify-content:flex-end; margin-bottom:16px;">
                <button class="btn btn-outline btn-sm" onclick="window.print()">Drukuj zestawienie</button>
            </div>
        ` + data.map(l => `
            <div class="card" style="margin-bottom:24px;">
                <div class="card-header"><span style="font-weight:700;">Wyciag: ${l.liftName}</span></div>
                <div class="card-body">
                    <div style="display:flex; align-items:flex-end; gap:4px; height:100px; padding-top:20px;">
                        ${Array.from({length: 24}, (_, h) => {
                            const hourStat = l.hourlyStats.find(s => s.hour === h);
                            const count = hourStat ? hourStat.count : 0;
                            const max = Math.max(...l.hourlyStats.flatMap(ll => ll.hourlyStats.map(s => s.count)), 1);
                            return `
                                <div style="flex:1; display:flex; flex-direction:column; align-items:center; gap:2px;">
                                    <div title="${h}:00 - ${count} osob" style="width:100%; background:var(--primary); height:${(count/max)*80}px; min-height:${count>0?2:0}px;"></div>
                                    <span style="font-size:8px; color:var(--text-muted);">${h}</span>
                                </div>
                            `;
                        }).join('')}
                    </div>
                </div>
            </div>
        `).join('');
    }
}

