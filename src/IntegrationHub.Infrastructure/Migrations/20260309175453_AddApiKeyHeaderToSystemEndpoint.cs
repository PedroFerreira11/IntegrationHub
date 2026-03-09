using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyHeaderToSystemEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHeaderName",
                table: "SystemEndpoints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyHeaderName",
                table: "SystemEndpoints");
        }
    }
}
