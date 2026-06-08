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

function formatDateTime(value) {
    return value ? new Date(value).toLocaleString('pl-PL') : '-';
}

function formatMoney(value) {
    return `${Number(value ?? 0).toFixed(2)} zl`;
}

function formatBool(value) {
    return value ? 'tak' : 'nie';
}

function statusClass(status) {
    if (['aktywny', 'potwierdzona', 'ok'].includes(status)) return 'active';
    if (['zablokowany', 'zastrzezony', 'niewazny'].includes(status)) return 'inactive';
    return 'info';
}

function gateReasonLabel(reason) {
    const labels = {
        brak_statusu: 'brak statusu',
        jeszcze_niewazny: 'jeszcze niewazny',
        wygasl: 'wygasl',
        brak_przejazdow: 'brak przejazdow'
    };
    if (!reason) return 'bramka przepusci';
    if (reason.startsWith('status_')) return `status: ${reason.replace('status_', '')}`;
    return labels[reason] ?? reason;
}

function renderHistoryStat(label, value) {
    return `
        <div class="history-stat">
            <div class="history-stat-label">${escapeHtml(label)}</div>
            <div class="history-stat-value">${escapeHtml(value)}</div>
        </div>
    `;
}

function renderPass(pass) {
    const rides = pass.remainingRides == null
        ? 'bez limitu przejazdow'
        : `${pass.remainingRides}/${pass.initialRides ?? pass.remainingRides} przejazdow`;
    const gateClass = pass.isUsableAtGate ? 'active' : 'inactive';

    return `
        <div class="history-pass">
            <div>
                <div class="history-item-title">${escapeHtml(pass.tariff ?? 'Karnet bez taryfy')}</div>
                <div class="history-muted">${escapeHtml(pass.cardId ?? 'brak karty')} / ${escapeHtml(pass.passType ?? 'typ nieznany')}</div>
            </div>
            <div class="history-pass-meta">
                <span class="badge badge-${statusClass(pass.status)}">${escapeHtml(pass.status ?? 'brak statusu')}</span>
                <span class="badge badge-${gateClass}">${escapeHtml(gateReasonLabel(pass.gateBlockReason))}</span>
                <span>${formatDateTime(pass.validFrom)} - ${formatDateTime(pass.validTo)}</span>
                <span>${escapeHtml(rides)}</span>
                <strong>${formatMoney(pass.price)}</strong>
            </div>
        </div>
    `;
}

function renderTransaction(transaction) {
    return `
        <tr>
            <td>#${transaction.id}</td>
            <td>${escapeHtml(transaction.operationType ?? '-')}</td>
            <td>${formatMoney(transaction.amount)}</td>
            <td>${formatDateTime(transaction.transactionDate)}</td>
            <td>${escapeHtml(transaction.cashier ?? '-')}</td>
        </tr>
    `;
}

function renderReservation(reservation) {
    const passes = reservation.passes?.length
        ? reservation.passes.map(renderPass).join('')
        : '<div class="history-empty">Brak karnetow w tej rezerwacji.</div>';

    const transactions = reservation.transactions?.length
        ? `
            <table class="history-table">
                <thead><tr><th>ID</th><th>Operacja</th><th>Kwota</th><th>Data</th><th>Kasjer</th></tr></thead>
                <tbody>${reservation.transactions.map(renderTransaction).join('')}</tbody>
            </table>
        `
        : '<div class="history-empty">Brak transakcji dla tej rezerwacji.</div>';

    return `
        <section class="history-section">
            <div class="history-section-head">
                <div>
                        <div class="history-item-title">${escapeHtml(reservation.reservationNumber)}</div>
                        <div class="history-muted">${formatDateTime(reservation.reservationDate)}</div>
                </div>
                <div class="history-pass-meta">
                    <strong>${formatMoney(reservation.totalAmount)}</strong>
                    <span class="badge badge-${statusClass(reservation.status)}">${escapeHtml(reservation.status ?? 'brak statusu')}</span>
                </div>
            </div>
            <div class="history-subtitle">Karnety</div>
            <div class="history-pass-list">${passes}</div>
            <div class="history-subtitle">Transakcje</div>
            ${transactions}
        </section>
    `;
}

function renderCard(card) {
    return `
        <div class="history-card-detail">
            <div>
                <div class="history-item-title">${escapeHtml(card.id)}</div>
                <div class="history-muted">dodano: ${formatDateTime(card.addedToPoolAt)}</div>
            </div>
            <div class="history-detail-grid">
                <span>Status: <strong>${escapeHtml(card.status ?? '-')}</strong></span>
                <span>Kaucja: <strong>${formatBool(card.depositPaid)}</strong></span>
                <span>Stan: <strong>${escapeHtml(card.physicalCondition ?? '-')}</strong></span>
                <span>Karnety: <strong>${card.passesCount ?? 0}</strong></span>
                <span>Aktywne: <strong>${card.activePassesCount ?? 0}</strong></span>
                <span>Skanow: <strong>${card.scansCount ?? 0}</strong></span>
                <span>Ostatni skan: <strong>${formatDateTime(card.lastScanAt)}</strong></span>
                ${card.blockReason ? `<span>Blokada: <strong>${escapeHtml(card.blockReason)}</strong></span>` : ''}
            </div>
        </div>
    `;
}

function renderTransactionStat(stat) {
    return `
        <tr>
            <td>${escapeHtml(stat.operationType ?? '-')}</td>
            <td>${stat.count ?? 0}</td>
            <td>${formatMoney(stat.amount)}</td>
        </tr>
    `;
}

function renderScan(scan) {
    return `
        <tr>
            <td>${formatDateTime(scan.scanTime)}</td>
            <td>${escapeHtml(scan.cardId ?? '-')}</td>
            <td>${escapeHtml(scan.gate ?? '-')}</td>
            <td><span class="badge badge-${statusClass(scan.result)}">${escapeHtml(scan.result ?? 'brak')}</span></td>
        </tr>
    `;
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
            <tr id="customer-row-${u.id}">
                <td>${u.id}</td>
                <td>${escapeHtml(u.login)}</td>
                <td><span class="badge badge-info">OK</span></td>
                <td><button class="btn btn-outline" onclick='openHistoryModal(${u.id}, ${jsStringArg(u.login)}, this)'>Historia</button></td>
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

export async function openHistoryModal(id, email, button = null) {
    const currentRow = document.getElementById(`customer-history-${id}`);
    if (currentRow) {
        currentRow.remove();
        if (button) button.textContent = 'Historia';
        return;
    }

    document.querySelectorAll('.customer-history-row').forEach(row => row.remove());
    document.querySelectorAll('#customers-tbody button').forEach(btn => {
        if (btn.textContent === 'Zwin') btn.textContent = 'Historia';
    });

    const baseRow = document.getElementById(`customer-row-${id}`);
    if (button) button.textContent = 'Laduje...';

    const h = await apiFetch(`/api/users/${id}/history`);
    if (h) {
        if (button) button.textContent = 'Zwin';
        const summary = h.summary ?? {};
        const customer = h.customer ?? {};
        const cardChips = h.cards?.length
            ? h.cards.map(card => `<span class="history-chip">${escapeHtml(card.id)}</span>`).join('')
            : '<span class="history-empty">Brak kart RFID</span>';
        const cardDetails = h.cards?.length
            ? h.cards.map(renderCard).join('')
            : '<div class="history-empty">Brak przypisanych kart RFID.</div>';
        const transactionStats = h.transactionStats?.length
            ? `
                <table class="history-table">
                    <thead><tr><th>Operacja</th><th>Liczba</th><th>Suma</th></tr></thead>
                    <tbody>${h.transactionStats.map(renderTransactionStat).join('')}</tbody>
                </table>
            `
            : '<div class="history-empty">Brak operacji finansowych.</div>';
        const reservations = h.reservations?.length
            ? h.reservations.map(renderReservation).join('')
            : '<div class="history-empty">Brak rezerwacji i karnetow dla tego klienta.</div>';
        const scans = h.recentScans?.length
            ? `
                <table class="history-table">
                    <thead><tr><th>Data</th><th>Karta</th><th>Bramka</th><th>Wynik</th></tr></thead>
                    <tbody>${h.recentScans.map(renderScan).join('')}</tbody>
                </table>
            `
            : '<div class="history-empty">Brak zarejestrowanych przejazdow.</div>';

        const row = document.createElement('tr');
        row.id = `customer-history-${id}`;
        row.className = 'customer-history-row';
        row.innerHTML = `
            <td colspan="4">
                <div class="customer-history-panel">
                    <div class="history-summary">
                        ${renderHistoryStat('Rezerwacje', summary.reservationsCount ?? 0)}
                        ${renderHistoryStat('Karnety', summary.passesCount ?? 0)}
                        ${renderHistoryStat('Aktywne', summary.activePassesCount ?? 0)}
                        ${renderHistoryStat('Skanow', summary.scansCount ?? 0)}
                        ${renderHistoryStat('OK / odmowy', `${summary.acceptedScansCount ?? 0} / ${summary.rejectedScansCount ?? 0}`)}
                        ${renderHistoryStat('Wydano', formatMoney(summary.totalSpent))}
                        ${renderHistoryStat('Ostatnia aktywnosc', formatDateTime(summary.lastActivityAt))}
                    </div>
                    <section class="history-section">
                        <div class="history-section-head">
                            <div>
                                <div class="history-item-title">Profil klienta</div>
                                <div class="history-muted">ID #${escapeHtml(customer.id ?? '-')} / konto od ${formatDateTime(customer.createdAt)}</div>
                            </div>
                            <span class="badge badge-info">${escapeHtml(customer.email ?? email)}</span>
                        </div>
                        <div class="history-cards">${cardChips}</div>
                    </section>
                    <section class="history-section">
                        <div class="history-section-head">
                            <div>
                                <div class="history-item-title">Karty RFID</div>
                                <div class="history-muted">Status, kaucja, liczba karnetow i ostatnie uzycie kazdej karty</div>
                            </div>
                        </div>
                        <div class="history-card-list">${cardDetails}</div>
                    </section>
                    <section class="history-section">
                        <div class="history-section-head">
                            <div>
                                <div class="history-item-title">Podsumowanie finansowe</div>
                                <div class="history-muted">Operacje pogrupowane po typie</div>
                            </div>
                        </div>
                        ${transactionStats}
                    </section>
                    ${reservations}
                    <section class="history-section">
                        <div class="history-section-head">
                            <div>
                                <div class="history-item-title">Ostatnie przejazdy</div>
                                <div class="history-muted">Maksymalnie 20 najnowszych skanow kart klienta</div>
                            </div>
                        </div>
                        ${scans}
                    </section>
                </div>
            </td>
        `;
        baseRow?.after(row);
    } else if (button) {
        button.textContent = 'Historia';
    }
}
export function closeHistoryModal() { document.getElementById('modal-overlay-history').style.display = 'none'; }
