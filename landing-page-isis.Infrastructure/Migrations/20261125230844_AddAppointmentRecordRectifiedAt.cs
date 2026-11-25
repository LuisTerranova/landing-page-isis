using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace landingpageisis.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentRecordRectifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rectified_at",
                table: "appointment_records",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rectified_at",
                table: "appointment_records");
        }
    }
}
