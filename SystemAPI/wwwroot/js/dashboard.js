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
        const ctx = document.getElementById('hourly-traffic-chart').getContext('2d');
        if (window.hourlyChart) window.hourlyChart.destroy();
        window.hourlyChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: traffic.map(t => t.hour + ':00'),
                datasets: [{
                    label: 'Skanowania bramek',
                    data: traffic.map(t => t.count),
                    borderColor: '#3b82f6',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(255,255,255,0.1)' }, ticks: { color: '#9ca3af' } },
                    x: { grid: { display: false }, ticks: { color: '#9ca3af' } }
                }
            }
        });
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

    if (window.feedInterval) clearInterval(window.feedInterval);
    loadFeed();
    window.feedInterval = setInterval(loadFeed, 10000);
}

async function loadFeed() {
    const feed = await apiFetch('/api/statystyki/activity-feed');
    if (feed && document.getElementById('activity-feed-list')) {
        document.getElementById('activity-feed-list').innerHTML = feed.map(f => `
            <div class="failed-scan" style="border-left: 3px solid ${f.status === 'ok' ? '#10b981' : '#ef4444'}; padding-left: 12px; margin-bottom: 8px;">
                <span style="font-family:monospace; display:inline-block; width:130px;">${f.cardRfid}</span>
                <span style="flex:1;">${f.location}</span>
                <span style="color:${f.status === 'ok' ? '#10b981' : '#ef4444'}; font-weight:600; width:120px; text-align:right;">${f.status === 'ok' ? 'ZATWIERDZONO' : (f.reason || 'BŁĄD')}</span>
                <span style="color:var(--text-muted); width:70px; text-align:right;">${new Date(f.timestamp).toLocaleTimeString()}</span>
            </div>
        `).join('');
    }
}
