using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class PipelineHistoryProjectScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE audit
                SET ProjectId = stage.ProjectId
                FROM AuditLogs audit
                INNER JOIN PipelineStages stage ON stage.Id = audit.EntityId
                WHERE audit.EntityType = 'PipelineStage' AND audit.ProjectId IS NULL;

                UPDATE audit
                SET ProjectId = stage.ProjectId
                FROM AuditLogs audit
                INNER JOIN PipelineCheckpoints checkpointItem ON checkpointItem.Id = audit.EntityId
                INNER JOIN PipelineStages stage ON stage.Id = checkpointItem.PipelineStageId
                WHERE audit.EntityType = 'PipelineCheckpoint' AND audit.ProjectId IS NULL;

                UPDATE audit
                SET ProjectId = taskItem.ProjectId
                FROM AuditLogs audit
                INNER JOIN TaskItems taskItem ON taskItem.Id = audit.EntityId
                WHERE audit.EntityType = 'TaskItem' AND audit.ProjectId IS NULL;

                UPDATE audit
                SET ProjectId = relation.SourceProjectId
                FROM AuditLogs audit
                INNER JOIN ProjectRelations relation ON relation.Id = audit.EntityId
                WHERE audit.EntityType = 'ProjectRelation' AND audit.ProjectId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ProjectId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "ProjectId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Projects_ProjectId",
                table: "AuditLogs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Projects_ProjectId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ProjectId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "AuditLogs");
        }
    }
}
