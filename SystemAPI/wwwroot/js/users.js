import { apiFetch, showToast, token } from './core.js';

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function roleLabel(role) {
    if (role === 'admin') return 'Administrator';
    if (role === 'kasjer') return 'Kasjer';
    return 'Narciarz';
}

function jsStringArg(value) {
    return JSON.stringify(String(value ?? '')).replace(/'/g, '&#39;');
}

export async function loadUsers() {
    const users = await apiFetch('/api/users/all');
    if (users) {
        const staff = users.filter(u => u.role !== 'narciarz');
        const customers = users.filter(u => u.role === 'narciarz');
        document.getElementById('staff-tbody').innerHTML = staff.map(u => `
            <tr>
                <td>${u.id}</td>
                <td>${escapeHtml(u.login)}</td>
                <td>${roleLabel(u.role)}</td>
                <td><span class="badge badge-${u.isActive ? 'active' : 'inactive'}">${u.isActive ? 'TAK' : 'NIE'}</span></td>
                <td><button class="btn btn-outline" onclick='openUserModal(true, ${u.id}, ${jsStringArg(u.login)}, "${u.role}", ${u.isActive})'>Edytuj</button></td>
            </tr>
        `).join('');
        document.getElementById('customers-tbody').innerHTML = customers.map(u => `
            <tr>
                <td>${u.id}</td>
                <td>${escapeHtml(u.login)}</td>
                <td><span class="badge badge-info">OK</span></td>
                <td><button class="btn btn-outline" onclick='openHistoryModal(${u.id}, ${jsStringArg(u.login)})'>Historia</button></td>
            </tr>
        `).join('');
    }
}

export let isUserEdit = false, currentUserId = null, currentUserRole = null;
export function openUserModal(edit = false, id = null, login = '', role = 'kasjer', active = true) {
    isUserEdit = edit; currentUserId = id; currentUserRole = role;
    document.getElementById('modal-overlay').style.display = 'grid';
    document.getElementById('modal-title').textContent = edit ? 'Edycja pracownika' : 'Nowy pracownik';
    document.getElementById('user-login').value = login;
    document.getElementById('user-password').value = '';
    const passwordInput = document.getElementById('user-password');
    const roleSelect = document.getElementById('user-role');
    passwordInput.required = !edit;
    passwordInput.placeholder = edit ? 'Pozostaw puste, aby nie zmieniac' : 'Minimum 8 znakow';
    roleSelect.value = role;
    roleSelect.disabled = edit;
    document.getElementById('user-active').checked = active;
}
export function closeUserModal() {
    document.getElementById('user-role').disabled = false;
    document.getElementById('modal-overlay').style.display = 'none';
    currentUserRole = null;
}

export async function handleUserSubmit(e) {
    e.preventDefault();
    const password = document.getElementById('user-password').value;
    if (!isUserEdit && password.length < 8) {
        showToast('Haslo musi miec co najmniej 8 znakow', 'error');
        return;
    }

    const payload = {
        login: document.getElementById('user-login').value.trim(),
        password,
        role: document.getElementById('user-role').value,
        isActive: document.getElementById('user-active').checked
    };
    const url = isUserEdit
        ? `/api/users/${currentUserId}?role=${encodeURIComponent(currentUserRole)}`
        : '/api/users';
    const res = await fetch(url, {
        method: isUserEdit ? 'PUT' : 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.ok) {
        showToast('Zapisano');
        closeUserModal();
        loadUsers();
    } else {
        const err = await res.json().catch(() => ({}));
        showToast(err.message || 'Blad zapisu', 'error');
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
