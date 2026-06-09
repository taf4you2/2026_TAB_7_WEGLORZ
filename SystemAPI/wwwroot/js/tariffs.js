import { apiFetch, showToast, token } from './core.js';

export async function loadTariffs() {
    const data = await apiFetch('/api/taryfy');
    if (data) {
        document.getElementById('tariffs-tbody').innerHTML = data.map(t => `
            <tr>
                <td style="font-weight:600;">${t.name}</td>
                <td><span class="badge badge-info">${t.season || 'Caloroczny'}</span></td>
                <td>${t.passType}</td>
                <td style="font-weight:700;">${t.price} zl</td>
                <td>${t.poolLimit || '-'}</td>
                <td><span class="badge badge-${t.isActive ? 'active' : 'inactive'}">${t.isActive ? 'AKTYWNA' : 'NIEAKTYWNA'}</span></td>
                <td>
                    <button class="btn btn-outline" style="padding:4px 8px; font-size:12px;" onclick="openTariffModal(true, ${t.id}, '${t.name}', ${t.price}, ${t.poolLimit || ''}, '${t.season}', '${t.passType}')">E</button>
                    ${t.isActive ? `<button class="btn btn-outline" style="padding:4px 8px; font-size:12px; color:var(--danger);" onclick="deleteTariff(${t.id})">Dezaktywuj</button>` : ''}
                </td>
            </tr>
        `).join('');
    }
}

export let isTariffEdit = false, currentTariffId = null;
export function openTariffModal(edit = false, id = null, name = '', price = '', pool = '', season = 'wysoki', type = 'czasowy') {
    isTariffEdit = edit; currentTariffId = id;
    document.getElementById('modal-overlay-tariff').style.display = 'grid';
    document.getElementById('modal-tariff-title').textContent = edit ? 'Edytuj Taryfe' : 'Nowa Taryfa';
    document.getElementById('tariff-name').value = name;
    document.getElementById('tariff-price').value = price;
    document.getElementById('tariff-pool').value = pool;
    document.getElementById('tariff-season').value = season?.toLowerCase() || 'wysoki';
    document.getElementById('tariff-type').value = type?.toLowerCase() || 'czasowy';
}
export function closeTariffModal() { document.getElementById('modal-overlay-tariff').style.display = 'none'; }

export async function handleTariffSubmit(e) {
    e.preventDefault();
    const payload = {
        name: document.getElementById('tariff-name').value,
        price: parseFloat(document.getElementById('tariff-price').value),
        poolLimit: document.getElementById('tariff-pool').value ? parseInt(document.getElementById('tariff-pool').value) : null,
        season: document.getElementById('tariff-season').value,
        passType: document.getElementById('tariff-type').value
    };
    const res = await fetch(isTariffEdit ? `/api/taryfy/${currentTariffId}` : '/api/taryfy', {
        method: isTariffEdit ? 'PUT' : 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.ok) { showToast('Zapisano taryfe'); closeTariffModal(); loadTariffs(); }
    else { const err = await res.json().catch(()=>({})); showToast(err.message || 'Blad zapisu', 'error'); }
}

export async function deleteTariff(id) {
    if (!confirm('Czy na pewno chcesz dezaktywowac te taryfe?')) return;
    const res = await fetch(`/api/taryfy/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
    if (res.ok) { showToast('Dezaktywowano taryfe'); loadTariffs(); }
    else { const err = await res.text(); showToast(err || 'Blad dezaktywacji', 'error'); }
}
