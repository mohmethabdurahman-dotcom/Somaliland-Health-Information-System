using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using KMU.HisOrder.MVC.Models;

namespace KMU.HisOrder.MVC.Models
{
    public partial class KMUContext : DbContext
    {
        public KMUContext()
        {
        }

        public KMUContext(DbContextOptions<KMUContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ClinicSchedule> ClinicSchedules { get; set; }
        public virtual DbSet<Dhis2Disease> Dhis2Diseases { get; set; }
        public virtual DbSet<TestResult> testresults { get; set; }

        public virtual DbSet<Hisorderplan> Hisorderplans { get; set; }
        public virtual DbSet<HisorderplanAttr> HisorderplanAttrs { get; set; }
        public virtual DbSet<Hisordersoa> Hisordersoas { get; set; }
        public virtual DbSet<KmuAttribute> KmuAttributes { get; set; }
        public virtual DbSet<KmuAuth> KmuAuths { get; set; }
        public virtual DbSet<KmuAuthsLog> KmuAuthsLogs { get; set; }
        public virtual DbSet<KmuChart> KmuCharts { get; set; }
        public virtual DbSet<KmuChartLog> KmuChartLogs { get; set; }
        public virtual DbSet<KmuCoderef> KmuCoderefs { get; set; }
        public virtual DbSet<KmuCondition> KmuConditions { get; set; }
        public virtual DbSet<KmuDepartment> KmuDepartments { get; set; }
        public virtual DbSet<KmuIcd> KmuIcds { get; set; }
        public virtual DbSet<KmuMedfrequency> KmuMedfrequencies { get; set; }
        public virtual DbSet<KmuMedfrequencyInd> KmuMedfrequencyInds { get; set; }
        public virtual DbSet<KmuMedicine> KmuMedicines { get; set; }
        public virtual DbSet<KmuMedpathway> KmuMedpathways { get; set; }
        public virtual DbSet<KmuNonMedicine> KmuNonMedicines { get; set; }
        public virtual DbSet<KmuProject> KmuProjects { get; set; }
        public virtual DbSet<KmuSerialpool> KmuSerialpools { get; set; }
        public virtual DbSet<KmuUser> KmuUsers { get; set; }
        public virtual DbSet<KmuUsersLog> KmuUsersLogs { get; set; }
        public virtual DbSet<PhysicalSign> PhysicalSigns { get; set; }
        public virtual DbSet<Registration> Registrations { get; set; }
        public virtual DbSet<TransactionCall> TransactionCalls { get; set; }
        public virtual DbSet<TransactionFee> TransactionFees { get; set; }
        public DbSet<KMU.HisOrder.MVC.Models.kmu_chart_MergeHistory> kmu_chart_MergeHistory { get; set; }
        public DbSet<KMU.HisOrder.MVC.Models.KMU_MergeHistory> KMU_MergeHistory { get; set; }
        public DbSet<KMU.HisOrder.MVC.Models.kmu_ncd> kmu_ncd { get; set; }
        public DbSet<KMU.HisOrder.MVC.Models.home_physicalsign> home_physicalsign { get; set; }
        public DbSet<KMU.HisOrder.MVC.Models.kmu_mental> kmu_mental { get; set; }
        public DbSet<Bed> beds { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<MedicalAdministration> MedicalAdministrations { get; set; }
        public DbSet<InpatientReservation> InpatientReservations { get; set; }
        public DbSet<room> rooms { get; set; }
        public DbSet<radiologyexamrequest> radiologyexamrequests { get; set; }
        public DbSet<radiologyreport> radiologyreports { get; set; }
        public DbSet<radiologyreportversion> radiologyreportversions { get; set; }
        public DbSet<radiologyreportaudittrail> radiologyreportaudittrails { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClinicSchedule>(entity =>
            {
                entity.HasKey(e => new { e.ScheWeek, e.ScheNoon, e.ScheRoom, e.shift })
                    .HasName("clinic_schedule_pkey");

                entity.ToTable("clinic_schedule");

                entity.Property(e => e.ScheWeek)
                    .HasMaxLength(1)
                    .HasColumnName("sche_week")
                    .HasComment("???");

                entity.Property(e => e.shift)
               .HasMaxLength(1)
               .HasColumnName("shift")
               .HasComment("???");

                entity.Property(e => e.ScheNoon)
                    .HasMaxLength(5)
                    .HasColumnName("sche_noon")
                    .HasComment("??");

                entity.Property(e => e.ScheRoom)
                    .HasMaxLength(3)
                    .HasColumnName("sche_room")
                    .HasComment("????");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.ScheCallNo)
                    .HasColumnName("sche_call_no")
                    .HasComment("????");

                entity.Property(e => e.ScheCallTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("sche_call_time")
                    .HasComment("Calling Time Update");

                entity.Property(e => e.ScheDoctor)
                    .HasMaxLength(7)
                    .HasColumnName("sche_doctor")
                    .HasComment("????");

                entity.Property(e => e.ScheDoctorName)
                    .HasMaxLength(200)
                    .HasColumnName("sche_doctor_name")
                    .HasComment("????");

                entity.Property(e => e.ScheDptCode)
                    .HasMaxLength(6)
                    .HasColumnName("sche_dpt_code")
                    .HasComment("????");

                entity.Property(e => e.ScheDptName)
                    .HasMaxLength(200)
                    .HasColumnName("sche_dpt_name")
                    .HasComment("????");

                entity.Property(e => e.ScheOpenFlag)
                    .HasMaxLength(1)
                    .HasColumnName("sche_open_flag")
                    .HasComment("??????");

                entity.Property(e => e.ScheRemark)

                    .HasMaxLength(1000)
                    .HasColumnName("sche_remark");
            });

            modelBuilder.Entity<Dhis2Disease>(entity =>
            {
                entity.HasKey(e => e.Dhis2Code)
                    .HasName("DHIS2DISEASES_pkey");

                entity.ToTable("dhis2_diseases");

                entity.Property(e => e.Dhis2Code)
                    .HasColumnName("dhis2_code")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Diseases)
                    .IsRequired()
                    .HasMaxLength(400)
                    .HasColumnName("diseases");

                entity.Property(e => e.ShowSeq).HasColumnName("show_seq");
            });

            modelBuilder.Entity<Hisorderplan>(entity =>
            {
                entity.HasKey(e => e.Orderplanid)
                    .HasName("hisorderplan_pkey");

                entity.ToTable("hisorderplan");

                entity.HasIndex(e => e.HealthId, "idx_hisorderplan_01")
                    .UseCollation(new[] { "C" });

                entity.HasIndex(e => e.Inhospid, "idx_hisorderplan_02")
                    .HasOperators(new[] { "bpchar_pattern_ops" })
                    .UseCollation(new[] { "C" });

                entity.Property(e => e.Orderplanid)
                    .ValueGeneratedNever()
                    .HasColumnName("orderplanid");

                entity.Property(e => e.AddFlag)
                    .HasMaxLength(1)
                    .HasColumnName("add_flag")
                    .HasDefaultValueSql("'N'::bpchar");

                entity.Property(e => e.ChargeStatus)
                    .HasMaxLength(1)
                    .HasColumnName("charge_status");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.DcDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("dc_date");

                entity.Property(e => e.DcStatus)
                    .HasMaxLength(1)
                    .HasColumnName("dc_status")
                    .HasDefaultValueSql("'0'::bpchar");

                entity.Property(e => e.DcUser)
                    .HasMaxLength(7)
                    .HasColumnName("dc_user");

                entity.Property(e => e.DoseIndication)
                    .HasMaxLength(20)
                    .HasColumnName("dose_indication");

                entity.Property(e => e.DosePath)
                    .HasMaxLength(20)
                    .HasColumnName("dose_path");

                entity.Property(e => e.ExamLoc)
                    .HasMaxLength(20)
                    .HasColumnName("exam_loc");

                entity.Property(e => e.ExecDateFrom)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("exec_date_from");

                entity.Property(e => e.ExecDateTo)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("exec_date_to");

                entity.Property(e => e.ExecStatus)
                    .HasMaxLength(1)
                    .HasColumnName("exec_status");

                entity.Property(e => e.FreeCharge)
                    .HasMaxLength(1)
                    .HasColumnName("free_charge")
                    .HasDefaultValueSql("'N'::bpchar");

                entity.Property(e => e.FreqCode)
                    .HasMaxLength(20)
                    .HasColumnName("freq_code");

                entity.Property(e => e.HealthId)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("health_id")
                    .IsFixedLength();

                entity.Property(e => e.HplanType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("hplan_type");

                entity.Property(e => e.Inhospid)
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("inhospid");

                entity.Property(e => e.KeepspecFlag)
                    .HasMaxLength(1)
                    .HasColumnName("keepspec_flag")
                    .HasDefaultValueSql("'N'::bpchar");

                entity.Property(e => e.LocationCode)
                    .HasMaxLength(10)
                    .HasColumnName("location_code");

                entity.Property(e => e.MadeType)
                    .HasMaxLength(20)
                    .HasColumnName("made_type");

                entity.Property(e => e.MedBag).HasColumnName("med_bag");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_date");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.OrderDept)
                    .HasMaxLength(6)
                    .HasColumnName("order_dept")
                    .IsFixedLength();

                entity.Property(e => e.OrderDr)
                    .HasMaxLength(7)
                    .HasColumnName("order_dr")
                    .IsFixedLength();

                entity.Property(e => e.PlanCode)
                    .HasMaxLength(20)
                    .HasColumnName("plan_code");

                entity.Property(e => e.PlanDays)
                    .HasColumnName("plan_days")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.PlanDes)
                    .HasMaxLength(150)
                    .HasColumnName("plan_des");

                entity.Property(e => e.PreopFlag)
                    .HasMaxLength(1)
                    .HasColumnName("preop_flag")
                    .HasDefaultValueSql("'N'::bpchar");

                entity.Property(e => e.PrintDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("print_date");

                entity.Property(e => e.PrintUser)
                    .HasMaxLength(7)
                    .HasColumnName("print_user");

                entity.Property(e => e.QtyDaily)
                    .HasPrecision(9, 2)
                    .HasColumnName("qty_daily");

                entity.Property(e => e.QtyDose)
                    .HasPrecision(9, 2)
                    .HasColumnName("qty_dose");

                entity.Property(e => e.Remark)
                    .HasMaxLength(300)
                    .HasColumnName("remark");

                entity.Property(e => e.SeqNo).HasColumnName("seq_no");

                entity.Property(e => e.Status)
                    .HasMaxLength(1)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'0'::bpchar");

                entity.Property(e => e.TotalQty)
                    .HasPrecision(10, 2)
                    .HasColumnName("total_qty");

                entity.Property(e => e.TriggerRecid).HasColumnName("trigger_recid");

                entity.Property(e => e.TriggerTablecode)
                    .HasMaxLength(30)
                    .HasColumnName("trigger_tablecode");

                entity.Property(e => e.UnitDose)
                    .HasMaxLength(20)
                    .HasColumnName("unit_dose");

                entity.Property(e => e.UrgFlag)
                    .HasMaxLength(1)
                    .HasColumnName("urg_flag")
                    .HasDefaultValueSql("'N'::bpchar");
            });

            modelBuilder.Entity<HisorderplanAttr>(entity =>
            {
                entity.HasKey(e => e.Orderplanatrrid)
                    .HasName("hisorderplan_attr_pkey");

                entity.ToTable("hisorderplan_attr");

                entity.Property(e => e.Orderplanatrrid).HasColumnName("orderplanatrrid");

                entity.Property(e => e.AttrCode)
                    .HasMaxLength(30)
                    .HasColumnName("attr_code");

                entity.Property(e => e.Des)
                    .HasMaxLength(1000)
                    .HasColumnName("des");

                entity.Property(e => e.Orderplanid)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("orderplanid");

                entity.Property(e => e.Parameter1)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_1");

                entity.Property(e => e.Parameter2)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_2");

                entity.Property(e => e.Parameter3)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_3");

                entity.Property(e => e.Parameter4)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_4");

                entity.Property(e => e.Parameter5)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_5");

                entity.Property(e => e.Parameter6)
                    .HasMaxLength(50)
                    .HasColumnName("parameter_6");

                entity.HasOne(d => d.Orderplan)
                    .WithMany(p => p.HisorderplanAttrs)
                    .HasForeignKey(d => d.Orderplanid)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hisorderplan_attr_fkey");
            });

            modelBuilder.Entity<Hisordersoa>(entity =>
            {
                entity.HasKey(e => e.Soaid)
                    .HasName("hisorderplan_soa_pkey");

                entity.ToTable("hisordersoa");

                entity.HasIndex(e => e.Inhospid, "idx_hisordersoa_01")
                    .HasOperators(new[] { "bpchar_pattern_ops" })
                    .UseCollation(new[] { "C" });

                entity.HasIndex(e => e.HealthId, "idx_hisordersoa_02")
                    .HasOperators(new[] { "bpchar_pattern_ops" })
                    .UseCollation(new[] { "C" });

                entity.Property(e => e.Soaid)
                    .ValueGeneratedNever()
                    .HasColumnName("soaid");

                entity.Property(e => e.Context).HasColumnName("context");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_date");

                entity.Property(e => e.CreateUser)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.DcDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("dc_date");

                entity.Property(e => e.DcUser)
                    .HasMaxLength(7)
                    .HasColumnName("dc_user");

                entity.Property(e => e.HealthId)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("health_id")
                    .IsFixedLength();

                entity.Property(e => e.Inhospid)
                    .IsRequired()
                    .HasColumnType("character varying")
                    .HasColumnName("inhospid");

                entity.Property(e => e.Kind)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("kind");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_date");

                entity.Property(e => e.ModifyUser)
                    .HasColumnType("character varying")
                    .HasColumnName("modify_user");

                entity.Property(e => e.SourceType)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("source_type");

                entity.Property(e => e.Status)
                    .HasColumnType("char")
                    .HasColumnName("status");

                entity.Property(e => e.VersionCode).HasColumnName("version_code");
            });

            modelBuilder.Entity<KmuAttribute>(entity =>
            {
                entity.HasKey(e => e.AttrCode)
                    .HasName("kmu_attribute_pkey");

                entity.ToTable("kmu_attribute");

                entity.Property(e => e.AttrCode)
                    .HasMaxLength(3)
                    .HasColumnName("attr_code")
                    .HasComment("????");

                entity.Property(e => e.AttrName)
                    .HasMaxLength(100)
                    .HasColumnName("attr_name")
                    .HasComment("????");

                entity.Property(e => e.AttrRegFee)
                    .HasColumnName("attr_reg_fee")
                    .HasComment("??????????");

                entity.Property(e => e.AttrStatus)
                    .HasMaxLength(1)
                    .HasColumnName("attr_status")
                    .HasComment("????");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");
            });

            modelBuilder.Entity<KmuAuth>(entity =>
            {
                entity.HasKey(e => new { e.UserIdno, e.ProjectId })
                    .HasName("kmu_auths_pkey");

                entity.ToTable("kmu_auths");

                entity.HasComment("User Auth File(Account permissions)");

                entity.Property(e => e.UserIdno)
                    .HasMaxLength(7)
                    .HasColumnName("user_idno");

                entity.Property(e => e.ProjectId)
                    .HasMaxLength(32)
                    .HasColumnName("project_id");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_time");

                entity.Property(e => e.Creator)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("creator");

                entity.HasOne(d => d.UserIdnoNavigation)
                    .WithMany(p => p.KmuAuths)
                    .HasForeignKey(d => d.UserIdno)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("kmu_auths_fk01");
            });

            modelBuilder.Entity<KmuAuthsLog>(entity =>
            {
                entity.HasKey(e => new { e.EditUser, e.EditTime, e.UserIdno, e.ProjectId })
                    .HasName("kmu_auths_log_pkey");

                entity.ToTable("kmu_auths_log");

                entity.HasComment("Auth change log");

                entity.Property(e => e.EditUser)
                    .HasMaxLength(7)
                    .HasColumnName("edit_user");

                entity.Property(e => e.EditTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("edit_time")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.UserIdno)
                    .HasMaxLength(7)
                    .HasColumnName("user_idno");

                entity.Property(e => e.ProjectId)
                    .HasMaxLength(32)
                    .HasColumnName("project_id");

                entity.Property(e => e.EditType)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("edit_type");
            });

            modelBuilder.Entity<KmuChart>(entity =>
            {
                entity.HasKey(e => e.ChrHealthId)
                    .HasName("KMUCHART_pkey");

                entity.ToTable("kmu_chart");

                entity.HasIndex(e => new { e.ChrPatientFirstname, e.ChrPatientMidname, e.ChrPatientLastname }, "idx_kmu_chart_01");

                entity.HasIndex(e => e.ChrMobilePhone, "idx_kmu_chart_02");

                entity.Property(e => e.ChrHealthId)
                    .HasMaxLength(10)
                    .HasColumnName("chr_health_id")
                    .IsFixedLength();

                entity.Property(e => e.ChrAddress).HasColumnName("chr_address");

                entity.Property(e => e.ChrAreaCode)
                    .HasMaxLength(20)
                    .HasColumnName("chr_area_code");

                entity.Property(e => e.ChrBirthDate).HasColumnName("chr_birth_date");

                entity.Property(e => e.ChrCombineFlag)
                    .HasMaxLength(1)
                    .HasColumnName("chr_combine_flag");

                entity.Property(e => e.ChrContactPhone)
                    .HasMaxLength(30)
                    .HasColumnName("chr_contact_phone");

                entity.Property(e => e.ChrContactRelation)
                    .HasMaxLength(2)
                    .HasColumnName("chr_contact_relation")
                    .IsFixedLength();

                entity.Property(e => e.ChrEmgContact)
                    .HasMaxLength(650)
                    .HasColumnName("chr_emg_contact");

                entity.Property(e => e.ChrMobilePhone)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("chr_mobile_phone");

                entity.Property(e => e.ChrNationalId)
                    .HasMaxLength(10)
                    .HasColumnName("chr_national_id");

                entity.Property(e => e.ChrPatientFirstname)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_firstname");

                entity.Property(e => e.ChrPatientLastname)
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_lastname");

                entity.Property(e => e.ChrPatientMidname)
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_midname");

                entity.Property(e => e.ChrRefugeeFlag)
                    .HasMaxLength(1)
                    .HasColumnName("chr_refugee_flag")
                    .HasDefaultValueSql("'N'::bpchar")
                    .HasComment("refugee: Y");

                entity.Property(e => e.ChrRemark)
                    .HasMaxLength(1000)
                    .HasColumnName("chr_remark");

                entity.Property(e => e.ChrSex)
                    .IsRequired()
                    .HasMaxLength(1)
                    .HasColumnName("chr_sex");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user")
                    .IsFixedLength();
            });

            modelBuilder.Entity<KmuChartLog>(entity =>
            {
                entity.HasKey(e => e.LogId)
                    .HasName("KMUCHARTLOG_pkey");

                entity.ToTable("kmu_chart_log");

                entity.HasIndex(e => e.ChrHealthId, "idx_kmu_chart_log_01");

                entity.Property(e => e.LogId)
                    .HasMaxLength(20)
                    .HasColumnName("log_id");

                entity.Property(e => e.ChrAddress).HasColumnName("chr_address");

                entity.Property(e => e.ChrAreaCode)
                    .HasMaxLength(20)
                    .HasColumnName("chr_area_code");

                entity.Property(e => e.ChrBirthDate).HasColumnName("chr_birth_date");

                entity.Property(e => e.ChrCombineFlag)
                    .HasMaxLength(1)
                    .HasColumnName("chr_combine_flag");

                entity.Property(e => e.ChrContactPhone)
                    .HasMaxLength(30)
                    .HasColumnName("chr_contact_phone");

                entity.Property(e => e.ChrContactRelation)
                    .HasMaxLength(2)
                    .HasColumnName("chr_contact_relation")
                    .IsFixedLength();

                entity.Property(e => e.ChrEmgContact)
                    .HasMaxLength(650)
                    .HasColumnName("chr_emg_contact");

                entity.Property(e => e.ChrHealthId)
                    .HasMaxLength(10)
                    .HasColumnName("chr_health_id")
                    .IsFixedLength();

                entity.Property(e => e.ChrMobilePhone)
                    .HasMaxLength(30)
                    .HasColumnName("chr_mobile_phone");

                entity.Property(e => e.ChrNationalId)
                    .HasMaxLength(10)
                    .HasColumnName("chr_national_id");

                entity.Property(e => e.ChrPatientFirstname)
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_firstname");

                entity.Property(e => e.ChrPatientLastname)
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_lastname");

                entity.Property(e => e.ChrPatientMidname)
                    .HasMaxLength(200)
                    .HasColumnName("chr_patient_midname");

                entity.Property(e => e.ChrRefugeeFlag)
                    .HasMaxLength(1)
                    .HasColumnName("chr_refugee_flag");

                entity.Property(e => e.ChrRemark)
                    .HasMaxLength(1000)
                    .HasColumnName("chr_remark");

                entity.Property(e => e.ChrSex)
                    .HasMaxLength(1)
                    .HasColumnName("chr_sex");

                entity.Property(e => e.LogMode)
                    .HasMaxLength(1)
                    .HasColumnName("log_mode")
                    .HasComment("Command Mode: Insert, Update, Delete");

                entity.Property(e => e.LogTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("log_time")
                    .HasComment("Modify Time");

                entity.Property(e => e.LogUser)
                    .HasMaxLength(7)
                    .HasColumnName("log_user")
                    .IsFixedLength()
                    .HasComment("Modify by User");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user")
                    .IsFixedLength();
            });

            modelBuilder.Entity<KmuCoderef>(entity =>
            {
                entity.HasKey(e => e.RefId)
                    .HasName("kmu_coderef_pkey");

                entity.ToTable("kmu_coderef");

                entity.Property(e => e.RefId)
                    .HasMaxLength(20)
                    .HasColumnName("ref_id");

                entity.Property(e => e.ModifyId)
                    .HasMaxLength(7)
                    .HasColumnName("modify_id");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.RefCasetype)
                    .HasMaxLength(5)
                    .HasColumnName("ref_casetype");

                entity.Property(e => e.RefCode)
                    .HasMaxLength(500)
                    .HasColumnName("ref_code");

                entity.Property(e => e.RefCodetype)
                    .HasMaxLength(100)
                    .HasColumnName("ref_codetype");

                entity.Property(e => e.RefDefaultFlag)
                    .HasMaxLength(5)
                    .HasColumnName("ref_default_flag")
                    .HasComment("??????");

                entity.Property(e => e.RefDes)
                    .HasMaxLength(2000)
                    .HasColumnName("ref_des");

                entity.Property(e => e.RefDes2)
                    .HasMaxLength(2000)
                    .HasColumnName("ref_des2");

                entity.Property(e => e.RefName)
                    .HasMaxLength(1000)
                    .HasColumnName("ref_name");

                entity.Property(e => e.RefShowseq).HasColumnName("ref_showseq");
            });

            modelBuilder.Entity<KmuCondition>(entity =>
            {
                entity.HasKey(e => new { e.CndCodetype, e.CndCode })
                    .HasName("kmu_condition_pkey");

                entity.ToTable("kmu_condition");

                entity.Property(e => e.CndCodetype)
                    .HasMaxLength(100)
                    .HasColumnName("cnd_codetype");

                entity.Property(e => e.CndCode)
                    .HasMaxLength(500)
                    .HasColumnName("cnd_code");

                entity.Property(e => e.CndDesc)
                    .HasMaxLength(500)
                    .HasColumnName("cnd_desc");

                entity.Property(e => e.CndEnable)
                    .HasMaxLength(1)
                    .HasColumnName("cnd_enable");

                entity.Property(e => e.CndNoon)
                    .HasMaxLength(5)
                    .HasColumnName("cnd_noon");

                entity.Property(e => e.CndRoom)
                    .HasMaxLength(3)
                    .HasColumnName("cnd_room");

                entity.Property(e => e.CndSymbol1)
                    .HasMaxLength(2)
                    .HasColumnName("cnd_symbol1")
                    .IsFixedLength();

                entity.Property(e => e.CndSymbol2)
                    .HasMaxLength(2)
                    .HasColumnName("cnd_symbol2")
                    .IsFixedLength();

                entity.Property(e => e.CndValue1)
                    .HasMaxLength(100)
                    .HasColumnName("cnd_value1");

                entity.Property(e => e.CndValue2)
                    .HasMaxLength(100)
                    .HasColumnName("cnd_value2");

                entity.Property(e => e.CndWeek)
                    .HasMaxLength(1)
                    .HasColumnName("cnd_week");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");
            });

            modelBuilder.Entity<KmuDepartment>(entity =>
            {
                entity.HasKey(e => e.DptCode)
                    .HasName("kmu_department_pkey");

                entity.ToTable("kmu_department");

                entity.Property(e => e.DptCode)
                    .HasMaxLength(6)
                    .HasColumnName("dpt_code");

                entity.Property(e => e.DptCategory)
                    .HasMaxLength(3)
                    .HasColumnName("dpt_category");

                entity.Property(e => e.DptDefaultAttr)
                    .HasMaxLength(3)
                    .HasColumnName("dpt_default_attr")
                    .HasComment("?????");

                entity.Property(e => e.DptDepth).HasColumnName("dpt_depth");

                entity.Property(e => e.DptName)
                    .HasMaxLength(200)
                    .HasColumnName("dpt_name");

                entity.Property(e => e.DptParent)
                    .HasMaxLength(6)
                    .HasColumnName("dpt_parent");

                entity.Property(e => e.DptRemark)
                    .HasMaxLength(1000)
                    .HasColumnName("dpt_remark");

                entity.Property(e => e.DptStatus)
                    .HasMaxLength(1)
                    .HasColumnName("dpt_status");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");
            });

            modelBuilder.Entity<KmuIcd>(entity =>
            {
                entity.HasKey(e => e.IcdCode)
                    .HasName("KMUICD_pkey");

                entity.ToTable("kmu_icd");

                entity.HasComment("Diagnosis data.");

                entity.HasIndex(e => e.ParentCode, "idx_icd_01")
                    .HasOperators(new[] { "varchar_ops" });

                entity.Property(e => e.IcdCode)
                    .HasMaxLength(8)
                    .HasColumnName("icd_code");

                entity.Property(e => e.Dhis2Code).HasColumnName("dhis2_code");

                entity.Property(e => e.IcdCodeUndot)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("icd_code_undot")
                    .HasComment("ICD Code without decimal point.");

                entity.Property(e => e.IcdEnglishName)
                    .IsRequired()
                    .HasMaxLength(400)
                    .HasColumnName("icd_english_name");

                entity.Property(e => e.IcdType)
                    .HasMaxLength(10)
                    .HasColumnName("icd_type")
                    .HasComment("CM/PCS");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_date");

                entity.Property(e => e.ModifyUser)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.ParentCode)
                    .HasMaxLength(8)
                    .HasColumnName("parent_code")
                    .HasComment("Parent ICD Code for HisOrder UI Design.");

                entity.Property(e => e.ShowMode)
                    .HasMaxLength(10)
                    .HasColumnName("show_mode")
                    .HasComment("Show position for HisOrder UI Design.");

                entity.Property(e => e.Status)
                    .HasMaxLength(1)
                    .HasColumnName("status")
                    .HasComment("ICD Code.");

                entity.Property(e => e.Versioncode)
                    .HasMaxLength(2)
                    .HasColumnName("versioncode")
                    .HasComment("ICD 9 / ICD 10 ...");
            });

            modelBuilder.Entity<KmuMedfrequency>(entity =>
            {
                entity.HasKey(e => e.FrqCode)
                    .HasName("kmu_medfrequency_pkey");

                entity.ToTable("kmu_medfrequency");

                entity.Property(e => e.FrqCode)
                    .HasMaxLength(20)
                    .HasColumnName("frq_code");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.EnableStatus)
                    .HasMaxLength(1)
                    .HasColumnName("enable_status");

                entity.Property(e => e.FreqDesc)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("freq_desc");

                entity.Property(e => e.FrqForDays).HasColumnName("frq_for_days");

                entity.Property(e => e.FrqForTimes).HasColumnName("frq_for_times");

                entity.Property(e => e.FrqOneDayTimes).HasColumnName("frq_one_day_times");

                entity.Property(e => e.FrqSeqNo)
                    .HasColumnName("frq_seq_no")
                    .HasDefaultValueSql("999");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");
            });

            modelBuilder.Entity<KmuMedfrequencyInd>(entity =>
            {
                entity.HasKey(e => new { e.FrqCode, e.IndCode })
                    .HasName("kmu_medfrequency_ind_pkey");

                entity.ToTable("kmu_medfrequency_ind");

                entity.Property(e => e.FrqCode)
                    .HasMaxLength(20)
                    .HasColumnName("frq_code");

                entity.Property(e => e.IndCode)
                    .HasMaxLength(20)
                    .HasColumnName("ind_code");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.EnableStatus)
                    .HasMaxLength(1)
                    .HasColumnName("enable_status");

                entity.Property(e => e.IndDesc)
                    .HasMaxLength(100)
                    .HasColumnName("ind_desc");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.Showseq)
                    .HasPrecision(3)
                    .HasColumnName("showseq");
            });

            modelBuilder.Entity<KmuMedicine>(entity =>
            {
                entity.HasKey(e => e.MedId)
                    .HasName("KMUMEDICINE_pkey");

                entity.ToTable("kmu_medicine");

                entity.Property(e => e.MedId)
                    .HasMaxLength(10)
                    .HasColumnName("med_id");

                entity.Property(e => e.BrandName)
                    .HasMaxLength(150)
                    .HasColumnName("brand_name");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.DefaultFreq)
                    .HasMaxLength(10)
                    .HasColumnName("default_freq")
                    .HasComment("???????(???)");

                entity.Property(e => e.EndDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("end_date");

                entity.Property(e => e.GenericName)
                    .HasMaxLength(150)
                    .HasColumnName("generic_name");

                entity.Property(e => e.MedType)
                    .IsRequired()
                    .HasMaxLength(2)
                    .HasColumnName("med_type")
                    .HasComment("1-??\n2-??\n3-??");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.PackSpec)
                    .HasMaxLength(20)
                    .HasColumnName("pack_spec")
                    .HasComment("????(????)");

                entity.Property(e => e.RefDuration)
                    .HasMaxLength(3)
                    .HasColumnName("ref_duration")
                    .HasComment("???????(????)");

                entity.Property(e => e.Remarks)
                    .HasMaxLength(500)
                    .HasColumnName("remarks")
                    .HasComment("??????");

                entity.Property(e => e.StartDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("start_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Status)
                    .HasMaxLength(1)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'0'::bpchar")
                    .HasComment("????????");

                entity.Property(e => e.UnitSpec)
                    .HasMaxLength(20)
                    .HasColumnName("unit_spec")
                    .HasComment("????");
            });

            modelBuilder.Entity<KmuMedpathway>(entity =>
            {
                entity.HasKey(e => new { e.MedType, e.PathCode })
                    .HasName("kmu_medpathway_pkey");

                entity.ToTable("kmu_medpathway");

                entity.Property(e => e.MedType)
                    .HasMaxLength(1)
                    .HasColumnName("med_type");

                entity.Property(e => e.PathCode)
                    .HasMaxLength(20)
                    .HasColumnName("path_code");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.EnableStatus)
                    .HasMaxLength(1)
                    .HasColumnName("enable_status");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.PathDesc)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("path_desc");

                entity.Property(e => e.Showseq)
                    .HasPrecision(3)
                    .HasColumnName("showseq");
            });

            modelBuilder.Entity<KmuNonMedicine>(entity =>
            {
                entity.HasKey(e => e.ItemId)
                    .HasName("kmu_non_medicine_pkey");

                entity.ToTable("kmu_non_medicine");

                entity.Property(e => e.ItemId)
                    .HasMaxLength(10)
                    .HasColumnName("item_id");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(7)
                    .HasColumnName("create_user");

                entity.Property(e => e.EndDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("end_date");

                entity.Property(e => e.GroupCode)
                    .HasMaxLength(10)
                    .HasColumnName("group_code");

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnName("item_name");

                entity.Property(e => e.ItemSpec)
                    .HasMaxLength(20)
                    .HasColumnName("item_spec");

                entity.Property(e => e.ItemType)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("item_type")
                    .HasComment("5.Laboratory 6.Radiology 7.Pathology 8.Material");

                entity.Property(e => e.ModifyDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.Remark)
                    .HasMaxLength(500)
                    .HasColumnName("remark");

                entity.Property(e => e.ShowSeq)
                    .HasPrecision(7, 2)
                    .HasColumnName("show_seq");

                entity.Property(e => e.StartDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("start_date");

                entity.Property(e => e.Status)
                    .HasMaxLength(1)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'0'::bpchar");
            });

            modelBuilder.Entity<KmuProject>(entity =>
            {
                entity.HasKey(e => e.ProjectId)
                    .HasName("kmu_projects_pkey");

                entity.ToTable("kmu_projects");

                entity.HasComment("Auth Setting reference Project File(main function node)");

                entity.Property(e => e.ProjectId)
                    .HasMaxLength(32)
                    .HasColumnName("project_id");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Creator)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("creator");

                entity.Property(e => e.ProjectName)
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnName("project_name");

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("url");
            });

            modelBuilder.Entity<KmuSerialpool>(entity =>
            {
                entity.HasKey(e => e.SerialOwner)
                    .HasName("kmu_serialpool_pkey");

                entity.ToTable("kmu_serialpool");

                entity.Property(e => e.SerialOwner)
                    .HasMaxLength(50)
                    .HasColumnName("serial_owner");

                entity.Property(e => e.SerialMaxno)
                    .HasMaxLength(20)
                    .HasColumnName("serial_maxno");

                entity.Property(e => e.SerialNo).HasColumnName("serial_no");

                entity.Property(e => e.SerialPrefix)
                    .HasMaxLength(5)
                    .HasColumnName("serial_prefix");
            });

            modelBuilder.Entity<KmuUser>(entity =>
            {
                entity.HasKey(e => e.UserIdno)
                    .HasName("kmu_users_pkey");

                entity.ToTable("kmu_users");

                entity.HasComment("Account Basic File(user account )");

                entity.Property(e => e.UserIdno)
                    .HasMaxLength(7)
                    .HasColumnName("user_idno");

                entity.Property(e => e.AccountStatus)
                    .IsRequired()
                    .HasMaxLength(1)
                    .HasColumnName("account_status")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.CreateTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("create_time");

                entity.Property(e => e.Creator)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnName("creator");

                entity.Property(e => e.EndDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("end_date");

                entity.Property(e => e.StartDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("start_date");

                entity.Property(e => e.UserBirthDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("user_birth_date");

                entity.Property(e => e.UserCategory)
                    .IsRequired()
                    .HasMaxLength(1)
                    .HasColumnName("user_category")
                    .HasComment("??(1:Doctor,2:Nurse,3.Staff");

                entity.Property(e => e.UserEmail).HasColumnName("user_email");

                entity.Property(e => e.UserMobilePhone)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("user_mobile_phone");

                entity.Property(e => e.UserNameFirstname)
                    .IsRequired()
                    .HasColumnName("user_name_firstname");

                entity.Property(e => e.UserNameLastname)
                    .IsRequired()
                    .HasColumnName("user_name_lastname");

                entity.Property(e => e.UserNameMidname)
                    .IsRequired()
                    .HasColumnName("user_name_midname");

                entity.Property(e => e.UserPassword)
                    .IsRequired()
                    .HasColumnName("user_password");

                entity.Property(e => e.UserSex).HasColumnName("user_sex");
            });

            modelBuilder.Entity<KmuUsersLog>(entity =>
            {
                entity.HasKey(e => new { e.UserIdno, e.EventType, e.EventTime, e.Ip })
                    .HasName("kmu_users_log_pkey");

                entity.ToTable("kmu_users_log");

                entity.HasComment("Account change log(??????????2023.03.03)");

                entity.Property(e => e.UserIdno).HasColumnName("user_idno");

                entity.Property(e => e.EventType).HasColumnName("event_type");

                entity.Property(e => e.EventTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("event_time")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.Ip)
                    .HasMaxLength(64)
                    .HasColumnName("ip");

                entity.Property(e => e.EventErrorInput).HasColumnName("event_error_input");

                entity.Property(e => e.EventLoggingUser)
                    .HasMaxLength(7)
                    .HasColumnName("event_logging_user");

                entity.Property(e => e.IsSuccess).HasColumnName("is_success");

                entity.Property(e => e.Message).HasColumnName("message");
            });

            modelBuilder.Entity<PhysicalSign>(entity =>
            {
                entity.HasKey(e => e.PhyId)
                    .HasName("physical_sign_pkey");

                entity.ToTable("physical_sign");

                entity.HasIndex(e => new { e.Inhospid, e.PhyType }, "idx_physical_sign_01");

                entity.Property(e => e.PhyId)
                    .HasMaxLength(50)
                    .HasColumnName("phy_id");

                entity.Property(e => e.Inhospid)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("inhospid");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.PhyType)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("phy_type");

                entity.Property(e => e.PhyValue)
                    .HasMaxLength(500)
                    .HasColumnName("phy_value");
            });

            modelBuilder.Entity<Registration>(entity =>
            {
                entity.HasKey(e => new { e.RegDate, e.RegDepartment, e.RegNoon, e.RegSeqNo })
                    .HasName("registration_pkey");

                entity.ToTable("registration");

                entity.HasIndex(e => e.Inhospid, "idx_registration_01");

                entity.Property(e => e.RegDate)
                    .HasColumnName("reg_date")
                    .HasComment("???");

                entity.Property(e => e.RegDepartment)
                    .HasMaxLength(6)
                    .HasColumnName("reg_department")
                    .HasComment("????");

                entity.Property(e => e.RegNoon)
                    .HasMaxLength(5)
                    .HasColumnName("reg_noon")
                    .HasComment("??");

                entity.Property(e => e.RegSeqNo)
                    .HasColumnName("reg_seq_no")
                    .HasComment("??:???\n??:????");

                entity.Property(e => e.Inhospid)
                    .HasMaxLength(20)
                    .HasColumnName("inhospid")
                    .HasComment("????");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.RegAttrDesc)
                    .HasMaxLength(200)
                    .HasColumnName("reg_attr_desc")
                    .HasComment("????");

                entity.Property(e => e.RegAttribute)
                    .HasMaxLength(3)
                    .HasColumnName("reg_attribute")
                    .HasComment("????->??kmu_attribute");

                entity.Property(e => e.RegBedNo)
                    .HasMaxLength(3)
                    .HasColumnName("reg_bed_no")
                    .HasComment("????");

                entity.Property(e => e.RegCallTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_call_time")
                    .HasComment("????");

                entity.Property(e => e.RegCreateTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_create_time")
                    .HasComment("create datetime for registration ");

                entity.Property(e => e.RegDoctorId)
                    .HasMaxLength(7)
                    .HasColumnName("reg_doctor_id")
                    .HasComment("????");

                entity.Property(e => e.RegEndTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_end_time")
                    .HasComment("??????");

                entity.Property(e => e.RegExamEndTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_exam_end_time")
                    .HasComment("finish examining return time");

                entity.Property(e => e.RegExamStartTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_exam_start_time")
                    .HasComment("click examining start time");

                entity.Property(e => e.RegFollowCode)
                    .HasMaxLength(20)
                    .HasColumnName("reg_follow_code");

                entity.Property(e => e.RegFollowDesc)
                    .HasMaxLength(500)
                    .HasColumnName("reg_follow_desc");

                entity.Property(e => e.RegHealthId)
                    .HasMaxLength(10)
                    .HasColumnName("reg_health_id")
                    .IsFixedLength()
                    .HasComment("???");

                entity.Property(e => e.RegRoomNo)
                    .HasMaxLength(3)
                    .HasColumnName("reg_room_no")
                    .HasComment("???");

                entity.Property(e => e.RegStartTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("reg_start_time")
                    .HasComment("??????");

                entity.Property(e => e.RegStatus)
                    .HasMaxLength(1)
                    .HasColumnName("reg_status")
                    .HasComment("????\nN:???\nT :??\n* :???\nC:????");

                entity.Property(e => e.RegTriage)
                    .HasMaxLength(1)
                    .HasColumnName("reg_triage")
                    .HasComment("????\n0:????(??)\n1:????(??)\n2:????(??)\n3:????(??)\n4:????(??)");
            });

            modelBuilder.Entity<TransactionCall>(entity =>
            {
                entity.HasKey(e => e.CallId)
                    .HasName("transaction_call_pkey");

                entity.ToTable("transaction_call");

                entity.Property(e => e.CallId)
                    .HasColumnName("call_id")
                    .HasComment("???")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.CallPatientId)
                    .HasMaxLength(10)
                    .HasColumnName("call_patient_id")
                    .IsFixedLength()
                    .HasComment("???");

                entity.Property(e => e.CallRegDate)
                    .HasColumnName("call_reg_date")
                    .HasComment("???");

                entity.Property(e => e.CallRegDepartment)
                    .HasMaxLength(6)
                    .HasColumnName("call_reg_department")
                    .HasComment("???");

                entity.Property(e => e.CallRegNoon)
                    .HasMaxLength(5)
                    .HasColumnName("call_reg_noon")
                    .HasComment("??");

                entity.Property(e => e.CallRegSeqNo)
                    .HasColumnName("call_reg_seq_no")
                    .HasComment("???");

                entity.Property(e => e.CallTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("call_time")
                    .HasComment("????");

                entity.Property(e => e.Inhospid)
                    .HasColumnName("inhospid")
                    .HasComment("????");

                entity.Property(e => e.ModifySuer)
                    .HasMaxLength(7)
                    .HasColumnName("modify_suer");

                entity.Property(e => e.ModifyTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_time")
                    .HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<TransactionFee>(entity =>
            {
                entity.HasKey(e => e.TransationId)
                    .HasName("transaction_fee_pkey");

                entity.ToTable("transaction_fee");

                entity.Property(e => e.TransationId)
                    .HasColumnName("transation_id")
                    .HasComment("???")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.FeePaidFlag)
                    .HasMaxLength(1)
                    .HasColumnName("fee_paid_flag")
                    .HasComment("?????");

                entity.Property(e => e.FeePaidMoney)
                    .HasColumnName("fee_paid_money")
                    .HasComment("????");

                entity.Property(e => e.FeeType)
                    .HasMaxLength(10)
                    .HasColumnName("fee_type")
                    .HasComment("????");

                entity.Property(e => e.Inhospid)
                    .HasColumnName("inhospid")
                    .HasComment("????");

                entity.Property(e => e.ModifyUser)
                    .HasMaxLength(7)
                    .HasColumnName("modify_user");

                entity.Property(e => e.TransactionTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("transaction_time")
                    .HasComment("????");
            });
            modelBuilder.Entity<Bed>(entity =>
            {
                entity.HasKey(e => e.bedId)
                .HasName("bed_pkey");

                entity.ToTable("beds");
                entity.Property(e => e.bedId).HasColumnName("bedid");

                entity.Property(e => e.wardId)
                     .HasColumnName("ward_id");

                entity.Property(e => e.bedName)
                    .HasColumnName("bed_name")
                    .HasComment("name of the bed");

                entity.Property(e => e.createAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("create_at")
                  .HasDefaultValueSql("now()");

                entity.Property(e => e.createBy)
                    .HasMaxLength(7)
                    .HasColumnName("create_by");

                entity.Property(e => e.modifyAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modify_at")
                .HasDefaultValueSql("now()");

                entity.Property(e => e.modifyBy)
                    .HasMaxLength(7)
                    .HasColumnName("modify_by");

            });
            modelBuilder.Entity<Ward>(entity =>
            {
                entity.HasKey(e => e.wardid)
                 .HasName("ward_pkey");

                entity.ToTable("wards");
                entity.Property(e => e.wardName)
                .HasColumnName("ward_name");

                entity.Property(e => e.createAt)
              .HasColumnType("timestamp without time zone")
              .HasColumnName("create_at")
              .HasDefaultValueSql("now()");

                entity.Property(e => e.createBy)
                    .HasMaxLength(7)
                    .HasColumnName("create_by");

                entity.Property(e => e.modifyAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modify_at")
                .HasDefaultValueSql("now()");

                entity.Property(e => e.modifyBy)
                    .HasMaxLength(7)
                    .HasColumnName("modify_by");

                entity.Property(e => e.capacity)
                    .HasColumnName("capacity")
                    .HasComment("number of beds in ward");
                entity.Property(e => e.department)
                    .HasColumnName("deparment");

            });
            modelBuilder.Entity<MedicalAdministration>(entity =>
            {
                entity.ToTable("medical_administration");

                entity.HasKey(e => e.Id)
                .HasName("medical_pkey");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.healthId).HasColumnName("health_id");
                entity.Property(e => e.inhospId).HasColumnName("inhosp_id");
                entity.Property(e => e.medicalType).HasColumnName("medical_type");
                entity.Property(e => e.medCode).HasColumnName("med_code");
                entity.Property(e => e.medDes).HasColumnName("med_des");
                entity.Property(e => e.milkAmount).HasColumnName("milk_amount");
                entity.Property(e => e.shift).HasColumnName("shift");
                entity.Property(e => e.admisteredAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("modify_at")
                .HasDefaultValueSql("now()");

                entity.Property(e => e.administeredBy)
                   .HasMaxLength(7)
                   .HasColumnName("administered_by");

            });
            modelBuilder.Entity<InpatientReservation>(entity =>
            {
                entity.ToTable("inptient_reservation");
                entity.HasKey(e => new
                {
                    e.inhospId,
                    e.reserveDate,
                    e.healthId
                })
                .HasName("reservation_pkey");
                entity.Property(entity => entity.inhospId).HasColumnName("inhospid").HasMaxLength(20);
                entity.Property(entity => entity.healthId).HasColumnName("healthid").HasMaxLength(20);
                entity.Property(e => e.bedId).HasColumnName("bed_id");
                entity.Property(e => e.reserveDate).HasColumnName("reservation_date");

                entity.Property(e => e.createAt)
                  .HasColumnType("timestamp without time zone")
                  .HasColumnName("create_at")
                  .HasDefaultValueSql("now()");

                entity.Property(e => e.createBy)
                    .HasMaxLength(7)
                    .HasColumnName("create_by");

                entity.Property(e => e.modifyAt)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("modify_at")
                    .HasDefaultValueSql("now()");
                entity.Property(e => e.dischargeDate)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("discharge_date")
                    .HasDefaultValueSql("now()");

                entity.Property(e => e.modifyBy)
                    .HasMaxLength(7)
                    .HasColumnName("modify_by");
                entity.Property(e => e.dischargeApprover)
                    .HasMaxLength(7)
                    .HasColumnName("discharge_approver");
                entity.Property(e => e.wardId).HasColumnName("ward_id");

            });

            modelBuilder.Entity<radiologyexamrequest>(entity =>
            {
                entity.ToTable("radiologyexamrequests");
                entity.HasCheckConstraint(
                    "ck_radiologyexamrequests_status",
                    "status IN ('Ordered','Scheduled','In_Progress','Interpreted','Finalized','Cancelled','PENDING','SCHEDULED','COMPLETED','FINALIZED','CANCELLED')");
            });

            modelBuilder.Entity<radiologyreport>(entity =>
            {
                entity.ToTable("radiologyreports");
                entity.Property(e => e.structured_findings)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb");
                entity.Property(e => e.updatedat)
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("now()");
                entity.Property(e => e.xmin)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
                entity.HasIndex(e => e.radrequestid);
                entity.HasIndex(e => e.structured_findings)
                    .HasMethod("gin");
            });

            modelBuilder.Entity<radiologyreportversion>(entity =>
            {
                entity.ToTable("radiologyreport_versions");
                entity.HasKey(e => e.version_id);
                entity.Property(e => e.snapshot).HasColumnType("jsonb");
                entity.Property(e => e.created_at).HasColumnType("timestamp with time zone");
                entity.HasIndex(e => new { e.report_id, e.version_no }).IsUnique();
            });

            modelBuilder.Entity<radiologyreportaudittrail>(entity =>
            {
                entity.ToTable("radiology_report_audit_trails");
                entity.HasKey(e => e.audit_id);
                entity.Property(e => e.payload).HasColumnType("jsonb");
                entity.Property(e => e.created_at).HasColumnType("timestamp with time zone");
                entity.HasIndex(e => e.payload).HasMethod("gin");
            });

            modelBuilder.HasSequence("hisorderplan_soa_soaid_seq");


            ConfigureBloodBankEntities(modelBuilder);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}
