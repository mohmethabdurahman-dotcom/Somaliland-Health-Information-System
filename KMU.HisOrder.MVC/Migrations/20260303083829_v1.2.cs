using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class v12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bodyPart",
                table: "radiologyexamrequests");

            migrationBuilder.RenameColumn(
                name: "modality",
                table: "radiologyexamrequests",
                newName: "item_id");

            migrationBuilder.AddColumn<int>(
                name: "roomid",
                table: "radiologyexamrequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
                name: "roomid",
                table: "radiologyexamrequests");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "radiologyexamrequests",
                newName: "modality");

            migrationBuilder.AddColumn<string>(
                name: "bodyPart",
                table: "radiologyexamrequests",
                type: "text",
                nullable: true);

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
