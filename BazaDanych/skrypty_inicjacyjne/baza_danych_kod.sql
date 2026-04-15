CREATE TABLE "dict_card_status" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_pass_status" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_pass_type" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_operation_type" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_season" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_trail_difficulty" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_reservation_status" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_verification_result" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL
);

CREATE TABLE "dict_report_type" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL,
  "description" text
);

CREATE TABLE "user" (
  "id" integer PRIMARY KEY,
  "email" varchar UNIQUE,
  "password_hash" varchar,
  "created_at" timestamp
);

CREATE TABLE "cashier" (
  "id" integer PRIMARY KEY,
  "login" varchar UNIQUE NOT NULL,
  "password_hash" varchar NOT NULL,
  "is_active" boolean
);

CREATE TABLE "administrator" (
  "id" integer PRIMARY KEY,
  "login" varchar UNIQUE NOT NULL,
  "password_hash" varchar NOT NULL,
  "is_active" boolean
);

CREATE TABLE "trail_planner" (
  "id" integer PRIMARY KEY,
  "login" varchar UNIQUE NOT NULL,
  "password_hash" varchar NOT NULL,
  "is_active" boolean
);

CREATE TABLE "lift" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL,
  "location" varchar,
  "length" decimal,
  "planner_id" integer
);

CREATE TABLE "trail" (
  "id" integer PRIMARY KEY,
  "name" varchar UNIQUE NOT NULL,
  "location" varchar,
  "length" decimal,
  "difficulty_id" integer,
  "planner_id" integer
);

CREATE TABLE "lift_trail" (
  "lift_id" integer,
  "trail_id" integer,
  PRIMARY KEY ("lift_id", "trail_id")
);

CREATE TABLE "lift_schedule" (
  "id" integer PRIMARY KEY,
  "lift_id" integer,
  "day_of_week" integer,
  "opening_time" time,
  "closing_time" time
);

CREATE TABLE "trail_schedule" (
  "id" integer PRIMARY KEY,
  "trail_id" integer,
  "is_open" boolean,
  "closure_reason" varchar,
  "updated_at" timestamp
);

CREATE TABLE "card" (
  "id" varchar PRIMARY KEY,
  "status_id" integer,
  "physical_condition" varchar,
  "added_to_pool_at" timestamp
);

CREATE TABLE "tariff" (
  "id" integer PRIMARY KEY,
  "name" varchar NOT NULL,
  "season_id" integer,
  "pass_type_id" integer,
  "price" decimal,
  "pool_limit" integer
);

CREATE TABLE "ski_pass" (
  "id" integer PRIMARY KEY,
  "card_id" varchar,
  "tariff_id" integer,
  "reservation_id" integer,
  "status_id" integer,
  "valid_from" timestamp,
  "valid_to" timestamp,
  "block_reason" text
);

CREATE TABLE "reservation" (
  "id" integer PRIMARY KEY,
  "reservation_number" varchar UNIQUE NOT NULL,
  "user_id" integer,
  "reservation_date" timestamp,
  "status_id" integer
);

CREATE TABLE "transaction" (
  "id" integer PRIMARY KEY,
  "reservation_id" integer,
  "cashier_id" integer,
  "operation_type_id" integer,
  "amount" decimal NOT NULL,
  "transaction_date" timestamp
);

CREATE TABLE "gate" (
  "id" integer PRIMARY KEY,
  "lift_id" integer,
  "name" varchar,
  "is_active" boolean
);

CREATE TABLE "gate_scan" (
  "id" integer PRIMARY KEY,
  "card_id" varchar,
  "gate_id" integer,
  "scan_time" timestamp,
  "time_blocked_until" timestamp,
  "verification_result_id" integer
);

CREATE TABLE "shift_report" (
  "id" integer PRIMARY KEY,
  "cashier_id" integer,
  "start_time" timestamp,
  "end_time" timestamp,
  "total_revenue" decimal,
  "total_deposit_returns" decimal,
  "cards_issued_count" integer
);

CREATE TABLE "admin_report" (
  "id" integer PRIMARY KEY,
  "admin_id" integer,
  "report_type_id" integer,
  "generated_at" timestamp,
  "report_parameters" text
);

ALTER TABLE "card" ADD FOREIGN KEY ("status_id") REFERENCES "dict_card_status" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "tariff" ADD FOREIGN KEY ("season_id") REFERENCES "dict_season" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "tariff" ADD FOREIGN KEY ("pass_type_id") REFERENCES "dict_pass_type" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "reservation" ADD FOREIGN KEY ("status_id") REFERENCES "dict_reservation_status" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "transaction" ADD FOREIGN KEY ("operation_type_id") REFERENCES "dict_operation_type" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ski_pass" ADD FOREIGN KEY ("status_id") REFERENCES "dict_pass_status" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "trail" ADD FOREIGN KEY ("difficulty_id") REFERENCES "dict_trail_difficulty" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "gate_scan" ADD FOREIGN KEY ("verification_result_id") REFERENCES "dict_verification_result" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "reservation" ADD FOREIGN KEY ("user_id") REFERENCES "user" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "transaction" ADD FOREIGN KEY ("reservation_id") REFERENCES "reservation" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "transaction" ADD FOREIGN KEY ("cashier_id") REFERENCES "cashier" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ski_pass" ADD FOREIGN KEY ("card_id") REFERENCES "card" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ski_pass" ADD FOREIGN KEY ("tariff_id") REFERENCES "tariff" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "ski_pass" ADD FOREIGN KEY ("reservation_id") REFERENCES "reservation" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "shift_report" ADD FOREIGN KEY ("cashier_id") REFERENCES "cashier" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "trail" ADD FOREIGN KEY ("planner_id") REFERENCES "trail_planner" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "lift" ADD FOREIGN KEY ("planner_id") REFERENCES "trail_planner" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "lift_trail" ADD FOREIGN KEY ("lift_id") REFERENCES "lift" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "lift_trail" ADD FOREIGN KEY ("trail_id") REFERENCES "trail" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "lift_schedule" ADD FOREIGN KEY ("lift_id") REFERENCES "lift" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "trail_schedule" ADD FOREIGN KEY ("trail_id") REFERENCES "trail" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "gate" ADD FOREIGN KEY ("lift_id") REFERENCES "lift" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "gate_scan" ADD FOREIGN KEY ("card_id") REFERENCES "card" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "gate_scan" ADD FOREIGN KEY ("gate_id") REFERENCES "gate" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "admin_report" ADD FOREIGN KEY ("admin_id") REFERENCES "administrator" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "admin_report" ADD FOREIGN KEY ("report_type_id") REFERENCES "dict_report_type" ("id") DEFERRABLE INITIALLY IMMEDIATE;
