using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R2.ShopNet.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Alternative text for accessibility"),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "MinIO object key (full path in bucket)"),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Original filename"),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "MIME type (e.g., image/jpeg)"),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false, comment: "File size in bytes")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryImages_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryImages_CategoryId",
                schema: "catalog",
                table: "CategoryImages",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryImages_CreatedAt",
                schema: "catalog",
                table: "CategoryImages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryImages_IsDeleted",
                schema: "catalog",
                table: "CategoryImages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryImages_ObjectKey",
                schema: "catalog",
                table: "CategoryImages",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryImages",
                schema: "catalog");
        }
    }
}
