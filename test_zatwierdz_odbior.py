#!/usr/bin/env python3
"""
Testy end-to-end dla endpointu POST /api/karnety/zatwierdz-odbior
Wymagania: pip install requests
Uruchom z działającym docker-compose.
"""

import requests
import sys
from datetime import datetime, timedelta

BASE = "http://localhost:5000"
FREE_CARD = "55:66:77:88"  # wolna karta z danych testowych


def login(email, password, role):
    r = requests.post(f"{BASE}/api/auth/login", json={"email": email, "password": password, "role": role})
    assert r.status_code == 200, f"Login failed ({r.status_code}): {r.text}"
    return r.json()["token"]


def reserve_online(token, tariff_id=13):
    valid_from = (datetime.utcnow() + timedelta(days=1)).strftime("%Y-%m-%dT00:00:00")
    valid_to = (datetime.utcnow() + timedelta(days=4)).strftime("%Y-%m-%dT00:00:00")
    r = requests.post(
        f"{BASE}/api/zakup/online",
        json={"tariffId": tariff_id, "validFrom": valid_from, "validTo": valid_to},
        headers={"Authorization": f"Bearer {token}"},
    )
    assert r.status_code == 200, f"Rezerwacja online failed ({r.status_code}): {r.text}"
    return r.json()["reservationNumber"]


def zatwierdz(token, reservation_number, card_rfid):
    return requests.post(
        f"{BASE}/api/karnety/zatwierdz-odbior",
        json={"reservationNumber": reservation_number, "cardRFID": card_rfid},
        headers={"Authorization": f"Bearer {token}"},
    )


def free_card(card_rfid):
    """Zwalnia kartę bezpośrednio przez API blokowania — reset do wolna przez DB nie jest dostępny przez API,
    więc testy używają różnych kart lub zakładają czysty stan."""
    pass


def run_tests():
    passed = 0
    failed = 0

    def ok(name):
        nonlocal passed
        passed += 1
        print(f"  PASS  {name}")

    def fail(name, reason):
        nonlocal failed
        failed += 1
        print(f"  FAIL  {name}: {reason}")

    print("Logowanie...")
    try:
        narciarz_token = login("narciarz@example.com", "password", "narciarz")
        kasjer_token = login("kasjer@stacja.pl", "password", "kasjer")
    except AssertionError as e:
        print(f"Nie można zalogować: {e}")
        sys.exit(1)

    # --- TC1: happy path ---
    print("\nTC1: happy path — poprawna rezerwacja i wolna karta")
    try:
        res_num = reserve_online(narciarz_token)
        r = zatwierdz(kasjer_token, res_num, FREE_CARD)
        if r.status_code == 200:
            ok("TC1 happy path")
        else:
            fail("TC1 happy path", f"status {r.status_code}: {r.text}")
    except AssertionError as e:
        fail("TC1 happy path", str(e))

    # --- TC2: ta sama rezerwacja drugi raz ---
    print("\nTC2: próba podwójnego zatwierdzenia tej samej rezerwacji")
    try:
        res_num2 = reserve_online(narciarz_token)
        r1 = zatwierdz(kasjer_token, res_num2, "11:22:33:44")
        r2 = zatwierdz(kasjer_token, res_num2, "AA:BB:CC:DD")
        if r1.status_code == 200 and r2.status_code == 400:
            ok("TC2 podwójne zatwierdzenie odrzucone")
        else:
            fail("TC2", f"r1={r1.status_code}, r2={r2.status_code}: {r2.text}")
    except AssertionError as e:
        fail("TC2", str(e))

    # --- TC3: nieistniejący numer rezerwacji ---
    print("\nTC3: nieistniejący numer rezerwacji")
    r = zatwierdz(kasjer_token, "ONL-NIEISTNIEJE999", FREE_CARD)
    if r.status_code == 400:
        ok("TC3 nieistniejąca rezerwacja → 400")
    else:
        fail("TC3", f"oczekiwano 400, dostano {r.status_code}: {r.text}")

    # --- TC4: nieistniejąca karta RFID ---
    print("\nTC4: nieistniejąca karta RFID")
    try:
        res_num4 = reserve_online(narciarz_token)
        r = zatwierdz(kasjer_token, res_num4, "FF:FF:FF:FF")
        if r.status_code == 400:
            ok("TC4 nieistniejący RFID → 400")
        else:
            fail("TC4", f"oczekiwano 400, dostano {r.status_code}: {r.text}")
    except AssertionError as e:
        fail("TC4", str(e))

    # --- TC5: brak autoryzacji (bez tokena) ---
    print("\nTC5: brak tokena → 401")
    r = requests.post(f"{BASE}/api/karnety/zatwierdz-odbior", json={"reservationNumber": "x", "cardRFID": "x"})
    if r.status_code == 401:
        ok("TC5 brak tokena → 401")
    else:
        fail("TC5", f"oczekiwano 401, dostano {r.status_code}")

    # --- TC6: narciarz próbuje zatwierdzić (zamiast kasjera) ---
    print("\nTC6: narciarz próbuje wywołać endpoint kasjera")
    try:
        res_num6 = reserve_online(narciarz_token)
        r = zatwierdz(narciarz_token, res_num6, FREE_CARD)
        if r.status_code in (401, 403):
            ok("TC6 narciarz odrzucony → 401/403")
        else:
            fail("TC6", f"oczekiwano 401/403, dostano {r.status_code}: {r.text}")
    except AssertionError as e:
        fail("TC6", str(e))

    print(f"\n{'='*40}")
    print(f"Wyniki: {passed} passed, {failed} failed")
    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    run_tests()
