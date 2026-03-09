using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithCurrentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHeaderName",
                table: "SystemEndpoints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Integrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "IntegrationRuns",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "IntegrationRuns",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "IntegrationRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAt",
                table: "IntegrationRuns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "IntegrationRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyHeaderName",
                table: "SystemEndpoints");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "IntegrationRuns");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "IntegrationRuns");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "IntegrationRuns");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "IntegrationRuns");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "IntegrationRuns",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
