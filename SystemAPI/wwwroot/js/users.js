import { apiFetch, showToast, token } from './core.js';

export async function loadUsers() {
    const users = await apiFetch('/api/users/all');
    if (users) {
        const staff = users.filter(u => u.role !== 'narciarz');
        const customers = users.filter(u => u.role === 'narciarz');
        document.getElementById('staff-tbody').innerHTML = staff.map(u => `
            <tr>
                <td>${u.id}</td>
                <td>${u.login}</td>
                <td>${u.role}</td>
                <td><span class="badge badge-${u.isActive ? 'active' : 'inactive'}">${u.isActive ? 'TAK' : 'NIE'}</span></td>
                <td><button class="btn btn-outline" onclick="openUserModal(true, ${u.id}, '${u.login}', '${u.role}', ${u.isActive})">E</button></td>
            </tr>
        `).join('');
        document.getElementById('customers-tbody').innerHTML = customers.map(u => `
            <tr>
                <td>${u.id}</td>
                <td>${u.login}</td>
                <td><span class="badge badge-info">OK</span></td>
                <td><button class="btn btn-outline" onclick="openHistoryModal(${u.id}, '${u.login}')">S</button></td>
            </tr>
        `).join('');
    }
}

export let isUserEdit = false, currentUserId = null;
export function openUserModal(edit = false, id = null, login = '', role = 'kasjer', active = true) {
    isUserEdit = edit; currentUserId = id;
    document.getElementById('modal-overlay').style.display = 'grid';
    document.getElementById('modal-title').textContent = edit ? 'Edycja pracownika' : 'Nowy pracownik';
    document.getElementById('user-login').value = login;
    document.getElementById('user-password').value = '';
    document.getElementById('user-role').value = role;
    document.getElementById('user-active').checked = active;
}
export function closeUserModal() { document.getElementById('modal-overlay').style.display = 'none'; }

export async function handleUserSubmit(e) {
    e.preventDefault();
    const payload = {
        login: document.getElementById('user-login').value,
        password: document.getElementById('user-password').value,
        role: document.getElementById('user-role').value,
        isActive: document.getElementById('user-active').checked
    };
    const res = await fetch(isUserEdit ? `/api/users/${currentUserId}` : '/api/users', {
        method: isUserEdit ? 'PUT' : 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.ok) {
        showToast('Zapisano');
        closeUserModal();
        loadUsers();
    } else {
        showToast('Blad zapisu', 'error');
    }
}

export async function openHistoryModal(id, email) {
    const h = await apiFetch(`/api/users/${id}/history`);
    if (h) {
        document.getElementById('modal-history-title').textContent = email;
        document.getElementById('modal-overlay-history').style.display = 'grid';
        document.getElementById('modal-history-content').innerHTML = h.reservations.map(r => `
            <div class="card" style="margin-bottom:12px; padding:12px;">
                <div style="font-weight:700;">Rez: ${r.reservationNumber} (${r.status})</div>
                <div style="font-size:12px; color:var(--text-muted);">${new Date(r.reservationDate).toLocaleString()}</div>
            </div>
        `).join('') || 'Brak danych';
    }
}
export function closeHistoryModal() { document.getElementById('modal-overlay-history').style.display = 'none'; }
