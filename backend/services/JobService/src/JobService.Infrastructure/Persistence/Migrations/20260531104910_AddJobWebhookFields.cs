using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWebhookFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebhookSecret",
                table: "Jobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookUrl",
                table: "Jobs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookSecret",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Jobs");
        }
    }
}
