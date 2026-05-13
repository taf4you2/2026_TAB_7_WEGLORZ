-- Dane słownikowe systemu sprzedaży biletów narciarskich.
-- Uruchamiany po 01_schema.sql i 02_sekwencje.sql, przed danymi testowymi.
-- Te wartości są wymagane przez aplikację i nie powinny być usuwane.

INSERT INTO "dict_card_status" (id, name) VALUES
  (1, 'wolna'),
  (2, 'zajeta'),
  (3, 'zastrzezony');

INSERT INTO "dict_pass_status" (id, name) VALUES
  (1, 'aktywny'),
  (2, 'zablokowany'),
  (3, 'zwrocony'),
  (4, 'oczekuje_na_zwrot'),
  (5, 'wygasly'),
  (6, 'oczekuje_na_odbior');

INSERT INTO "dict_pass_type" (id, name) VALUES
  (1, 'bilet_jednorazowy'),
  (2, 'karnet');

INSERT INTO "dict_operation_type" (id, name) VALUES
  (1, 'sprzedaz_biletu'),
  (2, 'sprzedaz_karnetu'),
  (3, 'zwrot_karnetu'),
  (4, 'kaucja'),
  (5, 'odbieranie_karnetu');

INSERT INTO "dict_season" (id, name) VALUES
  (1, 'Sezon Zimowy 2025/26');

INSERT INTO "dict_trail_difficulty" (id, name) VALUES
  (1, 'latwa'),
  (2, 'srednia'),
  (3, 'trudna'),
  (4, 'ekstremalna');

INSERT INTO "dict_reservation_status" (id, name) VALUES
  (1, 'potwierdzona'),
  (2, 'anulowana'),
  (3, 'oczekujaca');

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
