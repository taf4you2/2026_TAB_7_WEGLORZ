import { token, parseJwt, logout } from './core.js';
import { loadDashboard } from './dashboard.js';
import { loadInfra, openLiftModal, closeLiftModal, handleLiftSubmit, openGateModal, closeGateModal, handleGateSubmit } from './infra.js';
import { loadSales } from './sales.js';
import { loadUsers, openUserModal, closeUserModal, handleUserSubmit, openHistoryModal, closeHistoryModal } from './users.js';

// Nawigacja
function showSection(id, el) {
    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.getElementById('sec-' + id).classList.add('active');
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    if (el) el.classList.add('active');

    if (id === 'dashboard') loadDashboard();
    if (id === 'infra') loadInfra();
    if (id === 'sales') loadSales();
    if (id === 'staff' || id === 'customers') loadUsers();
}

// Inicjalizacja
document.addEventListener('DOMContentLoaded', () => {
    const p = parseJwt(token);
    if (!p || (p.role !== 'admin' && p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] !== 'admin')) {
        logout();
        return;
    }
    document.getElementById('user-display').textContent = 'Zalogowano: ' + (p.unique_name || p.name || 'Admin');
    loadDashboard();

    // Podpiecie formularzy
    document.getElementById('user-form').onsubmit = handleUserSubmit;
    document.getElementById('lift-form').onsubmit = handleLiftSubmit;
    document.getElementById('gate-form').onsubmit = handleGateSubmit;
});

// Ekspozycja do okna (dla onclick w HTML)
window.showSection = showSection;
window.logout = logout;
window.openUserModal = openUserModal;
window.closeUserModal = closeUserModal;
window.openLiftModal = openLiftModal;
window.closeLiftModal = closeLiftModal;
window.openGateModal = openGateModal;
window.closeGateModal = closeGateModal;
window.openHistoryModal = openHistoryModal;
window.closeHistoryModal = closeHistoryModal;
