using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoGarageManager.Migrations
{
    public partial class GarageBusinessFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "assigned_employee", table: "repair_orders", type: "nvarchar(150)", maxLength: 150, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "completed_date", table: "repair_orders", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "diagnosis", table: "repair_orders", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "estimated_completion_date", table: "repair_orders", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "problem_description", table: "repair_orders", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "received_date", table: "repair_orders", type: "datetime2", nullable: false, defaultValueSql: "GETDATE()");
            migrationBuilder.AddColumn<string>(name: "technical_note", table: "repair_orders", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "technician_name", table: "repair_orders", type: "nvarchar(150)", maxLength: 150, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "vehicle_condition", table: "repair_orders", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<decimal>(name: "labor_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "part_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "discount_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "vat_percent", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "vat_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "paid_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "remaining_amount", table: "invoices", type: "decimal(18,2)", nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<int>(name: "customer_id", table: "warranties", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "car_id", table: "warranties", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "service_id", table: "warranties", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "spare_part_id", table: "warranties", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "invoice_id", table: "warranties", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "status", table: "warranties", type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Còn hạn");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "assigned_employee", table: "repair_orders");
            migrationBuilder.DropColumn(name: "completed_date", table: "repair_orders");
            migrationBuilder.DropColumn(name: "diagnosis", table: "repair_orders");
            migrationBuilder.DropColumn(name: "estimated_completion_date", table: "repair_orders");
            migrationBuilder.DropColumn(name: "problem_description", table: "repair_orders");
            migrationBuilder.DropColumn(name: "received_date", table: "repair_orders");
            migrationBuilder.DropColumn(name: "technical_note", table: "repair_orders");
            migrationBuilder.DropColumn(name: "technician_name", table: "repair_orders");
            migrationBuilder.DropColumn(name: "vehicle_condition", table: "repair_orders");
            migrationBuilder.DropColumn(name: "labor_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "part_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "discount_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "vat_percent", table: "invoices");
            migrationBuilder.DropColumn(name: "vat_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "paid_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "remaining_amount", table: "invoices");
            migrationBuilder.DropColumn(name: "customer_id", table: "warranties");
            migrationBuilder.DropColumn(name: "car_id", table: "warranties");
            migrationBuilder.DropColumn(name: "service_id", table: "warranties");
            migrationBuilder.DropColumn(name: "spare_part_id", table: "warranties");
            migrationBuilder.DropColumn(name: "invoice_id", table: "warranties");
            migrationBuilder.DropColumn(name: "status", table: "warranties");
        }
    }
}
