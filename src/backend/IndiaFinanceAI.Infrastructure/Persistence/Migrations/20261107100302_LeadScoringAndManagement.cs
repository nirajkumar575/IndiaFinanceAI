using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndiaFinanceAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeadScoringAndManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CallbackRequested",
                table: "CustomerLeads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FollowUpDate",
                table: "CustomerLeads",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpNotes",
                table: "CustomerLeads",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadSource",
                table: "CustomerLeads",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Website");

            migrationBuilder.AddColumn<string>(
                name: "PreferredContactTime",
                table: "CustomerLeads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLeads_LoanType_LeadStatus",
                table: "CustomerLeads",
                columns: new[] { "LoanType", "LeadStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerLeads_LoanType_LeadStatus",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "CallbackRequested",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "FollowUpNotes",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "LeadSource",
                table: "CustomerLeads");

            migrationBuilder.DropColumn(
                name: "PreferredContactTime",
                table: "CustomerLeads");
        }
    }
}
