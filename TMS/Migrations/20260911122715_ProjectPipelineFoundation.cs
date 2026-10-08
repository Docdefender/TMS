using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class ProjectPipelineFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PipelineCheckpointId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PipelineStageId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PipelineStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineStages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PipelineCheckpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PipelineStageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineCheckpoints_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PipelineCheckpoints_PipelineStages_PipelineStageId",
                        column: x => x.PipelineStageId,
                        principalTable: "PipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_PipelineCheckpointId",
                table: "TaskItems",
                column: "PipelineCheckpointId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_PipelineStageId",
                table: "TaskItems",
                column: "PipelineStageId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineCheckpoints_ApprovedByUserId",
                table: "PipelineCheckpoints",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineCheckpoints_PipelineStageId_SortOrder",
                table: "PipelineCheckpoints",
                columns: new[] { "PipelineStageId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_ProjectId_SortOrder",
                table: "PipelineStages",
                columns: new[] { "ProjectId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_PipelineCheckpoints_PipelineCheckpointId",
                table: "TaskItems",
                column: "PipelineCheckpointId",
                principalTable: "PipelineCheckpoints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_PipelineStages_PipelineStageId",
                table: "TaskItems",
                column: "PipelineStageId",
                principalTable: "PipelineStages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_PipelineCheckpoints_PipelineCheckpointId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_PipelineStages_PipelineStageId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "PipelineCheckpoints");

            migrationBuilder.DropTable(
                name: "PipelineStages");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_PipelineCheckpointId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_PipelineStageId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "PipelineCheckpointId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "PipelineStageId",
                table: "TaskItems");
        }
    }
}
