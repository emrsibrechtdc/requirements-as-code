using Microsoft.EntityFrameworkCore.Migrations;

namespace Platform.Locations.Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Locations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Product = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                LocationTypeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                LocationTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Locations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Locations_IsActive",
            table: "Locations",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Locations_LocationCode",
            table: "Locations",
            column: "LocationCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Locations_LocationTypeCode",
            table: "Locations",
            column: "LocationTypeCode");

        migrationBuilder.CreateIndex(
            name: "IX_Locations_Product",
            table: "Locations",
            column: "Product");

        migrationBuilder.CreateIndex(
            name: "IX_Locations_Product_LocationCode",
            table: "Locations",
            columns: new[] { "Product", "LocationCode" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Locations");
    }
}