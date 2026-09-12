using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantVerificationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_merchant_verification_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<int>(type: "integer", nullable: false),
                    country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_merchant_verification_profiles", x => x.id);
                    table.UniqueConstraint("ak_commerce_merchant_verification_profiles_tenant_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_commerce_merchant_verification_profiles_country_code", "char_length(country_code) = 2");
                    table.CheckConstraint("ck_commerce_merchant_verification_profiles_legal_name", "char_length(btrim(legal_name)) > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_profiles_status", "status > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_profiles_subject_type", "subject_type > 0");
                    table.ForeignKey(
                        name: "fk_commerce_merchant_verification_profiles_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_merchant_verification_profiles_users",
                        column: x => x.principal_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_merchant_verification_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    issuing_country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protected_document_number = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    document_number_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    review_status = table.Column<int>(type: "integer", nullable: false),
                    review_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_merchant_verification_documents", x => x.id);
                    table.UniqueConstraint("ak_commerce_merchant_verification_documents_tenant_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_country_code", "char_length(issuing_country_code) = 2");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_dates", "issue_date IS NULL OR expiry_date IS NULL OR expiry_date >= issue_date");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_fingerprint", "char_length(document_number_fingerprint) = 64");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_holder_name", "char_length(btrim(holder_name)) > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_protected_number", "char_length(btrim(protected_document_number)) > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_review_status", "review_status > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_documents_type", "document_type > 0");
                    table.ForeignKey(
                        name: "fk_commerce_merchant_verification_documents_profiles",
                        columns: x => new { x.tenant_id, x.profile_id },
                        principalTable: "commerce_merchant_verification_profiles",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_merchant_verification_documents_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_merchant_verification_document_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_merchant_verification_document_files", x => x.id);
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_content_type", "char_length(btrim(content_type)) > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_file_name", "char_length(btrim(original_file_name)) > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_sha256", "char_length(sha256) = 64");
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_side", "side > 0");
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_size", "file_size_bytes > 0 AND file_size_bytes <= 15728640");
                    table.CheckConstraint("ck_commerce_merchant_verification_document_files_storage_key", "char_length(btrim(storage_key)) > 0");
                    table.ForeignKey(
                        name: "fk_commerce_merchant_verification_document_files_documents",
                        columns: x => new { x.tenant_id, x.document_id },
                        principalTable: "commerce_merchant_verification_documents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_document_files_document_side",
                table: "commerce_merchant_verification_document_files",
                columns: new[] { "tenant_id", "document_id", "side" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_document_files_sha256",
                table: "commerce_merchant_verification_document_files",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_merchant_verification_document_files_document_sha256",
                table: "commerce_merchant_verification_document_files",
                columns: new[] { "tenant_id", "document_id", "sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_merchant_verification_document_files_storage_key",
                table: "commerce_merchant_verification_document_files",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_documents_expiry_date",
                table: "commerce_merchant_verification_documents",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_documents_fingerprint",
                table: "commerce_merchant_verification_documents",
                column: "document_number_fingerprint");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_documents_profile_type",
                table: "commerce_merchant_verification_documents",
                columns: new[] { "tenant_id", "profile_id", "document_type" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_documents_review_status",
                table: "commerce_merchant_verification_documents",
                column: "review_status");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_merchant_verification_documents_profile_fingerprint",
                table: "commerce_merchant_verification_documents",
                columns: new[] { "tenant_id", "profile_id", "document_number_fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_profiles_principal_user",
                table: "commerce_merchant_verification_profiles",
                column: "principal_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_merchant_verification_profiles_status",
                table: "commerce_merchant_verification_profiles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_merchant_verification_profiles_tenant",
                table: "commerce_merchant_verification_profiles",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_merchant_verification_document_files");

            migrationBuilder.DropTable(
                name: "commerce_merchant_verification_documents");

            migrationBuilder.DropTable(
                name: "commerce_merchant_verification_profiles");
        }
    }
}
