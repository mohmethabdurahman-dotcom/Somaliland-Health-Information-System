-- Run on existing Blood Bank schema (after BloodBank_CreateTables.sql).
-- Safe to re-run: uses IF NOT EXISTS.

BEGIN;

ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS status character varying(20) NOT NULL DEFAULT 'Registered';
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS screening_result character varying(20) NOT NULL DEFAULT 'Pending';
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS screening_notes character varying(500) NULL;

ALTER TABLE blood_bank_screening ADD COLUMN IF NOT EXISTS donor_id bigint NULL;
ALTER TABLE blood_bank_screening ADD COLUMN IF NOT EXISTS notes character varying(500) NULL;
ALTER TABLE blood_bank_screening ALTER COLUMN unit_id DROP NOT NULL;

ALTER TABLE blood_bank_blood_unit ADD COLUMN IF NOT EXISTS volume_ml integer NULL;
ALTER TABLE blood_bank_blood_unit ADD COLUMN IF NOT EXISTS storage_location character varying(100) NULL;

COMMIT;
