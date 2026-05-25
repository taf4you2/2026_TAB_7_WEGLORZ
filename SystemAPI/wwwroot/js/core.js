export const token = localStorage.getItem('jwt_token');

export function parseJwt(t) {
    try {
        return JSON.parse(atob(t.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    } catch (e) {
        return null;
    }
}

export async function apiFetch(path) {
    try {
        const res = await fetch(path, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.status === 401) logout();
        if (!res.ok) throw new Error();
        return await res.json();
    } catch (e) {
        showToast('Blad komunikacji z serwerem', 'error');
        return null;
    }
}

export function showToast(m, type = 'info') {
    const t = document.getElementById('toast');
    t.textContent = m;
    t.style.borderColor = type === 'error' ? 'var(--danger)' : 'var(--border)';
    t.style.transform = 'translateY(0)';
    setTimeout(() => { t.style.transform = 'translateY(150%)'; }, 3000);
}

export function logout() {
    localStorage.removeItem('jwt_token');
    window.location.href = 'admin-login.html';
}
