using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHasDataSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "DeletedAt", "DeletedByUserId", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Bug fixes and patches", false, "Bug Fix" },
                    { 2, null, null, "New feature development", false, "Feature" },
                    { 3, null, null, "Improvements to existing features", false, "Improvement" },
                    { 4, null, null, "Research and investigation", false, "Research" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "DeletedAt", "DeletedByUserId", "Description", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Software development and engineering", false, "Engineering" },
                    { 2, null, null, "Marketing and communications", false, "Marketing" },
                    { 3, null, null, "HR and people operations", false, "Human Resources" },
                    { 4, null, null, "Finance and accounting", false, "Finance" }
                });
        }
    }
}
