/* ============================================================================
 * walkthrough.js — kontekstowe hinty + samouczek dla portalu narciarza.
 *
 * Jeden silnik, sterowany deklaratywnym rejestrem treści (REGISTRIES).
 * Strona aktywuje się przez <body data-wt-page="login|register|narciarz">.
 *
 * Specyfikacja: Dokumentacja/walkthrough-spec.md
 *  - Typy elementów: A=pole, B=przycisk (kotwica statyczna, 5 s),
 *                    C=informacja, D=panel (podąża za kursorem, 1 s)
 *  - Tryby: hover-hint, hint po powtarzającym się błędzie, samouczek (1. użycie)
 *  - Stan zapamiętywany w localStorage (klucze wt_*)
 * ========================================================================== */
(function () {
  'use strict';

  // ---- Konfiguracja globalna ------------------------------------------------
  var VERSIONS        = { login: '1', register: '1', narciarz: '1' };
  var HOVER_DELAY     = { A: 5000, B: 5000, C: 1000, D: 1000 };
  var FOCUS_DELAY     = 1200;   // hint po zatrzymaniu na polu (klawiatura/klik)
  var ANCHOR_PADDING  = 8;      // odstęp popupa od pola/przycisku (typ A/B)
  var CURSOR_OFFSET   = 12;     // odstęp popupa od kursora (typ C/D)
  var ERROR_THRESHOLD = 2;      // ile nieudanych walidacji do pokazania hinta
  var HIDE_GRACE      = 160;    // ms zanim hover-popup zniknie (by trafić w link)

  var IS_TOUCH = window.matchMedia && window.matchMedia('(hover: none)').matches;

  // =========================================================================
  //  REJESTRY TREŚCI
  // =========================================================================
  var REGISTRIES = {

    // ----------------------------- LOGOWANIE -------------------------------
    login: {
      hints: [
        { id: 'email', selector: '#email', type: 'A', tour: 1,
          title: 'Adres e-mail',
          body: ['Pole <b>obowiązkowe</b>.',
                 'Musi zawierać <code>@</code> i domenę, np. <code>jan@poczta.pl</code>.',
                 'Dane testowe: <code>narciarz@example.com</code>.'] },
        { id: 'password', selector: '#password', type: 'A', tour: 2,
          title: 'Hasło',
          body: ['Pole <b>obowiązkowe</b>.',
                 'Dane testowe: <code>haslo123</code>.'] },
        { id: 'testdata', selector: '.hint', type: 'C', tour: 3,
          title: 'Dane testowe',
          body: ['Konto demonstracyjne — pozwala zalogować się bez rejestracji.',
                 'Przepisz e-mail i hasło do pól powyżej.'],
          links: [{ to: 'email', label: 'pole E-mail' }] },
        { id: 'loginbtn', selector: '#loginButton', type: 'B', tour: 4,
          title: 'Zaloguj się',
          body: ['Sprawdza dane logowania.',
                 'Po sukcesie przeniesie Cię do <b>portalu narciarza</b>.'] },
        { id: 'reglink', selector: '.links a', type: 'B',
          title: 'Zarejestruj się',
          body: ['Przejdziesz do formularza zakładania nowego konta.'] }
      ],
      errors: [
        { selector: '#email', message: 'To pole to <b>adres e-mail</b> — musi zawierać <code>@</code> i domenę, np. <code>jan@poczta.pl</code>.',
          bad: function (el) { return !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(el.value.trim()); } },
        { selector: '#password', message: '<b>Hasło jest wymagane.</b>',
          bad: function (el) { return el.value.length === 0; } }
      ],
      submits: [ { selector: '#loginButton', fields: ['#email', '#password'] } ]
    },

    // ----------------------------- REJESTRACJA -----------------------------
    register: {
      hints: [
        { id: 'email', selector: '#email', type: 'A', tour: 1,
          title: 'Adres e-mail',
          body: ['Pole <b>obowiązkowe</b>.',
                 'Format: <code>nazwa@domena.pl</code> — musi zawierać <code>@</code> i kropkę w domenie.'] },
        { id: 'password', selector: '#password', type: 'A', tour: 2,
          title: 'Hasło',
          body: ['Pole <b>obowiązkowe</b>.',
                 'Wymagane <b>minimum 8 znaków</b>.'] },
        { id: 'passhint', selector: '.hint-pass', type: 'C',
          title: 'Wymóg hasła',
          body: ['Hasło krótsze niż 8 znaków zostanie odrzucone.'],
          links: [{ to: 'password', label: 'pole Hasło' }] },
        { id: 'password2', selector: '#password2', type: 'A', tour: 3,
          title: 'Powtórz hasło',
          body: ['Pole <b>obowiązkowe</b>.',
                 'Musi być <b>identyczne</b> z hasłem powyżej.'],
          links: [{ to: 'password', label: 'pole Hasło' }] },
        { id: 'regbtn', selector: '#registerBtn', type: 'B', tour: 4,
          title: 'Zarejestruj się',
          body: ['Zakłada konto i od razu Cię loguje,',
                 'a następnie przenosi do portalu narciarza.'] },
        { id: 'loginlink', selector: '.login-link a', type: 'B', tour: 5,
          title: 'Masz już konto?',
          body: ['Przejdziesz do ekranu logowania.'] }
      ],
      errors: [
        { selector: '#email', message: 'To pole to <b>adres e-mail</b> — musi zawierać <code>@</code> i domenę, np. <code>jan@poczta.pl</code>.',
          bad: function (el) { return !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(el.value.trim()); } },
        { selector: '#password', message: 'Hasło musi mieć <b>min. 8 znaków</b>.',
          bad: function (el) { return el.value.length < 8; } },
        { selector: '#password2', message: 'Hasła <b>muszą być identyczne</b>.',
          bad: function (el) { var p = document.getElementById('password'); return el.value !== (p ? p.value : ''); } }
      ],
      submits: [ { selector: '#registerBtn', fields: ['#email', '#password', '#password2'] } ]
    },

    // ----------------------------- PORTAL NARCIARZA ------------------------
    narciarz: {
      hints: [
        // --- Hero / nawigacja
        { id: 'herocard', selector: '#hero-pass-card', type: 'C', tour: 1,
          title: 'Twój aktywny karnet',
          body: ['Skrót najważniejszych danych: typ karnetu, daty ważności, pozostałe dni i numer karty.',
                 'Aktualizuje się automatycznie po załadowaniu karnetów.'] },
        { id: 'tabs', selector: '.nav-tabs', type: 'D', tour: 2,
          title: 'Nawigacja portalu',
          body: ['Pięć widoków: <b>Moje karnety</b>, <b>Rozkład wyciągów</b>, <b>Historia</b>, <b>Zwróć karnet</b>, <b>Kup karnet</b>.',
                 'Kliknij zakładkę, aby przełączyć sekcję.'] },
        // --- Moje karnety
        { id: 'rfid', selector: '#narciarz-rfid', type: 'A', tour: 3, section: 'karnety',
          title: 'Numer karty RFID',
          body: ['Pole <b>obowiązkowe</b> — wybierz kartę z listy.',
                 'Ten wybór steruje całym portalem: <b>synchronizuje</b> Historię i Zwrot.'],
          links: [{ to: 'load', label: 'przycisk Załaduj' }] },
        { id: 'load', selector: '[onclick="zaladujMojeDane()"]', type: 'B',
          title: 'Załaduj',
          body: ['Pobiera karnety przypisane do wybranej karty i pokazuje je poniżej.',
                 'Bez wybranej karty operacja nic nie zrobi.'] },
        { id: 'passes', selector: '#karnety-passes', type: 'D', tour: 4, section: 'karnety',
          title: 'Twoje karnety',
          body: ['Kafelki karnetów dla wybranej karty.',
                 'Pasek postępu pokazuje wykorzystanie okresu ważności.',
                 'Zmień kartę powyżej, aby zobaczyć inne karnety.'] },
        // --- Rozkład
        { id: 'weather', selector: '.weather-bar', type: 'D', tour: 5, section: 'rozklad',
          title: 'Warunki na stoku',
          body: ['Aktualna pogoda i liczba czynnych wyciągów.',
                 'Licznik wyciągów pobierany jest z API i odświeża się przy wejściu na zakładkę.'] },
        { id: 'lifttable', selector: '#sec-rozklad table', type: 'D', tour: 6, section: 'rozklad',
          title: 'Wyciągi — status i godziny',
          body: ['Kolorowa kropka = status: <b>zielony</b> czynny, <b>pomarańczowy</b> przed otwarciem, <b>czerwony</b> zamknięty.',
                 'Dane są tylko do odczytu.'] },
        // --- Historia
        { id: 'hist-rfid', selector: '#historia-rfid', type: 'A', tour: 7, section: 'historia',
          title: 'Karta RFID',
          body: ['Wybierz kartę, dla której chcesz zobaczyć przejazdy.'] },
        { id: 'hist-date', selector: '#historia-date', type: 'A', section: 'historia',
          title: 'Data',
          body: ['Pole <b>opcjonalne</b>. Format kalendarza (DD.MM.RRRR).',
                 '<b>Puste = wszystkie</b> przejazdy.'] },
        { id: 'hist-load', selector: '[onclick="zaladujHistorie()"]', type: 'B',
          title: 'Załaduj',
          body: ['Pobiera log skanowań bramek dla wybranej karty (i daty, jeśli ustawiona).'] },
        { id: 'hist-print', selector: '#sec-historia .card-header button', type: 'B',
          title: 'Drukuj raport przejazdów',
          body: ['Generuje raport przejazdów (UC10) do wydruku.'] },
        // --- Zwrot
        { id: 'zwrot-rfid', selector: '#zwrot-rfid', type: 'A', section: 'zwrot',
          title: 'Numer karty RFID',
          body: ['Wybierz kartę, której karnet chcesz zwrócić.'] },
        { id: 'zwrot-load', selector: '[onclick="zaladujZwrot()"]', type: 'B',
          title: 'Załaduj karnety',
          body: ['Pokazuje karnety możliwe do zwrotu dla wybranej karty.'] },
        { id: 'zwrot-rules', selector: '#sec-zwrot .alert-info', type: 'C', tour: 8, section: 'zwrot',
          title: 'Zasady zwrotu',
          body: ['Zwrot liczony <b>proporcjonalnie</b> do niewykorzystanych dni.',
                 'Opłata manipulacyjna: <b>10 zł</b>. Kaucja <b>20 zł</b> zwracana po oddaniu karty.'],
          links: [{ to: 'zwrot-card', label: 'opcja zwrotu kaucji' }] },
        { id: 'zwrot-reason', selector: '#zwrot-reason-select', type: 'A', section: 'zwrot',
          title: 'Powód zwrotu',
          body: ['Pole <b>obowiązkowe</b> — wybierz z listy.'] },
        { id: 'zwrot-note', selector: '#zwrot-reason', type: 'A', section: 'zwrot',
          title: 'Dodatkowe uwagi',
          body: ['Pole <b>opcjonalne</b>. Dowolny opis sytuacji.'] },
        { id: 'zwrot-card', selector: '#zwrot-return-card', type: 'A', section: 'zwrot',
          title: 'Zwrot karty RFID',
          body: ['Zaznacz, jeśli oddajesz fizyczną kartę.',
                 'Doliczymy wtedy <b>kaucję 20 zł</b> do kwoty zwrotu.'] },
        { id: 'zwrot-btn', selector: '#zwrot-btn', type: 'B', tour: 9, section: 'zwrot',
          title: 'Złóż wniosek o zwrot',
          body: ['Składa wniosek o zwrot zaznaczonego karnetu (UC11).',
                 'Wymaga <b>zaznaczonego karnetu</b> i <b>powodu</b>.'],
          links: [{ to: 'zwrot-reason', label: 'pole Powód zwrotu' }] },
        // --- Zakup
        { id: 'zakup-from', selector: '#zakup-from', type: 'A', tour: 10, section: 'zakup',
          title: 'Data rozpoczęcia',
          body: ['Pole <b>obowiązkowe</b>. Format kalendarza (DD.MM.RRRR).',
                 'Wybierasz tylko jeden dzień — początek ważności karnetu. Data końcowa jest obliczana automatycznie.'] },
        { id: 'zakup-btn', selector: '#zakup-btn', type: 'B', tour: 11, section: 'zakup',
          title: 'Zapłać i zarezerwuj',
          body: ['Rezerwuje i opłaca karnet online (UC2) — do odbioru przy kasie.',
                 'Aktywny dopiero po wyborze <b>taryfy</b> oraz <b>daty rozpoczęcia</b>.'] },
        // --- Globalne
        { id: 'logout', selector: '[onclick="logout()"]', type: 'B',
          title: 'Wyloguj',
          body: ['Kończy sesję i wraca do ekranu logowania.'] },
        { id: 'help', selector: '.wt-help', type: 'B', tour: 12,
          title: 'Pomoc / wskazówki',
          body: ['W każdej chwili kliknij ten przycisk, aby ponownie uruchomić samouczek.'] }
      ],
      errors: [
        { selector: '#narciarz-rfid', message: 'Najpierw <b>wybierz kartę RFID</b> z listy — bez niej nie pobiorę danych.',
          bad: emptyVal },
        { selector: '#historia-rfid', message: 'Najpierw <b>wybierz kartę RFID</b> z listy.', bad: emptyVal },
        { selector: '#zwrot-rfid', message: 'Najpierw <b>wybierz kartę RFID</b> z listy.', bad: emptyVal },
        { selector: '#zwrot-reason-select', message: '<b>Powód zwrotu jest wymagany</b> — wybierz z listy.', bad: emptyVal },
        { selector: '#zwrot-karnety-lista', message: 'Zaznacz <b>karnet do zwrotu</b> powyżej.',
          bad: function (el) { return !el.querySelector('input[type=radio]:checked'); } }
      ],
      submits: [
        { selector: '[onclick="zaladujMojeDane()"]', fields: ['#narciarz-rfid'] },
        { selector: '[onclick="zaladujHistorie()"]', fields: ['#historia-rfid'] },
        { selector: '[onclick="zaladujZwrot()"]', fields: ['#zwrot-rfid'] },
        { selector: '#zwrot-btn', fields: ['#zwrot-karnety-lista', '#zwrot-reason-select'] }
      ]
    }
  };

  function emptyVal(el) { return !el.value || el.value.trim() === ''; }

  // =========================================================================
  //  STAN
  // =========================================================================
  var page, reg;
  var lastMouse = { x: 0, y: 0 };
  var hoverEl = null, hoverTimer = null, hideTimer = null, focusTimer = null;
  var activePopup = null, followHandler = null;
  var errorCounts = {}, errorPopup = null, errorRule = null, errorEl = null;
  var overlay = null, tourPopup = null;
  var tour = { steps: [], i: 0, active: false, el: null };

  // =========================================================================
  //  POMOCNICZE
  // =========================================================================
  function lsGet(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
  function lsSet(k, v) { try { localStorage.setItem(k, v); } catch (e) {} }
  function hintsEnabled() { return lsGet('wt_hints_enabled') !== '0'; }
  function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
  function depth(el) { var d = 0; while (el) { d++; el = el.parentElement; } return d; }
  function byId(id) { for (var i = 0; i < reg.hints.length; i++) if (reg.hints[i].id === id) return reg.hints[i]; return null; }

  function bodyHtml(hint) {
    var b = hint.body;
    var html = '<div class="wt-body">';
    if (Array.isArray(b)) { for (var i = 0; i < b.length; i++) html += '<p>' + b[i] + '</p>'; }
    else if (b) { html += '<p>' + b + '</p>'; }
    html += '</div>';
    if (hint.links && hint.links.length) {
      html += '<div class="wt-links">';
      for (var j = 0; j < hint.links.length; j++)
        html += '<a href="#" data-wt-link="' + hint.links[j].to + '">→ ' + esc(hint.links[j].label) + '</a>';
      html += '</div>';
    }
    return html;
  }

  function wireLinks(popup) {
    var links = popup.querySelectorAll('[data-wt-link]');
    for (var i = 0; i < links.length; i++) {
      links[i].addEventListener('click', function (e) {
        e.preventDefault();
        openLinkedHint(this.getAttribute('data-wt-link'));
      });
    }
  }

  // =========================================================================
  //  POPUP — hover / informacja
  // =========================================================================
  function buildHintPopup(hint, variant) {
    var p = document.createElement('div');
    p.className = 'wt-popup wt-' + variant + ' wt-type-' + hint.type;
    p.setAttribute('role', 'tooltip');
    var html = '';
    if (hint.title) html += '<div class="wt-title">' + esc(hint.title) + '</div>';
    html += bodyHtml(hint);
    if (variant === 'hint') html += '<button type="button" class="wt-off">Nie pokazuj podpowiedzi</button>';
    p.innerHTML = html;
    wireLinks(p);
    var off = p.querySelector('.wt-off');
    if (off) off.addEventListener('click', function () { lsSet('wt_hints_enabled', '0'); hidePopup(); });
    return p;
  }

  function hidePopup() {
    if (followHandler) { document.removeEventListener('mousemove', followHandler); followHandler = null; }
    if (activePopup && activePopup.parentNode) activePopup.parentNode.removeChild(activePopup);
    activePopup = null;
  }

  function triggerHover(hint, el) {
    hoverTimer = null;
    hidePopup();
    var popup = buildHintPopup(hint, 'hint');
    document.body.appendChild(popup);
    activePopup = popup;
    popup.addEventListener('mouseenter', cancelHide);
    popup.addEventListener('mouseleave', scheduleHide);

    if (hint.type === 'A' || hint.type === 'B') {
      positionStatic(popup, el);
    } else {
      positionCursor(popup, lastMouse.x, lastMouse.y);
      followHandler = function (e) { positionCursor(popup, e.clientX, e.clientY); };
      document.addEventListener('mousemove', followHandler);
    }
  }

  function positionStatic(popup, el) {
    var r = el.getBoundingClientRect();
    var pw = popup.offsetWidth, ph = popup.offsetHeight;
    var left = r.right + ANCHOR_PADDING, top = r.top;
    if (left + pw > window.innerWidth - 4) left = r.left - pw - ANCHOR_PADDING;  // przerzuć w lewo
    left = Math.max(4, Math.min(left, window.innerWidth - pw - 4));
    top = Math.max(4, Math.min(top, window.innerHeight - ph - 4));
    popup.style.left = left + 'px';
    popup.style.top = top + 'px';
  }

  function positionCursor(popup, x, y) {
    var pw = popup.offsetWidth, ph = popup.offsetHeight;
    var left = x + CURSOR_OFFSET, top = y + CURSOR_OFFSET;
    if (left + pw > window.innerWidth - 4) left = x - pw - CURSOR_OFFSET;        // lustro w poziomie
    if (top + ph > window.innerHeight - 4) top = y - ph - CURSOR_OFFSET;         // lustro w pionie
    popup.style.left = Math.max(4, left) + 'px';
    popup.style.top = Math.max(4, top) + 'px';
  }

  // =========================================================================
  //  SILNIK HOVER / FOCUS
  // =========================================================================
  function findHint(target) {
    var best = null, bestDepth = -1;
    for (var i = 0; i < reg.hints.length; i++) {
      var sel = reg.hints[i].selector;
      var el = target.closest ? target.closest(sel) : null;
      if (el) { var d = depth(el); if (d > bestDepth) { bestDepth = d; best = { hint: reg.hints[i], el: el }; } }
    }
    return best;
  }

  function clearHoverTimer() { if (hoverTimer) { clearTimeout(hoverTimer); hoverTimer = null; } }
  function scheduleHide() { cancelHide(); hideTimer = setTimeout(hidePopup, HIDE_GRACE); }
  function cancelHide() { if (hideTimer) { clearTimeout(hideTimer); hideTimer = null; } }

  function onMouseOver(e) {
    if (!hintsEnabled() || tour.active) return;
    var found = findHint(e.target);
    if (!found) return;
    cancelHide();
    if (hoverEl === found.el) return;          // ten sam element — nic nie rób
    hoverEl = found.el;
    clearHoverTimer();
    var delay = HOVER_DELAY[found.hint.type] || 2000;
    hoverTimer = setTimeout(function () { triggerHover(found.hint, found.el); }, delay);
  }

  function onMouseOut(e) {
    if (!hoverEl) return;
    var to = e.relatedTarget;
    if (to && (hoverEl.contains(to) || (activePopup && activePopup.contains(to)))) return;
    clearHoverTimer();
    hoverEl = null;
    scheduleHide();
  }

  // Dostępność: po zatrzymaniu fokusu na polu typu A pokaż hint (klawiatura).
  function onFocusIn(e) {
    if (!hintsEnabled() || tour.active || IS_TOUCH) return;
    var found = findHint(e.target);
    if (!found || found.hint.type !== 'A') return;
    if (focusTimer) clearTimeout(focusTimer);
    focusTimer = setTimeout(function () { triggerHover(found.hint, found.el); }, FOCUS_DELAY);
  }
  function onFocusOut() { if (focusTimer) { clearTimeout(focusTimer); focusTimer = null; } }

  // =========================================================================
  //  SILNIK BŁĘDÓW
  // =========================================================================
  function findRule(sel) { var e = reg.errors || []; for (var i = 0; i < e.length; i++) if (e[i].selector === sel) return e[i]; return null; }

  function showErrorPopup(rule, el) {
    hideErrorPopup();
    var p = document.createElement('div');
    p.className = 'wt-popup wt-error wt-type-A';
    p.setAttribute('role', 'alert');
    p.innerHTML = '<div class="wt-title">⚠️ Sprawdź to pole</div><div class="wt-body"><p>' + rule.message + '</p></div>';
    document.body.appendChild(p);
    positionStatic(p, el);
    errorPopup = p; errorRule = rule; errorEl = el;
  }
  function hideErrorPopup() {
    if (errorPopup && errorPopup.parentNode) errorPopup.parentNode.removeChild(errorPopup);
    errorPopup = null; errorRule = null; errorEl = null;
  }

  function checkField(rule, el, fromSubmit) {
    if (!rule.bad(el)) { errorCounts[rule.selector] = 0; if (errorRule === rule) hideErrorPopup(); return; }
    errorCounts[rule.selector] = (errorCounts[rule.selector] || 0) + 1;
    if (fromSubmit || errorCounts[rule.selector] >= ERROR_THRESHOLD) showErrorPopup(rule, el);
  }

  function bindErrors() {
    document.addEventListener('blur', function (e) {
      var rules = reg.errors || [];
      for (var i = 0; i < rules.length; i++) {
        var el = e.target.closest ? e.target.closest(rules[i].selector) : null;
        if (el) checkField(rules[i], el, false);
      }
    }, true);

    document.addEventListener('input', function (e) {
      var rules = reg.errors || [];
      for (var i = 0; i < rules.length; i++) {
        var el = e.target.closest ? e.target.closest(rules[i].selector) : null;
        if (el && !rules[i].bad(el)) { errorCounts[rules[i].selector] = 0; if (errorRule === rules[i]) hideErrorPopup(); }
      }
    }, true);

    // Walidacja przy kliknięciu przycisku akcji — faza przechwytywania,
    // więc działa PRZED inline onclick i nie blokuje właściwej akcji.
    (reg.submits || []).forEach(function (s) {
      document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest(s.selector) : null;
        if (!btn) return;
        for (var i = 0; i < s.fields.length; i++) {
          var rule = findRule(s.fields[i]);
          if (!rule) continue;
          var el = document.querySelector(s.fields[i]);
          if (el && rule.bad(el)) { checkField(rule, el, true); break; }
        }
      }, true);
    });
  }

  // =========================================================================
  //  ODSYŁACZE MIĘDZY HINTAMI
  // =========================================================================
  function openLinkedHint(id) {
    var hint = byId(id);
    if (!hint) return;
    var el = document.querySelector(hint.selector);
    if (!el) return;
    if (typeof el.scrollIntoView === 'function') el.scrollIntoView({ block: 'center', behavior: 'smooth' });
    el.classList.add('wt-flash');
    setTimeout(function () { el.classList.remove('wt-flash'); }, 1200);
    triggerHover(hint, el);
  }

  // =========================================================================
  //  SAMOUCZEK (TOUR)
  // =========================================================================
  function activateSection(id) {
    var tabs = document.querySelectorAll('.nav-tab'), tab = null;
    for (var i = 0; i < tabs.length; i++) {
      if ((tabs[i].getAttribute('onclick') || '').indexOf("showSection('" + id + "'") !== -1) { tab = tabs[i]; break; }
    }
    if (tab) tab.click();
    else if (typeof window.showSection === 'function') window.showSection(id);
  }

  function createOverlay() {
    overlay = document.createElement('div');
    overlay.className = 'wt-overlay';
    document.body.appendChild(overlay);
  }

  function spotlight(el) {
    if (!overlay) return;
    if (!el) { overlay.style.opacity = '0'; return; }
    var r = el.getBoundingClientRect(), pad = 6;
    overlay.style.opacity = '1';
    overlay.style.top = (r.top - pad) + 'px';
    overlay.style.left = (r.left - pad) + 'px';
    overlay.style.width = (r.width + pad * 2) + 'px';
    overlay.style.height = (r.height + pad * 2) + 'px';
  }

  function positionTour(p, el) {
    var pw = p.offsetWidth, ph = p.offsetHeight, gap = 14, left, top;
    if (!el) { p.style.left = (window.innerWidth - pw) / 2 + 'px'; p.style.top = (window.innerHeight - ph) / 2 + 'px'; return; }
    var r = el.getBoundingClientRect();
    if (r.right + gap + pw <= window.innerWidth) { left = r.right + gap; top = r.top; }
    else if (r.bottom + gap + ph <= window.innerHeight) { left = r.left; top = r.bottom + gap; }
    else if (r.top - gap - ph >= 0) { left = r.left; top = r.top - gap - ph; }
    else { left = (window.innerWidth - pw) / 2; top = (window.innerHeight - ph) / 2; }
    p.style.left = Math.max(8, Math.min(left, window.innerWidth - pw - 8)) + 'px';
    p.style.top = Math.max(8, Math.min(top, window.innerHeight - ph - 8)) + 'px';
  }

  function removeTourPopup() { if (tourPopup && tourPopup.parentNode) tourPopup.parentNode.removeChild(tourPopup); tourPopup = null; }

  function renderTourPopup(step, el, i) {
    removeTourPopup();
    var n = tour.steps.length;
    var p = document.createElement('div');
    p.className = 'wt-popup wt-tour';
    p.setAttribute('role', 'dialog');
    p.setAttribute('aria-modal', 'true');
    p.innerHTML =
      '<div class="wt-step">Krok ' + (i + 1) + '/' + n + '</div>' +
      '<div class="wt-title">' + esc(step.title || '') + '</div>' +
      bodyHtml(step) +
      '<div class="wt-actions">' +
        '<button type="button" class="wt-skip">Pomiń</button>' +
        '<div class="wt-nav">' +
          '<button type="button" class="wt-back"' + (i === 0 ? ' disabled' : '') + '>Wstecz</button>' +
          '<button type="button" class="wt-next">' + (i === n - 1 ? 'Zakończ' : 'Dalej') + '</button>' +
        '</div>' +
      '</div>';
    document.body.appendChild(p);
    tourPopup = p;
    wireLinks(p);
    positionTour(p, el);
    p.querySelector('.wt-skip').addEventListener('click', function () { endTour(true); });
    p.querySelector('.wt-back').addEventListener('click', function () { if (tour.i > 0) showStep(tour.i - 1); });
    p.querySelector('.wt-next').addEventListener('click', function () { if (tour.i < n - 1) showStep(tour.i + 1); else endTour(false); });
    p.querySelector('.wt-next').focus();
  }

  function showStep(i) {
    tour.i = i;
    var step = tour.steps[i];
    if (step.section) activateSection(step.section);
    setTimeout(function () {
      var el = document.querySelector(step.selector);
      tour.el = el;
      if (overlay) overlay.style.transition = '';   // animuj zmianę kroku...
      spotlight(el);
      renderTourPopup(step, el, i);
    }, step.section ? 280 : 0);
  }

  // Trzyma spotlight i popup przy elemencie podczas scrolla/resize.
  // position:fixed liczone z getBoundingClientRect(), więc przeliczamy na bieżąco.
  function repositionTour() {
    if (!tour.active || !tour.el) return;
    if (overlay) overlay.style.transition = 'none';   // ...ale śledź scroll bez lagu
    spotlight(tour.el);
    if (tourPopup) positionTour(tourPopup, tour.el);
  }

  function onWindowScroll() {
    if (tour.active) repositionTour();
    else if (errorPopup && errorEl) positionStatic(errorPopup, errorEl);
  }

  function tourKeys(e) {
    if (!tour.active) return;
    if (e.key === 'Escape') { endTour(true); }
    else if (e.key === 'ArrowRight') { if (tour.i < tour.steps.length - 1) showStep(tour.i + 1); else endTour(false); }
    else if (e.key === 'ArrowLeft') { if (tour.i > 0) showStep(tour.i - 1); }
  }

  function startTour() {
    if (tour.active) return;
    tour.steps = reg.hints.filter(function (h) { return h.tour; }).sort(function (a, b) { return a.tour - b.tour; });
    if (!tour.steps.length) return;
    clearHoverTimer(); hidePopup(); hideErrorPopup();
    tour.active = true; tour.i = 0;
    createOverlay();
    document.addEventListener('keydown', tourKeys);
    showStep(0);
  }

  function endTour(skipped) {
    tour.active = false;
    removeTourPopup();
    if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
    overlay = null;
    document.removeEventListener('keydown', tourKeys);
    lsSet('wt_seen_' + page, VERSIONS[page]);
  }

  // =========================================================================
  //  PRZYCISK POMOCY + AUTO-START
  // =========================================================================
  function injectHelpButton() {
    var b = document.createElement('button');
    b.type = 'button';
    b.className = 'wt-help';
    b.textContent = '?';
    b.title = 'Pokaż wskazówki';
    b.setAttribute('aria-label', 'Pokaż samouczek');
    b.addEventListener('click', startTour);
    document.body.appendChild(b);
  }

  function maybeAutoTour() {
    if (lsGet('wt_seen_' + page) === VERSIONS[page]) return;
    setTimeout(startTour, 700);   // chwila na ułożenie layoutu / doładowanie danych
  }

  // =========================================================================
  //  STYLE
  // =========================================================================
  function injectStyles() {
    var css = [
      '.wt-popup{position:fixed;z-index:10000;max-width:300px;background:#fff;color:#212121;',
        'border:1px solid #E0E0E0;border-radius:8px;box-shadow:0 8px 28px rgba(0,0,0,.22);',
        'padding:12px 14px;font:14px/1.45 "Segoe UI",system-ui,sans-serif;animation:wtIn .12s ease-out}',
      '@keyframes wtIn{from{opacity:0;transform:translateY(4px)}to{opacity:1;transform:none}}',
      '.wt-popup .wt-title{font-weight:700;font-size:14px;margin-bottom:6px;color:#0D47A1}',
      '.wt-popup .wt-body p{margin:0 0 6px;font-size:13px;color:#37474F}',
      '.wt-popup .wt-body p:last-child{margin-bottom:0}',
      '.wt-popup code{background:#ECEFF1;border-radius:4px;padding:1px 5px;font-family:Consolas,monospace;font-size:12px;color:#37474F}',
      '.wt-popup .wt-links{margin-top:8px;display:flex;flex-direction:column;gap:4px}',
      '.wt-popup .wt-links a{font-size:12px;font-weight:600;color:#1565C0;text-decoration:none}',
      '.wt-popup .wt-links a:hover{text-decoration:underline}',
      '.wt-popup .wt-off{margin-top:10px;background:none;border:0;color:#90A4AE;font-size:11px;cursor:pointer;padding:0;font-family:inherit}',
      '.wt-popup .wt-off:hover{color:#607D8B;text-decoration:underline}',
      '.wt-popup.wt-type-C,.wt-popup.wt-type-D{border-radius:6px}',
      '.wt-popup.wt-error{border-color:#C62828;border-left:4px solid #C62828;background:#FFF5F5}',
      '.wt-popup.wt-error .wt-title{color:#C62828}',
      // tour
      '.wt-popup.wt-tour{max-width:340px;z-index:10001;border-top:4px solid #1565C0}',
      '.wt-popup.wt-tour .wt-step{font-size:11px;font-weight:700;letter-spacing:.05em;text-transform:uppercase;color:#90A4AE;margin-bottom:6px}',
      '.wt-popup.wt-tour .wt-title{font-size:16px}',
      '.wt-popup.wt-tour .wt-actions{display:flex;justify-content:space-between;align-items:center;margin-top:14px;gap:10px}',
      '.wt-popup.wt-tour .wt-nav{display:flex;gap:8px}',
      '.wt-popup.wt-tour button{font:inherit;font-size:13px;font-weight:600;border-radius:6px;cursor:pointer;padding:7px 14px;border:1px solid transparent}',
      '.wt-popup.wt-tour .wt-skip{background:none;border-color:transparent;color:#90A4AE;padding-left:0}',
      '.wt-popup.wt-tour .wt-skip:hover{color:#607D8B}',
      '.wt-popup.wt-tour .wt-back{background:#fff;border-color:#E0E0E0;color:#212121}',
      '.wt-popup.wt-tour .wt-back:disabled{opacity:.4;cursor:default}',
      '.wt-popup.wt-tour .wt-next{background:#1565C0;color:#fff}',
      '.wt-popup.wt-tour .wt-next:hover{background:#0D47A1}',
      // overlay / spotlight
      '.wt-overlay{position:fixed;z-index:9500;border-radius:8px;pointer-events:none;',
        'box-shadow:0 0 0 9999px rgba(13,33,64,.55);transition:all .25s ease;opacity:0}',
      '.wt-flash{animation:wtFlash 1.2s ease}',
      '@keyframes wtFlash{0%,100%{box-shadow:0 0 0 0 rgba(21,101,192,0)}30%{box-shadow:0 0 0 4px rgba(21,101,192,.5)}}',
      // help button
      '.wt-help{position:fixed;right:20px;bottom:20px;z-index:9000;width:44px;height:44px;border-radius:50%;',
        'border:0;background:#1565C0;color:#fff;font-size:22px;font-weight:700;cursor:pointer;',
        'box-shadow:0 4px 14px rgba(0,0,0,.25);font-family:inherit}',
      '.wt-help:hover{background:#0D47A1}',
      '@media (prefers-reduced-motion: reduce){.wt-popup,.wt-overlay{animation:none;transition:none}}'
    ].join('');
    var style = document.createElement('style');
    style.id = 'wt-styles';
    style.textContent = css;
    document.head.appendChild(style);
  }

  // =========================================================================
  //  INIT
  // =========================================================================
  function init() {
    page = document.body.getAttribute('data-wt-page');
    reg = REGISTRIES[page];
    if (!reg) return;

    injectStyles();
    injectHelpButton();
    bindErrors();

    if (!IS_TOUCH) {
      document.addEventListener('mousemove', function (e) { lastMouse.x = e.clientX; lastMouse.y = e.clientY; }, { passive: true });
      document.addEventListener('mouseover', onMouseOver);
      document.addEventListener('mouseout', onMouseOut);
      document.addEventListener('scroll', function () { clearHoverTimer(); hoverEl = null; hidePopup(); }, true);
    }
    // Śledzenie scrolla/resize dla popupów samouczka i błędu (działa też na dotyku).
    window.addEventListener('scroll', onWindowScroll, true);
    window.addEventListener('resize', onWindowScroll);

    document.addEventListener('focusin', onFocusIn);
    document.addEventListener('focusout', onFocusOut);
    window.addEventListener('keydown', function (e) { if (e.key === 'Escape' && !tour.active) { hidePopup(); hideErrorPopup(); } });

    maybeAutoTour();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
