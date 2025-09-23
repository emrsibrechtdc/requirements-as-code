using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Platform.Locations.Infrastructure.Data;

namespace Platform.Locations.Infrastructure.Migrations;

[DbContext(typeof(LocationsDbContext))]
partial class LocationsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.7")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("Platform.Locations.Domain.Locations.Location", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier");

            b.Property<string>("AddressLine1")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<string>("AddressLine2")
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<string>("City")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<string>("Country")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("CreatedBy")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.Property<DateTime?>("DeletedAt")
                .HasColumnType("datetime2");

            b.Property<string>("DeletedBy")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.Property<bool>("IsActive")
                .HasColumnType("bit");

            b.Property<string>("LocationCode")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<string>("LocationTypeCode")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");

            b.Property<string>("LocationTypeName")
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<string>("Product")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<string>("State")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<DateTime?>("UpdatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("UpdatedBy")
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            b.Property<string>("ZipCode")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");

            b.HasKey("Id");

            b.HasIndex("IsActive")
                .HasDatabaseName("IX_Locations_IsActive");

            b.HasIndex("LocationCode")
                .IsUnique()
                .HasDatabaseName("IX_Locations_LocationCode");

            b.HasIndex("LocationTypeCode")
                .HasDatabaseName("IX_Locations_LocationTypeCode");

            b.HasIndex("Product")
                .HasDatabaseName("IX_Locations_Product");

            b.HasIndex("Product", "LocationCode")
                .IsUnique()
                .HasDatabaseName("IX_Locations_Product_LocationCode");

            b.ToTable("Locations", (string)null);
        });
    }
}