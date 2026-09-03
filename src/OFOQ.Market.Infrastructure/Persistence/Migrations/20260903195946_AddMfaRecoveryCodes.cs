using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaRecoveryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_mfa_recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_mfa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_mfa_recovery_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_mfa_recovery_codes_user_mfa_id",
                        column: x => x.user_mfa_id,
                        principalTable: "user_mfa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_recovery_codes_user_mfa_id",
                table: "user_mfa_recovery_codes",
                column: "user_mfa_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_mfa_recovery_codes_mfa_hash",
                table: "user_mfa_recovery_codes",
                columns: new[] { "user_mfa_id", "code_hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_mfa_recovery_codes");
        }
    }
}
