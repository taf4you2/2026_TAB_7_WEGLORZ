import { apiFetch, showToast, token } from './core.js';

export async function loadCards() {
    const search = document.getElementById('card-search').value;
    const data = await apiFetch(`/api/karty${search ? '?search=' + encodeURIComponent(search) : ''}`);
    if (data) {
        document.getElementById('cards-tbody').innerHTML = data.map(c => `
            <tr>
                <td style="font-family:monospace; font-weight:600;">${c.id}</td>
                <td><span class="badge badge-${c.status === 'wolna' ? 'active' : (c.status === 'zastrzezony' ? 'inactive' : 'info')}">${c.status.toUpperCase()}</span></td>
                <td>${c.owner || '-'}</td>
                <td>${c.activePassType || '-'}</td>
                <td>${c.depositPaid ? 'TAK' : 'NIE'}</td>
                <td>
                    <div style="display:flex; gap:4px;">
                        ${c.status === 'zastrzezony' 
                            ? `<button class="btn btn-outline" style="padding:4px 8px; font-size:11px;" onclick="unblockCard('${c.id}')">Odblokuj</button>`
                            : `<button class="btn btn-outline" style="padding:4px 8px; font-size:11px; color:var(--danger);" onclick="blockCard('${c.id}')">Blokuj</button>`
                        }
                        <button class="btn btn-outline" style="padding:4px 8px; font-size:11px; color:var(--warning);" onclick="returnCard('${c.id}')">Zwrot</button>
                        ${c.status === 'zastrzezony' 
                            ? ''
                            : `<button class="btn btn-outline" style="padding:4px 8px; font-size:11px; color:var(--danger);" onclick="deleteCard('${c.id}')">Dezaktywuj</button>`
                        }
                    </div>
                </td>
            </tr>
        `).join('');
    }
}

export function openCardModal() {
    document.getElementById('modal-overlay-card').style.display = 'grid';
    document.getElementById('card-rfid').value = '';
}
export function closeCardModal() { document.getElementById('modal-overlay-card').style.display = 'none'; }

export async function handleCardSubmit(e) {
    e.preventDefault();
    const rfid = document.getElementById('card-rfid').value;
    const res = await fetch('/api/karty', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: rfid })
    });
    if (res.ok) { showToast('Dodano karte'); closeCardModal(); loadCards(); }
    else { const err = await res.json().catch(()=>({})); showToast(err.message || 'Blad dodawania', 'error'); }
}

export async function blockCard(id) {
    const reason = prompt('Podaj powod blokady:');
    if (reason === null) return;
    const res = await fetch(`/api/karty/${id}/blokuj`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason })
    });
    if (res.ok) { showToast('Zablokowano'); loadCards(); }
}

export async function unblockCard(id) {
    const res = await fetch(`/api/karty/${id}/odblokuj`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) { showToast('Odblokowano'); loadCards(); }
}

export async function returnCard(id) {
    if (!confirm('Czy na pewno chcesz zwrocic karte do puli wolnych? (Zresetuje to wlasciciela i depozyt)')) return;
    const res = await fetch(`/api/karty/${id}/zwrot`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (res.ok) { showToast('Karta zwrocona'); loadCards(); }
    else { const err = await res.json().catch(()=>({})); showToast(err.message || 'Blad zwrotu', 'error'); }
}

export async function deleteCard(id) {
    if (!confirm('Czy na pewno chcesz dezaktywowac te karte?')) return;
    const res = await fetch(`/api/karty/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
    if (res.ok) { showToast('Dezaktywowano karte'); loadCards(); }
    else { const err = await res.json().catch(()=>({})); showToast(err.message || 'Blad dezaktywacji', 'error'); }
}
