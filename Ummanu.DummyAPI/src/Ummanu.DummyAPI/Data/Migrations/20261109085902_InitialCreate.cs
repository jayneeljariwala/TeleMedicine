using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ummanu.DummyAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    disposition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nhs_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dx_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    dos_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    urgency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_clinician_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waiting_room_position = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_disposition_id",
                table: "appointments",
                column: "disposition_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments");
        }
    }
}
