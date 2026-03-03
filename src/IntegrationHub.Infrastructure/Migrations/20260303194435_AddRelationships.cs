using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegrationRun",
                table: "IntegrationRun");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegrationLog",
                table: "IntegrationLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Integration",
                table: "Integration");

            migrationBuilder.RenameTable(
                name: "IntegrationRun",
                newName: "IntegrationRuns");

            migrationBuilder.RenameTable(
                name: "IntegrationLog",
                newName: "IntegrationLogs");

            migrationBuilder.RenameTable(
                name: "Integration",
                newName: "Integrations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegrationRuns",
                table: "IntegrationRuns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegrationLogs",
                table: "IntegrationLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Integrations",
                table: "Integrations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRuns_IntegrationId",
                table: "IntegrationRuns",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_IntegrationRunId",
                table: "IntegrationLogs",
                column: "IntegrationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_SourceEndpointId",
                table: "Integrations",
                column: "SourceEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_TargetEndpointId",
                table: "Integrations",
                column: "TargetEndpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationLogs_IntegrationRuns_IntegrationRunId",
                table: "IntegrationLogs",
                column: "IntegrationRunId",
                principalTable: "IntegrationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationRuns_Integrations_IntegrationId",
                table: "IntegrationRuns",
                column: "IntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_SystemEndpoints_SourceEndpointId",
                table: "Integrations",
                column: "SourceEndpointId",
                principalTable: "SystemEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_SystemEndpoints_TargetEndpointId",
                table: "Integrations",
                column: "TargetEndpointId",
                principalTable: "SystemEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationLogs_IntegrationRuns_IntegrationRunId",
                table: "IntegrationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationRuns_Integrations_IntegrationId",
                table: "IntegrationRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_SystemEndpoints_SourceEndpointId",
                table: "Integrations");

            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_SystemEndpoints_TargetEndpointId",
                table: "Integrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Integrations",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_SourceEndpointId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_TargetEndpointId",
                table: "Integrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegrationRuns",
                table: "IntegrationRuns");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationRuns_IntegrationId",
                table: "IntegrationRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IntegrationLogs",
                table: "IntegrationLogs");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationLogs_IntegrationRunId",
                table: "IntegrationLogs");

            migrationBuilder.RenameTable(
                name: "Integrations",
                newName: "Integration");

            migrationBuilder.RenameTable(
                name: "IntegrationRuns",
                newName: "IntegrationRun");

            migrationBuilder.RenameTable(
                name: "IntegrationLogs",
                newName: "IntegrationLog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Integration",
                table: "Integration",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegrationRun",
                table: "IntegrationRun",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IntegrationLog",
                table: "IntegrationLog",
                column: "Id");
        }
    }
}
