using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoGarageManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ tạo bảng appointment_services nếu bảng này chưa tồn tại.
            // Không đụng vào bảng/cột/index cũ để tránh lỗi DB không đồng bộ migration.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[appointment_services]', N'U') IS NULL
BEGIN
    CREATE TABLE [appointment_services] (
        [appointment_service_id] int NOT NULL IDENTITY(1,1),
        [appointment_id] int NOT NULL,
        [service_id] int NULL,
        [service_name] nvarchar(200) NOT NULL,
        [price] decimal(18,2) NOT NULL,
        CONSTRAINT [p_k_appointment_services] PRIMARY KEY ([appointment_service_id])
    );

    CREATE INDEX [i_x_appointment_services_appointment_id]
        ON [appointment_services] ([appointment_id]);

    CREATE INDEX [i_x_appointment_services_service_id]
        ON [appointment_services] ([service_id]);

    ALTER TABLE [appointment_services]
        ADD CONSTRAINT [f_k_appointment_services_appointments_appointment_id]
        FOREIGN KEY ([appointment_id])
        REFERENCES [appointments] ([appointment_id])
        ON DELETE CASCADE;

    ALTER TABLE [appointment_services]
        ADD CONSTRAINT [f_k_appointment_services_services_service_id]
        FOREIGN KEY ([service_id])
        REFERENCES [services] ([service_id])
        ON DELETE SET NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[appointment_services]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [appointment_services] DROP CONSTRAINT IF EXISTS [f_k_appointment_services_appointments_appointment_id];
    ALTER TABLE [appointment_services] DROP CONSTRAINT IF EXISTS [f_k_appointment_services_services_service_id];
    DROP TABLE [appointment_services];
END
");
        }
    }
}
