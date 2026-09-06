using System;
using System.Linq;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    public static class BloodBankProjectSeeder
    {
        /// <summary>
        /// Blood Bank User Auth Setting roles — Admin (full) and Staff (all except Staff Audit).
        /// </summary>
        public static void EnsureBloodBankProjects(KMUContext context)
        {
            var projects = new[]
            {
                (BloodBankRoles.Admin, "Blood Bank Admin"),
                (BloodBankRoles.Staff, "Blood Bank Staff")
            };

            var changed = false;
            foreach (var (id, name) in projects)
            {
                var existing = context.KmuProjects.FirstOrDefault(p => p.ProjectId == id);
                if (existing == null)
                {
                    context.KmuProjects.Add(new KmuProject
                    {
                        ProjectId = id,
                        ProjectName = name,
                        Url = "/BloodBank/Home",
                        Creator = "SYSTEM",
                        CreateTime = DateTime.Now
                    });
                    changed = true;
                }
                else if (!string.Equals(existing.ProjectName, name, StringComparison.Ordinal))
                {
                    existing.ProjectName = name;
                    existing.Url = "/BloodBank/Home";
                    context.KmuProjects.Update(existing);
                    changed = true;
                }
            }

            // Migrate retired project ids → Admin / Staff, then remove old project rows.
            changed |= MigrateProjectAuths(context, "BloodBank", BloodBankRoles.Staff);
            changed |= MigrateProjectAuths(context, BloodBankRoles.LegacyReception, BloodBankRoles.Staff);
            changed |= MigrateProjectAuths(context, BloodBankRoles.LegacyLab, BloodBankRoles.Admin);

            if (changed)
                context.SaveChanges();
        }

        private static bool MigrateProjectAuths(KMUContext context, string fromProjectId, string toProjectId)
        {
            var project = context.KmuProjects.FirstOrDefault(p => p.ProjectId == fromProjectId);
            var auths = context.KmuAuths.Where(a => a.ProjectId == fromProjectId).ToList();
            if (project == null && auths.Count == 0)
                return false;

            var now = DateTime.Now;
            foreach (var auth in auths)
            {
                var hasTarget = context.KmuAuths.Any(a =>
                    a.UserIdno == auth.UserIdno && a.ProjectId == toProjectId);
                if (!hasTarget)
                {
                    context.KmuAuths.Add(new KmuAuth
                    {
                        UserIdno = auth.UserIdno,
                        ProjectId = toProjectId,
                        Creator = "SYSTEM",
                        CreateTime = now
                    });
                }
                context.KmuAuths.Remove(auth);
            }

            if (project != null)
                context.KmuProjects.Remove(project);

            return true;
        }

        /// <summary>
        /// Ensures the built-in admin account has Blood Bank Admin.
        /// Other users are assigned Admin / Staff only via User Auth Setting.
        /// </summary>
        public static void EnsureBloodBankAuths(KMUContext context)
        {
            const string adminId = "admin";
            if (!context.KmuUsers.Any(u => u.UserIdno == adminId))
                return;

            if (context.KmuAuths.Any(a =>
                    a.UserIdno == adminId
                    && (a.ProjectId == BloodBankRoles.Admin || a.ProjectId == BloodBankRoles.Staff)))
                return;

            context.KmuAuths.Add(new KmuAuth
            {
                UserIdno = adminId,
                ProjectId = BloodBankRoles.Admin,
                Creator = "SYSTEM",
                CreateTime = DateTime.Now
            });
            context.SaveChanges();
        }

        /// <summary>Creates Blood Bank tables when they are missing, then adds later columns.</summary>
        public static void EnsureBloodBankTables(KMUContext context)
        {
            context.Database.ExecuteSqlRaw(@"
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
    modify_date         TIMESTAMP WITHOUT TIME ZONE NULL
);
CREATE TABLE IF NOT EXISTS blood_bank_screening (
    screening_id        BIGSERIAL PRIMARY KEY,
    unit_id             BIGINT NULL,
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
    create_date         TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
CREATE TABLE IF NOT EXISTS blood_bank_dispense (
    dispense_id             BIGSERIAL PRIMARY KEY,
    unit_id                 BIGINT NOT NULL,
    request_id              BIGINT NULL,
    patient_id              VARCHAR(10) NOT NULL,
    inhospid                VARCHAR(17) NOT NULL,
    patient_name            VARCHAR(200) NOT NULL,
    collector_name          VARCHAR(100) NOT NULL,
    ward_destination        VARCHAR(500) NOT NULL,
    units_released          INTEGER NOT NULL,
    serial_numbers          VARCHAR(500) NOT NULL,
    dispense_date_time      TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    dispensing_staff_id     VARCHAR(7) NOT NULL,
    dispensing_staff_name   VARCHAR(100) NOT NULL,
    create_user             VARCHAR(7) NOT NULL,
    create_date             TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
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
");
        }

        /// <summary>Ensures donor extended columns exist (registration: middle name, age, job, malaria).</summary>
        public static void EnsureBloodBankDonorSchema(KMUContext context)
        {
            context.Database.ExecuteSqlRaw(@"
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS middle_name character varying(50) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS age integer NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS job_description character varying(100) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS pre_donation_malaria_result character varying(10) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS national_id character varying(100) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS email character varying(255) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS address character varying(500) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS donor_notes character varying(500) NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS last_donation_date timestamp without time zone NULL;
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS status character varying(20);
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS screening_result character varying(20);
ALTER TABLE blood_bank_donor ADD COLUMN IF NOT EXISTS screening_notes character varying(500) NULL;
ALTER TABLE blood_bank_screening ADD COLUMN IF NOT EXISTS donor_id bigint NULL;
ALTER TABLE blood_bank_screening ADD COLUMN IF NOT EXISTS notes character varying(500) NULL;
ALTER TABLE blood_bank_blood_unit ADD COLUMN IF NOT EXISTS volume_ml integer NULL;
ALTER TABLE blood_bank_blood_unit ADD COLUMN IF NOT EXISTS storage_location character varying(100) NULL;
ALTER TABLE blood_bank_dispense ADD COLUMN IF NOT EXISTS source_donor_id bigint NULL;
ALTER TABLE blood_bank_dispense ADD COLUMN IF NOT EXISTS donation_source character varying(100) NULL;
UPDATE blood_bank_donor SET status = 'Registered' WHERE status IS NULL;
UPDATE blood_bank_donor SET screening_result = 'Pending' WHERE screening_result IS NULL;
");
        }
    }
}
