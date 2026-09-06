using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class v13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "orderid",
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
                name: "orderid",
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
