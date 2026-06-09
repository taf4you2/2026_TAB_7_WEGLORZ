import { apiFetch, showToast, token } from './core.js';

export async function loadInfra() {
    const data = await apiFetch('/api/statystyki/infrastruktura');
    if (data) {
        document.getElementById('infra-list').innerHTML = data.map(l => `
            <div class="card infra-lift">
                <div class="card-header">
                    <div style="display:flex; align-items:center; gap:10px;">
                        <span style="font-weight:700;">Wyciag: ${l.name}</span>
                        <span class="badge badge-${l.isActive ? 'active' : 'inactive'}">${l.isActive ? 'AKTYWNY' : 'NIEAKTYWNY'}</span>
                    </div>
                    <div style="display:flex; gap:8px;">
                        <button class="btn btn-outline" style="padding:4px 8px; font-size:12px;" onclick="openLiftModal(true, ${l.id}, '${l.name}', ${l.isActive})">Edytuj</button>
                        ${l.isActive ? `<button class="btn btn-outline" style="padding:4px 8px; font-size:12px; color:var(--danger);" onclick="deactivateLift(${l.id})">Dezaktywuj</button>` : ''}
                    </div>
                </div>
                <div class="card-body"><div class="infra-gates">
                    ${l.gates.map(g => `
                        <div class="infra-gate">
                            <div style="font-size:13px; font-weight:600;">${g.name}</div>
                            <div style="display:flex; align-items:center; gap:8px;">
                                <span class="badge badge-${g.isActive ? 'active' : 'inactive'}">${g.isActive ? 'ON' : 'OFF'}</span>
                                <button class="btn btn-outline" style="padding:2px 6px; font-size:10px;" onclick="openGateModal(true, ${g.id}, '${g.name}', ${l.id}, ${g.isActive})">E</button>
                                ${g.isActive ? `<button class="btn btn-outline" style="padding:2px 6px; font-size:10px; color:var(--danger);" onclick="deactivateGate(${g.id})">OFF</button>` : ''}
                            </div>
                        </div>
                    `).join('')}
                    ${l.isActive ? `<div class="infra-gate" style="border-style:dashed; cursor:pointer; color:var(--primary); justify-content:center;" onclick="openGateModal(false, null, '', ${l.id})">+ Bramka</div>` : ''}
                </div></div>
            </div>
        `).join('');
    }
}

export let isLiftEdit = false, currentLiftId = null;
export function openLiftModal(edit = false, id = null, name = '', active = true) {
    isLiftEdit = edit; currentLiftId = id;
    document.getElementById('modal-overlay-lift').style.display = 'grid';
    document.getElementById('lift-name').value = name;
    document.getElementById('lift-status').value = active ? 'otwarty' : 'zamkniety';
}
export function closeLiftModal() { document.getElementById('modal-overlay-lift').style.display = 'none'; }

export async function handleLiftSubmit(e) {
    e.preventDefault();
    const payload = {
        name: document.getElementById('lift-name').value,
        status: document.getElementById('lift-status').value,
        opensAt: document.getElementById('lift-opens').value + ":00",
        closesAt: document.getElementById('lift-closes').value + ":00"
    };
    const res = await fetch(isLiftEdit ? `/api/wyciagi/${currentLiftId}` : '/api/wyciagi', {
        method: isLiftEdit ? 'PUT' : 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.ok) { showToast('Zapisano'); closeLiftModal(); loadInfra(); }
}

export async function deactivateLift(id) {
    if (!confirm('Czy na pewno chcesz dezaktywowac ten wyciag i jego bramki?')) return;
    const res = await fetch(`/api/wyciagi/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) { showToast('Dezaktywowano wyciag'); loadInfra(); }
    else { showToast('Blad dezaktywacji wyciagu', 'error'); }
}

export let isGateEdit = false, currentGateId = null;
export async function openGateModal(edit = false, id = null, name = '', liftId = null, active = true) {
    isGateEdit = edit; currentGateId = id;
    const lifts = await apiFetch('/api/wyciagi');
    document.getElementById('gate-lift-id').innerHTML = lifts.map(l => `<option value="${l.id}" ${l.id == liftId ? 'selected' : ''}>${l.name}</option>`).join('');
    document.getElementById('modal-overlay-gate').style.display = 'grid';
    document.getElementById('gate-name').value = name;
    document.getElementById('gate-active').checked = active;
}
export function closeGateModal() { document.getElementById('modal-overlay-gate').style.display = 'none'; }

export async function handleGateSubmit(e) {
    e.preventDefault();
    const payload = {
        name: document.getElementById('gate-name').value,
        liftId: parseInt(document.getElementById('gate-lift-id').value),
        isActive: document.getElementById('gate-active').checked
    };
    const res = await fetch(isGateEdit ? `/api/bramki/${currentGateId}` : '/api/bramki', {
        method: isGateEdit ? 'PUT' : 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.ok) { showToast('Zapisano'); closeGateModal(); loadInfra(); }
}

export async function deactivateGate(id) {
    if (!confirm('Czy na pewno chcesz dezaktywowac te bramke?')) return;
    const res = await fetch(`/api/bramki/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) { showToast('Dezaktywowano bramke'); loadInfra(); }
    else { showToast('Blad dezaktywacji bramki', 'error'); }
}
