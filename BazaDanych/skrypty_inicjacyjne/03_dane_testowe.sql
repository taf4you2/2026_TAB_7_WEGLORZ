-- Dane testowe dla systemu sprzedazy biletow narciarskich.
-- Uruchamiany po 01_schema.sql, 02_sekwencje.sql i 02b_slowniki.sql.

-- ========== UZYTKOWNICY ==========

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

-- ========== WYCIAGI I TRASY ==========

INSERT INTO "lift" (id, name, location, length, planner_id) VALUES
  (1, 'Wyciag Glowny',           'Strefa A',   1200.0, 1),
  (2, 'Ekspres Orlica',        'Strefa B',    800.0, 1),
  (3, 'Wyciag Dolny',           'Dolna Stacja', 600.0, 1),
  (4, 'Orczyk Mala Turnia',     'Strefa C',    400.0, 1),
  (5, 'Kolej Linowa Szczyt',   'Szczyt',     1500.0, 1),
  (6, 'Gondola Wierch',        'Strefa D',   1100.0, 1);

INSERT INTO "trail" (id, name, location, length, difficulty_id, planner_id) VALUES
  (1, 'Trasa A - Glowna',        'Strefa A', 2000.0, 2, 1),
  (2, 'Trasa B - Slalom',      'Strefa A', 1500.0, 2, 1),
  (3, 'Trasa C - Czerwona',    'Strefa D', 1800.0, 3, 1),
  (4, 'Trasa D - Expert',      'Strefa B', 1200.0, 4, 1),
  (5, 'Trasa E - Narciarz',    'Dolna',     800.0, 1, 1),
  (6, 'Trasa F - Rodzinna',    'Strefa C',  600.0, 1, 1);

INSERT INTO "lift_trail" (lift_id, trail_id) VALUES
  (1, 1), (1, 2),
  (2, 4),
  (3, 5),
  (4, 5), (4, 6),
  (6, 3);

-- Rozklady: 1=Pon, 2=Wt, ..., 0=Nd
INSERT INTO "lift_schedule" (id, lift_id, day_of_week, opening_time, closing_time) VALUES
  -- Wyciag Glowny: pon-nd 08:00-16:30
  (1,  1, 1, '08:00', '16:30'), (2,  1, 2, '08:00', '16:30'), (3,  1, 3, '08:00', '16:30'),
  (4,  1, 4, '08:00', '16:30'), (5,  1, 5, '08:00', '16:30'), (6,  1, 6, '08:00', '17:00'),
  (7,  1, 0, '08:00', '17:00'),
  -- Ekspres Orlica: wt-sob 09:00-15:00
  (8,  2, 2, '09:00', '15:00'), (9,  2, 3, '09:00', '15:00'), (10, 2, 4, '09:00', '15:00'),
  (11, 2, 5, '09:00', '15:00'), (12, 2, 6, '09:00', '15:30'),
  -- Wyciag Dolny: pon-nd 08:00-17:00
  (13, 3, 1, '08:00', '17:00'), (14, 3, 2, '08:00', '17:00'), (15, 3, 3, '08:00', '17:00'),
  (16, 3, 4, '08:00', '17:00'), (17, 3, 5, '08:00', '17:00'), (18, 3, 6, '08:00', '17:30'),
  (19, 3, 0, '08:00', '17:30'),
  -- Orczyk: pon-nd 09:00-16:00
  (20, 4, 1, '09:00', '16:00'), (21, 4, 2, '09:00', '16:00'), (22, 4, 3, '09:00', '16:00'),
  (23, 4, 4, '09:00', '16:00'), (24, 4, 5, '09:00', '16:00'), (25, 4, 6, '09:00', '16:30'),
  (26, 4, 0, '09:00', '16:30'),
  -- Kolej Linowa: sr-nd 10:00-14:00
  (27, 5, 3, '10:00', '14:00'), (28, 5, 4, '10:00', '14:00'), (29, 5, 5, '10:00', '14:00'),
  (30, 5, 6, '10:00', '15:00'), (31, 5, 0, '10:00', '15:00'),
  -- Gondola: pon-nd 08:30-16:00
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

INSERT INTO "tariff" (id, name, season_id, pass_type_id, price, ride_count, pool_limit) VALUES
  -- Karnety punktowe
  (1, 'Karnet 10 zjazdow Dorosly',  1, 1,  89.00, 10, NULL),
  (2, 'Karnet 10 zjazdow Dziecko',  1, 1,  59.00, 10, NULL),
  (3, 'Karnet 10 zjazdow Senior',   1, 1,  69.00, 10, NULL),
  (4, 'Karnet 20 zjazdow Dorosly',  1, 1, 159.00, 20, NULL),
  (5, 'Karnet 20 zjazdow Dziecko',  1, 1, 109.00, 20, NULL),
  (6, 'Karnet 20 zjazdow Senior',   1, 1, 129.00, 20, NULL),
  (7, 'Karnet 40 zjazdow Dorosly',  1, 1, 289.00, 40, NULL),
  (8, 'Karnet 40 zjazdow Dziecko',  1, 1, 199.00, 40, NULL),
  (9, 'Karnet 40 zjazdow Senior',   1, 1, 239.00, 40, NULL),
  -- Karnety czasowe
  (10, 'Karnet 1-dniowy Dorosly',   1, 2, 119.00, NULL, NULL),
  (11, 'Karnet 1-dniowy Dziecko',   1, 2,  79.00, NULL, NULL),
  (12, 'Karnet 1-dniowy Senior',    1, 2,  99.00, NULL, NULL),
  (13, 'Karnet 3-dniowy Dorosly',   1, 2, 299.00, NULL, NULL),
  (14, 'Karnet 3-dniowy Dziecko',   1, 2, 199.00, NULL, NULL),
  (15, 'Karnet 3-dniowy Senior',    1, 2, 249.00, NULL, NULL),
  (16, 'Karnet 5-dniowy Dorosly',   1, 2, 459.00, NULL, NULL),
  (17, 'Karnet 5-dniowy Dziecko',   1, 2, 319.00, NULL, NULL),
  (18, 'Karnet 5-dniowy Senior',    1, 2, 369.00, NULL, NULL),
  (19, 'Karnet 7-dniowy Dorosly',   1, 2, 589.00, NULL, NULL),
  (20, 'Karnet 7-dniowy Dziecko',   1, 2, 419.00, NULL, NULL),
  (21, 'Karnet 7-dniowy Senior',    1, 2, 489.00, NULL, NULL);

SELECT setval(pg_get_serial_sequence('"tariff"', 'id'), 40);

-- ========== KARTY RFID ==========
-- Wszystkie karty startuja ze statusem "wolna" (1) - bez przypisanych karnetow.

INSERT INTO "card" (id, status_id, user_id, deposit_paid, block_reason, physical_condition, added_to_pool_at) VALUES
  ('A3:F2:11:CC', 1, 4, true, NULL, 'dobry', NOW() - INTERVAL '30 days'),
  ('B7:AA:32:0E', 1, NULL, false, NULL, 'dobry', NOW() - INTERVAL '20 days'),
  ('C1:DE:84:57', 1, NULL, false, NULL, 'dobry', NOW() - INTERVAL '10 days'),
  ('E9:CC:01:A3', 1, NULL, false, NULL, 'dobry', NOW() - INTERVAL '5 days'),
  ('F0:11:22:33', 1, NULL, false, NULL, 'dobry', NOW() - INTERVAL '3 days'),
  ('AA:BB:CC:DD', 1, NULL, false, NULL, 'dobry', NOW() - INTERVAL '1 day'),
  ('11:22:33:44', 1, NULL, false, NULL, 'nowy',  NOW()),
  ('55:66:77:88', 1, NULL, false, NULL, 'nowy',  NOW());

-- ========== REZERWACJE, KARNETY I TRANSAKCJE ==========
-- Dane obejmuja aktywne i wygasle karnety, rezerwacje online oraz przypadki testowe dla bramek.

INSERT INTO "reservation" (id, reservation_number, user_id, reservation_date, status_id) VALUES
  (1, 'RES-20260430120000000', 4, NOW() - INTERVAL '3 days', 1),
  (2, 'RES-20250115090000000', 4, NOW() - INTERVAL '110 days', 1),
  (3, 'ONL-20260523090000000', 4, NOW(), 3),
  (4, 'RES-20260608090000000', 1, NOW() - INTERVAL '1 day', 1),
  (5, 'RES-20260608100000000', 2, NOW() - INTERVAL '2 hours', 1),
  (6, 'RES-20260608110000000', 3, NOW(), 3),
  (7, 'RES-20260608120000000', 4, NOW() - INTERVAL '4 days', 1);

INSERT INTO "ski_pass" (id, card_id, tariff_id, reservation_id, status_id, valid_from, valid_to, initial_rides, remaining_rides) VALUES
  (1, 'A3:F2:11:CC', 16, 1, 1, NOW() - INTERVAL '2 days', NOW() + INTERVAL '3 days', NULL, NULL),
  (2, 'B7:AA:32:0E', 13, 2, 5, NOW() - INTERVAL '107 days', NOW() - INTERVAL '104 days', NULL, NULL),
  (3, NULL, 13, 3, 6, NOW(), NOW() + INTERVAL '3 days', NULL, NULL),
  -- Karty testowe dla bramki:
  -- C1:DE:84:57 ma aktywny karnet punktowy z przejazdami i powinna zostac przepuszczona.
  -- E9:CC:01:A3 ma aktywny karnet czasowy i powinna zostac przepuszczona.
  -- F0:11:22:33 ma karnet aktywny, ale dopiero od jutra, wiec bramka powinna go odrzucic.
  -- AA:BB:CC:DD ma karnet zablokowany, wiec bramka powinna go odrzucic.
  (4, 'C1:DE:84:57', 1, 4, 1, NOW() - INTERVAL '1 day', NOW() + INTERVAL '14 days', 10, 10),
  (5, 'E9:CC:01:A3', 10, 5, 1, NOW() - INTERVAL '2 hours', NOW() + INTERVAL '22 hours', NULL, NULL),
  (6, 'F0:11:22:33', 12, 6, 1, NOW() + INTERVAL '1 day', NOW() + INTERVAL '2 days', NULL, NULL),
  (7, 'AA:BB:CC:DD', 4, 7, 2, NOW() - INTERVAL '3 days', NOW() + INTERVAL '4 days', 20, 20);

UPDATE "card" SET status_id = 2 WHERE id IN ('A3:F2:11:CC', 'C1:DE:84:57', 'E9:CC:01:A3', 'F0:11:22:33', 'AA:BB:CC:DD');

INSERT INTO "transaction" (id, reservation_id, cashier_id, operation_type_id, amount, transaction_date) VALUES
  (1, 1, 1, 2, 459.00, NOW() - INTERVAL '3 days'),
  (2, 2, 1, 2, 299.00, NOW() - INTERVAL '110 days'),
  -- Dane pod raport sprzedazy ogolnej: kasy, online, zwroty i kaucje.
  (3, 1, 1,    1, 119.00, date_trunc('day', NOW()) + INTERVAL '08 hours 15 minutes'),
  (4, 1, 1,    2, 459.00, date_trunc('day', NOW()) + INTERVAL '09 hours 05 minutes'),
  (5, 1, 2,    1,  79.00, date_trunc('day', NOW()) + INTERVAL '10 hours 40 minutes'),
  (6, 1, NULL, 2, 299.00, date_trunc('day', NOW()) + INTERVAL '11 hours 20 minutes'),
  (7, 1, NULL, 2, 199.00, date_trunc('day', NOW()) - INTERVAL '1 day' + INTERVAL '14 hours 00 minutes'),
  (8, 1, 1,    3, -89.00, date_trunc('day', NOW()) + INTERVAL '12 hours 10 minutes'),
  (9, 1, 1,    4, -20.00, date_trunc('day', NOW()) + INTERVAL '12 hours 25 minutes'),
  (10, 1, 2,   2, 159.00, date_trunc('day', NOW()) - INTERVAL '2 days' + INTERVAL '15 hours 30 minutes');

-- Kilka skanow bramki dla aktywnego karnetu (historia przejazdow)
INSERT INTO "gate_scan" (id, card_id, gate_id, pass_type_id, scan_time, verification_result_id) VALUES
  (1, 'A3:F2:11:CC', 1, 2, NOW() - INTERVAL '2 days 7 hours',  1),
  (2, 'A3:F2:11:CC', 1, 2, NOW() - INTERVAL '2 days 5 hours',  1),
  (3, 'A3:F2:11:CC', 3, 2, NOW() - INTERVAL '2 days 3 hours',  1),
  (4, 'A3:F2:11:CC', 2, 2, NOW() - INTERVAL '1 day 8 hours',   1),
  (5, 'A3:F2:11:CC', 3, 2, NOW() - INTERVAL '1 day 6 hours',   1),
  (6, 'A3:F2:11:CC', 1, 2, NOW() - INTERVAL '1 day 4 hours',   1),
  (7, 'A3:F2:11:CC', 4, 2, NOW() - INTERVAL '3 hours',         1),
  (8, 'A3:F2:11:CC', 1, 2, NOW() - INTERVAL '1 hour 30 minutes', 1),
  -- Dane pod raport przepustowosci: dzisiejszy ruch na kilku wyciagach i godzinach.
  (9,  'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '08 hours 05 minutes', 1),
  (10, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '08 hours 25 minutes', 1),
  (11, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 10 minutes', 1),
  (12, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 35 minutes', 1),
  (13, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 50 minutes', 1),
  (14, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '10 hours 15 minutes', 1),
  (15, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '10 hours 45 minutes', 1),
  (16, 'A3:F2:11:CC', 4, 2, date_trunc('day', NOW()) + INTERVAL '11 hours 05 minutes', 1),
  (17, 'A3:F2:11:CC', 4, 2, date_trunc('day', NOW()) + INTERVAL '11 hours 25 minutes', 1),
  (18, 'A3:F2:11:CC', 5, 2, date_trunc('day', NOW()) + INTERVAL '12 hours 00 minutes', 1),
  (19, 'A3:F2:11:CC', 6, 2, date_trunc('day', NOW()) + INTERVAL '13 hours 10 minutes', 1),
  (20, 'A3:F2:11:CC', 6, 2, date_trunc('day', NOW()) + INTERVAL '13 hours 40 minutes', 1),
  (21, 'B7:AA:32:0E', 1, 2, date_trunc('day', NOW()) + INTERVAL '14 hours 05 minutes', 2),
  (22, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '15 hours 20 minutes', 1),
  -- Dodatkowa probka do wykresu przepustowosci: wyrazny szczyt poranny na glownych wyciagach.
  (23, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '08 hours 40 minutes', 1),
  (24, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '08 hours 55 minutes', 1),
  (25, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 05 minutes', 1),
  (26, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 20 minutes', 1),
  (27, 'A3:F2:11:CC', 1, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 45 minutes', 1),
  (28, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 15 minutes', 1),
  (29, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 30 minutes', 1),
  (30, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '09 hours 55 minutes', 1),
  (31, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '10 hours 05 minutes', 1),
  (32, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '10 hours 25 minutes', 1),
  (33, 'A3:F2:11:CC', 3, 2, date_trunc('day', NOW()) + INTERVAL '10 hours 55 minutes', 1),
  (34, 'A3:F2:11:CC', 4, 2, date_trunc('day', NOW()) + INTERVAL '11 hours 45 minutes', 1),
  (35, 'A3:F2:11:CC', 4, 2, date_trunc('day', NOW()) + INTERVAL '12 hours 15 minutes', 1),
  (36, 'A3:F2:11:CC', 5, 2, date_trunc('day', NOW()) + INTERVAL '12 hours 35 minutes', 1),
  (37, 'A3:F2:11:CC', 5, 2, date_trunc('day', NOW()) + INTERVAL '13 hours 05 minutes', 1),
  (38, 'A3:F2:11:CC', 6, 2, date_trunc('day', NOW()) + INTERVAL '14 hours 30 minutes', 1),
  (39, 'A3:F2:11:CC', 6, 2, date_trunc('day', NOW()) + INTERVAL '15 hours 05 minutes', 1),
  (40, 'A3:F2:11:CC', 2, 2, date_trunc('day', NOW()) + INTERVAL '16 hours 10 minutes', 1);

SELECT setval(pg_get_serial_sequence('"reservation"', 'id'), 100);
SELECT setval(pg_get_serial_sequence('"ski_pass"',    'id'), 100);
SELECT setval(pg_get_serial_sequence('"transaction"', 'id'), 100);
SELECT setval(pg_get_serial_sequence('"gate_scan"',   'id'), 100);
