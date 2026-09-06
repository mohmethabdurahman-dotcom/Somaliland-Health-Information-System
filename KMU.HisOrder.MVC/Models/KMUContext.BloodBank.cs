using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KMUContext
    {
        public virtual DbSet<BloodBankDonor> BloodBankDonors { get; set; }
        public virtual DbSet<BloodBankRequest> BloodBankRequests { get; set; }
        public virtual DbSet<BloodBankBloodUnit> BloodBankBloodUnits { get; set; }
        public virtual DbSet<BloodBankScreeningRecord> BloodBankScreeningRecords { get; set; }
        public virtual DbSet<BloodBankDispenseRecord> BloodBankDispenseRecords { get; set; }
        public virtual DbSet<BloodBankPatientEvent> BloodBankPatientEvents { get; set; }
        private static void ConfigureBloodBankEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BloodBankDonor>(entity =>
            {
                entity.HasKey(e => e.DonorId);
                entity.ToTable("blood_bank_donor");
                entity.Property(e => e.DonorId).HasColumnName("donor_id");
                entity.Property(e => e.FirstName).HasMaxLength(50).HasColumnName("first_name");
                entity.Property(e => e.MiddleName).HasMaxLength(50).HasColumnName("middle_name");
                entity.Property(e => e.LastName).HasMaxLength(50).HasColumnName("last_name");
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.JobDescription).HasMaxLength(100).HasColumnName("job_description");
                entity.Property(e => e.PreDonationMalariaResult).HasMaxLength(10).HasColumnName("pre_donation_malaria_result");
                entity.Property(e => e.Gender).HasMaxLength(10).HasColumnName("gender");
                entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
                entity.Property(e => e.NationalId).HasMaxLength(100).HasColumnName("national_id");
                entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
                entity.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
                entity.Property(e => e.DonorNotes).HasMaxLength(500).HasColumnName("donor_notes");
                entity.Property(e => e.LastDonationDate).HasColumnType("timestamp without time zone").HasColumnName("last_donation_date");
                entity.HasIndex(e => e.NationalId).IsUnique().HasFilter("national_id IS NOT NULL AND national_id <> ''");
                entity.Property(e => e.BloodType).HasMaxLength(5).HasColumnName("blood_type");
                entity.Property(e => e.DonationType).HasMaxLength(20).HasColumnName("donation_type");
                entity.Property(e => e.PatientId).HasMaxLength(10).HasColumnName("patient_id");
                entity.Property(e => e.Inhospid).HasMaxLength(17).HasColumnName("inhospid");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status").HasDefaultValue(DonorStatuses.Registered);
                entity.Property(e => e.ScreeningResult).HasMaxLength(20).HasColumnName("screening_result").HasDefaultValue(ScreeningResults.Pending);
                entity.Property(e => e.ScreeningNotes).HasMaxLength(500).HasColumnName("screening_notes");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
                entity.Property(e => e.ModifyUser).HasMaxLength(7).HasColumnName("modify_user");
                entity.Property(e => e.ModifyDate).HasColumnType("timestamp without time zone").HasColumnName("modify_date");
                entity.HasOne(d => d.Request).WithMany().HasForeignKey(d => d.RequestId);
            });

            modelBuilder.Entity<BloodBankRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.ToTable("blood_bank_request");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.Orderplanid).HasColumnName("orderplanid");
                entity.Property(e => e.PatientId).HasMaxLength(10).HasColumnName("patient_id");
                entity.Property(e => e.Inhospid).HasMaxLength(17).HasColumnName("inhospid");
                entity.Property(e => e.PatientName).HasMaxLength(200).HasColumnName("patient_name");
                entity.Property(e => e.Ward).HasMaxLength(50).HasColumnName("ward");
                entity.Property(e => e.BedLocation).HasMaxLength(50).HasColumnName("bed_location");
                entity.Property(e => e.RequestingDoctorId).HasMaxLength(7).HasColumnName("requesting_doctor_id");
                entity.Property(e => e.RequestingDoctorName).HasMaxLength(100).HasColumnName("requesting_doctor_name");
                entity.Property(e => e.BloodType).HasMaxLength(5).HasColumnName("blood_type");
                entity.Property(e => e.ComponentType).HasMaxLength(50).HasColumnName("component_type");
                entity.Property(e => e.UnitsRequested).HasColumnName("units_requested");
                entity.Property(e => e.UrgencyLevel).HasMaxLength(20).HasColumnName("urgency_level");
                entity.Property(e => e.PatientType).HasMaxLength(10).HasColumnName("patient_type");
                entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
                entity.Property(e => e.RequestDateTime).HasColumnType("timestamp without time zone").HasColumnName("request_date_time");
                entity.Property(e => e.RejectReason).HasMaxLength(500).HasColumnName("reject_reason");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
                entity.Property(e => e.ModifyUser).HasMaxLength(7).HasColumnName("modify_user");
                entity.Property(e => e.ModifyDate).HasColumnType("timestamp without time zone").HasColumnName("modify_date");
            });

            modelBuilder.Entity<BloodBankBloodUnit>(entity =>
            {
                entity.HasKey(e => e.UnitId);
                entity.ToTable("blood_bank_blood_unit");
                entity.HasIndex(e => e.SerialNumber).IsUnique();
                entity.Property(e => e.UnitId).HasColumnName("unit_id");
                entity.Property(e => e.SerialNumber).HasMaxLength(30).HasColumnName("serial_number");
                entity.Property(e => e.DonorId).HasColumnName("donor_id");
                entity.Property(e => e.BloodType).HasMaxLength(5).HasColumnName("blood_type");
                entity.Property(e => e.ComponentType).HasMaxLength(50).HasColumnName("component_type");
                entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.VolumeMl).HasColumnName("volume_ml");
                entity.Property(e => e.StorageLocation).HasMaxLength(100).HasColumnName("storage_location");
                entity.Property(e => e.DonationDate).HasColumnType("timestamp without time zone").HasColumnName("donation_date");
                entity.Property(e => e.ExpiryDate).HasColumnType("timestamp without time zone").HasColumnName("expiry_date");
                entity.Property(e => e.ScreeningResult).HasMaxLength(20).HasColumnName("screening_result");
                entity.Property(e => e.PatientId).HasMaxLength(10).HasColumnName("patient_id");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
                entity.Property(e => e.ModifyUser).HasMaxLength(7).HasColumnName("modify_user");
                entity.Property(e => e.ModifyDate).HasColumnType("timestamp without time zone").HasColumnName("modify_date");
                entity.HasOne(u => u.Donor)
                    .WithMany()
                    .HasForeignKey(u => u.DonorId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_blood_bank_blood_unit_blood_bank_donor_donor_id");
                entity.HasOne(e => e.Request).WithMany().HasForeignKey(e => e.RequestId);
            });

            modelBuilder.Entity<BloodBankScreeningRecord>(entity =>
            {
                entity.HasKey(e => e.ScreeningId);
                entity.ToTable("blood_bank_screening");
                entity.Property(e => e.ScreeningId).HasColumnName("screening_id");
                entity.Property(e => e.DonorId).HasColumnName("donor_id");
                entity.Property(e => e.UnitId).HasColumnName("unit_id");
                entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
                entity.Property(e => e.StaffUserId).HasMaxLength(7).HasColumnName("staff_user_id");
                entity.Property(e => e.StaffName).HasMaxLength(100).HasColumnName("staff_name");
                entity.Property(e => e.ScreeningDateTime).HasColumnType("timestamp without time zone").HasColumnName("screening_date_time");
                entity.Property(e => e.HivResult).HasMaxLength(20).HasColumnName("hiv_result");
                entity.Property(e => e.HepBResult).HasMaxLength(20).HasColumnName("hep_b_result");
                entity.Property(e => e.HepCResult).HasMaxLength(20).HasColumnName("hep_c_result");
                entity.Property(e => e.SyphilisResult).HasMaxLength(20).HasColumnName("syphilis_result");
                entity.Property(e => e.MalariaResult).HasMaxLength(20).HasColumnName("malaria_result");
                entity.Property(e => e.OverallResult).HasMaxLength(20).HasColumnName("overall_result");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
                entity.HasOne(s => s.Donor)
                    .WithMany()
                    .HasForeignKey(s => s.DonorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasOne(s => s.Unit)
                    .WithMany()
                    .HasForeignKey(s => s.UnitId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_blood_bank_screening_blood_bank_blood_unit_unit_id")
                    .IsRequired(false);
            });

            modelBuilder.Entity<BloodBankDispenseRecord>(entity =>
            {
                entity.HasKey(e => e.DispenseId);
                entity.ToTable("blood_bank_dispense");
                entity.Property(e => e.DispenseId).HasColumnName("dispense_id");
                entity.Property(e => e.UnitId).HasColumnName("unit_id");
                entity.Property(e => e.SourceDonorId).HasColumnName("source_donor_id");
                entity.Property(e => e.DonationSource).HasMaxLength(100).HasColumnName("donation_source");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.PatientId).HasMaxLength(10).HasColumnName("patient_id");
                entity.Property(e => e.Inhospid).HasMaxLength(17).HasColumnName("inhospid");
                entity.Property(e => e.PatientName).HasMaxLength(200).HasColumnName("patient_name");
                entity.Property(e => e.CollectorName).HasMaxLength(100).HasColumnName("collector_name");
                entity.Property(e => e.WardDestination).HasMaxLength(500).HasColumnName("ward_destination");
                entity.Property(e => e.UnitsReleased).HasColumnName("units_released");
                entity.Property(e => e.SerialNumbers).HasMaxLength(500).HasColumnName("serial_numbers");
                entity.Property(e => e.DispenseDateTime).HasColumnType("timestamp without time zone").HasColumnName("dispense_date_time");
                entity.Property(e => e.DispensingStaffId).HasMaxLength(7).HasColumnName("dispensing_staff_id");
                entity.Property(e => e.DispensingStaffName).HasMaxLength(100).HasColumnName("dispensing_staff_name");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
                entity.HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId);
                entity.HasOne(e => e.Request).WithMany().HasForeignKey(e => e.RequestId);
            });

            modelBuilder.Entity<BloodBankPatientEvent>(entity =>
            {
                entity.HasKey(e => e.EventId);
                entity.ToTable("blood_bank_patient_event");
                entity.Property(e => e.EventId).HasColumnName("event_id");
                entity.Property(e => e.PatientId).HasMaxLength(10).HasColumnName("patient_id");
                entity.Property(e => e.Inhospid).HasMaxLength(17).HasColumnName("inhospid");
                entity.Property(e => e.EventType).HasMaxLength(50).HasColumnName("event_type");
                entity.Property(e => e.EventDescription).HasColumnName("event_description");
                entity.Property(e => e.EventDateTime).HasColumnType("timestamp without time zone").HasColumnName("event_date_time");
                entity.Property(e => e.CreateUser).HasMaxLength(7).HasColumnName("create_user");
                entity.Property(e => e.CreateDate).HasColumnType("timestamp without time zone").HasColumnName("create_date");
            });
        }

    }
}

