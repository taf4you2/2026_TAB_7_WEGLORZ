import { token, parseJwt, logout } from './core.js?v=soft-delete-1';
import { loadDashboard } from './dashboard.js?v=soft-delete-1';
import { loadInfra, openLiftModal, closeLiftModal, handleLiftSubmit, deactivateLift, openGateModal, closeGateModal, handleGateSubmit, deactivateGate } from './infra.js?v=soft-delete-1';
import { loadTariffs, openTariffModal, closeTariffModal, handleTariffSubmit, deleteTariff } from './tariffs.js?v=soft-delete-1';
import { loadCards, openCardModal, closeCardModal, handleCardSubmit, blockCard, unblockCard, returnCard, deleteCard } from './cards.js?v=soft-delete-1';
import { loadSales } from './sales.js?v=soft-delete-1';
import { loadUsers, openUserModal, closeUserModal, handleUserSubmit, openHistoryModal, closeHistoryModal } from './users.js?v=soft-delete-1';
import { loadShiftReports, loadAdminReportHistory, generateGeneralReport, loadThroughputReport, setSalesRange, loadAdvancedReport, setAdvancedRange } from './reports.js?v=soft-delete-1';

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
    setDefaultReportDates();
    
    if (tab === 'shift') loadShiftReports();
    if (tab === 'history') loadAdminReportHistory();
    if (tab === 'advanced') loadAdvancedReport();
}

function setDefaultReportDates() {
    const today = new Date();
    const weekAgo = new Date(today);
    weekAgo.setDate(today.getDate() - 6);

    const toDateInput = value => value.toISOString().slice(0, 10);
    const salesFrom = document.getElementById('report-sales-from');
    const salesTo = document.getElementById('report-sales-to');
    const infraDate = document.getElementById('report-infra-date');
    const advancedFrom = document.getElementById('report-advanced-from');
    const advancedTo = document.getElementById('report-advanced-to');

    if (salesFrom && !salesFrom.value) salesFrom.value = toDateInput(weekAgo);
    if (salesTo && !salesTo.value) salesTo.value = toDateInput(today);
    if (infraDate && !infraDate.value) infraDate.value = toDateInput(today);
    if (advancedFrom && !advancedFrom.value) advancedFrom.value = toDateInput(weekAgo);
    if (advancedTo && !advancedTo.value) advancedTo.value = toDateInput(today);
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
    setDefaultReportDates();

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
window.setSalesRange = setSalesRange;
window.loadAdvancedReport = loadAdvancedReport;
window.setAdvancedRange = setAdvancedRange;
window.logout = logout;
window.openUserModal = openUserModal;
window.closeUserModal = closeUserModal;
window.openLiftModal = openLiftModal;
window.closeLiftModal = closeLiftModal;
window.deactivateLift = deactivateLift;
window.openGateModal = openGateModal;
window.closeGateModal = closeGateModal;
window.deactivateGate = deactivateGate;
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

