import { apiFetch } from './core.js';

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
            </tr>
        `).join('');
    }
}
