-- Dane testowe dla systemu sprzedaży biletów narciarskich.
-- Uruchamiany po 01_schema.sql i 02_sekwencje.sql.

-- ========== SŁOWNIKI ==========

INSERT INTO "dict_card_status" (id, name) VALUES
  (1, 'wolna'),
  (2, 'zajeta'),
  (3, 'zastrzezony');

INSERT INTO "dict_pass_status" (id, name) VALUES
  (1, 'aktywny'),
  (2, 'zablokowany'),
  (3, 'zwrocony'),
  (4, 'oczekuje_na_zwrot'),
  (5, 'wygasly');

INSERT INTO "dict_pass_type" (id, name) VALUES
  (1, 'bilet_jednorazowy'),
  (2, 'karnet');

INSERT INTO "dict_operation_type" (id, name) VALUES
  (1, 'sprzedaz_biletu'),
  (2, 'sprzedaz_karnetu'),
  (3, 'zwrot_karnetu'),
  (4, 'kaucja');

INSERT INTO "dict_season" (id, name) VALUES
  (1, 'Sezon Zimowy 2025/26');

INSERT INTO "dict_trail_difficulty" (id, name) VALUES
  (1, 'latwa'),
  (2, 'srednia'),
  (3, 'trudna'),
  (4, 'ekstremalna');

INSERT INTO "dict_reservation_status" (id, name) VALUES
  (1, 'potwierdzona'),
  (2, 'anulowana');

INSERT INTO "dict_verification_result" (id, name) VALUES
  (1, 'ok'),
  (2, 'niewazny'),
  (3, 'zablokowany');

INSERT INTO "dict_report_type" (id, name, description) VALUES
  (1, 'zmiana',     'Raport dzienny kasjera'),
  (2, 'zarzadczy', 'Raport zarządczy administratora');

-- Aktualizacja sekwencji po ręcznych INSERT z id
SELECT setval(pg_get_serial_sequence('"dict_card_status"',       'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_pass_status"',       'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_pass_type"',         'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_operation_type"',    'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_season"',            'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_trail_difficulty"',  'id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_reservation_status"','id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_verification_result"','id'), 10);
SELECT setval(pg_get_serial_sequence('"dict_report_type"',       'id'), 10);

-- ========== UŻYTKOWNICY ==========

INSERT INTO "cashier" (id, login, password_hash, is_active) VALUES
  (1, 'kasjer@stacja.pl',  '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', true),
  (2, 'kasjer2@stacja.pl', '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', true);

INSERT INTO "administrator" (id, login, password_hash, is_active) VALUES
  (1, 'admin@stacja.pl', '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', true);

INSERT INTO "trail_planner" (id, login, password_hash, is_active) VALUES
  (1, 'planista@stacja.pl', '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', true);

INSERT INTO "user" (id, email, password_hash, created_at) VALUES
  (1, 'anna.nowak@gmail.com',  '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', NOW()),
  (2, 'piotr.kowal@gmail.com', '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', NOW()),
  (3, 'maria.test@gmail.com',  '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', NOW()),
  (4, 'narciarz@example.com',  '$2a$11$qClV91s8x9Gerg/zss45t.D.JaJq4AK.yd8xHVV5qJ0Y0kibjcDYu', NOW());

SELECT setval(pg_get_serial_sequence('"cashier"',      'id'), 10);
SELECT setval(pg_get_serial_sequence('"administrator"','id'), 10);
SELECT setval(pg_get_serial_sequence('"trail_planner"','id'), 10);
SELECT setval(pg_get_serial_sequence('"user"',         'id'), 10);

-- ========== WYCIĄGI I TRASY ==========

INSERT INTO "lift" (id, name, location, length, planner_id) VALUES
  (1, 'Wyciąg Główny',         'Strefa A',   1200.0, 1),
  (2, 'Ekspres Orlica',        'Strefa B',    800.0, 1),
  (3, 'Wyciąg Dolny',          'Dolna Stacja', 600.0, 1),
  (4, 'Orczyk Mała Turnia',    'Strefa C',    400.0, 1),
  (5, 'Kolej Linowa Szczyt',   'Szczyt',     1500.0, 1),
  (6, 'Gondola Wierch',        'Strefa D',   1100.0, 1);

INSERT INTO "trail" (id, name, location, length, difficulty_id, planner_id) VALUES
  (1, 'Trasa A – Główna',      'Strefa A', 2000.0, 2, 1),
  (2, 'Trasa B – Slalom',      'Strefa A', 1500.0, 2, 1),
  (3, 'Trasa C – Czerwona',    'Strefa D', 1800.0, 3, 1),
  (4, 'Trasa D – Expert',      'Strefa B', 1200.0, 4, 1),
  (5, 'Trasa E – Narciarz',    'Dolna',     800.0, 1, 1),
  (6, 'Trasa F – Rodzinna',    'Strefa C',  600.0, 1, 1);

INSERT INTO "lift_trail" (lift_id, trail_id) VALUES
  (1, 1), (1, 2),
  (2, 4),
  (3, 5),
  (4, 5), (4, 6),
  (6, 3);

-- Rozkłady: 1=Pon, 2=Wt, ..., 0=Nd
INSERT INTO "lift_schedule" (id, lift_id, day_of_week, opening_time, closing_time) VALUES
  -- Wyciąg Główny: pon–nd 08:00–16:30
  (1,  1, 1, '08:00', '16:30'), (2,  1, 2, '08:00', '16:30'), (3,  1, 3, '08:00', '16:30'),
  (4,  1, 4, '08:00', '16:30'), (5,  1, 5, '08:00', '16:30'), (6,  1, 6, '08:00', '17:00'),
  (7,  1, 0, '08:00', '17:00'),
  -- Ekspres Orlica: wt–sob 09:00–15:00
  (8,  2, 2, '09:00', '15:00'), (9,  2, 3, '09:00', '15:00'), (10, 2, 4, '09:00', '15:00'),
  (11, 2, 5, '09:00', '15:00'), (12, 2, 6, '09:00', '15:30'),
  -- Wyciąg Dolny: pon–nd 08:00–17:00
  (13, 3, 1, '08:00', '17:00'), (14, 3, 2, '08:00', '17:00'), (15, 3, 3, '08:00', '17:00'),
  (16, 3, 4, '08:00', '17:00'), (17, 3, 5, '08:00', '17:00'), (18, 3, 6, '08:00', '17:30'),
  (19, 3, 0, '08:00', '17:30'),
  -- Orczyk: pon–nd 09:00–16:00
  (20, 4, 1, '09:00', '16:00'), (21, 4, 2, '09:00', '16:00'), (22, 4, 3, '09:00', '16:00'),
  (23, 4, 4, '09:00', '16:00'), (24, 4, 5, '09:00', '16:00'), (25, 4, 6, '09:00', '16:30'),
  (26, 4, 0, '09:00', '16:30'),
  -- Kolej Linowa: śr–nd 10:00–14:00
  (27, 5, 3, '10:00', '14:00'), (28, 5, 4, '10:00', '14:00'), (29, 5, 5, '10:00', '14:00'),
  (30, 5, 6, '10:00', '15:00'), (31, 5, 0, '10:00', '15:00'),
  -- Gondola: pon–nd 08:30–16:00
  (32, 6, 1, '08:30', '16:00'), (33, 6, 2, '08:30', '16:00'), (34, 6, 3, '08:30', '16:00'),
  (35, 6, 4, '08:30', '16:00'), (36, 6, 5, '08:30', '16:00'), (37, 6, 6, '08:30', '16:30'),
  (38, 6, 0, '08:30', '16:30');

INSERT INTO "gate" (id, lift_id, name, is_active) VALUES
  (1, 1, 'Bramka-A1', true),
  (2, 1, 'Bramka-A2', true),
  (3, 2, 'Bramka-B1', true),
  (4, 3, 'Bramka-C1', true),
  (5, 4, 'Bramka-D1', true),
  (6, 6, 'Bramka-E1', true);

SELECT setval(pg_get_serial_sequence('"lift"',          'id'), 20);
SELECT setval(pg_get_serial_sequence('"trail"',         'id'), 20);
SELECT setval(pg_get_serial_sequence('"lift_schedule"', 'id'), 100);
SELECT setval(pg_get_serial_sequence('"gate"',          'id'), 20);

-- ========== TARYFY ==========

INSERT INTO "tariff" (id, name, season_id, pass_type_id, price, pool_limit) VALUES
  -- Bilety jednorazowe
  (1, 'Bilet Dorosły',    1, 1,  89.00, NULL),
  (2, 'Bilet Dziecko',    1, 1,  59.00, NULL),
  (3, 'Bilet Senior',     1, 1,  69.00, NULL),
  (4, 'Bilet Grupowy',    1, 1,  79.00, 500),
  -- Karnety
  (5, 'Karnet 1-dniowy Dorosły',  1, 2, 119.00, NULL),
  (6, 'Karnet 3-dniowy Dorosły',  1, 2, 299.00, NULL),
  (7, 'Karnet 5-dniowy Dorosły',  1, 2, 459.00, NULL),
  (8, 'Karnet 7-dniowy Dorosły',  1, 2, 589.00, NULL),
  (9, 'Karnet 1-dniowy Dziecko',  1, 2,  79.00, NULL),
  (10,'Karnet 3-dniowy Dziecko',  1, 2, 199.00, NULL),
  (11,'Karnet 5-dniowy Senior',   1, 2, 369.00, NULL);

SELECT setval(pg_get_serial_sequence('"tariff"', 'id'), 20);

-- ========== KARTY RFID ==========
-- Wszystkie karty startują ze statusem "wolna" (1) — bez przypisanych karnetów.

INSERT INTO "card" (id, status_id, physical_condition, added_to_pool_at) VALUES
  ('A3:F2:11:CC', 1, 'dobry', NOW() - INTERVAL '30 days'),
  ('B7:AA:32:0E', 1, 'dobry', NOW() - INTERVAL '20 days'),
  ('C1:DE:84:57', 1, 'dobry', NOW() - INTERVAL '10 days'),
  ('E9:CC:01:A3', 1, 'dobry', NOW() - INTERVAL '5 days'),
  ('F0:11:22:33', 1, 'dobry', NOW() - INTERVAL '3 days'),
  ('AA:BB:CC:DD', 1, 'dobry', NOW() - INTERVAL '1 day'),
  ('11:22:33:44', 1, 'nowy',  NOW()),
  ('55:66:77:88', 1, 'nowy',  NOW());

-- ========== REZERWACJE, KARNETY I TRANSAKCJE ==========
-- Brak danych startowych — system gotowy do testowania sprzedaży od zera.

SELECT setval(pg_get_serial_sequence('"reservation"', 'id'), 100);
SELECT setval(pg_get_serial_sequence('"ski_pass"',    'id'), 100);
SELECT setval(pg_get_serial_sequence('"transaction"', 'id'), 100);
