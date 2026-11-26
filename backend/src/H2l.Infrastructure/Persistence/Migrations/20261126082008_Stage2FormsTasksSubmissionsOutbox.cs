using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace H2l.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stage2FormsTasksSubmissionsOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_versions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    schema_jsonb = table.Column<string>(type: "jsonb", nullable: false),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_versions_form_definitions_form_definition_id",
                        column: x => x.form_definition_id,
                        principalSchema: "app",
                        principalTable: "form_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_versions_users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "human_tasks",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_id = table.Column<Guid>(type: "uuid", nullable: true),
                    form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_task_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sensitive_data_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prefilled_data_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    encrypted_resume_url = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_human_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_human_tasks_form_versions_form_version_id",
                        column: x => x.form_version_id,
                        principalSchema: "app",
                        principalTable: "form_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_human_tasks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_human_tasks_users_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_human_tasks_users_claimed_by_user_id",
                        column: x => x.claimed_by_user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    human_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    public_data_jsonb = table.Column<string>(type: "jsonb", nullable: false),
                    encrypted_sensitive_data = table.Column<string>(type: "text", nullable: true),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_submissions_form_versions_form_version_id",
                        column: x => x.form_version_id,
                        principalSchema: "app",
                        principalTable: "form_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_submissions_human_tasks_human_task_id",
                        column: x => x.human_task_id,
                        principalSchema: "app",
                        principalTable: "human_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_submissions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_form_submissions_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalSchema: "app",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    human_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dead_letter_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_messages_form_submissions_form_submission_id",
                        column: x => x.form_submission_id,
                        principalSchema: "app",
                        principalTable: "form_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outbox_messages_human_tasks_human_task_id",
                        column: x => x.human_task_id,
                        principalSchema: "app",
                        principalTable: "human_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_outbox_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "submission_attachments",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    human_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    field_variable_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_encrypted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submission_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_submission_attachments_form_submissions_form_submission_id",
                        column: x => x.form_submission_id,
                        principalSchema: "app",
                        principalTable: "form_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submission_attachments_human_tasks_human_task_id",
                        column: x => x.human_task_id,
                        principalSchema: "app",
                        principalTable: "human_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_submission_attachments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_form_version_id",
                schema: "app",
                table: "form_submissions",
                column: "form_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_human_task_id",
                schema: "app",
                table: "form_submissions",
                column: "human_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_retention_until",
                schema: "app",
                table: "form_submissions",
                column: "retention_until",
                filter: "anonymized_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_submitted_by_user_id",
                schema: "app",
                table: "form_submissions",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_tenant_id_human_task_id_idempotency_key",
                schema: "app",
                table: "form_submissions",
                columns: new[] { "tenant_id", "human_task_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_form_definition_id",
                schema: "app",
                table: "form_versions",
                column: "form_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_published_by_user_id",
                schema: "app",
                table: "form_versions",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_tenant_id_form_definition_id_version_number",
                schema: "app",
                table: "form_versions",
                columns: new[] { "tenant_id", "form_definition_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_human_tasks_assigned_user_id",
                schema: "app",
                table: "human_tasks",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_human_tasks_claimed_by_user_id",
                schema: "app",
                table: "human_tasks",
                column: "claimed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_human_tasks_form_version_id",
                schema: "app",
                table: "human_tasks",
                column: "form_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_human_tasks_tenant_id_connector_id_external_task_id",
                schema: "app",
                table: "human_tasks",
                columns: new[] { "tenant_id", "connector_id", "external_task_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_human_tasks_tenant_id_status_expires_at",
                schema: "app",
                table: "human_tasks",
                columns: new[] { "tenant_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_form_submission_id",
                schema: "app",
                table: "outbox_messages",
                column: "form_submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_human_task_id",
                schema: "app",
                table: "outbox_messages",
                column: "human_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_next_attempt_at",
                schema: "app",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_tenant_id",
                schema: "app",
                table: "outbox_messages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_submission_attachments_form_submission_id",
                schema: "app",
                table: "submission_attachments",
                column: "form_submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_submission_attachments_human_task_id",
                schema: "app",
                table: "submission_attachments",
                column: "human_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_submission_attachments_tenant_id_human_task_id",
                schema: "app",
                table: "submission_attachments",
                columns: new[] { "tenant_id", "human_task_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "app");

            migrationBuilder.DropTable(
                name: "submission_attachments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "form_submissions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "human_tasks",
                schema: "app");

            migrationBuilder.DropTable(
                name: "form_versions",
                schema: "app");
        }
    }
}
