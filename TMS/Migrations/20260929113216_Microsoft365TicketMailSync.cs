using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class Microsoft365TicketMailSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalConversationId",
                table: "Tickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceMailboxAddress",
                table: "Tickets",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalConversationId",
                table: "TicketMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "TicketMessages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalConversationId",
                table: "TicketIntakes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "TicketIntakes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceMailboxAddress",
                table: "TicketIntakes",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketMailboxSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MailboxAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    SyncLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessfulSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMailboxSyncStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SourceMailboxAddress_ExternalConversationId",
                table: "Tickets",
                columns: new[] { "SourceMailboxAddress", "ExternalConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketMailboxSyncStates_MailboxAddress",
                table: "TicketMailboxSyncStates",
                column: "MailboxAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketMailboxSyncStates");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SourceMailboxAddress_ExternalConversationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalConversationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SourceMailboxAddress",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ExternalConversationId",
                table: "TicketMessages");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "TicketMessages");

            migrationBuilder.DropColumn(
                name: "ExternalConversationId",
                table: "TicketIntakes");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "TicketIntakes");

            migrationBuilder.DropColumn(
                name: "SourceMailboxAddress",
                table: "TicketIntakes");
        }
    }
}
