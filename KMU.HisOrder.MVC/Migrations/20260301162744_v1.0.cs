using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KMU.HisOrder.MVC.Migrations
{
    public partial class v10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {




      migrationBuilder.CreateTable(
    name: "radiologyexamrequests",
    columns: table => new
    {
        id = table.Column<int>(type: "integer", nullable: false)
            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
        orderplanid = table.Column<long>(type: "bigint", nullable: true),
        inhospid = table.Column<string>(type: "text", nullable: true),
        patientid = table.Column<string>(type: "text", nullable: true),
        seq_no = table.Column<int>(type: "integer", nullable: false),
        sourcetype = table.Column<string>(type: "text", nullable: true),
        requestedby = table.Column<string>(type: "text", nullable: true),
        modality = table.Column<string>(type: "text", nullable: true),
        bodyPart = table.Column<string>(type: "text", nullable: true),
        remark = table.Column<string>(type: "text", nullable: true),
        status = table.Column<string>(type: "text", nullable: true),
        ordereddate = table.Column<DateOnly>(type: "date", nullable: false),
        createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_radiologyexamrequests", x => x.id);
    });



            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    roomid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    department = table.Column<string>(type: "text", nullable: true),
                    modality = table.Column<string>(type: "text", nullable: false),
                    room_number = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.roomid);
                });

            migrationBuilder.CreateTable(
                name: "radiologyreports",
                columns: table => new
                {
                    reportid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    radrequestid = table.Column<int>(type: "integer", nullable: false),
                    inhospid = table.Column<string>(type: "text", nullable: false),
                    radiologistid = table.Column<string>(type: "text", nullable: false),
                    report_text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    signedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    radiologyexamrequestid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radiologyreports", x => x.reportid);
                    table.ForeignKey(
                        name: "FK_radiologyreports_radiologyexamrequests_radiologyexamrequest~",
                        column: x => x.radiologyexamrequestid,
                        principalTable: "radiologyexamrequests",
                        principalColumn: "id");
                });


            migrationBuilder.CreateIndex(
                name: "IX_radiologyreports_radiologyexamrequestid",
                table: "radiologyreports",
                column: "radiologyexamrequestid");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
         

            migrationBuilder.DropTable(
                name: "radiologyreports");

     

            migrationBuilder.DropTable(
                name: "rooms");

         

            migrationBuilder.DropTable(
                name: "radiologyexamrequests");

        }
    }
}
