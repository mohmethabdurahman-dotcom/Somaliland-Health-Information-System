using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class radiology_hardening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "impression",
                table: "radiologyreports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "structured_findings",
                table: "radiologyreports",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "is_locked",
                table: "radiologyreports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "finalized_at",
                table: "radiologyreports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updatedat",
                table: "radiologyreports",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "radiologyreport_versions",
                columns: table => new
                {
                    version_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_id = table.Column<int>(type: "integer", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radiologyreport_versions", x => x.version_id);
                    table.ForeignKey(
                        name: "FK_radiologyreport_versions_radiologyreports_report_id",
                        column: x => x.report_id,
                        principalTable: "radiologyreports",
                        principalColumn: "reportid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "radiology_report_audit_trails",
                columns: table => new
                {
                    audit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_id = table.Column<int>(type: "integer", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radiology_report_audit_trails", x => x.audit_id);
                    table.ForeignKey(
                        name: "FK_radiology_report_audit_trails_radiologyreports_report_id",
                        column: x => x.report_id,
                        principalTable: "radiologyreports",
                        principalColumn: "reportid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_radiologyreports_structured_findings_gin",
                table: "radiologyreports",
                column: "structured_findings")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_radiologyreport_versions_report_id_version_no",
                table: "radiologyreport_versions",
                columns: new[] { "report_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_radiology_report_audit_trails_report_id",
                table: "radiology_report_audit_trails",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_radiology_report_audit_payload_gin",
                table: "radiology_report_audit_trails",
                column: "payload")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.Sql(
                "ALTER TABLE radiologyexamrequests ADD CONSTRAINT ck_radiologyexamrequests_status CHECK (status IN ('Ordered','Scheduled','In_Progress','Interpreted','Finalized','Cancelled','PENDING','SCHEDULED','COMPLETED','FINALIZED','CANCELLED'));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE radiologyexamrequests DROP CONSTRAINT IF EXISTS ck_radiologyexamrequests_status;");

            migrationBuilder.DropTable(name: "radiology_report_audit_trails");
            migrationBuilder.DropTable(name: "radiologyreport_versions");

            migrationBuilder.DropIndex(name: "ix_radiologyreports_structured_findings_gin", table: "radiologyreports");

            migrationBuilder.DropColumn(name: "impression", table: "radiologyreports");
            migrationBuilder.DropColumn(name: "structured_findings", table: "radiologyreports");
            migrationBuilder.DropColumn(name: "is_locked", table: "radiologyreports");
            migrationBuilder.DropColumn(name: "finalized_at", table: "radiologyreports");
            migrationBuilder.DropColumn(name: "updatedat", table: "radiologyreports");
        }
    }
}
