using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaSessionReopenPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_mfa_verified_at_utc",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reopen_policy",
                table: "user_mfa",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "EveryBrowserSession");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_mfa_verified_at_utc",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "reopen_policy",
                table: "user_mfa");
        }
    }
}
