using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoGarageManager.Migrations
{
    public partial class AddEmailOtpVerification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration an toàn: chỉ thêm các cột/bảng cần cho Email OTP nếu chưa tồn tại.
            // Không DROP/RENAME các cột cũ để tránh lỗi khi database hiện tại khác migration snapshot.

            migrationBuilder.Sql(@"
IF COL_LENGTH('customers', 'is_email_verified') IS NULL
BEGIN
    ALTER TABLE [customers] ADD [is_email_verified] bit NOT NULL CONSTRAINT [DF_customers_is_email_verified] DEFAULT CAST(1 AS bit);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[email_otps]', N'U') IS NULL
BEGIN
    CREATE TABLE [email_otps] (
        [id] int NOT NULL IDENTITY,
        [email] nvarchar(256) NOT NULL,
        [code] nvarchar(10) NOT NULL,
        [purpose] nvarchar(50) NOT NULL,
        [expires_at] datetime2 NOT NULL,
        [is_used] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [p_k_email_otps] PRIMARY KEY ([id])
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_email_otps_email_purpose' AND object_id = OBJECT_ID(N'[email_otps]'))
BEGIN
    CREATE INDEX [idx_email_otps_email_purpose] ON [email_otps] ([email], [purpose]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[email_otps]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [email_otps];
END
");
            migrationBuilder.Sql(@"
IF COL_LENGTH('customers', 'is_email_verified') IS NOT NULL
BEGIN
    ALTER TABLE [customers] DROP COLUMN [is_email_verified];
END
");
        }
    }
}
