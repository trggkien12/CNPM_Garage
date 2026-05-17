using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoGarageManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    position = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "spare_parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    stock_quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_spare_parts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warranties",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customer_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    serial_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    purchase_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_warranties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cars",
                columns: table => new
                {
                    car_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    license_plate = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    year = table.Column<int>(type: "int", nullable: false),
                    customer_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_cars", x => x.car_id);
                    table.ForeignKey(
                        name: "f_k_cars__customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repair_orders",
                columns: table => new
                {
                    repair_order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    car_id = table.Column<int>(type: "int", nullable: false),
                    repair_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_repair_orders", x => x.repair_order_id);
                    table.ForeignKey(
                        name: "f_k_repair_orders_cars_car_id",
                        column: x => x.car_id,
                        principalTable: "cars",
                        principalColumn: "car_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    invoice_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    repair_order_id = table.Column<int>(type: "int", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_invoices", x => x.invoice_id);
                    table.ForeignKey(
                        name: "f_k_invoices__repair_orders_repair_order_id",
                        column: x => x.repair_order_id,
                        principalTable: "repair_orders",
                        principalColumn: "repair_order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repair_details",
                columns: table => new
                {
                    repair_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    repair_order_id = table.Column<int>(type: "int", nullable: false),
                    service_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_repair_details", x => x.repair_detail_id);
                    table.ForeignKey(
                        name: "f_k_repair_details__repair_orders_repair_order_id",
                        column: x => x.repair_order_id,
                        principalTable: "repair_orders",
                        principalColumn: "repair_order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_repair_details__services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repair_parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    repair_order_id = table.Column<int>(type: "int", nullable: false),
                    spare_part_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_repair_parts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_repair_parts__spare_parts_spare_part_id",
                        column: x => x.spare_part_id,
                        principalTable: "spare_parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_repair_parts_repair_orders_repair_order_id",
                        column: x => x.repair_order_id,
                        principalTable: "repair_orders",
                        principalColumn: "repair_order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payment_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_payments", x => x.payment_id);
                    table.ForeignKey(
                        name: "f_k_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_cars_customer_id",
                table: "cars",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_cars_license_plate",
                table: "cars",
                column: "license_plate");

            migrationBuilder.CreateIndex(
                name: "idx_customers_email",
                table: "customers",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_customers_phone_number",
                table: "customers",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "i_x_invoices_repair_order_id",
                table: "invoices",
                column: "repair_order_id");

            migrationBuilder.CreateIndex(
                name: "i_x_payments_invoice_id",
                table: "payments",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "i_x_repair_details_repair_order_id",
                table: "repair_details",
                column: "repair_order_id");

            migrationBuilder.CreateIndex(
                name: "i_x_repair_details_service_id",
                table: "repair_details",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "i_x_repair_orders_car_id",
                table: "repair_orders",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "idx_repair_orders_status",
                table: "repair_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_repair_parts_repair_order_id",
                table: "repair_parts",
                column: "repair_order_id");

            migrationBuilder.CreateIndex(
                name: "i_x_repair_parts_spare_part_id",
                table: "repair_parts",
                column: "spare_part_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "repair_details");

            migrationBuilder.DropTable(
                name: "repair_parts");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "warranties");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "spare_parts");

            migrationBuilder.DropTable(
                name: "repair_orders");

            migrationBuilder.DropTable(
                name: "cars");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
