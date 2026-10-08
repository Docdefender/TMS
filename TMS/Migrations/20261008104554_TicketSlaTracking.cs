using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class TicketSlaTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaBreachedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaDueAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Tickets] SET [SlaDueAt] = DATEADD(hour, 72, [CreatedAt]);");
            migrationBuilder.Sql("UPDATE [Tickets] SET [SlaBreachedAt] = [UpdatedAt] WHERE [Status] IN (3, 4) AND [UpdatedAt] > [SlaDueAt];");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SupportDepartmentId_SlaDueAt",
                table: "Tickets",
                columns: new[] { "SupportDepartmentId", "SlaDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_SupportDepartmentId_SlaDueAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SlaBreachedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SlaDueAt",
                table: "Tickets");
        }
    }
}
