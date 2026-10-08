using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class TicketSupportDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTicketSupport",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [Departments] SET [IsTicketSupport] = 1 WHERE [Id] = (SELECT TOP (1) [Id] FROM [Departments] WHERE [Name] = N'Sistem Geliştirme' AND [IsDeleted] = 0 ORDER BY [Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTicketSupport",
                table: "Departments");
        }
    }
}
