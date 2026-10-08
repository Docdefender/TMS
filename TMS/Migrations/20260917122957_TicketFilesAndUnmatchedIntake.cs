using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations;

public partial class TicketFilesAndUnmatchedIntake : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsInternal", table: "Attachments", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>(name: "TicketId", table: "Attachments", type: "int", nullable: true);
        migrationBuilder.CreateTable(
            name: "TicketIntakes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SenderEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ExternalMessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                SupportDepartmentId = table.Column<int>(type: "int", nullable: false),
                MatchedTicketId = table.Column<int>(type: "int", nullable: true),
                MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                MatchedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketIntakes", x => x.Id);
                table.ForeignKey("FK_TicketIntakes_Departments_SupportDepartmentId", x => x.SupportDepartmentId, "Departments", "Id");
                table.ForeignKey("FK_TicketIntakes_Tickets_MatchedTicketId", x => x.MatchedTicketId, "Tickets", "Id");
            });
        migrationBuilder.CreateIndex(name: "IX_Attachments_TicketId", table: "Attachments", column: "TicketId");
        migrationBuilder.CreateIndex(name: "IX_TicketIntakes_ExternalMessageId", table: "TicketIntakes", column: "ExternalMessageId", unique: true, filter: "[ExternalMessageId] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_TicketIntakes_MatchedTicketId", table: "TicketIntakes", column: "MatchedTicketId");
        migrationBuilder.CreateIndex(name: "IX_TicketIntakes_SupportDepartmentId_MatchedTicketId", table: "TicketIntakes", columns: new[] { "SupportDepartmentId", "MatchedTicketId" });
        migrationBuilder.AddForeignKey(name: "FK_Attachments_Tickets_TicketId", table: "Attachments", column: "TicketId", principalTable: "Tickets", principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Attachments_Tickets_TicketId", table: "Attachments");
        migrationBuilder.DropTable(name: "TicketIntakes");
        migrationBuilder.DropIndex(name: "IX_Attachments_TicketId", table: "Attachments");
        migrationBuilder.DropColumn(name: "IsInternal", table: "Attachments");
        migrationBuilder.DropColumn(name: "TicketId", table: "Attachments");
    }
}
