using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_NMCNPM_QuanLyChamSocThuCung.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseCoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Service",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Service",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Pet",
                type: "varchar(508)",
                unicode: false,
                maxLength: 508,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Invoice",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "Invoice",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurchargeReason",
                table: "Invoice",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Appointment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Account",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Service_DurationMinutes",
                table: "Service",
                sql: "[DurationMinutes] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Service_DurationMinutes",
                table: "Service");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Service");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Service");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Pet");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "SurchargeReason",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Appointment");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Account");
        }
    }
}
