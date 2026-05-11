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

    // Zakładamy, że rolą uprawnioną jest "kasjer" (lub potencjalnie "admin")
    if (!payload || (userRole !== 'kasjer' && userRole !== 'admin')) {
        localStorage.removeItem('jwt_token');
        sessionStorage.removeItem('jwt_token');
        alert('Brak uprawnień. Zaloguj się jako administrator.');
        window.location.href = 'admin-login.html';
        return;
    }

    // Pokaż aplikację, gdy autoryzacja się powiedzie
    document.getElementById('app-container').style.display = 'flex';

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
                case 'tariffs':
                    loadTariffsModule(mainContent, token);
                    break;
                case 'users':
                    loadUsersModule(mainContent, token);
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
    
    const payload = {
        name: document.getElementById('tariff-name').value,
        passType: document.getElementById('tariff-passType').value || null,
        season: document.getElementById('tariff-season').value || null,
        price: parseFloat(document.getElementById('tariff-price').value),
        poolLimit: document.getElementById('tariff-poolLimit').value ? parseInt(document.getElementById('tariff-poolLimit').value) : null
    };

    const method = tariffId ? 'PUT' : 'POST';
    const url = tariffId ? \`/api/taryfy/\${tariffId}\` : '/api/taryfy';

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
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas zapisywania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error saving tariff:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

async function deleteTariff(tariffId) {
    if (!confirm('Czy na pewno chcesz usunąć tę taryfę? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(\`/api/taryfy/\${tariffId}\`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            loadTariffsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas usuwania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error deleting tariff:', error);
        alert('Błąd połączenia z serwerem.');
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
    
    let url = '/api/transakcje?';
    if (dateVal) url += `date=${dateVal}&`;
    if (cashierVal) url += `cashierId=${cashierVal}`;

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
        <div class="module-content">
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
                <tr style="border-bottom: 1px solid var(--border-color);">
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
    
    const role = userId ? existingRole : document.getElementById('user-role').value;
    const isActive = document.getElementById('user-active').value === 'true';
    
    const payload = {
        login: document.getElementById('user-login').value,
        password: document.getElementById('user-password').value || null,
        role: role,
        isActive: isActive
    };

    const method = userId ? 'PUT' : 'POST';
    const url = userId ? \`/api/users/\${userId}\` : '/api/users';

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
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas zapisywania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error saving user:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

function resetUserPassword(userId, role, login, isActive) {
    const title = 'Wymuś zmianę hasła';
    
    const content = `
        <form id="reset-password-form" onsubmit="handlePasswordResetSubmit(event, ${userId}, '${role}', '${login}', ${isActive})">
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
        const response = await fetch(\`/api/users/\${userId}\`, {
            method: 'PUT',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            closeModal();
            alert('Hasło zostało pomyślnie zmienione.');
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas zmiany hasła: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error resetting password:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

async function deleteUser(userId, role) {
    if (!confirm('Czy na pewno chcesz usunąć/deaktywować to konto?')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(\`/api/users/\${userId}?role=\${role}\`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            loadUsersModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas usuwania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error deleting user:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

// Zewnętrzna asynchroniczna funkcja do modułu Wyciągów
async function loadLiftsModule(container, token) {
    // 1. Wstrzyknięcie szkieletu tabeli HTML z komunikatem ładowania
    container.innerHTML = `
        <div class="module-content">
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
                                <th style="padding: 12px; color: var(--text-muted);">ID</th>
                                <th style="padding: 12px; color: var(--text-muted);">Nazwa</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status</th>
                                <th style="padding: 12px; color: var(--text-muted);">Otwarcie</th>
                                <th style="padding: 12px; color: var(--text-muted);">Zamknięcie</th>
                                <th style="padding: 12px; color: var(--text-muted); text-align: right;">Akcje</th>
                            </tr>
                        </thead>
                        <tbody>
                            <!-- Tutaj pętla wygeneruje wiersze -->
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

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
            throw new Error(`Błąd HTTP: ${response.status}`);
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

                // Stylowanie w zależności od statusu wyciągu
                let statusColor = 'var(--text-main)';
                let statusText = lift.status;
                if (statusText === 'czynny') { statusColor = '#10b981'; statusText = 'Czynny'; }
                else if (statusText === 'nieczynny') { statusColor = '#ef4444'; statusText = 'Nieczynny'; }
                else if (statusText === 'przed_otwarciem') { statusColor = '#f59e0b'; statusText = 'Przed otwarciem'; }
                else if (statusText === 'po_zamknieciu') { statusColor = '#f59e0b'; statusText = 'Po zamknięciu'; }

                // Obcinanie sekund z TimeSpan (jeżeli są przesyłane w formacie hh:mm:ss)
                const opens = lift.opensAt ? lift.opensAt.substring(0, 5) : '-';
                const closes = lift.closesAt ? lift.closesAt.substring(0, 5) : '-';

                // Serializacja obiektu do JSON dla przycisku edycji
                const liftJson = JSON.stringify(lift).replace(/"/g, '&quot;');

                tr.innerHTML = `
                    <td style="padding: 12px;">${lift.id}</td>
                    <td style="padding: 12px; font-weight: 500;">${lift.name}</td>
                    <td style="padding: 12px; color: ${statusColor}; font-weight: 600;">${statusText}</td>
                    <td style="padding: 12px;">${opens}</td>
                    <td style="padding: 12px;">${closes}</td>
                    <td style="padding: 12px; text-align: right;">
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
            `<span style="color: var(--danger);">Nie udało się pobrać wyciągów: ${error.message}</span>`;
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
        <form id="lift-form" onsubmit="handleLiftSubmit(event, ${isEdit ? lift.id : 'null'})">
            <div class="form-group">
                <label for="lift-name">Nazwa wyciągu</label>
                <input type="text" id="lift-name" class="form-control" required value="${isEdit ? lift.name : ''}">
            </div>
            <div class="form-group">
                <label for="lift-status">Status</label>
                <select id="lift-status" class="form-control" required>
                    <option value="czynny" ${isEdit && lift.status === 'czynny' ? 'selected' : ''}>Czynny</option>
                    <option value="nieczynny" ${isEdit && lift.status === 'nieczynny' ? 'selected' : ''}>Nieczynny</option>
                    <option value="przed_otwarciem" ${isEdit && lift.status === 'przed_otwarciem' ? 'selected' : ''}>Przed otwarciem</option>
                    <option value="po_zamknieciu" ${isEdit && lift.status === 'po_zamknieciu' ? 'selected' : ''}>Po zamknięciu</option>
                </select>
            </div>
            <div style="display: flex; gap: 16px;">
                <div class="form-group" style="flex: 1;">
                    <label for="lift-opens">Otwarcie</label>
                    <input type="time" id="lift-opens" class="form-control" required value="${opens}">
                </div>
                <div class="form-group" style="flex: 1;">
                    <label for="lift-closes">Zamknięcie</label>
                    <input type="time" id="lift-closes" class="form-control" required value="${closes}">
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

async function handleLiftSubmit(event, liftId) {
    event.preventDefault();
    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    const opensInput = document.getElementById('lift-opens').value;
    const closesInput = document.getElementById('lift-closes').value;
    
    const payload = {
        name: document.getElementById('lift-name').value,
        status: document.getElementById('lift-status').value,
        opensAt: opensInput.length === 5 ? opensInput + ':00' : opensInput,
        closesAt: closesInput.length === 5 ? closesInput + ':00' : closesInput
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
            // Odświeżenie danych
            loadLiftsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas zapisywania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error saving lift:', error);
        alert('Błąd połączenia z serwerem.');
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
            // Odświeżenie danych
            loadLiftsModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas usuwania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error deleting lift:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

// Moduł Bramek
async function loadGatesModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
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
            throw new Error(`Błąd HTTP: ${response.status}`);
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

                const gateJson = JSON.stringify(gate).replace(/"/g, '&quot;');

                tr.innerHTML = `
                    <td style="padding: 12px;">${gate.id}</td>
                    <td style="padding: 12px; font-weight: 500;">${gate.name || '-'}</td>
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
            `<span style="color: var(--danger);">Nie udało się pobrać bramek: ${error.message}</span>`;
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
                liftsOptions += `<option value="${l.id}" ${selected}>${l.name}</option>`;
            });
        }
    } catch (e) {
        console.error("Could not load lifts", e);
    }

    const content = `
        <form id="gate-form" onsubmit="handleGateSubmit(event, ${isEdit ? gate.id : 'null'})">
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
    
    const payload = {
        name: document.getElementById('gate-name').value,
        liftId: parseInt(document.getElementById('gate-lift').value),
        isActive: document.getElementById('gate-active').value === 'true'
    };

    const method = gateId ? 'PUT' : 'POST';
    const url = gateId ? \`/api/bramki/\${gateId}\` : '/api/bramki';

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
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas zapisywania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error saving gate:', error);
        alert('Błąd połączenia z serwerem.');
    }
}

async function deleteGate(gateId) {
    if (!confirm('Czy na pewno chcesz usunąć tę bramkę? Operacji nie można cofnąć.')) {
        return;
    }

    const token = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    try {
        const response = await fetch(\`/api/bramki/\${gateId}\`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });

        if (response.ok) {
            loadGatesModule(document.getElementById('main-content'), token);
        } else {
            const errorMsg = await response.text();
            alert('Błąd podczas usuwania: ' + errorMsg);
        }
    } catch (error) {
        console.error('Error deleting gate:', error);
        alert('Błąd połączenia z serwerem.');
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
            alert('Zwrot zatwierdzony pomyślnie.');
        } else {
            alert('Wystąpił błąd podczas zatwierdzania zwrotu.');
        }
    } catch (error) {
        console.error('Error approving return:', error);
        alert('Błąd połączenia z serwerem.');
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
            alert('Zwrot odrzucony pomyślnie.');
        } else {
            alert('Wystąpił błąd podczas odrzucania zwrotu.');
        }
    } catch (error) {
        console.error('Error rejecting return:', error);
        alert('Błąd połączenia z serwerem.');
    }
}
