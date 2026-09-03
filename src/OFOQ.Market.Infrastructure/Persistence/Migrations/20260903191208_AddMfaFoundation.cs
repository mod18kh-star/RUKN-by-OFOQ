using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_mfa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_secret = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    enabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_accepted_time_step = table.Column<long>(type: "bigint", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_mfa", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_mfa_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_user_mfa_user_id",
                table: "user_mfa",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_mfa");
        }
    }
}
