import { apiFetch } from './core.js';

export async function loadDashboard() {
    const stats = await apiFetch('/api/statystyki/dzisiaj');
    if (stats) {
        document.getElementById('stat-tickets').textContent = stats.ticketsSoldToday;
        document.getElementById('stat-passes').textContent = stats.activePasses;
        document.getElementById('stat-revenue').textContent = stats.shiftRevenue.toLocaleString() + ' zl';
        document.getElementById('stat-returns').textContent = stats.pendingReturns;
    }

    const traffic = await apiFetch('/api/statystyki/ruch-godzinowy');
    if (traffic) {
        const max = Math.max(...traffic.map(t => t.count), 1);
        document.getElementById('hourly-traffic-chart').innerHTML = traffic.map(t => `
            <div style="flex:1; display:flex; flex-direction:column; align-items:center; gap:4px;">
                <div style="width:100%; background:var(--primary); border-radius:4px 4px 0 0; height:${(t.count / max) * 100}px; min-height:2px;"></div>
                <span style="font-size:9px; color:var(--text-muted);">${t.hour}h</span>
            </div>
        `).join('');
    }

    const cardStats = await apiFetch('/api/statystyki/statusy-kart');
    if (cardStats) {
        document.getElementById('card-status-list').innerHTML = cardStats.map(s => `
            <div style="display:flex; justify-content:space-between; margin-bottom:8px; font-size:13px;">
                <span>${s.status}</span><span style="font-weight:700;">${s.count}</span>
            </div>
        `).join('');
    }

    const occ = await apiFetch('/api/statystyki/oblozenie-minuty');
    if (occ) {
        document.getElementById('realtime-occupancy-list').innerHTML = occ.map(i => `
            <div style="margin-bottom:12px;">
                <div style="display:flex; justify-content:space-between; font-size:13px; margin-bottom:4px;">
                    <span>${i.liftName}</span><span>${i.count} os. / 15min</span>
                </div>
                <div style="height:6px; background:var(--border); border-radius:3px; overflow:hidden;">
                    <div style="width:${Math.min(100, i.count / 2)}%; height:100%; background:var(--primary);"></div>
                </div>
            </div>
        `).join('');
    }

    const failed = await apiFetch('/api/statystyki/nieudane-odbicia');
    if (failed) {
        document.getElementById('failed-scans-list').innerHTML = failed.map(f => `
            <div class="failed-scan">
                <span style="font-family:monospace;">${f.cardId}</span>
                <span>${f.gateName}</span>
                <span style="color:var(--danger); font-weight:600;">${f.result}</span>
                <span style="color:var(--text-muted);">${new Date(f.scanTime).toLocaleTimeString()}</span>
            </div>
        `).join('');
    }
}
