import { apiFetch, showToast, token } from './core.js';

export async function loadSales() {
    const comp = await apiFetch('/api/statystyki/sprzedaz-porownanie');
    if (comp) {
        document.getElementById('sales-comp').innerHTML = `
            <div style="background:rgba(59,130,246,0.1); padding:20px; border-radius:12px; text-align:center;">
                ONLINE<br><span style="font-size:24px; font-weight:700;">${comp.online.amount} zl</span>
            </div>
            <div style="background:rgba(16,185,129,0.1); padding:20px; border-radius:12px; text-align:center;">
                KASA<br><span style="font-size:24px; font-weight:700;">${comp.onsite.amount} zl</span>
            </div>`;
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

