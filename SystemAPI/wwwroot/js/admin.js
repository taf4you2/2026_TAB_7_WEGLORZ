import { token, parseJwt, logout } from './core.js';
import { loadDashboard } from './dashboard.js';
import { loadInfra, openLiftModal, closeLiftModal, handleLiftSubmit, openGateModal, closeGateModal, handleGateSubmit } from './infra.js';
import { loadTariffs, openTariffModal, closeTariffModal, handleTariffSubmit, deleteTariff } from './tariffs.js';
import { loadCards, openCardModal, closeCardModal, handleCardSubmit, blockCard, unblockCard, returnCard, deleteCard } from './cards.js';
import { loadSales } from './sales.js';
import { loadUsers, openUserModal, closeUserModal, handleUserSubmit, openHistoryModal, closeHistoryModal } from './users.js';
import { loadShiftReports, generateGeneralReport, loadThroughputReport } from './reports.js';

// Nawigacja
function showSection(id, el) {
    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.getElementById('sec-' + id).classList.add('active');
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    if (el) el.classList.add('active');

    if (id === 'dashboard') loadDashboard();
    if (id === 'infra') loadInfra();
    if (id === 'tariffs') loadTariffs();
    if (id === 'cards') loadCards();
    if (id === 'sales') loadSales();
    if (id === 'reports') loadShiftReports();
    if (id === 'staff' || id === 'customers') loadUsers();
}

function showReportTab(tab) {
    document.querySelectorAll('.report-tab').forEach(t => t.style.display = 'none');
    document.getElementById('report-tab-' + tab).style.display = 'block';
    
    if (tab === 'shift') loadShiftReports();
    if (tab === 'history') loadAdminReportHistory();
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
    document.getElementById('tariff-form').onsubmit = handleTariffSubmit;
    document.getElementById('card-form').onsubmit = handleCardSubmit;

    // Obsluga szukania kart na zywo
    const cardSearch = document.getElementById('card-search');
    if (cardSearch) {
        cardSearch.addEventListener('input', () => loadCards());
    }
});

// Ekspozycja do okna (dla onclick w HTML)
window.showSection = showSection;
window.showReportTab = showReportTab;
window.generateGeneralReport = generateGeneralReport;
window.loadThroughputReport = loadThroughputReport;
window.logout = logout;
window.openUserModal = openUserModal;
window.closeUserModal = closeUserModal;
window.openLiftModal = openLiftModal;
window.closeLiftModal = closeLiftModal;
window.openGateModal = openGateModal;
window.closeGateModal = closeGateModal;
window.openTariffModal = openTariffModal;
window.closeTariffModal = closeTariffModal;
window.deleteTariff = deleteTariff;
window.openCardModal = openCardModal;
window.closeCardModal = closeCardModal;
window.blockCard = blockCard;
window.unblockCard = unblockCard;
window.returnCard = returnCard;
window.deleteCard = deleteCard;
window.openHistoryModal = openHistoryModal;
window.closeHistoryModal = closeHistoryModal;
window.closeShift = (id, login) => import('./sales.js').then(m => m.closeShift(id, login));

