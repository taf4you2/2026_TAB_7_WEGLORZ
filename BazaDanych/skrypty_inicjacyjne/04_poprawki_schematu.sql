-- Poprawki kompatybilnosci dla baz utworzonych przed rozszerzeniem modeli EF.
-- Skrypt jest idempotentny: mozna uruchamiac go wielokrotnie.

ALTER TABLE "card"
    ADD COLUMN IF NOT EXISTS "deposit_paid" boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS "block_reason" text,
    ADD COLUMN IF NOT EXISTS "physical_condition" varchar,
    ADD COLUMN IF NOT EXISTS "added_to_pool_at" timestamp;

ALTER TABLE "gate_scan"
    ADD COLUMN IF NOT EXISTS "pass_type_id" integer;

ALTER TABLE "tariff"
    ADD COLUMN IF NOT EXISTS "is_active" boolean DEFAULT true;

ALTER TABLE "lift"
    ADD COLUMN IF NOT EXISTS "is_active" boolean DEFAULT true;

ALTER TABLE "trail"
    ADD COLUMN IF NOT EXISTS "is_active" boolean DEFAULT true;

UPDATE "tariff" SET "is_active" = true WHERE "is_active" IS NULL;
UPDATE "lift" SET "is_active" = true WHERE "is_active" IS NULL;
UPDATE "trail" SET "is_active" = true WHERE "is_active" IS NULL;
