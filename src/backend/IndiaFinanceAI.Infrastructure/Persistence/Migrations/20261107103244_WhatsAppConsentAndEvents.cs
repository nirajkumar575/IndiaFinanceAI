using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaFinanceAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppConsentAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastWhatsAppActivityAt",
                table: "CustomerLeads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWhatsAppStatus",
                table: "CustomerLeads",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConsentAt",
                table: "CustomerLeads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppConsentGiven",
                table: "CustomerLeads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppOptedOutAt",
                table: "CustomerLeads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WhatsAppCommunicationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerLeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppCommunicationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppCommunicationEvents_CustomerLeads_CustomerLeadId",
                        column: x => x.CustomerLeadId,
                        principalTable: "CustomerLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppCommunicationEvents_CustomerLeadId_CreatedAt",
                table: "WhatsAppCommunicationEvents",
                columns: new[] { "CustomerLeadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppCommunicationEvents_MessageId",
                table: "WhatsAppCommunicationEvents",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppCommunicationEvents");

            migrationBuilder.DropColumn(
                name: "LastWhatsAppActivityAt",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "LastWhatsAppStatus",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "WhatsAppConsentAt",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "WhatsAppConsentGiven",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptedOutAt",
                table: "CustomerLeads");
        }
    }
}
