using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationAndAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletionBatchId",
                table: "TaskItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstAssignedByUserId",
                table: "TaskItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletionBatchId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstAssignedByUserId",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManagerDepartments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerDepartments", x => new { x.DepartmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ManagerDepartments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagerDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    PreviousUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskHistoryAccesses",
                columns: table => new
                {
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskHistoryAccesses", x => new { x.TaskItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TaskHistoryAccesses_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDepartments_DepartmentId",
                table: "ManagerDepartments",
                column: "DepartmentId",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDepartments_UserId",
                table: "ManagerDepartments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_TaskItemId",
                table: "TaskAssignments",
                column: "TaskItemId");
            // Preserve only explicit existing Manager -> primary department relations.
            // Multiple Managers require an Admin to select the default; never choose arbitrarily.
            migrationBuilder.Sql("""
                INSERT INTO ManagerDepartments (DepartmentId, UserId, IsDefault)
                SELECT u.DepartmentId, u.Id, 0 FROM AspNetUsers u
                INNER JOIN Departments d ON d.Id = u.DepartmentId AND d.IsDeleted = 0
                WHERE EXISTS (SELECT 1 FROM AspNetUserRoles ur INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                              WHERE ur.UserId = u.Id AND r.Name = 'Manager')
                  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                                  WHERE ur.UserId = u.Id AND r.Name = 'Admin');
                UPDATE md SET IsDefault = 1 FROM ManagerDepartments md
                WHERE (SELECT COUNT(*) FROM ManagerDepartments other WHERE other.DepartmentId = md.DepartmentId) = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerDepartments");

            migrationBuilder.DropTable(
                name: "TaskAssignments");

            migrationBuilder.DropTable(
                name: "TaskHistoryAccesses");

            migrationBuilder.DropColumn(
                name: "DeletionBatchId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "FirstAssignedByUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletionBatchId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FirstAssignedByUserId",
                table: "Projects");
        }
    }
}
