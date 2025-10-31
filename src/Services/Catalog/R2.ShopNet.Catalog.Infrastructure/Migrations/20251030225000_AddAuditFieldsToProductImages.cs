using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R2.ShopNet.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add audit fields to ProductImages table
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "ProductImages",
                type: "timestamp with time zone",
                nullable: true);

            // Add soft delete fields to ProductImages table
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "catalog",
                table: "ProductImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "catalog",
                table: "ProductImages",
                type: "timestamp with time zone",
                nullable: true);

            // Add index for IsDeleted
            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_IsDeleted",
                schema: "catalog",
                table: "ProductImages",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_IsDeleted",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "catalog",
                table: "ProductImages");
        }
    }
}
