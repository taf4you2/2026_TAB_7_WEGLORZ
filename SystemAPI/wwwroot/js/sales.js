import { apiFetch, showToast, token } from './core.js';

export async function loadSales() {
    const comp = await apiFetch('/api/statystyki/sales-chart');
    if (comp) {
        const ctxCh = document.getElementById('sales-channels-chart').getContext('2d');
        if (window.salesChChart) window.salesChChart.destroy();
        window.salesChChart = new Chart(ctxCh, {
            type: 'bar',
            data: {
                labels: comp.labels,
                datasets: [
                    { label: 'Online', data: comp.onlineValues, backgroundColor: '#10b981' },
                    { label: 'Kasa', data: comp.onsiteValues, backgroundColor: '#3b82f6' }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: { stacked: true, grid: { display: false }, ticks: { color: '#9ca3af' } },
                    y: { stacked: true, grid: { color: 'rgba(255,255,255,0.1)' }, ticks: { color: '#9ca3af' } }
                },
                plugins: { legend: { labels: { color: '#e5e7eb' } } }
            }
        });
    }

    const structure = await apiFetch('/api/statystyki/sales-structure');
    if (structure) {
        const ctxSt = document.getElementById('sales-structure-chart').getContext('2d');
        if (window.salesStChart) window.salesStChart.destroy();
        window.salesStChart = new Chart(ctxSt, {
            type: 'doughnut',
            data: {
                labels: structure.labels,
                datasets: [{
                    data: structure.values,
                    backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'right', labels: { color: '#e5e7eb' } } }
            }
        });
    }
    const active = await apiFetch('/api/statystyki/aktywni-kasjerzy');
    if (active) {
        document.getElementById('active-cashiers-tbody').innerHTML = active.map(c => `
            <tr>
                <td>${c.login}</td>
                <td>${new Date(c.startTime).toLocaleTimeString()}</td>
                <td>-</td>
                <td><span class="badge badge-active">ZMIANA</span></td>
                <td>
                    <button class="btn btn-outline" style="padding:4px 8px; font-size:11px; color:var(--danger);" onclick="closeShift(${c.cashierId}, '${c.login}')">Zamknij dniowke</button>
                </td>
            </tr>
        `).join('') || '<tr><td colspan="5" style="text-align:center; padding:20px; color:var(--text-muted);">Brak aktywnych kasjerow.</td></tr>';
    }
}

export async function closeShift(id, login) {
    if (!confirm(`Czy na pewno chcesz ROZLICZYC I ZAMKNAC dniowke kasjera: ${login}?`)) return;

    try {
        const res = await fetch(`/api/raporty/zamknij-kasjera/${id}`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (res.ok) {
            const data = await res.json();
            showToast(`Zmiana zamknieta. Utarg: ${data.revenue} zl`, 'success');
            loadSales();
        } else {
            const err = await res.json().catch(() => ({}));
            showToast(err.message || 'Blad zamykania zmiany', 'error');
        }
    } catch (e) {
        showToast('Blad polaczenia', 'error');
    }
}

