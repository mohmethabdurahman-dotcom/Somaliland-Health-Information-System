using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class v15 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "seq_no",
                table: "radiologyexamrequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "roomid",
                table: "radiologyexamrequests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ordereddate",
                table: "radiologyexamrequests",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

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
            migrationBuilder.AlterColumn<int>(
                name: "seq_no",
                table: "radiologyexamrequests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "roomid",
                table: "radiologyexamrequests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ordereddate",
                table: "radiologyexamrequests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

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
