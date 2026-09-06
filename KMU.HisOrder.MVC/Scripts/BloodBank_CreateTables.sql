-- Blood Bank module tables for KMU.HisOrder.MVC (PostgreSQL)
-- Run against the same database your app uses (see appsettings ConnectionStrings).
-- Safe to re-run: uses IF NOT EXISTS where supported.

BEGIN;

-- 1. HIS blood requests (from doctor orders)
CREATE TABLE IF NOT EXISTS blood_bank_request (
    request_id          BIGSERIAL PRIMARY KEY,
    orderplanid         BIGINT NULL,
    patient_id          VARCHAR(10) NOT NULL,
    inhospid            VARCHAR(17) NOT NULL,
    patient_name        VARCHAR(200) NOT NULL,
    ward                VARCHAR(50) NULL,
    bed_location        VARCHAR(50) NULL,
    requesting_doctor_id    VARCHAR(7) NOT NULL,
    requesting_doctor_name  VARCHAR(100) NOT NULL,
    blood_type          VARCHAR(5) NOT NULL,
    component_type      VARCHAR(50) NOT NULL,
    units_requested     INTEGER NOT NULL,
    urgency_level       VARCHAR(20) NOT NULL,
    patient_type        VARCHAR(10) NOT NULL,
    status              VARCHAR(20) NOT NULL,
    request_date_time   TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    reject_reason       VARCHAR(500) NULL,
    create_user         VARCHAR(7) NOT NULL,
    create_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    modify_user         VARCHAR(7) NULL,
    modify_date         TIMESTAMP WITHOUT TIME ZONE NULL
);

-- 2. Donors
CREATE TABLE IF NOT EXISTS blood_bank_donor (
    donor_id        BIGSERIAL PRIMARY KEY,
    first_name      VARCHAR(50) NOT NULL,
    last_name       VARCHAR(50) NOT NULL,
    gender          VARCHAR(10) NOT NULL,
    phone           VARCHAR(20) NOT NULL,
    blood_type      VARCHAR(5) NOT NULL,
    donation_type   VARCHAR(20) NOT NULL,
    patient_id      VARCHAR(10) NULL,
    inhospid        VARCHAR(17) NULL,
    request_id      BIGINT NULL,
    create_user     VARCHAR(7) NOT NULL,
    create_date     TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    modify_user     VARCHAR(7) NULL,
    modify_date     TIMESTAMP WITHOUT TIME ZONE NULL
);

-- 3. Blood units (inventory)
CREATE TABLE IF NOT EXISTS blood_bank_blood_unit (
    unit_id             BIGSERIAL PRIMARY KEY,
    serial_number       VARCHAR(30) NOT NULL,
    donor_id            BIGINT NOT NULL,
    blood_type          VARCHAR(5) NOT NULL,
    component_type      VARCHAR(50) NOT NULL,
    status              VARCHAR(20) NOT NULL,
    quantity            INTEGER NOT NULL,
    donation_date       TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    expiry_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    screening_result    VARCHAR(20) NOT NULL,
    patient_id          VARCHAR(10) NULL,
    request_id          BIGINT NULL,
    create_user         VARCHAR(7) NOT NULL,
    create_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    modify_user         VARCHAR(7) NULL,
    modify_date         TIMESTAMP WITHOUT TIME ZONE NULL,
    CONSTRAINT "FK_blood_bank_blood_unit_blood_bank_donor_donor_id"
        FOREIGN KEY (donor_id) REFERENCES blood_bank_donor (donor_id) ON DELETE CASCADE,
    CONSTRAINT "FK_blood_bank_blood_unit_blood_bank_request_request_id"
        FOREIGN KEY (request_id) REFERENCES blood_bank_request (request_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_blood_bank_blood_unit_serial_number"
    ON blood_bank_blood_unit (serial_number);
CREATE INDEX IF NOT EXISTS "IX_blood_bank_blood_unit_donor_id"
    ON blood_bank_blood_unit (donor_id);
CREATE INDEX IF NOT EXISTS "IX_blood_bank_blood_unit_request_id"
    ON blood_bank_blood_unit (request_id);

-- 4. Screening
CREATE TABLE IF NOT EXISTS blood_bank_screening (
    screening_id        BIGSERIAL PRIMARY KEY,
    unit_id             BIGINT NOT NULL,
    staff_user_id       VARCHAR(7) NOT NULL,
    staff_name          VARCHAR(100) NOT NULL,
    screening_date_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    hiv_result          VARCHAR(20) NOT NULL,
    hep_b_result        VARCHAR(20) NOT NULL,
    hep_c_result        VARCHAR(20) NOT NULL,
    syphilis_result     VARCHAR(20) NOT NULL,
    malaria_result      VARCHAR(20) NOT NULL,
    overall_result      VARCHAR(20) NOT NULL,
    create_user         VARCHAR(7) NOT NULL,
    create_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    CONSTRAINT "FK_blood_bank_screening_blood_bank_blood_unit_unit_id"
        FOREIGN KEY (unit_id) REFERENCES blood_bank_blood_unit (unit_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_blood_bank_screening_unit_id"
    ON blood_bank_screening (unit_id);

-- 5. Dispense
CREATE TABLE IF NOT EXISTS blood_bank_dispense (
    dispense_id             BIGSERIAL PRIMARY KEY,
    unit_id                 BIGINT NOT NULL,
    request_id              BIGINT NULL,
    patient_id              VARCHAR(10) NOT NULL,
    inhospid                VARCHAR(17) NOT NULL,
    patient_name            VARCHAR(200) NOT NULL,
    collector_name          VARCHAR(100) NOT NULL,
    ward_destination        VARCHAR(100) NOT NULL,
    units_released          INTEGER NOT NULL,
    serial_numbers          VARCHAR(500) NOT NULL,
    dispense_date_time      TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    dispensing_staff_id     VARCHAR(7) NOT NULL,
    dispensing_staff_name   VARCHAR(100) NOT NULL,
    create_user             VARCHAR(7) NOT NULL,
    create_date             TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    CONSTRAINT "FK_blood_bank_dispense_blood_bank_blood_unit_unit_id"
        FOREIGN KEY (unit_id) REFERENCES blood_bank_blood_unit (unit_id) ON DELETE CASCADE,
    CONSTRAINT "FK_blood_bank_dispense_blood_bank_request_request_id"
        FOREIGN KEY (request_id) REFERENCES blood_bank_request (request_id)
);

CREATE INDEX IF NOT EXISTS "IX_blood_bank_dispense_unit_id"
    ON blood_bank_dispense (unit_id);
CREATE INDEX IF NOT EXISTS "IX_blood_bank_dispense_request_id"
    ON blood_bank_dispense (request_id);

-- 6. Patient timeline events (HIS history integration)
CREATE TABLE IF NOT EXISTS blood_bank_patient_event (
    event_id            BIGSERIAL PRIMARY KEY,
    patient_id          VARCHAR(10) NOT NULL,
    inhospid            VARCHAR(17) NOT NULL,
    event_type          VARCHAR(50) NOT NULL,
    event_description   TEXT NOT NULL,
    event_date_time     TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    create_user         VARCHAR(7) NOT NULL,
    create_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

-- 7. RBAC menu entries (User Auth Setting)
INSERT INTO kmu_projects (project_id, project_name, url, creator, create_time)
SELECT 'BloodBank_Admin', 'Blood Bank Admin', '/BloodBank/Home', 'SYSTEM', NOW()
WHERE NOT EXISTS (SELECT 1 FROM kmu_projects WHERE project_id = 'BloodBank_Admin');

INSERT INTO kmu_projects (project_id, project_name, url, creator, create_time)
SELECT 'BloodBank_Staff', 'Blood Bank Staff', '/BloodBank/Home', 'SYSTEM', NOW()
WHERE NOT EXISTS (SELECT 1 FROM kmu_projects WHERE project_id = 'BloodBank_Staff');

COMMIT;

-- Verify:
-- SELECT table_name FROM information_schema.tables WHERE table_name LIKE 'blood_bank%' ORDER BY 1;
