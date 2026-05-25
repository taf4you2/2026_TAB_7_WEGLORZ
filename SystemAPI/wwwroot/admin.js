// Funkcja pomocnicza do dekodowania tokenu JWT po stronie klienta
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // 1. Weryfikacja dostępu (Token JWT)
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');

    if (!token) {
        window.location.href = 'admin-login.html';
        return; // Zatrzymuje dalsze renderowanie i logikę
    }

    const payload = parseJwt(token);
    // Odczytanie roli (w C# domyślnie używany jest pełen adres schematu lub pole 'role')
    const userRole = payload && (payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']);

    // Inicjalizacja kontenera Toast
    const toastContainer = document.createElement('div');
    toastContainer.className = 'toast-container';
    document.body.appendChild(toastContainer);

    window.showToast = (message, type = 'info') => {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;

        let icon = '';
        if (type === 'success') icon = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>';
        else if (type === 'error') icon = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="#ef4444" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>';
        else icon = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="#3b82f6" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>';

        toast.innerHTML = `
            ${icon}
            <div class="toast-message">${message}</div>
            <div class="toast-close" onclick="this.parentElement.remove()">&times;</div>
        `;

        toastContainer.appendChild(toast);
        setTimeout(() => toast.classList.add('show'), 10);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 4000);
    };

    // Zakładamy, że rolą uprawnioną jest "kasjer" (lub potencjalnie "admin")
    if (!payload || (userRole !== 'kasjer' && userRole !== 'admin')) {
        localStorage.removeItem('jwt_token');
        sessionStorage.removeItem('jwt_token');
        showToast('Brak uprawnień. Zaloguj się jako administrator.', 'error');
        setTimeout(() => {
            window.location.href = 'admin-login.html';
        }, 1500);
        return;
    }

    // Pokaż aplikację, gdy autoryzacja się powiedzie
    document.getElementById('app-container').style.display = 'flex';
    // Ukryj moduł Użytkownicy dla kasjerów
    if (userRole !== 'admin') {
        const usersNavLink = document.querySelector('.nav-link[data-target="users"]');
        if (usersNavLink && usersNavLink.parentElement) {
            usersNavLink.parentElement.style.display = 'none';
        }
    }

    // Inicjalizacja SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/infrastructureHub")
        .withAutomaticReconnect()
        .build();

    connection.on("LiftStatusUpdated", (id, statusId) => {
        showToast(`Status wyciągu #${id} został zmieniony.`, 'info');
        if (document.getElementById('lifts-table')) {
            loadLiftsModule(mainContent, token, false);
        }
    });

    connection.on("TrailStatusUpdated", (id, statusId) => {
        showToast(`Status trasy #${id} został zmieniony.`, 'info');
        if (document.getElementById('trails-table')) {
            loadTrailsModule(mainContent, token, false);
        }
    });

    connection.start().catch(err => console.error("SignalR Error:", err));


    const navLinks = document.querySelectorAll('.nav-link');
    const mainContent = document.getElementById('main-content');
    const pageTitle = document.getElementById('page-title');
    const menuToggle = document.getElementById('menu-toggle');
    const sidebar = document.querySelector('.sidebar');
    const logoutBtn = document.getElementById('logout-btn');

    // Definicja zawartości poszczególnych modułów
    const modulesContent = {
        dashboard: `
            <div class="module-content">
                <div class="card">
                    <h3>Witamy w panelu</h3>
                    <p>Wybierz moduł z menu po lewej stronie, aby zarządzać systemem stacji narciarskiej.</p>
                </div>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 24px;">
                    <div class="card" style="margin-bottom: 0; border-left: 4px solid var(--primary-color);">
                        <h4 style="color: var(--text-muted); margin-bottom: 8px;">Aktywne karnety dzisiaj</h4>
                        <p style="font-size: 28px; font-weight: 700; color: var(--text-main);">1 245</p>
                    </div>
                    <div class="card" style="margin-bottom: 0; border-left: 4px solid #10b981;">
                        <h4 style="color: var(--text-muted); margin-bottom: 8px;">Działające wyciągi</h4>
                        <p style="font-size: 28px; font-weight: 700; color: var(--text-main);">8 / 8</p>
                    </div>
                    <div class="card" style="margin-bottom: 0; border-left: 4px solid #f59e0b;">
                        <h4 style="color: var(--text-muted); margin-bottom: 8px;">Ostrzeżenia i alerty</h4>
                        <p style="font-size: 28px; font-weight: 700; color: var(--text-main);">0</p>
                    </div>
                </div>
            </div>
        `,
        lifts: `
            <div class="module-content">
                <div class="card">
                    <h3>Zarządzanie wyciągami</h3>
                    <p>W tym miejscu znajdzie się lista wszystkich wyciągów. Będziesz mógł sprawdzić ich status na żywo oraz zarządzać włączaniem/wyłączaniem awaryjnym.</p>
                </div>
            </div>
        `,
        tariffs: `
            <div class="module-content">
                <div class="card">
                    <h3>Taryfy i cenniki</h3>
                    <p>Moduł umożliwiający dodawanie i edycję taryf czasowych, punktowych i sezonowych. Zmiany dokonane tutaj natychmiast zaktualizują bazę danych.</p>
                </div>
            </div>
        `,
        users: `
            <div class="module-content">
                <div class="card">
                    <h3>Użytkownicy i uprawnienia</h3>
                    <p>Zarządzanie kontami kasjerów, administratorów oraz nadawanie ról i uprawnień w systemie.</p>
                </div>
            </div>
        `,
        reports: `
            <div class="module-content">
                <div class="card">
                    <h3>Raporty i statystyki</h3>
                    <p>Moduł z zaawansowanymi zestawieniami sprzedaży karnetów, użycia wyciągów w czasie oraz analizami finansowymi.</p>
                </div>
            </div>
        `
    };

    // Inicjalizacja domyślnego widoku
    loadDashboardModule(mainContent, token);

    // Obsługa nawigacji w menu
    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            // Usunięcie klasy active ze wszystkich linków i dodanie do klikniętego
            navLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');

            // Aktualizacja tytułu strony na górnym pasku
            const targetTitle = link.textContent;
            pageTitle.textContent = targetTitle;

            // Pobranie docelowego modułu i aktualizacja głównego kontenera
            const targetId = link.getAttribute('data-target');

            switch (targetId) {
                case 'dashboard':
                    loadDashboardModule(mainContent, token);
                    break;
                case 'lifts':
                    loadLiftsModule(mainContent, token);
                    break;
                case 'gates':
                    loadGatesModule(mainContent, token);
                    break;
                case 'trails':
                    loadTrailsModule(mainContent, token);
                    break;
                case 'cards':
                    loadCardsModule(mainContent, token);
                    break;
                case 'tariffs':
                    if (typeof loadTariffsModule === 'function') loadTariffsModule(mainContent, token);
                    else mainContent.innerHTML = modulesContent['tariffs'];
                    break;
                case 'users':
                    if (userRole === 'admin') {
                        loadUsersModule(mainContent, token);
                    } else {
                        showToast('Brak uprawnień do tego modułu.', 'error');
                        loadDashboardModule(mainContent, token);
                    }
                    break;
                case 'reports':
                    loadReportsModule(mainContent, token);
                    break;
                case 'returns':
                    loadReturnsModule(mainContent, token);
                    break;
                default:
                    mainContent.innerHTML = '<div class="module-content"><div class="card"><p>Trwają prace nad tym modułem.</p></div></div>';
            }

            // Automatyczne zamknięcie menu bocznego na urządzeniach mobilnych po kliknięciu
            if (window.innerWidth <= 768) {
                sidebar.classList.remove('open');
            }
        });
    });

    // Ustawienie nazwy użytkownika w topbarze
    const userInfoName = document.querySelector('.user-name');
    const avatar = document.querySelector('.avatar');
    if (payload && payload.unique_name) {
        userInfoName.textContent = payload.unique_name;
        avatar.textContent = payload.unique_name.substring(0, 2).toUpperCase();
    } else if (payload && payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']) {
        const name = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
        userInfoName.textContent = name;
        avatar.textContent = name.substring(0, 2).toUpperCase();
    }

    // Obsługa przycisku "hamburgera" dla urządzeń mobilnych
    if (menuToggle) {
        menuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });
    }

    // Obsługa przycisku wylogowania
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            // Czyszczenie tokenów z pamięci przeglądarki
            localStorage.removeItem('jwt_token');
            sessionStorage.removeItem('jwt_token');

            // Przekierowanie na stronę logowania
            window.location.href = 'admin-login.html';
        });
    }
});

// Moduł Pulpitu (Dashboard)
async function loadDashboardModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card" style="margin-bottom: 24px;">
                <h3>Pulpit</h3>
                <p>Statystyki systemowe w czasie rzeczywistym.</p>
            </div>
            <div id="stats-grid" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 24px; margin-bottom: 24px;">
                <div class="card" style="margin-bottom: 0; border-left: 4px solid var(--primary-color);">
                    <h4 style="color: var(--text-muted); margin-bottom: 8px;">Aktywne karnety</h4>
                    <p id="stat-active-passes" style="font-size: 28px; font-weight: 700; color: var(--text-main);">...</p>
                </div>
                <div class="card" style="margin-bottom: 0; border-left: 4px solid #10b981;">
                    <h4 style="color: var(--text-muted); margin-bottom: 8px;">Sprzedane bilety (dziś)</h4>
                    <p id="stat-tickets-sold" style="font-size: 28px; font-weight: 700; color: var(--text-main);">...</p>
                </div>
                <div class="card" style="margin-bottom: 0; border-left: 4px solid #6366f1;">
                    <h4 style="color: var(--text-muted); margin-bottom: 8px;">Przychód (dziś)</h4>
                    <p id="stat-revenue" style="font-size: 28px; font-weight: 700; color: var(--text-main);">...</p>
                </div>
                <div class="card" style="margin-bottom: 0; border-left: 4px solid #f59e0b;">
                    <h4 style="color: var(--text-muted); margin-bottom: 8px;">Oczekujące zwroty</h4>
                    <p id="stat-pending-returns" style="font-size: 28px; font-weight: 700; color: var(--text-main);">...</p>
                </div>
            </div>
            <div class="card">
                <h4 style="color: var(--text-main); margin-bottom: 16px;">Ruch na wyciągach (Skanowania bramek dzisiaj)</h4>
                <div style="position: relative; height: 350px; width: 100%;">
                    <canvas id="trafficChart"></canvas>
                </div>
            </div>
        </div>
    `;

    try {
        const response = await fetch('/api/statystyki/dzisiaj', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            document.getElementById('stat-active-passes').textContent = data.activePasses;
            document.getElementById('stat-tickets-sold').textContent = data.ticketsSoldToday;
            document.getElementById('stat-revenue').textContent = data.shiftRevenue.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' });
            document.getElementById('stat-pending-returns').textContent = data.pendingReturns;
        }
    } catch (error) {
        console.error("Dashboard error (dzisiaj):", error);
    }

    try {
        const response = await fetch('/api/statystyki/wyciagi', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();

            const labels = data.map(d => d.liftName);
            const values = data.map(d => d.count);

            const ctx = document.getElementById('trafficChart').getContext('2d');

            // Destroy existing chart if module is reloaded
            if (window.trafficChartInstance) {
                window.trafficChartInstance.destroy();
            }

            window.trafficChartInstance = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels.length > 0 ? labels : ['Brak danych'],
                    datasets: [{
                        label: 'Liczba przejazdów',
                        data: values.length > 0 ? values : [0],
                        backgroundColor: 'rgba(59, 130, 246, 0.7)',
                        borderColor: 'rgb(59, 130, 246)',
                        borderWidth: 1,
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                precision: 0
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            display: false
                        }
                    }
                }
            });
        }
    } catch (error) {
        console.error("Dashboard error (wyciagi):", error);
    }
}

// Moduł Taryf
async function loadTariffsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Taryfy i cenniki</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Zarządzaj ofertą: cennikami, biletami i karnetami.</p>
                    </div>
                    <button class="btn-primary" onclick="showTariffForm()">Dodaj Taryfę</button>
                </div>
                <div id="tariffs-container">
                    <p id="tariffs-loading">Pobieranie danych...</p>
                    <table class="data-table" id="tariffs-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Nazwa</th>
                                <th style="padding: 12px; color: var(--text-muted);">Typ karnetu</th>
                                <th style="padding: 12px; color: var(--text-muted);">Sezon</th>
                                <th style="padding: 12px; color: var(--text-muted);">Cena</th>
                                <th style="padding: 12px; color: var(--text-muted);">Limit pkt/h</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    try {
        const response = await fetch('/api/taryfy', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('#tariffs-table tbody');
            
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" style="padding: 12px; text-align: center; color: var(--text-muted);">Brak taryf w bazie danych.</td></tr>';
            } else {
                tbody.innerHTML = data.map(t => {
                    const tJson = JSON.stringify(t).replace(/"/g, '&quot;');
                    return `
                    <tr style="border-bottom: 1px solid var(--border-color);">
                        <td style="padding: 12px; font-weight: 500;">${t.name}</td>
                        <td style="padding: 12px;">${t.passType || '-'}</td>
                        <td style="padding: 12px;">${t.season || '-'}</td>
                        <td style="padding: 12px; font-weight: 600;">${t.price ? t.price.toFixed(2) + ' zł' : '-'}</td>
                        <td style="padding: 12px;">${t.poolLimit || '-'}</td>
                        <td style="padding: 12px; text-align: right;">
                            <button class="action-btn edit" onclick="showTariffForm(${tJson})" title="Edytuj">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                            </button>
                            <button class="action-btn delete" onclick="deleteTariff(${t.id})" title="Usuń">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                            </button>
                        </td>
                    </tr>
                `}).join('');
            }
            document.getElementById('tariffs-loading').style.display = 'none';
            document.getElementById('tariffs-table').style.display = 'table';
        }
    } catch (error) {
        console.error("Tariffs error:", error);
    }
}

function showTariffForm(tariff = null) {
    const isEdit = !!tariff;
    const title = isEdit ? 'Edytuj Taryfę' : 'Dodaj Taryfę';

    const content = `
        <form id="tariff-form" onsubmit="handleTariffSubmit(event, ${isEdit ? tariff.id : 'null'})">
            <div class="form-group">
                <label for="tariff-name">Nazwa taryfy</label>
                <input type="text" id="tariff-name" class="form-control" required value="${isEdit ? tariff.name : ''}">
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="tariff-passType">Typ karnetu</label>
                    <input type="text" id="tariff-passType" class="form-control" value="${isEdit && tariff.passType ? tariff.passType : ''}" placeholder="np. czasowy, punktowy">
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="tariff-season">Sezon</label>
                    <input type="text" id="tariff-season" class="form-control" value="${isEdit && tariff.season ? tariff.season : ''}" placeholder="np. wysoki, niski">
                </div>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="tariff-price">Cena (zł)</label>
                    <input type="number" step="0.01" id="tariff-price" class="form-control" required value="${isEdit && tariff.price !== null ? tariff.price : ''}">
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="tariff-poolLimit">Limit punktów/godzin</label>
                    <input type="number" id="tariff-poolLimit" class="form-control" value="${isEdit && tariff.poolLimit !== null ? tariff.poolLimit : ''}">
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
    `;
    openModal(title, content);
}

async function handleTariffSubmit(event, tariffId) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');

    const name = document.getElementById('tariff-name').value;
    const price = parseFloat(document.getElementById('tariff-price').value);

    if (name.length < 3) {
        showToast('Nazwa taryfy musi mieć co najmniej 3 znaki.', 'error');
        return;
    }
    if (isNaN(price) || price < 0) {
        showToast('Cena nie może być ujemna.', 'error');
        return;
    }

    const payload = {
        name: name,
        passType: document.getElementById('tariff-passType').value || null,
        season: document.getElementById('tariff-season').value || null,
        price: price,
        poolLimit: document.getElementById('tariff-poolLimit').value ? parseInt(document.getElementById('tariff-poolLimit').value) : null
    };

    const method = tariffId ? 'PUT' : 'POST';
    const url = tariffId ? `/api/taryfy/${tariffId}` : '/api/taryfy';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            loadTariffsModule(document.getElementById('main-content'), token);
            showToast(tariffId ? 'Taryfa zaktualizowana.' : 'Nowa taryfa dodana.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function deleteTariff(tariffId) {
    if (!confirm('Czy na pewno chcesz usunąć tę taryfę? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/taryfy/${tariffId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            showToast('Usunięto pomyślnie.', 'success');
            loadTariffsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

// Moduł Raportów (Transakcje)
async function loadReportsModule(container, token) {
    // Pobierzmy najpierw listę kasjerów by zasilić dropdown
    let cashiersOptions = '<option value="">Wszyscy kasjerzy</option>';
    try {
        const usersRes = await fetch('/api/users/all', { headers: { 'Authorization': 'Bearer ' + token } });
        if (usersRes.ok) {
            const users = await usersRes.json();
            const cashiers = users.filter(u => u.role === 'kasjer' || u.role === 'admin');
            cashiers.forEach(c => {
                cashiersOptions += `<option value="${c.id}">${c.login || c.email}</option>`;
            });
        }
    } catch (e) {
        console.error("Could not load users for reports filter", e);
    }

    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <h3>Raporty - Ostatnie transakcje</h3>
                <div style="margin-bottom: 24px; display: flex; gap: 16px; align-items: flex-end;">
                    <div class="form-group" style="margin-bottom: 0;">
                        <label for="filter-date">Data</label>
                        <input type="date" id="filter-date" class="form-control">
                    </div>
                    <div class="form-group" style="margin-bottom: 0;">
                        <label for="filter-cashier">Kasjer</label>
                        <select id="filter-cashier" class="form-control">
                            ${cashiersOptions}
                        </select>
                    </div>
                    <button class="btn-primary" onclick="fetchTransactions('${token}')">Filtruj</button>
                    <button class="btn-secondary" onclick="document.getElementById('filter-date').value=''; document.getElementById('filter-cashier').value=''; fetchTransactions('${token}')">Czyść</button>
                </div>
                <div id="reports-container">
                    <p id="reports-loading">Wybierz filtry i kliknij Filtruj...</p>
                    <table class="data-table" id="reports-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Data</th>
                                <th style="padding: 12px; color: var(--text-muted);">Typ operacji</th>
                                <th style="padding: 12px; color: var(--text-muted);">Taryfa</th>
                                <th style="padding: 12px; color: var(--text-muted);">Kwota</th>
                                <th style="padding: 12px; color: var(--text-muted);">Kasjer</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
        `;

    // Wywołaj domyślnie na starcie
    fetchTransactions(token);
}

async function fetchTransactions(token) {
    const dateVal = document.getElementById('filter-date').value;
    const cashierVal = document.getElementById('filter-cashier').value;
    
    const url = `/api/transakcje?date=${dateVal}&cashierId=${cashierVal}`;

    document.getElementById('reports-loading').style.display = 'block';
    document.getElementById('reports-loading').textContent = 'Pobieranie danych...';
    document.getElementById('reports-table').style.display = 'none';

    try {
        const response = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('#reports-table tbody');
            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="5" style="padding: 12px; text-align: center;">Brak transakcji w tym okresie/dla tego kasjera.</td></tr>';
            } else {
                tbody.innerHTML = data.map(t => `
                    <tr style="border-bottom: 1px solid var(--border-color);">
                        <td style="padding: 12px;">${new Date(t.date).toLocaleString('pl-PL')}</td>
                        <td style="padding: 12px;">${t.operationType || '-'}</td>
                        <td style="padding: 12px;">${t.tariff || '-'}</td>
                        <td style="padding: 12px; font-weight: 600; color: ${t.amount < 0 ? '#ef4444' : 'inherit'}">${t.amount.toFixed(2)} zł</td>
                        <td style="padding: 12px;">${t.cashierLogin || '-'}</td>
                    </tr>
                `).join('');
            }
            document.getElementById('reports-loading').style.display = 'none';
            document.getElementById('reports-table').style.display = 'table';
        } else {
            document.getElementById('reports-loading').textContent = 'Wystąpił błąd podczas pobierania transakcji.';
        }
    } catch (error) {
        console.error("Reports error:", error);
        document.getElementById('reports-loading').textContent = 'Błąd połączenia z serwerem.';
    }
}

// Moduł Użytkowników
async function loadUsersModule(container, token) {
    container.innerHTML = `
        <div class="module-content" >
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Użytkownicy i uprawnienia</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Zarządzanie kontami personelu (kasjerów, administratorów).</p>
                    </div>
                    <button class="btn-primary" onclick="showUserForm()">Dodaj Użytkownika</button>
                </div>
                <div id="users-container">
                    <p id="users-loading">Pobieranie listy użytkowników...</p>
                    <table class="data-table" id="users-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Login / Email</th>
                                <th style="padding: 12px; color: var(--text-muted);">Rola</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
        `;

    try {
        const response = await fetch('/api/users/all', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('#users-table tbody');
            tbody.innerHTML = data.map(u => {
                const uJson = JSON.stringify(u).replace(/"/g, '&quot;');
                const canEdit = u.role === 'admin' || u.role === 'kasjer';
                
                return `
        <tr style = "border-bottom: 1px solid var(--border-color);" >
                    <td style="padding: 12px; font-weight: 500;">${u.login || u.email}</td>
                    <td style="padding: 12px;">${u.role}</td>
                    <td style="padding: 12px;">${u.isActive ? '<span style="color: #10b981">Aktywny</span>' : '<span style="color: #ef4444">Nieaktywny</span>'}</td>
                    <td style="padding: 12px; text-align: right;">
                        ${canEdit ? `
                        <button class="action-btn edit" onclick="showUserForm(${uJson})" title="Edytuj">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                        </button>
                        <button class="action-btn edit" onclick="resetUserPassword(${u.id}, '${u.role}', '${u.login || u.email}', ${u.isActive})" title="Resetuj hasło" style="color: #f59e0b; border-color: #f59e0b; background-color: #fffbeb;">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4"></path></svg>
                        </button>
                        <button class="action-btn delete" onclick="deleteUser(${u.id}, '${u.role}')" title="Usuń">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                        </button>
                        ` : ''}
                    </td>
                </tr>
        `}).join('');
            document.getElementById('users-loading').style.display = 'none';
            document.getElementById('users-table').style.display = 'table';
        } else {
            document.getElementById('users-loading').textContent = "Brak uprawnień lub brak endpointu do pobrania listy użytkowników.";
        }
    } catch (error) {
        document.getElementById('users-loading').textContent = "Błąd połączenia z serwerem.";
    }
}

function showUserForm(user = null) {
    const isEdit = !!user;
    const title = isEdit ? 'Edytuj Użytkownika' : 'Dodaj Użytkownika';
    
    const content = `
        <form id="user-form" onsubmit="handleUserSubmit(event, ${isEdit ? user.id : 'null'}, '${isEdit ? user.role : ''}')">
            <div class="form-group">
                <label for="user-login">Login / Email</label>
                <input type="text" id="user-login" class="form-control" required value="${isEdit ? user.login : ''}">
            </div>
            <div class="form-group">
                <label for="user-password">Hasło ${isEdit ? '(Zostaw puste, aby nie zmieniać)' : ''}</label>
                <input type="password" id="user-password" class="form-control" ${isEdit ? '' : 'required'}>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="user-role">Rola</label>
                    <select id="user-role" class="form-control" ${isEdit ? 'disabled' : ''}>
                        <option value="kasjer" ${isEdit && user.role === 'kasjer' ? 'selected' : ''}>Kasjer</option>
                        <option value="admin" ${isEdit && user.role === 'admin' ? 'selected' : ''}>Administrator</option>
                    </select>
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="user-active">Status</label>
                    <select id="user-active" class="form-control">
                        <option value="true" ${!isEdit || user.isActive ? 'selected' : ''}>Aktywny</option>
                        <option value="false" ${isEdit && !user.isActive ? 'selected' : ''}>Nieaktywny</option>
                    </select>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
        `;
    openModal(title, content);
}

async function handleUserSubmit(event, userId, existingRole) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const login = document.getElementById('user-login').value;
    const password = document.getElementById('user-password').value;

    if (login.length < 3) {
        showToast('Login musi mieć co najmniej 3 znaki.', 'error');
        return;
    }
    if (!userId && password.length < 4) {
        showToast('Nowe hasło musi mieć co najmniej 4 znaki.', 'error');
        return;
    }

    const role = userId ? existingRole : document.getElementById('user-role').value;
    const isActive = document.getElementById('user-active').value === 'true';
    
    const payload = {
        login: login,
        password: password || null,
        role: role,
        isActive: isActive
    };

    const method = userId ? 'PUT' : 'POST';
    const url = userId ? `/api/users/${userId}` : '/api/users';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            loadUsersModule(document.getElementById('main-content'), token);
            showToast('Użytkownik zapisany pomyślnie.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

function resetUserPassword(userId, role, login, isActive) {
    const title = 'Wymuś zmianę hasła';
    
    const content = `
        <form id = "reset-password-form" onsubmit = "handlePasswordResetSubmit(event, ${userId}, '${role}', '${login}', ${isActive})" >
            <p>Zmieniasz hasło dla użytkownika: <strong>${login}</strong></p>
            <div class="form-group">
                <label for="new-password">Nowe hasło</label>
                <input type="password" id="new-password" class="form-control" required minlength="4">
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zmień hasło</button>
            </div>
        </form>
        `;
    openModal(title, content);
}

async function handlePasswordResetSubmit(event, userId, role, login, isActive) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const payload = {
        login: login,
        password: document.getElementById('new-password').value,
        role: role,
        isActive: isActive
    };

    try {
        const response = await fetch(`/api/users/${userId}`, {
            method: 'PUT',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            showToast('Hasło zostało pomyślnie zmienione.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function deleteUser(userId, role) {
    if (!confirm('Czy na pewno chcesz usunąć/deaktywować to konto?')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/users/${userId}?role=${role}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            loadUsersModule(document.getElementById('main-content'), token);
            showToast('Użytkownik usunięty.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

// Zewnętrzna asynchroniczna funkcja do modułu Wyciągów
async function loadLiftsModule(container, token, showSkeleton = true) {
    if (showSkeleton) {
        container.innerHTML = `
            <div class="module-content" >
                <div class="card">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                        <div>
                            <h3>Zarządzanie wyciągami</h3>
                            <p style="color: var(--text-muted); margin-top: 8px;">Poniżej znajduje się aktualny wykaz wyciągów pobrany bezpośrednio z systemu.</p>
                        </div>
                        <button class="btn-primary" onclick="showLiftForm()">Dodaj Wyciąg</button>
                    </div>

                    <div id="lifts-container">
                        <p id="lifts-loading">Pobieranie danych z bazy...</p>
                        <table class="data-table" id="lifts-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                            <thead>
                                <tr style="border-bottom: 2px solid var(--border-color);">
                                    <th style="padding: 12px; color: var(--text-muted);">Nazwa (ID)</th>
                                    <th style="padding: 12px; color: var(--text-muted);">Parametry</th>
                                    <th style="padding: 12px; color: var(--text-muted);">Status (Szybka Zmiana)</th>
                                    <th style="padding: 12px; color: var(--text-muted);">Trasy</th>
                                    <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                                </tr>
                            </thead>
                            <tbody>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
            `;
    }

    try {
        // 2. Fetch danych (żądanie GET do kontrolera WyciagController)
        const response = await fetch('/api/wyciagi', {
            method: 'GET',
            headers: {
                // Autoryzacja Bearer
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Błąd HTTP: ${ response.status } `);
        }

        const data = await response.json();

        // 3. Renderowanie i generowanie HTML na podstawie otrzymanego JSON
        const tbody = document.querySelector('#lifts-table tbody');
        tbody.innerHTML = ''; // czyszczenie wierszy

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="padding: 12px; text-align: center; color: var(--text-muted);">Brak wyciągów w bazie danych.</td></tr>';
        } else {
            data.forEach(lift => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = '1px solid var(--border-color)';

                const opens = lift.opensAt ? lift.opensAt.substring(0, 5) : '-';
                const closes = lift.closesAt ? lift.closesAt.substring(0, 5) : '-';

                const liftJson = JSON.stringify(lift).replace(/"/g, '&quot;');
                
                // Mapowanie statusu powracającego z API
                let statusVal = "";
                if(lift.status === 'Otwarty') statusVal = 1;
                else if(lift.status === 'Zamknięty z powodu warunków') statusVal = 2;
                else if(lift.status === 'Awaria') statusVal = 3;
                else if(lift.status === 'Przerwa techniczna') statusVal = 4;

                let statusSelect = `
                    <select class="form-control" style="width: auto; display: inline-block; padding: 4px; font-size: 13px;" onchange="updateLiftStatus(${lift.id}, this.value)">
                        <option value="" ${statusVal === "" ? 'selected' : ''}>Wg harmonogramu (${lift.status})</option>
                        <option value="1" ${statusVal === 1 ? 'selected' : ''} style="color: #10b981;">Otwarty</option>
                        <option value="2" ${statusVal === 2 ? 'selected' : ''} style="color: #f59e0b;">Zamknięty z powodu warunków</option>
                        <option value="3" ${statusVal === 3 ? 'selected' : ''} style="color: #ef4444;">Awaria</option>
                        <option value="4" ${statusVal === 4 ? 'selected' : ''} style="color: #f59e0b;">Przerwa techniczna</option>
                    </select>
                `;

                tr.innerHTML = `
                    <td style="padding: 12px; font-weight: 500;">${lift.name} <span style="color: var(--text-muted); font-size: 12px;">(#${lift.id})</span></td>
                    <td style="padding: 12px; font-size: 13px;">
                        Typ: ${lift.type || '-'} <br>
                        Przepustowość: ${lift.capacity ? lift.capacity + ' os/h' : '-'} <br>
                        Dziś: ${opens} - ${closes}
                    </td>
                    <td style="padding: 12px;">${statusSelect}</td>
                    <td style="padding: 12px; font-size: 13px;">${lift.trails && lift.trails.length > 0 ? lift.trails.map(t => t.name).join(', ') : '-'}</td>
                    <td style="padding: 12px; text-align: right; white-space: nowrap;">
                        <button class="action-btn edit" onclick="showLiftForm(${liftJson})" title="Edytuj">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                        </button>
                        <button class="action-btn delete" onclick="deleteLift(${lift.id})" title="Usuń">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }

        // Ukrycie komunikatu ładowania i pokazanie tabeli
        document.getElementById('lifts-loading').style.display = 'none';
        document.getElementById('lifts-table').style.display = 'table';

    } catch (error) {
        document.getElementById('lifts-loading').innerHTML =
            `<span style = "color: var(--danger);" > Nie udało się pobrać wyciągów: ${ error.message }</span> `;
        console.error("Fetch error:", error);
    }
}

// --- LOGIKA MODALI I CRUD ---

function openModal(title, contentHtml) {
    document.getElementById('modal-title').textContent = title;
    document.getElementById('modal-body').innerHTML = contentHtml;
    const modalContainer = document.getElementById('modal-container');
    modalContainer.style.display = 'flex';
    // Wymuszamy reflow, aby animacja zadziałała
    void modalContainer.offsetWidth;
    modalContainer.classList.add('show');

    // Podpięcie zamykania
    document.getElementById('modal-close-btn').onclick = closeModal;
}

function closeModal() {
    const modalContainer = document.getElementById('modal-container');
    modalContainer.classList.remove('show');
    setTimeout(() => {
        modalContainer.style.display = 'none';
    }, 300); // Odpowiada var(--transition-speed)
}

function showLiftForm(lift = null) {
    const isEdit = !!lift;
    const title = isEdit ? 'Edytuj Wyciąg' : 'Dodaj Wyciąg';
    
    // Obcinanie sekund, jeśli istnieją, dla input[type=time]
    let opens = lift && lift.opensAt ? lift.opensAt.substring(0, 5) : '';
    let closes = lift && lift.closesAt ? lift.closesAt.substring(0, 5) : '';
    
    // Konwersja formatu 'PT8H' na '08:00' gdyby API zwracało dziwny format
    if (opens.includes('PT')) opens = ''; 
    if (closes.includes('PT')) closes = '';

    const content = `
        <form id = "lift-form" onsubmit = "handleLiftSubmit(event, ${isEdit ? lift.id : 'null'})" >
            <div class="form-group">
                <label for="lift-name">Nazwa wyciągu</label>
                <input type="text" id="lift-name" class="form-control" required value="${isEdit ? lift.name : ''}">
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="lift-type">Rodzaj wyciągu</label>
                    <select id="lift-type" class="form-control">
                        <option value="">Wybierz...</option>
                        <option value="Orczyk" ${isEdit && lift.type === 'Orczyk' ? 'selected' : ''}>Orczyk</option>
                        <option value="Krzesełko" ${isEdit && lift.type === 'Krzesełko' ? 'selected' : ''}>Krzesełko</option>
                        <option value="Gondola" ${isEdit && lift.type === 'Gondola' ? 'selected' : ''}>Gondola</option>
                        <option value="Talerzyk" ${isEdit && lift.type === 'Talerzyk' ? 'selected' : ''}>Talerzyk</option>
                    </select>
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="lift-capacity">Przepustowość (os./h)</label>
                    <input type="number" id="lift-capacity" class="form-control" value="${isEdit && lift.capacity ? lift.capacity : ''}">
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
        `;
    openModal(title, content);
}

async function updateLiftStatus(liftId, statusId) {
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const response = await fetch(`/api/wyciagi/${liftId}/status`, {
            method: 'PUT',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ statusId: statusId ? parseInt(statusId) : null })
        });
        if (!response.ok) {
            showToast('Nie udało się zmienić statusu.', 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function handleLiftSubmit(event, liftId) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const name = document.getElementById('lift-name').value;
    if (name.length < 3) {
        showToast('Nazwa wyciągu jest za krótka.', 'error');
        return;
    }

    const typeInput = document.getElementById('lift-type').value;
    const capacityInput = document.getElementById('lift-capacity').value;
    
    const payload = {
        name: name,
        type: typeInput || null,
        capacity: capacityInput ? parseInt(capacityInput) : null,
        status: ''
    };

    if (liftId) {
        payload.id = liftId;
    }

    const method = liftId ? 'PUT' : 'POST';
    const url = liftId ? `/api/wyciagi/${liftId}` : '/api/wyciagi';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            loadLiftsModule(document.getElementById('main-content'), token);
            showToast('Wyciąg zapisany.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function deleteLift(liftId) {
    if (!confirm('Czy na pewno chcesz usunąć ten wyciąg? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/wyciagi/${liftId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            showToast('Wyciąg usunięty.', 'success');
            loadLiftsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}
// Moduł Bramek
async function loadGatesModule(container, token) {
    container.innerHTML = `
        <div class="module-content" >
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Zarządzanie bramkami</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Poniżej znajduje się wykaz bramek wejściowych przypisanych do wyciągów.</p>
                    </div>
                    <button class="btn-primary" onclick="showGateForm()">Dodaj Bramkę</button>
                </div>

                <div id="gates-container">
                    <p id="gates-loading">Pobieranie danych z bazy...</p>
                    <table class="data-table" id="gates-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">ID</th>
                                <th style="padding: 12px; color: var(--text-muted);">Nazwa Bramki</th>
                                <th style="padding: 12px; color: var(--text-muted);">Przypisany Wyciąg</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
        `;

    try {
        const response = await fetch('/api/bramki', {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Błąd HTTP: ${ response.status } `);
        }

        const data = await response.json();
        const tbody = document.querySelector('#gates-table tbody');
        tbody.innerHTML = '';

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="padding: 12px; text-align: center; color: var(--text-muted);">Brak bramek w bazie danych.</td></tr>';
        } else {
            data.forEach(gate => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = '1px solid var(--border-color)';

                let statusColor = gate.isActive ? '#10b981' : '#ef4444';
                let statusText = gate.isActive ? 'Aktywna' : 'Nieaktywna';
                
                let onlineColor = gate.isOnline ? '#10b981' : '#ef4444';
                let onlineText = gate.isOnline ? 'Online' : 'Offline';

                const gateJson = JSON.stringify(gate).replace(/"/g, '&quot;');

                tr.innerHTML = `
                    <td style="padding: 12px;">${gate.id}</td>
                    <td style="padding: 12px; font-weight: 500;">
                        ${gate.name || '-'}
                        <span style="margin-left: 8px; font-size: 11px; padding: 2px 6px; border-radius: 12px; background-color: ${onlineColor}22; color: ${onlineColor}; display: inline-flex; align-items: center; gap: 4px;">
                            <div style="width: 6px; height: 6px; border-radius: 50%; background-color: ${onlineColor};"></div>
                            ${onlineText}
                        </span>
                    </td>
                    <td style="padding: 12px;">${gate.liftName || '(Brak)'}</td>
                    <td style="padding: 12px; color: ${statusColor}; font-weight: 600;">${statusText}</td>
                    <td style="padding: 12px; text-align: right;">
                        <button class="action-btn edit" onclick="showGateForm(${gateJson})" title="Edytuj">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                        </button>
                        <button class="action-btn delete" onclick="deleteGate(${gate.id})" title="Usuń">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                        </button>
                    </td>
    `;
                tbody.appendChild(tr);
            });
        }

        document.getElementById('gates-loading').style.display = 'none';
        document.getElementById('gates-table').style.display = 'table';

    } catch (error) {
        document.getElementById('gates-loading').innerHTML =
            `<span style = "color: var(--danger);" > Nie udało się pobrać bramek: ${ error.message }</span> `;
        console.error("Fetch gates error:", error);
    }
}

async function showGateForm(gate = null) {
    const isEdit = !!gate;
    const title = isEdit ? 'Edytuj Bramkę' : 'Dodaj Bramkę';
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    // Pobierz wyciągi do selecta
    let liftsOptions = '<option value="">Wybierz wyciąg</option>';
    try {
        const res = await fetch('/api/wyciagi', { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const lifts = await res.json();
            lifts.forEach(l => {
                const selected = isEdit && gate.liftId === l.id ? 'selected' : '';
                liftsOptions += `<option value = "${l.id}" ${ selected }> ${ l.name }</option> `;
            });
        }
    } catch (e) {
        console.error("Could not load lifts", e);
    }

    const content = `
        <form id = "gate-form" onsubmit = "handleGateSubmit(event, ${isEdit ? gate.id : 'null'})" >
            <div class="form-group">
                <label for="gate-name">Nazwa bramki</label>
                <input type="text" id="gate-name" class="form-control" required value="${isEdit ? gate.name : ''}">
            </div>
            <div class="form-group">
                <label for="gate-lift">Wyciąg</label>
                <select id="gate-lift" class="form-control" required>
                    ${liftsOptions}
                </select>
            </div>
            <div class="form-group">
                <label for="gate-active">Status</label>
                <select id="gate-active" class="form-control">
                    <option value="true" ${!isEdit || gate.isActive ? 'selected' : ''}>Aktywna</option>
                    <option value="false" ${isEdit && !gate.isActive ? 'selected' : ''}>Nieaktywna</option>
                </select>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
        `;
    openModal(title, content);
}

async function handleGateSubmit(event, gateId) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const name = document.getElementById('gate-name').value;
    if (name.length < 2) {
        showToast('Nazwa bramki jest za krótka.', 'error');
        return;
    }

    const payload = {
        name: name,
        liftId: parseInt(document.getElementById('gate-lift').value),
        isActive: document.getElementById('gate-active').value === 'true'
    };

    const method = gateId ? 'PUT' : 'POST';
    const url = gateId ? `/api/bramki/${gateId}` : '/api/bramki';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            loadGatesModule(document.getElementById('main-content'), token);
            showToast('Bramka zapisana.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function deleteGate(gateId) {
    if (!confirm('Czy na pewno chcesz usunąć tę bramkę? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/bramki/${gateId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            showToast('Bramka usunięta.', 'success');
            loadGatesModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

// Moduł Zwrotów
async function loadReturnsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Weryfikacja zwrotów</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Zarządzaj oczekującymi wnioskami o zwrot karnetów zgłoszonymi przez użytkowników.</p>
                    </div>
                </div>

                <div id="returns-container">
                    <p id="returns-loading">Pobieranie oczekujących zwrotów...</p>
                    <table class="data-table" id="returns-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Karta RFID</th>
                                <th style="padding: 12px; color: var(--text-muted);">Użytkownik</th>
                                <th style="padding: 12px; color: var(--text-muted);">Typ karnetu</th>
                                <th style="padding: 12px; color: var(--text-muted);">Ważny do</th>
                                <th style="padding: 12px; color: var(--text-muted);">Zostało dni</th>
                                <th style="padding: 12px; color: var(--text-muted);">Estymowany zwrot</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
        `;

    try {
        const response = await fetch('/api/zwroty/oczekujace', {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Błąd HTTP: ${response.status}`);
        }

        const data = await response.json();
        const tbody = document.querySelector('#returns-table tbody');
        tbody.innerHTML = '';

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" style="padding: 12px; text-align: center; color: var(--text-muted);">Brak oczekujących zwrotów.</td></tr>';
        } else {
            data.forEach(ret => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = '1px solid var(--border-color)';

                const validTo = ret.validTo ? new Date(ret.validTo).toLocaleDateString('pl-PL') : '-';
                const refundStr = ret.estimatedRefund.toFixed(2) + ' zł';

                tr.innerHTML = `
                    <td style="padding: 12px; font-family: monospace;">${ret.cardRfid}</td>
                    <td style="padding: 12px;">${ret.ownerEmail || '-'}</td>
                    <td style="padding: 12px;">${ret.passType || '-'}</td>
                    <td style="padding: 12px;">${validTo}</td>
                    <td style="padding: 12px;">${ret.remainingDays}</td>
                    <td style="padding: 12px; font-weight: 600; color: #10b981;">${refundStr}</td>
                    <td style="padding: 12px; text-align: right; white-space: nowrap;">
                        <button class="btn-primary" style="padding: 6px 12px; font-size: 13px;" onclick="approveReturn(${ret.passId}, ${ret.estimatedRefund})">Zatwierdź</button>
                        <button class="btn-secondary" style="padding: 6px 12px; font-size: 13px; margin-left: 8px;" onclick="rejectReturn(${ret.passId})">Odrzuć</button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }

        document.getElementById('returns-loading').style.display = 'none';
        document.getElementById('returns-table').style.display = 'table';

    } catch (error) {
        document.getElementById('returns-loading').innerHTML =
            `<span style="color: var(--danger);">Nie udało się pobrać zwrotów: ${error.message}</span>`;
        console.error("Fetch returns error:", error);
    }
}

async function approveReturn(passId, amount) {
    if (!confirm(`Czy na pewno chcesz zatwierdzić zwrot w kwocie ${amount.toFixed(2)} zł? Karta zostanie zwrócona do puli.`)) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/zwroty/${passId}/zatwierdz`, {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ refundAmount: amount, returnCard: true })
        });

        if (response.ok) {
            loadReturnsModule(document.getElementById('main-content'), token);
            showToast('Zwrot zatwierdzony pomyślnie.', 'success');
        } else {
            showToast('Wystąpił błąd podczas zatwierdzania.', 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function rejectReturn(passId) {
    if (!confirm('Czy na pewno chcesz odrzucić ten wniosek o zwrot? Karnet znów będzie aktywny.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/zwroty/${passId}/odrzuc`, {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            loadReturnsModule(document.getElementById('main-content'), token);
            showToast('Zwrot odrzucony.', 'success');
        } else {
            showToast('Wystąpił błąd podczas odrzucania.', 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

// Moduł Tras
async function loadTrailsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Zarządzanie trasami</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Lista tras narciarskich wraz z poziomami trudności i długością.</p>
                    </div>
                    <button class="btn-primary" onclick="showTrailForm()">Dodaj Trasę</button>
                </div>

                <div id="trails-container">
                    <p id="trails-loading">Pobieranie danych z bazy...</p>
                    <table class="data-table" id="trails-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Nazwa (ID)</th>
                                <th style="padding: 12px; color: var(--text-muted);">Parametry</th>
                                <th style="padding: 12px; color: var(--text-muted);">Stan Trasy</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status (Szybka Zmiana)</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    try {
        const response = await fetch('/api/trasy', {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Błąd HTTP: ${response.status}`);
        }

        const data = await response.json();
        const tbody = document.querySelector('#trails-table tbody');
        tbody.innerHTML = '';

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="padding: 12px; text-align: center; color: var(--text-muted);">Brak tras w bazie danych.</td></tr>';
        } else {
            data.forEach(trail => {
                const tr = document.createElement('tr');
                tr.style.borderBottom = '1px solid var(--border-color)';

                const trailJson = JSON.stringify(trail).replace(/"/g, '&quot;');
                
                let statusVal = "";
                if(trail.statusName === 'Otwarta') statusVal = 1;
                else if(trail.statusName === 'Zamknięta') statusVal = 2;

                let statusSelect = `
                    <select class="form-control" style="width: auto; display: inline-block; padding: 4px; font-size: 13px;" onchange="updateTrailStatus(${trail.id}, this.value)">
                        <option value="" ${statusVal === "" ? 'selected' : ''}>Brak</option>
                        <option value="1" ${statusVal === 1 ? 'selected' : ''} style="color: #10b981;">Otwarta</option>
                        <option value="2" ${statusVal === 2 ? 'selected' : ''} style="color: #ef4444;">Zamknięta</option>
                    </select>
                `;

                tr.innerHTML = `
                    <td style="padding: 12px; font-weight: 500;">${trail.name} <span style="color: var(--text-muted); font-size: 12px;">(#${trail.id})</span></td>
                    <td style="padding: 12px; font-size: 13px;">
                        Trudność: ${trail.difficulty || '-'} <br>
                        Długość: ${trail.length ? trail.length + ' m' : '-'} <br>
                        Lokalizacja: ${trail.location || '-'}
                    </td>
                    <td style="padding: 12px; font-size: 13px;">
                        Naśnieżenie: ${trail.snowCondition || '-'} <br>
                        Przygotowanie: ${trail.preparationStatus || '-'}
                    </td>
                    <td style="padding: 12px;">${statusSelect}</td>
                    <td style="padding: 12px; text-align: right;">
                        <button class="action-btn edit" onclick="showTrailForm(${trailJson})" title="Edytuj">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                        </button>
                        <button class="action-btn delete" onclick="deleteTrail(${trail.id})" title="Usuń">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                        </button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }

        document.getElementById('trails-loading').style.display = 'none';
        document.getElementById('trails-table').style.display = 'table';

    } catch (error) {
        document.getElementById('trails-loading').innerHTML =
            `<span style="color: var(--danger);">Nie udało się pobrać tras: ${error.message}</span>`;
        console.error("Fetch trails error:", error);
    }
}

async function showTrailForm(trail = null) {
    const isEdit = !!trail;
    const title = isEdit ? 'Edytuj Trasę' : 'Dodaj Trasę';
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    // Pobierz trudności do selecta
    let diffOptions = '<option value="">Wybierz trudność</option>';
    try {
        const res = await fetch('/api/trasy/trudnosci', { headers: { 'Authorization': 'Bearer ' + token } });
        if (res.ok) {
            const difficulties = await res.json();
            difficulties.forEach(d => {
                const selected = isEdit && trail.difficulty === d.name ? 'selected' : '';
                diffOptions += `<option value="${d.name}" ${selected}>${d.name}</option>`;
            });
        }
    } catch (e) {
        console.error("Could not load difficulties", e);
    }

    const content = `
        <form id="trail-form" onsubmit="handleTrailSubmit(event, ${isEdit ? trail.id : 'null'})">
            <div class="form-group">
                <label for="trail-name">Nazwa trasy</label>
                <input type="text" id="trail-name" class="form-control" required value="${isEdit ? trail.name : ''}">
            </div>
            <div class="form-group">
                <label for="trail-difficulty">Poziom trudności</label>
                <select id="trail-difficulty" class="form-control">
                    ${diffOptions}
                </select>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="trail-length">Długość (m)</label>
                    <input type="number" id="trail-length" class="form-control" value="${isEdit && trail.length ? trail.length : ''}">
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="trail-location">Lokalizacja</label>
                    <input type="text" id="trail-location" class="form-control" value="${isEdit && trail.location ? trail.location : ''}">
                </div>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="trail-snow">Stan naśnieżenia</label>
                    <input type="text" id="trail-snow" class="form-control" value="${isEdit && trail.snowCondition ? trail.snowCondition : ''}">
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="trail-prep">Stan przygotowania</label>
                    <input type="text" id="trail-prep" class="form-control" value="${isEdit && trail.preparationStatus ? trail.preparationStatus : ''}">
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
    `;
    openModal(title, content);
}

async function handleTrailSubmit(event, trailId) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const name = document.getElementById('trail-name').value;
    if (name.length < 3) {
        showToast('Nazwa trasy musi mieć co najmniej 3 znaki.', 'error');
        return;
    }

    const payload = {
        name: name,
        difficulty: document.getElementById('trail-difficulty').value || null,
        length: document.getElementById('trail-length').value ? parseFloat(document.getElementById('trail-length').value) : null,
        location: document.getElementById('trail-location').value || null,
        snowCondition: document.getElementById('trail-snow').value || null,
        preparationStatus: document.getElementById('trail-prep').value || null
    };

    const method = trailId ? 'PUT' : 'POST';
    const url = trailId ? `/api/trasy/${trailId}` : '/api/trasy';

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            loadTrailsModule(document.getElementById('main-content'), token);
            showToast(trailId ? 'Trasa zaktualizowana.' : 'Nowa trasa dodana.', 'success');
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function updateTrailStatus(trailId, statusId) {
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const response = await fetch(`/api/trasy/${trailId}/status`, {
            method: 'PUT',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ statusId: statusId ? parseInt(statusId) : null })
        });
        if (!response.ok) {
            showToast('Nie udało się zmienić statusu.', 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

async function deleteTrail(trailId) {
    if (!confirm('Czy na pewno chcesz usunąć tę trasę? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(`/api/trasy/${trailId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            showToast('Usunięto pomyślnie.', 'success');
            loadTrailsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            showToast('Błąd: ' + errorMsg, 'error');
        }
    } catch (error) {
        showToast('Błąd połączenia z serwerem.', 'error');
    }
}

// Moduł Kart RFID i Karnetów
async function loadCardsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Zarządzanie kartami RFID</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Wyszukiwanie kart, podgląd przypisanych karnetów oraz blokowanie.</p>
                    </div>
                    <div>
                        <button class="btn-primary" onclick="showCardBatchForm()">Magazyn Kart (Generuj)</button>
                        <button class="btn-secondary" style="margin-left: 8px; color: var(--danger); border-color: var(--danger);" onclick="showBlockBatchForm()">Zablokuj Serię</button>
                    </div>
                </div>

                <div style="margin-bottom: 24px; display: flex; gap: 16px; align-items: flex-end;">
                    <div class="form-group" style="margin-bottom: 0; flex: 1;">
                        <label for="search-card">RFID lub email właściciela</label>
                        <input type="text" id="search-card" class="form-control" placeholder="np. A3:F2:..." onkeyup="if(event.key==='Enter') fetchCards()">
                    </div>
                    <button class="btn-primary" onclick="fetchCards()">Szukaj</button>
                </div>

                <div id="cards-container">
                    <p id="cards-loading">Wpisz RFID i kliknij Szukaj...</p>
                    <table class="data-table" id="cards-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">RFID</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status</th>
                                <th style="padding: 12px; color: var(--text-muted);">Właściciel</th>
                                <th style="padding: 12px; color: var(--text-muted);">Aktywny karnet</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    // Globalne udostępnienie funkcji dla przycisków HTML
    window.fetchCards = async () => {
        const searchVal = document.getElementById('search-card').value;
        const loading = document.getElementById('cards-loading');
        const table = document.getElementById('cards-table');
        const tbody = table.querySelector('tbody');

        loading.style.display = 'block';
        loading.textContent = 'Pobieranie danych...';
        table.style.display = 'none';

        try {
            const response = await fetch(`/api/karty?search=${encodeURIComponent(searchVal)}`, {
                headers: { 'Authorization': 'Bearer ' + token }
            });

            if (response.ok) {
                const data = await response.json();
                tbody.innerHTML = '';

                if (data.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="5" style="padding: 12px; text-align: center;">Nie znaleziono kart pasujących do kryteriów.</td></tr>';
                } else {
                    data.forEach(card => {
                        const tr = document.createElement('tr');
                        tr.style.borderBottom = '1px solid var(--border-color)';

                        let statusColor = 'var(--text-main)';
                        if (card.status === 'wolna') statusColor = '#10b981';
                        else if (card.status === 'zastrzezony') statusColor = '#ef4444';
                        else if (card.status === 'zajeta') statusColor = '#3b82f6';

                        tr.innerHTML = `
                            <td style="padding: 12px; font-family: monospace; font-weight: 500;">${card.id}</td>
                            <td style="padding: 12px;"><span style="color: ${statusColor}; font-weight: 600;">${card.status}</span></td>
                            <td style="padding: 12px;">${card.owner || '-'}</td>
                            <td style="padding: 12px;">${card.activePassType || '-'}</td>
                            <td style="padding: 12px; text-align: right;">
                                <button class="btn-secondary" style="padding: 6px 12px; font-size: 13px;" onclick="showCardDetails('${card.id}')">Szczegóły</button>
                            </td>
                        `;
                        tbody.appendChild(tr);
                    });
                }
                loading.style.display = 'none';
                table.style.display = 'table';
            }
        } catch (error) {
            loading.textContent = 'Błąd połączenia z serwerem.';
        }
    };

    // Jeśli jest coś w szukajce, to od razu odpal
    if (document.getElementById('search-card').value) fetchCards();
}

async function showCardDetails(rfid) {
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const cardRes = await fetch(`/api/karty/${rfid}`, { headers: { 'Authorization': 'Bearer ' + token } });
        const passesRes = await fetch(`/api/karnety?cardId=${rfid}`, { headers: { 'Authorization': 'Bearer ' + token } });

        if (cardRes.ok && passesRes.ok) {
            const card = await cardRes.json();
            const passes = await passesRes.json();

            let statusAction = '';
            if (card.status === 'zastrzezony') {
                statusAction = `<button class="btn-primary" onclick="unblockCard('${card.id}')">Odblokuj kartę</button>`;
            } else {
                statusAction = `<button class="btn-secondary" style="color: var(--danger); border-color: var(--danger);" onclick="blockCard('${card.id}')">Zablokuj (zastrzeż) kartę</button>`;
            }

            const content = `
                <div style="margin-bottom: 24px;">
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px;">
                        <div>
                            <p style="color: var(--text-muted); font-size: 13px;">RFID</p>
                            <p style="font-weight: 600; font-family: monospace;">${card.id}</p>
                        </div>
                        <div>
                            <p style="color: var(--text-muted); font-size: 13px;">Status</p>
                            <p style="font-weight: 600;">${card.status}</p>
                        </div>
                        <div>
                            <p style="color: var(--text-muted); font-size: 13px;">Właściciel</p>
                            <p>${card.owner || '-'}</p>
                        </div>
                        <div>
                            <p style="color: var(--text-muted); font-size: 13px;">Kaucja opłacona</p>
                            <p>${card.depositPaid ? 'Tak' : 'Nie'}</p>
                        </div>
                    </div>
                    <div style="display: flex; gap: 8px; align-items: center;">
                        ${statusAction}
                        <button class="btn-primary" style="background-color: var(--primary-color);" onclick="showSpecialPassForm('${card.id}')">Wydaj Specjalny</button>
                    </div>
                    ${card.blockReason ? `<p style="margin-top: 12px; padding: 12px; background: #fee2e2; border-radius: 8px; color: #991b1b;"><strong>Powód blokady:</strong> ${card.blockReason}</p>` : ''}
                </div>

                <h4>Historia karnetów</h4>
                <div style="max-height: 300px; overflow-y: auto; margin-top: 12px; border: 1px solid var(--border-color); border-radius: 8px;">
                    <table class="data-table" style="width: 100%; border-collapse: collapse; font-size: 13px;">
                        <thead style="position: sticky; top: 0; background: var(--bg-card);">
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 10px;">ID</th>
                                <th style="padding: 10px;">Taryfa</th>
                                <th style="padding: 10px;">Status</th>
                                <th style="padding: 10px;">Ważny do</th>
                                <th style="padding: 10px; text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${passes.length === 0 ? '<tr><td colspan="5" style="padding: 12px; text-align: center;">Brak karnetów przypisanych do tej karty.</td></tr>' : 
                                passes.map(p => `
                                <tr style="border-bottom: 1px solid var(--border-color);">
                                    <td style="padding: 10px;">${p.id}</td>
                                    <td style="padding: 10px; font-weight: 500;">${p.tariff || '-'}</td>
                                    <td style="padding: 10px;">${p.status}</td>
                                    <td style="padding: 10px;">${p.validTo ? new Date(p.validTo).toLocaleDateString('pl-PL') : '-'}</td>
                                    <td style="padding: 10px; text-align: right;">
                                        ${p.status === 'aktywny' ? `<button class="btn-secondary" style="font-size: 11px; padding: 4px 8px; color: var(--danger); border-color: var(--danger);" onclick="blockPass(${p.id}, '${rfid}')">Zablokuj</button>` : ''}
                                    </td>
                                </tr>
                                `).join('')
                            }
                        </tbody>
                    </table>
                </div>
            `;
            openModal('Szczegóły karty RFID', content);
        }
    } catch (error) {
        showToast('Błąd podczas pobierania szczegółów.', 'error');
    }
}

async function blockCard(rfid) {
    const reason = prompt('Podaj powód zablokowania karty:');
    if (reason === null) return;

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const res = await fetch(`/api/karty/${rfid}/blokuj`, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason })
        });
        if (res.ok) {
            closeModal();
            fetchCards();
            showToast('Karta została zablokowana.', 'success');
        } else {
            showToast('Błąd podczas blokowania.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

async function unblockCard(rfid) {
    if (!confirm('Czy na pewno chcesz odblokować tę kartę?')) return;
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const res = await fetch(`/api/karty/${rfid}/odblokuj`, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (res.ok) {
            closeModal();
            fetchCards();
            showToast('Karta została odblokowana.', 'success');
        } else {
            showToast('Błąd podczas odblokowywania.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

async function blockPass(passId, rfid) {
    const reason = prompt('Podaj powód zablokowania karnetu:');
    if (reason === null) return;

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const res = await fetch(`/api/karnety/${passId}/blokuj`, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason })
        });
        if (res.ok) {
            showToast('Karnet został zablokowany.', 'success');
            showCardDetails(rfid); // Odśwież widok szczegółów
        } else {
            showToast('Błąd podczas blokowania karnetu.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

// Moduł Raportów (Analityka i Wykresy)
async function loadReportsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card" style="margin-bottom: 24px; display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <h3>Analityka i Statystyki</h3>
                    <p>Zaawansowane raporty biznesowe w postaci interaktywnych wykresów.</p>
                </div>
                <select id="reports-period" class="form-control" style="width: 200px;" onchange="fetchAnalyticsData()">
                    <option value="day">Dzisiaj / Ostatnie 30 dni</option>
                    <option value="week">Ostatnie 12 tygodni</option>
                    <option value="month">Ostatnie 12 miesięcy</option>
                </select>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Obciążenie Wyciągów (Dzisiaj)</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-hourly-traffic"></canvas>
                    </div>
                </div>
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Przychody ze Sprzedaży</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-revenue-trend"></canvas>
                    </div>
                </div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Kanały Sprzedaży (Ten Miesiąc)</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-sales-channels"></canvas>
                    </div>
                </div>
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Popularność Karnetów</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-pass-popularity"></canvas>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Deklaracja zmiennych instancji wykresów dla późniejszego niszczenia
    window.chartInstances = window.chartInstances || {};
    
    window.fetchAnalyticsData = async () => {
        const period = document.getElementById('reports-period').value;
        const hdrs = { 'Authorization': 'Bearer ' + token };

        try {
            // 1. Obciążenie godzinowe
            const res1 = await fetch('/api/statystyki/obciazenie-godzinowe', { headers: hdrs });
            if (res1.ok) {
                const data = await res1.json();
                const ctx = document.getElementById('chart-hourly-traffic');
                if (window.chartInstances.hourly) window.chartInstances.hourly.destroy();
                
                const datasets = data.map((d, i) => {
                    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
                    return {
                        label: d.liftName,
                        data: d.data,
                        borderColor: colors[i % colors.length],
                        backgroundColor: colors[i % colors.length] + '22',
                        borderWidth: 2,
                        tension: 0.3,
                        fill: true
                    };
                });

                window.chartInstances.hourly = new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: Array.from({length: 24}, (_, i) => `${i}:00`),
                        datasets: datasets
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }

            // 2. Przychody
            const res2 = await fetch(`/api/statystyki/przychody-trend?period=${period}`, { headers: hdrs });
            if (res2.ok) {
                const data = await res2.json();
                const ctx = document.getElementById('chart-revenue-trend');
                if (window.chartInstances.revenue) window.chartInstances.revenue.destroy();
                
                window.chartInstances.revenue = new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: data.map(d => d.dateLabel),
                        datasets: [{
                            label: 'Przychód (PLN)',
                            data: data.map(d => d.revenue),
                            backgroundColor: '#6366f1',
                            borderRadius: 4
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }

            // 3. Kanały Sprzedaży
            const res3 = await fetch('/api/statystyki/kanaly-sprzedazy', { headers: hdrs });
            if (res3.ok) {
                const data = await res3.json();
                const ctx = document.getElementById('chart-sales-channels');
                if (window.chartInstances.channels) window.chartInstances.channels.destroy();
                
                window.chartInstances.channels = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: ['Kasy Fizyczne', 'Sprzedaż Online'],
                        datasets: [{
                            data: [data.posSales, data.onlineSales],
                            backgroundColor: ['#10b981', '#3b82f6'],
                            borderWidth: 0
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false, cutout: '70%' }
                });
            }

            // 4. Popularność Karnetów
            const res4 = await fetch('/api/statystyki/popularnosc-karnetow', { headers: hdrs });
            if (res4.ok) {
                const data = await res4.json();
                const ctx = document.getElementById('chart-pass-popularity');
                if (window.chartInstances.popularity) window.chartInstances.popularity.destroy();
                
                window.chartInstances.popularity = new Chart(ctx, {
                    type: 'pie',
                    data: {
                        labels: data.map(d => d.tariffName),
                        datasets: [{
                            data: data.map(d => d.count),
                            backgroundColor: ['#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6'],
                            borderWidth: 1,
                            borderColor: '#ffffff'
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }
        } catch (error) {
        } else {
            showToast('Błąd podczas blokowania.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

async function unblockCard(rfid) {
    if (!confirm('Czy na pewno chcesz odblokować tę kartę?')) return;
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const res = await fetch(`/api/karty/${rfid}/odblokuj`, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (res.ok) {
            closeModal();
            fetchCards();
            showToast('Karta została odblokowana.', 'success');
        } else {
            showToast('Błąd podczas odblokowywania.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

async function blockPass(passId, rfid) {
    const reason = prompt('Podaj powód zablokowania karnetu:');
    if (reason === null) return;

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    try {
        const res = await fetch(`/api/karnety/${passId}/blokuj`, {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason })
        });
        if (res.ok) {
            showToast('Karnet został zablokowany.', 'success');
            showCardDetails(rfid); // Odśwież widok szczegółów
        } else {
            showToast('Błąd podczas blokowania karnetu.', 'error');
        }
    } catch (e) { showToast('Błąd połączenia.', 'error'); }
}

// Moduł Raportów (Analityka i Wykresy)
async function loadReportsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card" style="margin-bottom: 24px; display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <h3>Analityka i Statystyki</h3>
                    <p>Zaawansowane raporty biznesowe w postaci interaktywnych wykresów.</p>
                </div>
                <select id="reports-period" class="form-control" style="width: 200px;" onchange="fetchAnalyticsData()">
                    <option value="day">Dzisiaj / Ostatnie 30 dni</option>
                    <option value="week">Ostatnie 12 tygodni</option>
                    <option value="month">Ostatnie 12 miesięcy</option>
                </select>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Obciążenie Wyciągów (Dzisiaj)</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-hourly-traffic"></canvas>
                    </div>
                </div>
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Przychody ze Sprzedaży</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-revenue-trend"></canvas>
                    </div>
                </div>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Kanały Sprzedaży (Ten Miesiąc)</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-sales-channels"></canvas>
                    </div>
                </div>
                <div class="card" style="margin-bottom: 0;">
                    <h4 style="margin-bottom: 16px;">Popularność Karnetów</h4>
                    <div style="position: relative; height: 300px; width: 100%;">
                        <canvas id="chart-pass-popularity"></canvas>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Deklaracja zmiennych instancji wykresów dla późniejszego niszczenia
    window.chartInstances = window.chartInstances || {};
    
    window.fetchAnalyticsData = async () => {
        const period = document.getElementById('reports-period').value;
        const hdrs = { 'Authorization': 'Bearer ' + token };

        try {
            // 1. Obciążenie godzinowe
            const res1 = await fetch('/api/statystyki/obciazenie-godzinowe', { headers: hdrs });
            if (res1.ok) {
                const data = await res1.json();
                const ctx = document.getElementById('chart-hourly-traffic');
                if (window.chartInstances.hourly) window.chartInstances.hourly.destroy();
                
                const datasets = data.map((d, i) => {
                    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
                    return {
                        label: d.liftName,
                        data: d.data,
                        borderColor: colors[i % colors.length],
                        backgroundColor: colors[i % colors.length] + '22',
                        borderWidth: 2,
                        tension: 0.3,
                        fill: true
                    };
                });

                window.chartInstances.hourly = new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: Array.from({length: 24}, (_, i) => `${i}:00`),
                        datasets: datasets
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }

            // 2. Przychody
            const res2 = await fetch(`/api/statystyki/przychody-trend?period=${period}`, { headers: hdrs });
            if (res2.ok) {
                const data = await res2.json();
                const ctx = document.getElementById('chart-revenue-trend');
                if (window.chartInstances.revenue) window.chartInstances.revenue.destroy();
                
                window.chartInstances.revenue = new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: data.map(d => d.dateLabel),
                        datasets: [{
                            label: 'Przychód (PLN)',
                            data: data.map(d => d.revenue),
                            backgroundColor: '#6366f1',
                            borderRadius: 4
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }

            // 3. Kanały Sprzedaży
            const res3 = await fetch('/api/statystyki/kanaly-sprzedazy', { headers: hdrs });
            if (res3.ok) {
                const data = await res3.json();
                const ctx = document.getElementById('chart-sales-channels');
                if (window.chartInstances.channels) window.chartInstances.channels.destroy();
                
                window.chartInstances.channels = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: ['Kasy Fizyczne', 'Sprzedaż Online'],
                        datasets: [{
                            data: [data.posSales, data.onlineSales],
                            backgroundColor: ['#10b981', '#3b82f6'],
                            borderWidth: 0
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false, cutout: '70%' }
                });
            }

            // 4. Popularność Karnetów
            const res4 = await fetch('/api/statystyki/popularnosc-karnetow', { headers: hdrs });
            if (res4.ok) {
                const data = await res4.json();
                const ctx = document.getElementById('chart-pass-popularity');
                if (window.chartInstances.popularity) window.chartInstances.popularity.destroy();
                
                window.chartInstances.popularity = new Chart(ctx, {
                    type: 'pie',
                    data: {
                        labels: data.map(d => d.tariffName),
                        datasets: [{
                            data: data.map(d => d.count),
                            backgroundColor: ['#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6'],
                            borderWidth: 1,
                            borderColor: '#ffffff'
                        }]
                    },
                    options: { responsive: true, maintainAspectRatio: false }
                });
            }
        } catch (error) {
            console.error('Błąd pobierania analityki', error);
            showToast('Nie udało się załadować danych analitycznych.', 'error');
        }
    };

    fetchAnalyticsData();
}

// --- FAZA 3: Karty i Taryfy ---
window.showCardBatchForm = function() {
    const content = `
        <form onsubmit="handleCardBatchSubmit(event)">
            <div class="form-group">
                <label>Przedrostek (np. SKI-)</label>
                <input type="text" id="batch-prefix" class="form-control" required value="SKI-">
            </div>
            <div class="form-group">
                <label>Numer startowy</label>
                <input type="number" id="batch-start" class="form-control" required value="1" min="1">
            </div>
            <div class="form-group">
                <label>Długość numeru (ilość cyfr z zerami, np. 4 dla 0001)</label>
                <input type="number" id="batch-len" class="form-control" required value="4" min="1">
            </div>
            <div class="form-group">
                <label>Ilość sztuk</label>
                <input type="number" id="batch-count" class="form-control" required value="100" min="1">
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Generuj</button>
            </div>
        </form>
    `;
    openModal('Generuj Partię Kart', content);
};

window.handleCardBatchSubmit = async function(event) {
    event.preventDefault();
    const payload = {
        prefix: document.getElementById('batch-prefix').value,
        startNumber: parseInt(document.getElementById('batch-start').value),
        numberLength: parseInt(document.getElementById('batch-len').value),
        count: parseInt(document.getElementById('batch-count').value)
    };
    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch('/api/karty/partia', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if(res.ok) {
            const data = await res.json();
            showToast(`Wygenerowano ${data.generatedCount} kart (pominięto ${data.skippedCount}).`, 'success');
            closeModal();
            fetchCards();
        } else showToast('Błąd generowania partii.', 'error');
    } catch(e) { showToast('Błąd', 'error'); }
};

window.showBlockBatchForm = function() {
    const content = `
        <form onsubmit="handleBlockBatchSubmit(event)">
            <div class="form-group">
                <label>Przedrostek (np. SKI-)</label>
                <input type="text" id="bb-prefix" class="form-control" required value="SKI-">
            </div>
            <div class="form-group">
                <label>Numer startowy (od)</label>
                <input type="number" id="bb-start" class="form-control" required min="1">
            </div>
            <div class="form-group">
                <label>Numer końcowy (do)</label>
                <input type="number" id="bb-end" class="form-control" required min="1">
            </div>
            <div class="form-group">
                <label>Powód</label>
                <input type="text" id="bb-reason" class="form-control" required value="Zgubiona partia w transporcie">
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary" style="background-color: var(--danger);">Zablokuj Serię</button>
            </div>
        </form>
    `;
    openModal('Zablokuj Serię Kart', content);
};

window.handleBlockBatchSubmit = async function(event) {
    event.preventDefault();
    const payload = {
        prefix: document.getElementById('bb-prefix').value,
        startNumber: parseInt(document.getElementById('bb-start').value),
        endNumber: parseInt(document.getElementById('bb-end').value),
        reason: document.getElementById('bb-reason').value
    };
    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch('/api/karty/partia/zablokuj', {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if(res.ok) {
            const data = await res.json();
            showToast(`Zablokowano ${data.blockedCount} kart.`, 'success');
            closeModal();
            fetchCards();
        } else showToast('Błąd.', 'error');
    } catch(e) { showToast('Błąd', 'error'); }
};

window.showSpecialPassForm = async function(rfid) {
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    let tariffOpts = '';
    try {
        const res = await fetch('/api/taryfy', { headers: { 'Authorization': 'Bearer ' + token } });
        if(res.ok) {
            const tariffs = await res.json();
            tariffs.forEach(t => { tariffOpts += `<option value="${t.id}">${t.name} ${t.discountType ? '('+t.discountType+')' : ''}</option>`; });
        }
    } catch(e){}

    const content = `
        <form onsubmit="handleSpecialPassSubmit(event, '${rfid}')">
            <div class="form-group">
                <label>Wybierz taryfę</label>
                <select id="sp-tariff" class="form-control" required>${tariffOpts}</select>
            </div>
            <div class="form-group">
                <label>Powód (np. GOPR, Rekompensata)</label>
                <input type="text" id="sp-reason" class="form-control" required>
            </div>
            <div class="form-group">
                <label>Ważny od (Opcjonalnie)</label>
                <input type="datetime-local" id="sp-from" class="form-control">
            </div>
            <div class="form-group">
                <label>Ważny do (Opcjonalnie)</label>
                <input type="datetime-local" id="sp-to" class="form-control">
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary" style="background-color: var(--primary-color);">Wydaj Karnet</button>
            </div>
        </form>
    `;
    openModal('Wydaj Karnet Specjalny dla ' + rfid, content);
};

window.handleSpecialPassSubmit = async function(event, rfid) {
    event.preventDefault();
    const fromV = document.getElementById('sp-from').value;
    const toV = document.getElementById('sp-to').value;
    const payload = {
        cardId: rfid,
        tariffId: parseInt(document.getElementById('sp-tariff').value),
        reason: document.getElementById('sp-reason').value,
        validFrom: fromV ? new Date(fromV).toISOString() : null,
        validTo: toV ? new Date(toV).toISOString() : null
    };
    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch('/api/karnety/specjalne', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if(res.ok) {
            showToast('Karnet specjalny wydany.', 'success');
            closeModal();
            showCardDetails(rfid);
            fetchCards();
        } else {
            const err = await res.text();
            showToast('Błąd: ' + err, 'error');
        }
    } catch(e) { showToast('Błąd.', 'error'); }
};

window.loadTariffsModule = async function(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Zarządzanie taryfami</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Kreator cenników i taryf narciarskich.</p>
                    </div>
                    <button class="btn-primary" onclick="showTariffCreator()">Kreator Taryf</button>
                </div>
                <div id="tariffs-container">
                    <table class="data-table" id="tariffs-table" style="width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Nazwa Grupy</th>
                                <th style="padding: 12px; color: var(--text-muted);">Ulga</th>
                                <th style="padding: 12px; color: var(--text-muted);">Cena</th>
                                <th style="padding: 12px; color: var(--text-muted);">Sezon / Typ</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    window.fetchTariffs = async () => {
        try {
            const res = await fetch('/api/taryfy', { headers: { 'Authorization': 'Bearer ' + token } });
            if(res.ok) {
                const data = await res.json();
                const tbody = document.querySelector('#tariffs-table tbody');
                tbody.innerHTML = '';
                data.forEach(t => {
                    const tr = document.createElement('tr');
                    tr.style.borderBottom = '1px solid var(--border-color)';
                    tr.innerHTML = `
                        <td style="padding: 12px; font-weight: 500;">${t.name}</td>
                        <td style="padding: 12px;">${t.discountType || 'Standard'}</td>
                        <td style="padding: 12px;">${t.price ? t.price.toFixed(2) + ' PLN' : '-'}</td>
                        <td style="padding: 12px;">${t.season || '-'} / ${t.passType || '-'}</td>
                        <td style="padding: 12px; text-align: right;">
                            <button class="btn-secondary" style="color: var(--danger); border-color: var(--danger); padding: 4px 8px; font-size: 12px;" onclick="deleteTariff(${t.id})">Usuń</button>
                        </td>
                    `;
                    tbody.appendChild(tr);
                });
            }
        } catch(e){}
    };
    fetchTariffs();
};

window.showTariffCreator = async function() {
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    let seasonOpts = '<option value="">Brak sezonu</option>', passOpts = '';
    try {
        const [resS, resP] = await Promise.all([
            fetch('/api/taryfy/sezony', { headers: { 'Authorization': 'Bearer ' + token } }).catch(()=>null),
            fetch('/api/taryfy/typy', { headers: { 'Authorization': 'Bearer ' + token } }).catch(()=>null)
        ]);
        if(resS && resS.ok) {
            const seasons = await resS.json();
            seasons.forEach(s => { seasonOpts += `<option value="${s.name}">${s.name}</option>`; });
        } else { seasonOpts += '<option value="Wysoki Sezon">Wysoki Sezon</option><option value="Niski Sezon">Niski Sezon</option>'; }
        
        if(resP && resP.ok) {
            const passTypes = await resP.json();
            passTypes.forEach(p => { passOpts += `<option value="${p.name}">${p.name}</option>`; });
        } else { passOpts += '<option value="Czasowy (4h)">Czasowy (4h)</option><option value="Całodniowy">Całodniowy</option><option value="Punktowy">Punktowy</option>'; }
    } catch(e){}

    const content = `
        <form onsubmit="handleTariffBulk(event)">
            <div class="form-group">
                <label>Nazwa Taryfy (Grupy) (np. Karnet 4h)</label>
                <input type="text" id="tb-name" class="form-control" required>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex:1;">
                    <label>Sezon</label>
                    <select id="tb-season" class="form-control">${seasonOpts}</select>
                </div>
                <div class="form-group" style="flex:1;">
                    <label>Typ Karnetu</label>
                    <select id="tb-pass" class="form-control">${passOpts}</select>
                </div>
            </div>
            <div class="form-group">
                <label>Limit Puli (Opcjonalnie)</label>
                <input type="number" id="tb-limit" class="form-control">
            </div>
            <hr style="border:0; border-top: 1px solid var(--border-color); margin: 16px 0;">
            <h4 style="margin-bottom: 12px;">Cennik (Dodaj Ulgi)</h4>
            <div id="discounts-container" style="display: flex; flex-direction: column; gap: 8px;">
                <div style="display: flex; gap: 8px; align-items: center;" class="discount-row">
                    <input type="text" class="form-control d-type" value="Normalny" required placeholder="Rodzaj (np. Normalny)">
                    <input type="number" step="0.01" class="form-control d-price" required placeholder="Cena (PLN)">
                </div>
            </div>
            <button type="button" class="btn-secondary" style="margin-top: 8px; font-size: 13px; padding: 4px 8px;" onclick="addDiscountRow()">+ Dodaj kolejną ulgę</button>

            <div class="modal-footer" style="margin-top: 24px;">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Utwórz Taryfy</button>
            </div>
        </form>
    `;
    openModal('Kreator Taryf', content);
};

window.addDiscountRow = function() {
    const div = document.createElement('div');
    div.style = "display: flex; gap: 8px; align-items: center;";
    div.className = "discount-row";
    div.innerHTML = `
        <input type="text" class="form-control d-type" required placeholder="Rodzaj (np. Ulgowy, Senior)">
        <input type="number" step="0.01" class="form-control d-price" required placeholder="Cena (PLN)">
        <button type="button" class="btn-secondary" style="padding: 4px; color: var(--danger); border-color: var(--danger);" onclick="this.parentElement.remove()">X</button>
    `;
    document.getElementById('discounts-container').appendChild(div);
};

window.handleTariffBulk = async function(event) {
    event.preventDefault();
    const rows = document.querySelectorAll('.discount-row');
    const discounts = [];
    rows.forEach(r => {
        discounts.push({
            discountType: r.querySelector('.d-type').value,
            price: parseFloat(r.querySelector('.d-price').value)
        });
    });

    const payload = {
        name: document.getElementById('tb-name').value,
        season: document.getElementById('tb-season').value || null,
        passType: document.getElementById('tb-pass').value || null,
        poolLimit: document.getElementById('tb-limit').value ? parseInt(document.getElementById('tb-limit').value) : null,
        discounts: discounts
    };

    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch('/api/taryfy/bulk', {
            method: 'POST',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if(res.ok) {
            showToast('Taryfy zostały wygenerowane.', 'success');
            closeModal();
            if(window.fetchTariffs) window.fetchTariffs();
        } else showToast('Błąd tworzenia taryf.', 'error');
    } catch(e) { showToast('Błąd.', 'error'); }
};

window.deleteTariff = async function(id) {
    if(!confirm('Czy na pewno chcesz usunąć tę taryfę?')) return;
    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch('/api/taryfy/' + id, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + token }});
        if(res.ok) {
            showToast('Taryfa usunięta.', 'success');
            if(window.fetchTariffs) window.fetchTariffs();
        } else {
            const txt = await res.text();
            showToast(txt || 'Błąd.', 'error');
        }
    } catch(e){}
};

// --- FAZA 4: Użytkownicy i uprawnienia ---
window.loadUsersModule = async function(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px;">
                    <div>
                        <h3>Zarządzanie Personelem (Kasjerzy / Admini)</h3>
                        <p style="color: var(--text-muted); margin-top: 8px;">Tworzenie i zarządzanie kontami pracowników.</p>
                    </div>
                    <button class="btn-primary" onclick="showUserForm()">Dodaj Pracownika</button>
                </div>
                <div id="users-container">
                    <table class="data-table" id="users-table" style="width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">ID</th>
                                <th style="padding: 12px; color: var(--text-muted);">Login</th>
                                <th style="padding: 12px; color: var(--text-muted);">Rola</th>
                                <th style="padding: 12px; color: var(--text-muted);">Aktywne</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    window.fetchUsers = async () => {
        try {
            const res = await fetch('/api/users/all', { headers: { 'Authorization': 'Bearer ' + token } });
            if(res.ok) {
                const data = await res.json();
                const tbody = document.querySelector('#users-table tbody');
                tbody.innerHTML = '';
                
                // Filtrujemy tylko admin i kasjer dla uproszczenia (narciarze mogą być odfiltrowani na backendzie, ale tutaj zrobimy to dodatkowo)
                const staff = data.filter(u => u.role === 'admin' || u.role === 'kasjer');
                
                staff.forEach(u => {
                    const tr = document.createElement('tr');
                    tr.style.borderBottom = '1px solid var(--border-color)';
                    
                    const roleColor = u.role === 'admin' ? '#ef4444' : '#3b82f6';
                    
                    tr.innerHTML = `
                        <td style="padding: 12px;">${u.id}</td>
                        <td style="padding: 12px; font-weight: 500;">${u.login}</td>
                        <td style="padding: 12px;">
                            <span style="font-size: 11px; padding: 2px 6px; border-radius: 12px; background-color: ${roleColor}22; color: ${roleColor};">
                                ${u.role.toUpperCase()}
                            </span>
                        </td>
                        <td style="padding: 12px;">${u.isActive ? 'Tak' : 'Nie'}</td>
                        <td style="padding: 12px; text-align: right;">
                            <button class="action-btn edit" onclick="showUserForm(${u.id}, '${u.login}', '${u.role}', ${u.isActive})" title="Edytuj">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                            </button>
                            <button class="action-btn delete" onclick="deleteUser(${u.id}, '${u.role}')" title="Usuń / Zablokuj">
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                            </button>
                        </td>
                    `;
                    tbody.appendChild(tr);
                });
            }
        } catch(e){}
    };
    fetchUsers();
};

window.showUserForm = function(id = null, login = '', role = 'kasjer', isActive = true) {
    const isEdit = id !== null;
    const content = `
        <form onsubmit="handleUserSubmit(event, ${id})">
            <div class="form-group">
                <label>Login / E-mail</label>
                <input type="text" id="u-login" class="form-control" required value="${login}">
            </div>
            <div class="form-group">
                <label>Hasło ${isEdit ? '(Zostaw puste by nie zmieniać)' : ''}</label>
                <input type="password" id="u-pass" class="form-control" ${isEdit ? '' : 'required'}>
            </div>
            <div class="form-group">
                <label>Rola</label>
                <select id="u-role" class="form-control" ${isEdit ? 'disabled' : ''}>
                    <option value="kasjer" ${role === 'kasjer' ? 'selected' : ''}>Kasjer</option>
                    <option value="admin" ${role === 'admin' ? 'selected' : ''}>Administrator</option>
                </select>
                ${isEdit ? `<input type="hidden" id="u-role-hidden" value="${role}">` : ''}
            </div>
            <div class="form-group">
                <label>Konto aktywne</label>
                <select id="u-active" class="form-control">
                    <option value="true" ${isActive ? 'selected' : ''}>Tak</option>
                    <option value="false" ${!isActive ? 'selected' : ''}>Nie</option>
                </select>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary" onclick="closeModal()">Anuluj</button>
                <button type="submit" class="btn-primary">Zapisz</button>
            </div>
        </form>
    `;
    openModal(isEdit ? 'Edytuj Pracownika' : 'Dodaj Pracownika', content);
};

window.handleUserSubmit = async function(event, id) {
    event.preventDefault();
    const isEdit = id !== null;
    const roleField = isEdit ? document.getElementById('u-role-hidden').value : document.getElementById('u-role').value;
    
    const payload = {
        login: document.getElementById('u-login').value,
        password: document.getElementById('u-pass').value || null,
        role: roleField,
        isActive: document.getElementById('u-active').value === 'true'
    };

    const method = isEdit ? 'PUT' : 'POST';
    const url = isEdit ? '/api/users/' + id : '/api/users';

    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch(url, {
            method: method,
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if(res.ok) {
            showToast('Zapisano pomyślnie.', 'success');
            closeModal();
            if(window.fetchUsers) window.fetchUsers();
        } else showToast('Błąd zapisywania.', 'error');
    } catch(e) { showToast('Błąd.', 'error'); }
};

window.deleteUser = async function(id, role) {
    if(!confirm('Czy na pewno chcesz usunąć to konto? Jeżeli posiada historię transakcji, zostanie tylko zablokowane.')) return;
    try {
        const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        const res = await fetch(`/api/users/${id}?role=${role}`, { method: 'DELETE', headers: { 'Authorization': 'Bearer ' + token }});
        if(res.ok || res.status === 204) {
            showToast('Operacja wykonana pomyślnie.', 'success');
            if(window.fetchUsers) window.fetchUsers();
        } else showToast('Błąd podczas usuwania.', 'error');
    } catch(e){}
};
