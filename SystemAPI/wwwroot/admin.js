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
                case 'tariffs':
                    loadTariffsModule(mainContent, token);
                    break;
                case 'users':
                    loadUsersModule(mainContent, token);
                    break;
                case 'reports':
                    loadReportsModule(mainContent, token);
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
            <div class="card">
                <h3>Pulpit</h3>
                <p>Statystyki systemowe w czasie rzeczywistym.</p>
            </div>
            <div id="stats-grid" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 24px;">
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
        console.error("Dashboard error:", error);
    }
}

// Moduł Taryf
async function loadTariffsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <h3>Taryfy i cenniki</h3>
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
            tbody.innerHTML = data.map(t => `
                <tr style="border-bottom: 1px solid var(--border-color);">
                    <td style="padding: 12px; font-weight: 500;">${t.name}</td>
                    <td style="padding: 12px;">${t.passType || '-'}</td>
                    <td style="padding: 12px;">${t.season || '-'}</td>
                    <td style="padding: 12px; font-weight: 600;">${t.price ? t.price.toFixed(2) + ' zł' : '-'}</td>
                    <td style="padding: 12px;">${t.poolLimit || '-'}</td>
                </tr>
            `).join('');
            document.getElementById('tariffs-loading').style.display = 'none';
            document.getElementById('tariffs-table').style.display = 'table';
        }
    } catch (error) {
        console.error("Tariffs error:", error);
    }
}

// Moduł Raportów (Transakcje)
async function loadReportsModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <h3>Raporty - Ostatnie transakcje</h3>
                <div id="reports-container">
                    <p id="reports-loading">Pobieranie danych...</p>
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

    try {
        const response = await fetch('/api/transakcje', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('#reports-table tbody');
            tbody.innerHTML = data.map(t => `
                <tr style="border-bottom: 1px solid var(--border-color);">
                    <td style="padding: 12px;">${new Date(t.date).toLocaleString('pl-PL')}</td>
                    <td style="padding: 12px;">${t.operationType || '-'}</td>
                    <td style="padding: 12px;">${t.tariff || '-'}</td>
                    <td style="padding: 12px; font-weight: 600; color: ${t.amount < 0 ? '#ef4444' : 'inherit'}">${t.amount.toFixed(2)} zł</td>
                    <td style="padding: 12px;">${t.cashierLogin || '-'}</td>
                </tr>
            `).join('');
            document.getElementById('reports-loading').style.display = 'none';
            document.getElementById('reports-table').style.display = 'table';
        }
    } catch (error) {
        console.error("Reports error:", error);
    }
}

// Moduł Użytkowników
async function loadUsersModule(container, token) {
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <h3>Użytkownicy i uprawnienia</h3>
                <p>Zarządzanie kontami systemowymi (funkcjonalność w trakcie integracji).</p>
                <div id="users-container">
                    <p id="users-loading">Pobieranie listy użytkowników...</p>
                    <table class="data-table" id="users-table" style="display: none; width: 100%; border-collapse: collapse; text-align: left;">
                        <thead>
                            <tr style="border-bottom: 2px solid var(--border-color);">
                                <th style="padding: 12px; color: var(--text-muted);">Login / Email</th>
                                <th style="padding: 12px; color: var(--text-muted);">Rola</th>
                                <th style="padding: 12px; color: var(--text-muted);">Status</th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    // Dla użytkowników dodamy prosty endpoint w kontrolerze, jeśli go nie ma,
    // albo na razie pokażemy informację o braku danych.
    try {
        const response = await fetch('/api/users/all', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('#users-table tbody');
            tbody.innerHTML = data.map(u => `
                <tr style="border-bottom: 1px solid var(--border-color);">
                    <td style="padding: 12px; font-weight: 500;">${u.login || u.email}</td>
                    <td style="padding: 12px;">${u.role}</td>
                    <td style="padding: 12px;">${u.isActive ? '<span style="color: #10b981">Aktywny</span>' : '<span style="color: #ef4444">Nieaktywny</span>'}</td>
                </tr>
            `).join('');
            document.getElementById('users-loading').style.display = 'none';
            document.getElementById('users-table').style.display = 'table';
        } else {
            document.getElementById('users-loading').textContent = "Brak uprawnień lub brak endpointu do pobrania listy użytkowników.";
        }
    } catch (error) {
        document.getElementById('users-loading').textContent = "Błąd połączenia z serwerem.";
    }
}

// Zewnętrzna asynchroniczna funkcja do modułu Wyciągów
async function loadLiftsModule(container, token) {
    // 1. Wstrzyknięcie szkieletu tabeli HTML z komunikatem ładowania
    container.innerHTML = `
        <div class="module-content">
            <div class="card">
                <h3>Zarządzanie wyciągami</h3>
                <p style="color: var(--text-muted); margin-bottom: 24px;">Poniżej znajduje się aktualny wykaz wyciągów pobrany bezpośrednio z systemu.</p>
                
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

                tr.innerHTML = `
                    <td style="padding: 12px;">${lift.id}</td>
                    <td style="padding: 12px; font-weight: 500;">${lift.name}</td>
                    <td style="padding: 12px; color: ${statusColor}; font-weight: 600;">${statusText}</td>
                    <td style="padding: 12px;">${opens}</td>
                    <td style="padding: 12px;">${closes}</td>
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
