using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R2.ShopNet.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMinIOFieldsToProductImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                comment: "Legacy field - use ObjectKey instead",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                comment: "MIME type (e.g., image/jpeg)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "catalog",
                table: "ProductImages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "Upload timestamp");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                comment: "Original filename");

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                comment: "MinIO object key (full path in bucket)");

            migrationBuilder.AddColumn<long>(
                name: "SizeInBytes",
                schema: "catalog",
                table: "ProductImages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "File size in bytes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_CreatedAt",
                schema: "catalog",
                table: "ProductImages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ObjectKey",
                schema: "catalog",
                table: "ProductImages",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_CreatedAt",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ObjectKey",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ContentType",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "SizeInBytes",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "ProductImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldComment: "Legacy field - use ObjectKey instead");
        }
    }
}
