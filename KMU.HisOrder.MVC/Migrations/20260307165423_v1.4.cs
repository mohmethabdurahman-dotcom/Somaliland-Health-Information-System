using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class v14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "studyinstanceuid",
                table: "radiologyreports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "accessionnumber",
                table: "radiologyexamrequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "orthancstudyid",
                table: "radiologyexamrequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "studyinstanceuid",
                table: "radiologyexamrequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "technicianid",
                table: "radiologyexamrequests",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "showseq",
                table: "kmu_medpathway",
                type: "numeric(3)",
                precision: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,0)",
                oldPrecision: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "showseq",
                table: "kmu_medfrequency_ind",
                type: "numeric(3)",
                precision: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,0)",
                oldPrecision: 3,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "studyinstanceuid",
                table: "radiologyreports");

            migrationBuilder.DropColumn(
                name: "accessionnumber",
                table: "radiologyexamrequests");

            migrationBuilder.DropColumn(
                name: "orthancstudyid",
                table: "radiologyexamrequests");

            migrationBuilder.DropColumn(
                name: "studyinstanceuid",
                table: "radiologyexamrequests");

            migrationBuilder.DropColumn(
                name: "technicianid",
                table: "radiologyexamrequests");

            migrationBuilder.AlterColumn<decimal>(
                name: "showseq",
                table: "kmu_medpathway",
                type: "numeric(3,0)",
                precision: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(3)",
                oldPrecision: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "showseq",
                table: "kmu_medfrequency_ind",
                type: "numeric(3,0)",
                precision: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(3)",
                oldPrecision: 3,
                oldNullable: true);
        }
    }
}
