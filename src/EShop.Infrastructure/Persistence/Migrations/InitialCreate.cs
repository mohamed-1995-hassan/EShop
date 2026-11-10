using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShop.Infrastructure.Persistence.Migrations;

/// <summary>
/// Initial database migration - creates all core tables
/// Run: dotnet ef migrations add InitialCreate -p src/EShop.Infrastructure -s src/EShop.API
/// Apply: dotnet ef database update -p src/EShop.Infrastructure -s src/EShop.API
/// </summary>
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ========================================
        // CUSTOMERS TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "customers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customers", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_customers_email",
            table: "customers",
            column: "email",
            unique: true);

        // ========================================
        // PRODUCTS TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                stock_quantity = table.Column<int>(type: "integer", nullable: false),
                sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_products", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_products_sku",
            table: "products",
            column: "sku",
            unique: true);

        // ========================================
        // ORDERS TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                shipping_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                shipping_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                shipping_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                shipping_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_orders", x => x.id);
                table.ForeignKey(
                    name: "fk_orders_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_orders_customer_id",
            table: "orders",
            column: "customer_id");

        migrationBuilder.CreateIndex(
            name: "ix_orders_status",
            table: "orders",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_orders_order_date",
            table: "orders",
            column: "order_date");

        // ========================================
        // ORDER LINES TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "order_lines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                line_total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                line_total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_order_lines", x => x.id);
                table.ForeignKey(
                    name: "fk_order_lines_orders_order_id",
                    column: x => x.order_id,
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_order_lines_order_id",
            table: "order_lines",
            column: "order_id");

        // ========================================
        // USERS TABLE (Identity)
        // ========================================
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                lockout_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                access_failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_normalized_email",
            table: "users",
            column: "normalized_email",
            unique: true);

        // ========================================
        // REFRESH TOKENS TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                revoked_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                replaced_by_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id",
            table: "refresh_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token",
            table: "refresh_tokens",
            column: "token");

        // ========================================
        // OUTBOX MESSAGES TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                payload = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_processed_at",
            table: "outbox_messages",
            column: "processed_at",
            filter: "processed_at IS NULL");

        // ========================================
        // INBOX MESSAGES TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "inbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                message_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                consumer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_inbox_messages_message_id_consumer",
            table: "inbox_messages",
            columns: new[] { "message_id", "consumer" },
            unique: true);

        // ========================================
        // DEAD LETTER MESSAGES TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "dead_letter_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                original_message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                message_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                payload = table.Column<string>(type: "text", nullable: false),
                error = table.Column<string>(type: "text", nullable: false),
                queue_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                reprocessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_dead_letter_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_dead_letter_messages_reprocessed_at",
            table: "dead_letter_messages",
            column: "reprocessed_at",
            filter: "reprocessed_at IS NULL");

        // ========================================
        // ORDER PROCESSING SAGAS TABLE
        // ========================================
        migrationBuilder.CreateTable(
            name: "order_processing_sagas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                current_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                order_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                reservation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_order_processing_sagas", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_order_processing_sagas_order_id",
            table: "order_processing_sagas",
            column: "order_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "order_processing_sagas");
        migrationBuilder.DropTable(name: "dead_letter_messages");
        migrationBuilder.DropTable(name: "inbox_messages");
        migrationBuilder.DropTable(name: "outbox_messages");
        migrationBuilder.DropTable(name: "refresh_tokens");
        migrationBuilder.DropTable(name: "users");
        migrationBuilder.DropTable(name: "order_lines");
        migrationBuilder.DropTable(name: "orders");
        migrationBuilder.DropTable(name: "products");
        migrationBuilder.DropTable(name: "customers");
    }
}
