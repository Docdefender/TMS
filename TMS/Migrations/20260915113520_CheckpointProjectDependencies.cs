using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class CheckpointProjectDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlockingCheckpointId",
                table: "ProjectRelations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRelations_BlockingCheckpointId",
                table: "ProjectRelations",
                column: "BlockingCheckpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectRelations_PipelineCheckpoints_BlockingCheckpointId",
                table: "ProjectRelations",
                column: "BlockingCheckpointId",
                principalTable: "PipelineCheckpoints",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectRelations_PipelineCheckpoints_BlockingCheckpointId",
                table: "ProjectRelations");

            migrationBuilder.DropIndex(
                name: "IX_ProjectRelations_BlockingCheckpointId",
                table: "ProjectRelations");

            migrationBuilder.DropColumn(
                name: "BlockingCheckpointId",
                table: "ProjectRelations");
        }
    }
}
