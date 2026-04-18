using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_Hotels_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "flight");

            migrationBuilder.EnsureSchema(
                name: "hotels");

            migrationBuilder.EnsureSchema(
                name: "fleet");

            migrationBuilder.EnsureSchema(
                name: "geo");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "bus");

            migrationBuilder.EnsureSchema(
                name: "train");

            migrationBuilder.EnsureSchema(
                name: "tenants");

            migrationBuilder.CreateTable(
                name: "AircraftModels",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TypicalSeatCapacity = table.Column<int>(type: "int", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AircraftModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Airlines",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IcaoCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BedTypes",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeoSyncLogs",
                schema: "geo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    HttpStatus = table.Column<int>(type: "int", nullable: false),
                    ProvincesInserted = table.Column<int>(type: "int", nullable: false),
                    ProvincesUpdated = table.Column<int>(type: "int", nullable: false),
                    DistrictsInserted = table.Column<int>(type: "int", nullable: false),
                    DistrictsUpdated = table.Column<int>(type: "int", nullable: false),
                    WardsInserted = table.Column<int>(type: "int", nullable: false),
                    WardsUpdated = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HotelAmenities",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelAmenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hotels",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StarRating = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DefaultCheckInTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    DefaultCheckOutTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SeoTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SeoKeywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Robots = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SchemaJsonLd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PoliciesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hotels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StorageProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsCategories",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsRedirects",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ToPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    IsRegex = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsRedirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsTags",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                schema: "geo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegionCode = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomAmenities",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAmenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeatMaps",
                schema: "fleet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    TotalColumns = table.Column<int>(type: "int", nullable: false),
                    DeckCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LayoutVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeatLabelScheme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultRobots = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultOgImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultTwitterCard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultTwitterSite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultSchemaJsonLd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrandLogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HoldMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                schema: "fleet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CabinSeatMaps",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CabinClass = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    TotalColumns = table.Column<int>(type: "int", nullable: false),
                    LayoutVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeatLabelScheme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CabinSeatMaps", x => x.Id);
                    table.CheckConstraint("CK_flight_CabinSeatMaps_Cols", "[TotalColumns] > 0");
                    table.CheckConstraint("CK_flight_CabinSeatMaps_Rows", "[TotalRows] > 0");
                    table.ForeignKey(
                        name: "FK_CabinSeatMaps_AircraftModels_AircraftModelId",
                        column: x => x.AircraftModelId,
                        principalSchema: "flight",
                        principalTable: "AircraftModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Aircrafts",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aircrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aircrafts_AircraftModels_AircraftModelId",
                        column: x => x.AircraftModelId,
                        principalSchema: "flight",
                        principalTable: "AircraftModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aircrafts_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalSchema: "flight",
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AncillaryDefinitions",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AncillaryDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AncillaryDefinitions_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalSchema: "flight",
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FareClasses",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CabinClass = table.Column<int>(type: "int", nullable: false),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsChangeable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FareClasses_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalSchema: "flight",
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicies",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationPolicies_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CheckInOutRules",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CheckInFrom = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    CheckInTo = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    CheckOutFrom = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    CheckOutTo = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    AllowsEarlyCheckIn = table.Column<bool>(type: "bit", nullable: false),
                    AllowsLateCheckOut = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInOutRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInOutRules_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtraServices",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    Taxable = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtraServices_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HotelAmenityLinks",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelAmenityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelAmenityLinks_HotelAmenities_AmenityId",
                        column: x => x.AmenityId,
                        principalSchema: "hotels",
                        principalTable: "HotelAmenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HotelAmenityLinks_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HotelContacts",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelContacts_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HotelImages",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelImages_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HotelReviews",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsVerifiedStay = table.Column<bool>(type: "bit", nullable: false),
                    HelpfulCount = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotelReviews_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyPolicies",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyPolicies_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AreaSquareMeters = table.Column<int>(type: "int", nullable: true),
                    HasBalcony = table.Column<bool>(type: "bit", nullable: true),
                    HasWindow = table.Column<bool>(type: "bit", nullable: true),
                    SmokingAllowed = table.Column<bool>(type: "bit", nullable: true),
                    DefaultAdults = table.Column<int>(type: "int", nullable: false),
                    DefaultChildren = table.Column<int>(type: "int", nullable: false),
                    MaxAdults = table.Column<int>(type: "int", nullable: false),
                    MaxChildren = table.Column<int>(type: "int", nullable: false),
                    MaxGuests = table.Column<int>(type: "int", nullable: false),
                    TotalUnits = table.Column<int>(type: "int", nullable: false),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypes_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsPosts",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SeoTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SeoKeywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Robots = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OgDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OgType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwitterCard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwitterSite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TwitterCreator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TwitterTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TwitterDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TwitterImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SchemaJsonLd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnpublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EditorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastEditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    ReadingTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsPosts_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NewsPosts_AspNetUsers_EditorUserId",
                        column: x => x.EditorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NewsPosts_MediaAssets_CoverMediaAssetId",
                        column: x => x.CoverMediaAssetId,
                        principalSchema: "cms",
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "auth",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Effect = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "auth",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                schema: "geo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "geo",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                schema: "fleet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    ColumnIndex = table.Column<int>(type: "int", nullable: false),
                    DeckIndex = table.Column<int>(type: "int", nullable: false),
                    SeatType = table.Column<int>(type: "int", nullable: false),
                    SeatClass = table.Column<int>(type: "int", nullable: false),
                    IsAisle = table.Column<bool>(type: "bit", nullable: false),
                    IsWindow = table.Column<bool>(type: "bit", nullable: false),
                    PriceModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_SeatMaps_SeatMapId",
                        column: x => x.SeatMapId,
                        principalSchema: "fleet",
                        principalTable: "SeatMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantRoles",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRoles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenants",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsOwner = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenants",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CabinSeats",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CabinSeatMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    ColumnIndex = table.Column<int>(type: "int", nullable: false),
                    IsAisle = table.Column<bool>(type: "bit", nullable: false),
                    IsWindow = table.Column<bool>(type: "bit", nullable: false),
                    SeatClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CabinSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CabinSeats_CabinSeatMaps_CabinSeatMapId",
                        column: x => x.CabinSeatMapId,
                        principalSchema: "flight",
                        principalTable: "CabinSeatMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FareRules",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FareClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FareRules_FareClasses_FareClassId",
                        column: x => x.FareClassId,
                        principalSchema: "flight",
                        principalTable: "FareClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicyRules",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CancellationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CancelBeforeHours = table.Column<int>(type: "int", nullable: true),
                    CancelBeforeDays = table.Column<int>(type: "int", nullable: true),
                    ChargeType = table.Column<int>(type: "int", nullable: false),
                    ChargeValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationPolicyRules_CancellationPolicies_CancellationPolicyId",
                        column: x => x.CancellationPolicyId,
                        principalSchema: "hotels",
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtraServicePrices",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraServicePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtraServicePrices_ExtraServices_ExtraServiceId",
                        column: x => x.ExtraServiceId,
                        principalSchema: "hotels",
                        principalTable: "ExtraServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatePlans",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckInOutRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Refundable = table.Column<bool>(type: "bit", nullable: false),
                    BreakfastIncluded = table.Column<bool>(type: "bit", nullable: false),
                    MinNights = table.Column<int>(type: "int", nullable: true),
                    MaxNights = table.Column<int>(type: "int", nullable: true),
                    MinAdvanceDays = table.Column<int>(type: "int", nullable: true),
                    MaxAdvanceDays = table.Column<int>(type: "int", nullable: true),
                    RequiresGuarantee = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatePlans_CancellationPolicies_CancellationPolicyId",
                        column: x => x.CancellationPolicyId,
                        principalSchema: "hotels",
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RatePlans_CheckInOutRules_CheckInOutRuleId",
                        column: x => x.CheckInOutRuleId,
                        principalSchema: "hotels",
                        principalTable: "CheckInOutRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RatePlans_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatePlans_PropertyPolicies_PropertyPolicyId",
                        column: x => x.PropertyPolicyId,
                        principalSchema: "hotels",
                        principalTable: "PropertyPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryHolds",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Units = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryHolds_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomAmenityLinks",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAmenityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomAmenityLinks_RoomAmenities_AmenityId",
                        column: x => x.AmenityId,
                        principalSchema: "hotels",
                        principalTable: "RoomAmenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomAmenityLinks_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeBeds",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BedTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeBeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeBeds_BedTypes_BedTypeId",
                        column: x => x.BedTypeId,
                        principalSchema: "hotels",
                        principalTable: "BedTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomTypeBeds_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeImages",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeImages_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeInventories",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalUnits = table.Column<int>(type: "int", nullable: false),
                    SoldUnits = table.Column<int>(type: "int", nullable: false),
                    HeldUnits = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MinNights = table.Column<int>(type: "int", nullable: true),
                    MaxNights = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeInventories_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeMealPlans",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeMealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeMealPlans_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalSchema: "hotels",
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomTypeMealPlans_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypeOccupancyRules",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinAdults = table.Column<int>(type: "int", nullable: false),
                    MaxAdults = table.Column<int>(type: "int", nullable: false),
                    MinChildren = table.Column<int>(type: "int", nullable: false),
                    MaxChildren = table.Column<int>(type: "int", nullable: false),
                    MinGuests = table.Column<int>(type: "int", nullable: false),
                    MaxGuests = table.Column<int>(type: "int", nullable: false),
                    AllowsInfants = table.Column<bool>(type: "bit", nullable: false),
                    MaxInfants = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypeOccupancyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypeOccupancyRules_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypePolicies",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomTypePolicies_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsPostCategories",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPostCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsPostCategories_NewsCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "cms",
                        principalTable: "NewsCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsPostCategories_NewsPosts_PostId",
                        column: x => x.PostId,
                        principalSchema: "cms",
                        principalTable: "NewsPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsPostRevisions",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Robots = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OgDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TwitterCard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwitterTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TwitterDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TwitterImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SchemaJsonLd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EditorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPostRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsPostRevisions_AspNetUsers_EditorUserId",
                        column: x => x.EditorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NewsPostRevisions_NewsPosts_PostId",
                        column: x => x.PostId,
                        principalSchema: "cms",
                        principalTable: "NewsPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsPostTags",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPostTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsPostTags_NewsPosts_PostId",
                        column: x => x.PostId,
                        principalSchema: "cms",
                        principalTable: "NewsPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsPostTags_NewsTags_TagId",
                        column: x => x.TagId,
                        principalSchema: "cms",
                        principalTable: "NewsTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                schema: "geo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wards_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "geo",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantRolePermissions",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantRolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "auth",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantRolePermissions_TenantRoles_TenantRoleId",
                        column: x => x.TenantRoleId,
                        principalSchema: "tenants",
                        principalTable: "TenantRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantRolePermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenants",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantUserRoles",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantUserRoles_TenantRoles_TenantRoleId",
                        column: x => x.TenantRoleId,
                        principalSchema: "tenants",
                        principalTable: "TenantRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantUserRoles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "tenants",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatePlanPolicies",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlanPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatePlanPolicies_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalSchema: "hotels",
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatePlanRoomTypes",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlanRoomTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatePlanRoomTypes_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalSchema: "hotels",
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatePlanRoomTypes_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AirportIataCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AirportIcaoCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TrainStationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusStationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "geo",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "geo",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "geo",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyRates",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Taxes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Fees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyRates_RatePlanRoomTypes_RatePlanRoomTypeId",
                        column: x => x.RatePlanRoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RatePlanRoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromoRateOverrides",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromoCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromoCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OverridePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoRateOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoRateOverrides_RatePlanRoomTypes_RatePlanRoomTypeId",
                        column: x => x.RatePlanRoomTypeId,
                        principalSchema: "hotels",
                        principalTable: "RatePlanRoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Airports",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IcaoCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Airports_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "catalog",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RatingAverage = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    RatingCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Providers_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "geo",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "catalog",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "geo",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Providers_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "geo",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StopPoints",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopPoints_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "catalog",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StopPoints",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopPoints_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "catalog",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AircraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromAirportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToAirportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_Aircrafts_AircraftId",
                        column: x => x.AircraftId,
                        principalSchema: "flight",
                        principalTable: "Aircrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalSchema: "flight",
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Airports_FromAirportId",
                        column: x => x.FromAirportId,
                        principalSchema: "flight",
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Airports_ToAirportId",
                        column: x => x.ToAirportId,
                        principalSchema: "flight",
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "fleet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SeatMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeatCapacity = table.Column<int>(type: "int", nullable: false),
                    InServiceFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InServiceTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "catalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_SeatMaps_SeatMapId",
                        column: x => x.SeatMapId,
                        principalSchema: "fleet",
                        principalTable: "SeatMaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "fleet",
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromStopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DistanceKm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "catalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_StopPoints_FromStopPointId",
                        column: x => x.FromStopPointId,
                        principalSchema: "bus",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_StopPoints_ToStopPointId",
                        column: x => x.ToStopPointId,
                        principalSchema: "bus",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromStopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DistanceKm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_StopPoints_FromStopPointId",
                        column: x => x.FromStopPointId,
                        principalSchema: "train",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_StopPoints_ToStopPointId",
                        column: x => x.ToStopPointId,
                        principalSchema: "train",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FareClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxesFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SeatsAvailable = table.Column<int>(type: "int", nullable: false, defaultValue: 9),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.CheckConstraint("CK_flight_Offers_ExpiresAt", "[ExpiresAt] > [RequestedAt]");
                    table.CheckConstraint("CK_flight_Offers_TotalPrice", "[TotalPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_Offers_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalSchema: "flight",
                        principalTable: "Airlines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_FareClasses_FareClassId",
                        column: x => x.FareClassId,
                        principalSchema: "flight",
                        principalTable: "FareClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Flights_FlightId",
                        column: x => x.FlightId,
                        principalSchema: "flight",
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusVehicleDetails",
                schema: "fleet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AmenitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusVehicleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusVehicleDetails_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "fleet",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopIndex = table.Column<int>(type: "int", nullable: false),
                    DistanceFromStartKm = table.Column<int>(type: "int", nullable: true),
                    MinutesFromStart = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "bus",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStops_StopPoints_StopPointId",
                        column: x => x.StopPointId,
                        principalSchema: "bus",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FareRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaggagePolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoardingPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "catalog",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "bus",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "fleet",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopIndex = table.Column<int>(type: "int", nullable: false),
                    DistanceFromStartKm = table.Column<int>(type: "int", nullable: true),
                    MinutesFromStart = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "train",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStops_StopPoints_StopPointId",
                        column: x => x.StopPointId,
                        principalSchema: "train",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FareRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaggagePolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoardingPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "train",
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferSegments",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIndex = table.Column<int>(type: "int", nullable: false),
                    FromAirportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToAirportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartureAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferSegments_Airports_FromAirportId",
                        column: x => x.FromAirportId,
                        principalSchema: "flight",
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferSegments_Airports_ToAirportId",
                        column: x => x.ToAirportId,
                        principalSchema: "flight",
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferSegments_Offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "flight",
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferTaxFeeLines",
                schema: "flight",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferTaxFeeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferTaxFeeLines_Offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "flight",
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStopTimes",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopIndex = table.Column<int>(type: "int", nullable: false),
                    ArriveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DepartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MinutesFromStart = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopTimes_StopPoints_StopPointId",
                        column: x => x.StopPointId,
                        principalSchema: "bus",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopTimes_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "bus",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainCars",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CarType = table.Column<int>(type: "int", nullable: false),
                    CabinClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainCars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainCars_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStopTimes",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopIndex = table.Column<int>(type: "int", nullable: false),
                    ArriveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DepartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MinutesFromStart = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopTimes_StopPoints_StopPointId",
                        column: x => x.StopPointId,
                        principalSchema: "train",
                        principalTable: "StopPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopTimes_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripSeatHolds",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HoldToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSeatHolds", x => x.Id);
                    table.CheckConstraint("CK_bus_TripSeatHolds_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_Seats_SeatId",
                        column: x => x.SeatId,
                        principalSchema: "fleet",
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_TripStopTimes_FromTripStopTimeId",
                        column: x => x.FromTripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_TripStopTimes_ToTripStopTimeId",
                        column: x => x.ToTripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "bus",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripSegmentPrices",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxesFees = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSegmentPrices", x => x.Id);
                    table.CheckConstraint("CK_bus_TripSegmentPrices_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_TripStopTimes_FromTripStopTimeId",
                        column: x => x.FromTripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_TripStopTimes_ToTripStopTimeId",
                        column: x => x.ToTripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "bus",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStopDropoffPoints",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopDropoffPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopDropoffPoints_TripStopTimes_TripStopTimeId",
                        column: x => x.TripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStopPickupPoints",
                schema: "bus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopPickupPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopPickupPoints_TripStopTimes_TripStopTimeId",
                        column: x => x.TripStopTimeId,
                        principalSchema: "bus",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainCarSeats",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SeatType = table.Column<int>(type: "int", nullable: false),
                    CompartmentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompartmentIndex = table.Column<int>(type: "int", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    ColumnIndex = table.Column<int>(type: "int", nullable: false),
                    IsWindow = table.Column<bool>(type: "bit", nullable: false),
                    IsAisle = table.Column<bool>(type: "bit", nullable: false),
                    SeatClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainCarSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainCarSeats_TrainCars_CarId",
                        column: x => x.CarId,
                        principalSchema: "train",
                        principalTable: "TrainCars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripSegmentPrices",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BaseFare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxesFees = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSegmentPrices", x => x.Id);
                    table.CheckConstraint("CK_train_TripSegmentPrices_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_TripStopTimes_FromTripStopTimeId",
                        column: x => x.FromTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_TripStopTimes_ToTripStopTimeId",
                        column: x => x.ToTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSegmentPrices_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripSeatHolds",
                schema: "train",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainCarSeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToTripStopTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStopIndex = table.Column<int>(type: "int", nullable: false),
                    ToStopIndex = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HoldToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoldExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripSeatHolds", x => x.Id);
                    table.CheckConstraint("CK_train_TripSeatHolds_StopIndex", "[FromStopIndex] < [ToStopIndex]");
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_TrainCarSeats_TrainCarSeatId",
                        column: x => x.TrainCarSeatId,
                        principalSchema: "train",
                        principalTable: "TrainCarSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_TripStopTimes_FromTripStopTimeId",
                        column: x => x.FromTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_TripStopTimes_ToTripStopTimeId",
                        column: x => x.ToTripStopTimeId,
                        principalSchema: "train",
                        principalTable: "TripStopTimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripSeatHolds_Trips_TripId",
                        column: x => x.TripId,
                        principalSchema: "train",
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AircraftModels_TenantId_Code",
                schema: "flight",
                table: "AircraftModels",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AircraftModels_TenantId_IsActive",
                schema: "flight",
                table: "AircraftModels",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_AircraftModelId",
                schema: "flight",
                table: "Aircrafts",
                column: "AircraftModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_AirlineId",
                schema: "flight",
                table: "Aircrafts",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_TenantId_Code",
                schema: "flight",
                table: "Aircrafts",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_TenantId_IsActive",
                schema: "flight",
                table: "Aircrafts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Aircrafts_TenantId_Registration",
                schema: "flight",
                table: "Aircrafts",
                columns: new[] { "TenantId", "Registration" });

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_TenantId_Code",
                schema: "flight",
                table: "Airlines",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_TenantId_IsActive",
                schema: "flight",
                table: "Airlines",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_LocationId",
                schema: "flight",
                table: "Airports",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_Code",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_IataCode",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "IataCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_TenantId_IsActive",
                schema: "flight",
                table: "Airports",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AncillaryDefinitions_AirlineId",
                schema: "flight",
                table: "AncillaryDefinitions",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_AncillaryDefinitions_TenantId_AirlineId_Code",
                schema: "flight",
                table: "AncillaryDefinitions",
                columns: new[] { "TenantId", "AirlineId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AncillaryDefinitions_TenantId_IsActive",
                schema: "flight",
                table: "AncillaryDefinitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsActive",
                table: "AspNetUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BedTypes_TenantId_Code",
                schema: "hotels",
                table: "BedTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BedTypes_TenantId_IsActive",
                schema: "hotels",
                table: "BedTypes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusVehicleDetails_TenantId_VehicleId",
                schema: "fleet",
                table: "BusVehicleDetails",
                columns: new[] { "TenantId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusVehicleDetails_VehicleId",
                schema: "fleet",
                table: "BusVehicleDetails",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeatMaps_AircraftModelId",
                schema: "flight",
                table: "CabinSeatMaps",
                column: "AircraftModelId");

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeatMaps_TenantId_AircraftModelId_CabinClass",
                schema: "flight",
                table: "CabinSeatMaps",
                columns: new[] { "TenantId", "AircraftModelId", "CabinClass" });

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeatMaps_TenantId_Code",
                schema: "flight",
                table: "CabinSeatMaps",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeatMaps_TenantId_IsActive",
                schema: "flight",
                table: "CabinSeatMaps",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeats_CabinSeatMapId_SeatNumber",
                schema: "flight",
                table: "CabinSeats",
                columns: new[] { "CabinSeatMapId", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CabinSeats_TenantId_IsActive",
                schema: "flight",
                table: "CabinSeats",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_HotelId",
                schema: "hotels",
                table: "CancellationPolicies",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_TenantId_HotelId_Code",
                schema: "hotels",
                table: "CancellationPolicies",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "CancellationPolicies",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicies_TenantId_HotelId_Type",
                schema: "hotels",
                table: "CancellationPolicies",
                columns: new[] { "TenantId", "HotelId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicyRules_CancellationPolicyId",
                schema: "hotels",
                table: "CancellationPolicyRules",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicyRules_TenantId_CancellationPolicyId_IsActive",
                schema: "hotels",
                table: "CancellationPolicyRules",
                columns: new[] { "TenantId", "CancellationPolicyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicyRules_TenantId_CancellationPolicyId_Priority",
                schema: "hotels",
                table: "CancellationPolicyRules",
                columns: new[] { "TenantId", "CancellationPolicyId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInOutRules_HotelId",
                schema: "hotels",
                table: "CheckInOutRules",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInOutRules_TenantId_HotelId_Code",
                schema: "hotels",
                table: "CheckInOutRules",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckInOutRules_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "CheckInOutRules",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyRates_RatePlanRoomTypeId",
                schema: "hotels",
                table: "DailyRates",
                column: "RatePlanRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRates_TenantId_RatePlanRoomTypeId_Date",
                schema: "hotels",
                table: "DailyRates",
                columns: new[] { "TenantId", "RatePlanRoomTypeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyRates_TenantId_RatePlanRoomTypeId_IsActive",
                schema: "hotels",
                table: "DailyRates",
                columns: new[] { "TenantId", "RatePlanRoomTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Code",
                schema: "geo",
                table: "Districts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Name",
                schema: "geo",
                table: "Districts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_ProvinceId",
                schema: "geo",
                table: "Districts",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServicePrices_ExtraServiceId",
                schema: "hotels",
                table: "ExtraServicePrices",
                column: "ExtraServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServicePrices_TenantId_ExtraServiceId_StartDate_EndDate",
                schema: "hotels",
                table: "ExtraServicePrices",
                columns: new[] { "TenantId", "ExtraServiceId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServices_HotelId",
                schema: "hotels",
                table: "ExtraServices",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServices_TenantId_HotelId_Code",
                schema: "hotels",
                table: "ExtraServices",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServices_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "ExtraServices",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtraServices_TenantId_HotelId_Type",
                schema: "hotels",
                table: "ExtraServices",
                columns: new[] { "TenantId", "HotelId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_AirlineId",
                schema: "flight",
                table: "FareClasses",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_TenantId_AirlineId_Code",
                schema: "flight",
                table: "FareClasses",
                columns: new[] { "TenantId", "AirlineId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_TenantId_IsActive",
                schema: "flight",
                table: "FareClasses",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_FareClassId",
                schema: "flight",
                table: "FareRules",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TenantId_FareClassId",
                schema: "flight",
                table: "FareRules",
                columns: new[] { "TenantId", "FareClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareRules_TenantId_IsActive",
                schema: "flight",
                table: "FareRules",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_AircraftId",
                schema: "flight",
                table: "Flights",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_AirlineId",
                schema: "flight",
                table: "Flights",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_FromAirportId",
                schema: "flight",
                table: "Flights",
                column: "FromAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_TenantId_AirlineId_DepartureAt",
                schema: "flight",
                table: "Flights",
                columns: new[] { "TenantId", "AirlineId", "DepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_TenantId_FlightNumber_DepartureAt",
                schema: "flight",
                table: "Flights",
                columns: new[] { "TenantId", "FlightNumber", "DepartureAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_TenantId_IsActive",
                schema: "flight",
                table: "Flights",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_ToAirportId",
                schema: "flight",
                table: "Flights",
                column: "ToAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_GeoSyncLogs_CreatedAt",
                schema: "geo",
                table: "GeoSyncLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeoSyncLogs_Source_Depth",
                schema: "geo",
                table: "GeoSyncLogs",
                columns: new[] { "Source", "Depth" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenities_TenantId_Scope_Code",
                schema: "hotels",
                table: "HotelAmenities",
                columns: new[] { "TenantId", "Scope", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenities_TenantId_Scope_IsActive",
                schema: "hotels",
                table: "HotelAmenities",
                columns: new[] { "TenantId", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenities_TenantId_Scope_SortOrder",
                schema: "hotels",
                table: "HotelAmenities",
                columns: new[] { "TenantId", "Scope", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenityLinks_AmenityId",
                schema: "hotels",
                table: "HotelAmenityLinks",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenityLinks_HotelId",
                schema: "hotels",
                table: "HotelAmenityLinks",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenityLinks_TenantId_HotelId",
                schema: "hotels",
                table: "HotelAmenityLinks",
                columns: new[] { "TenantId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelAmenityLinks_TenantId_HotelId_AmenityId",
                schema: "hotels",
                table: "HotelAmenityLinks",
                columns: new[] { "TenantId", "HotelId", "AmenityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelContacts_HotelId",
                schema: "hotels",
                table: "HotelContacts",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelContacts_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "HotelContacts",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelContacts_TenantId_HotelId_IsPrimary",
                schema: "hotels",
                table: "HotelContacts",
                columns: new[] { "TenantId", "HotelId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId",
                schema: "hotels",
                table: "HotelImages",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_TenantId_HotelId_Kind",
                schema: "hotels",
                table: "HotelImages",
                columns: new[] { "TenantId", "HotelId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_TenantId_HotelId_SortOrder",
                schema: "hotels",
                table: "HotelImages",
                columns: new[] { "TenantId", "HotelId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_HotelId",
                schema: "hotels",
                table: "HotelReviews",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_TenantId_BookingId",
                schema: "hotels",
                table: "HotelReviews",
                columns: new[] { "TenantId", "BookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_TenantId_HotelId_Rating",
                schema: "hotels",
                table: "HotelReviews",
                columns: new[] { "TenantId", "HotelId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_HotelReviews_TenantId_HotelId_Status",
                schema: "hotels",
                table: "HotelReviews",
                columns: new[] { "TenantId", "HotelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_TenantId_Code",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_TenantId_IsActive",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_TenantId_LocationId",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_TenantId_Slug",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_TenantId_Status",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHolds_RoomTypeId",
                schema: "hotels",
                table: "InventoryHolds",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHolds_TenantId_BookingId",
                schema: "hotels",
                table: "InventoryHolds",
                columns: new[] { "TenantId", "BookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHolds_TenantId_RoomTypeId_HoldExpiresAt",
                schema: "hotels",
                table: "InventoryHolds",
                columns: new[] { "TenantId", "RoomTypeId", "HoldExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHolds_TenantId_RoomTypeId_Status",
                schema: "hotels",
                table: "InventoryHolds",
                columns: new[] { "TenantId", "RoomTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_DistrictId",
                schema: "catalog",
                table: "Locations",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ProvinceId",
                schema: "catalog",
                table: "Locations",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_AirportIataCode",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "AirportIataCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_BusStationCode",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "BusStationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Code",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_NormalizedName",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_TrainStationCode",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "TrainStationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Type_IsActive",
                schema: "catalog",
                table: "Locations",
                columns: new[] { "TenantId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_WardId",
                schema: "catalog",
                table: "Locations",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_TenantId_Code",
                schema: "hotels",
                table: "MealPlans",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_TenantId_IsActive",
                schema: "hotels",
                table: "MealPlans",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_TenantId_IsActive",
                schema: "cms",
                table: "MediaAssets",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_TenantId_Type",
                schema: "cms",
                table: "MediaAssets",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCategories_TenantId_IsActive",
                schema: "cms",
                table: "NewsCategories",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCategories_TenantId_Slug",
                schema: "cms",
                table: "NewsCategories",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsCategories_TenantId_SortOrder",
                schema: "cms",
                table: "NewsCategories",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostCategories_CategoryId",
                schema: "cms",
                table: "NewsPostCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostCategories_PostId",
                schema: "cms",
                table: "NewsPostCategories",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostCategories_TenantId_PostId_CategoryId",
                schema: "cms",
                table: "NewsPostCategories",
                columns: new[] { "TenantId", "PostId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostRevisions_EditorUserId",
                schema: "cms",
                table: "NewsPostRevisions",
                column: "EditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostRevisions_PostId",
                schema: "cms",
                table: "NewsPostRevisions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostRevisions_TenantId_PostId_EditedAt",
                schema: "cms",
                table: "NewsPostRevisions",
                columns: new[] { "TenantId", "PostId", "EditedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostRevisions_TenantId_PostId_VersionNumber",
                schema: "cms",
                table: "NewsPostRevisions",
                columns: new[] { "TenantId", "PostId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_AuthorUserId",
                schema: "cms",
                table: "NewsPosts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_CoverMediaAssetId",
                schema: "cms",
                table: "NewsPosts",
                column: "CoverMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_EditorUserId",
                schema: "cms",
                table: "NewsPosts",
                column: "EditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_TenantId_PublishedAt",
                schema: "cms",
                table: "NewsPosts",
                columns: new[] { "TenantId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_TenantId_ScheduledAt",
                schema: "cms",
                table: "NewsPosts",
                columns: new[] { "TenantId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_TenantId_Slug",
                schema: "cms",
                table: "NewsPosts",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_TenantId_Status",
                schema: "cms",
                table: "NewsPosts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostTags_PostId",
                schema: "cms",
                table: "NewsPostTags",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostTags_TagId",
                schema: "cms",
                table: "NewsPostTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPostTags_TenantId_PostId_TagId",
                schema: "cms",
                table: "NewsPostTags",
                columns: new[] { "TenantId", "PostId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsRedirects_TenantId_FromPath",
                schema: "cms",
                table: "NewsRedirects",
                columns: new[] { "TenantId", "FromPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsRedirects_TenantId_IsActive",
                schema: "cms",
                table: "NewsRedirects",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsTags_TenantId_IsActive",
                schema: "cms",
                table: "NewsTags",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsTags_TenantId_Slug",
                schema: "cms",
                table: "NewsTags",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_AirlineId",
                schema: "flight",
                table: "Offers",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_FareClassId",
                schema: "flight",
                table: "Offers",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_FlightId",
                schema: "flight",
                table: "Offers",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_ExpiresAt",
                schema: "flight",
                table: "Offers",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_FlightId",
                schema: "flight",
                table: "Offers",
                columns: new[] { "TenantId", "FlightId" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_Status",
                schema: "flight",
                table: "Offers",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_FromAirportId",
                schema: "flight",
                table: "OfferSegments",
                column: "FromAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_OfferId_SegmentIndex",
                schema: "flight",
                table: "OfferSegments",
                columns: new[] { "OfferId", "SegmentIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferSegments_ToAirportId",
                schema: "flight",
                table: "OfferSegments",
                column: "ToAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferTaxFeeLines_OfferId_SortOrder_Code",
                schema: "flight",
                table: "OfferTaxFeeLines",
                columns: new[] { "OfferId", "SortOrder", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Category",
                schema: "auth",
                table: "Permissions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "auth",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive",
                schema: "auth",
                table: "Permissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PromoRateOverrides_RatePlanRoomTypeId",
                schema: "hotels",
                table: "PromoRateOverrides",
                column: "RatePlanRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoRateOverrides_TenantId_PromoCodeId",
                schema: "hotels",
                table: "PromoRateOverrides",
                columns: new[] { "TenantId", "PromoCodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoRateOverrides_TenantId_RatePlanRoomTypeId_StartDate_EndDate",
                schema: "hotels",
                table: "PromoRateOverrides",
                columns: new[] { "TenantId", "RatePlanRoomTypeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPolicies_HotelId",
                schema: "hotels",
                table: "PropertyPolicies",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPolicies_TenantId_HotelId_Code",
                schema: "hotels",
                table: "PropertyPolicies",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPolicies_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "PropertyPolicies",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_DistrictId",
                schema: "catalog",
                table: "Providers",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_LocationId",
                schema: "catalog",
                table: "Providers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProvinceId",
                schema: "catalog",
                table: "Providers",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TenantId_Code",
                schema: "catalog",
                table: "Providers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TenantId_Slug",
                schema: "catalog",
                table: "Providers",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_TenantId_Type_IsActive",
                schema: "catalog",
                table: "Providers",
                columns: new[] { "TenantId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_WardId",
                schema: "catalog",
                table: "Providers",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Code",
                schema: "geo",
                table: "Provinces",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Name",
                schema: "geo",
                table: "Provinces",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanPolicies_RatePlanId",
                schema: "hotels",
                table: "RatePlanPolicies",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanPolicies_TenantId_RatePlanId",
                schema: "hotels",
                table: "RatePlanPolicies",
                columns: new[] { "TenantId", "RatePlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanRoomTypes_RatePlanId",
                schema: "hotels",
                table: "RatePlanRoomTypes",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanRoomTypes_RoomTypeId",
                schema: "hotels",
                table: "RatePlanRoomTypes",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanRoomTypes_TenantId_RatePlanId_RoomTypeId",
                schema: "hotels",
                table: "RatePlanRoomTypes",
                columns: new[] { "TenantId", "RatePlanId", "RoomTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlanRoomTypes_TenantId_RoomTypeId_IsActive",
                schema: "hotels",
                table: "RatePlanRoomTypes",
                columns: new[] { "TenantId", "RoomTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_CancellationPolicyId",
                schema: "hotels",
                table: "RatePlans",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_CheckInOutRuleId",
                schema: "hotels",
                table: "RatePlans",
                column: "CheckInOutRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_HotelId",
                schema: "hotels",
                table: "RatePlans",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_PropertyPolicyId",
                schema: "hotels",
                table: "RatePlans",
                column: "PropertyPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_TenantId_HotelId_Code",
                schema: "hotels",
                table: "RatePlans",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "RatePlans",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_TenantId_HotelId_Status",
                schema: "hotels",
                table: "RatePlans",
                columns: new[] { "TenantId", "HotelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_TenantId_HotelId_Type",
                schema: "hotels",
                table: "RatePlans",
                columns: new[] { "TenantId", "HotelId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "auth",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "auth",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenities_TenantId_Code",
                schema: "hotels",
                table: "RoomAmenities",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenities_TenantId_IsActive",
                schema: "hotels",
                table: "RoomAmenities",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenities_TenantId_SortOrder",
                schema: "hotels",
                table: "RoomAmenities",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenityLinks_AmenityId",
                schema: "hotels",
                table: "RoomAmenityLinks",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenityLinks_RoomTypeId",
                schema: "hotels",
                table: "RoomAmenityLinks",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAmenityLinks_TenantId_RoomTypeId_AmenityId",
                schema: "hotels",
                table: "RoomAmenityLinks",
                columns: new[] { "TenantId", "RoomTypeId", "AmenityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeBeds_BedTypeId",
                schema: "hotels",
                table: "RoomTypeBeds",
                column: "BedTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeBeds_RoomTypeId",
                schema: "hotels",
                table: "RoomTypeBeds",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeBeds_TenantId_RoomTypeId_BedTypeId",
                schema: "hotels",
                table: "RoomTypeBeds",
                columns: new[] { "TenantId", "RoomTypeId", "BedTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeImages_RoomTypeId",
                schema: "hotels",
                table: "RoomTypeImages",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeImages_TenantId_RoomTypeId_Kind",
                schema: "hotels",
                table: "RoomTypeImages",
                columns: new[] { "TenantId", "RoomTypeId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeImages_TenantId_RoomTypeId_SortOrder",
                schema: "hotels",
                table: "RoomTypeImages",
                columns: new[] { "TenantId", "RoomTypeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeInventories_RoomTypeId",
                schema: "hotels",
                table: "RoomTypeInventories",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeInventories_TenantId_RoomTypeId_Date",
                schema: "hotels",
                table: "RoomTypeInventories",
                columns: new[] { "TenantId", "RoomTypeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeInventories_TenantId_RoomTypeId_Status",
                schema: "hotels",
                table: "RoomTypeInventories",
                columns: new[] { "TenantId", "RoomTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeMealPlans_MealPlanId",
                schema: "hotels",
                table: "RoomTypeMealPlans",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeMealPlans_RoomTypeId",
                schema: "hotels",
                table: "RoomTypeMealPlans",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeMealPlans_TenantId_RoomTypeId_IsActive",
                schema: "hotels",
                table: "RoomTypeMealPlans",
                columns: new[] { "TenantId", "RoomTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeMealPlans_TenantId_RoomTypeId_MealPlanId",
                schema: "hotels",
                table: "RoomTypeMealPlans",
                columns: new[] { "TenantId", "RoomTypeId", "MealPlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeOccupancyRules_RoomTypeId",
                schema: "hotels",
                table: "RoomTypeOccupancyRules",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeOccupancyRules_TenantId_RoomTypeId_IsActive",
                schema: "hotels",
                table: "RoomTypeOccupancyRules",
                columns: new[] { "TenantId", "RoomTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypePolicies_RoomTypeId",
                schema: "hotels",
                table: "RoomTypePolicies",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypePolicies_TenantId_RoomTypeId",
                schema: "hotels",
                table: "RoomTypePolicies",
                columns: new[] { "TenantId", "RoomTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_HotelId",
                schema: "hotels",
                table: "RoomTypes",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_TenantId_HotelId_Code",
                schema: "hotels",
                table: "RoomTypes",
                columns: new[] { "TenantId", "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_TenantId_HotelId_IsActive",
                schema: "hotels",
                table: "RoomTypes",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_TenantId_HotelId_SortOrder",
                schema: "hotels",
                table: "RoomTypes",
                columns: new[] { "TenantId", "HotelId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_TenantId_HotelId_Status",
                schema: "hotels",
                table: "RoomTypes",
                columns: new[] { "TenantId", "HotelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromStopPointId",
                schema: "bus",
                table: "Routes",
                column: "FromStopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ProviderId",
                schema: "bus",
                table: "Routes",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TenantId_Code",
                schema: "bus",
                table: "Routes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TenantId_IsActive",
                schema: "bus",
                table: "Routes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TenantId_ProviderId",
                schema: "bus",
                table: "Routes",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToStopPointId",
                schema: "bus",
                table: "Routes",
                column: "ToStopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromStopPointId",
                schema: "train",
                table: "Routes",
                column: "FromStopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TenantId_Code",
                schema: "train",
                table: "Routes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_TenantId_ProviderId",
                schema: "train",
                table: "Routes",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToStopPointId",
                schema: "train",
                table: "Routes",
                column: "ToStopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_StopIndex",
                schema: "bus",
                table: "RouteStops",
                columns: new[] { "RouteId", "StopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_StopPointId",
                schema: "bus",
                table: "RouteStops",
                columns: new[] { "RouteId", "StopPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_StopPointId",
                schema: "bus",
                table: "RouteStops",
                column: "StopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_StopIndex",
                schema: "train",
                table: "RouteStops",
                columns: new[] { "RouteId", "StopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId_StopPointId",
                schema: "train",
                table: "RouteStops",
                columns: new[] { "RouteId", "StopPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_StopPointId",
                schema: "train",
                table: "RouteStops",
                column: "StopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatMaps_TenantId_VehicleType_Code",
                schema: "fleet",
                table: "SeatMaps",
                columns: new[] { "TenantId", "VehicleType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatMaps_TenantId_VehicleType_IsActive",
                schema: "fleet",
                table: "SeatMaps",
                columns: new[] { "TenantId", "VehicleType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Seats_SeatMapId",
                schema: "fleet",
                table: "Seats",
                column: "SeatMapId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_SeatMapId_SeatNumber",
                schema: "fleet",
                table: "Seats",
                columns: new[] { "SeatMapId", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_TenantId",
                schema: "cms",
                table: "SiteSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_TenantId_IsActive",
                schema: "cms",
                table: "SiteSettings",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_LocationId",
                schema: "bus",
                table: "StopPoints",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_TenantId_LocationId",
                schema: "bus",
                table: "StopPoints",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_TenantId_Type_IsActive",
                schema: "bus",
                table: "StopPoints",
                columns: new[] { "TenantId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_LocationId",
                schema: "train",
                table: "StopPoints",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_TenantId_LocationId",
                schema: "train",
                table: "StopPoints",
                columns: new[] { "TenantId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StopPoints_TenantId_Type_IsActive",
                schema: "train",
                table: "StopPoints",
                columns: new[] { "TenantId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRolePermissions_PermissionId",
                schema: "tenants",
                table: "TenantRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRolePermissions_TenantId",
                schema: "tenants",
                table: "TenantRolePermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRolePermissions_TenantId_TenantRoleId_PermissionId",
                schema: "tenants",
                table: "TenantRolePermissions",
                columns: new[] { "TenantId", "TenantRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantRolePermissions_TenantRoleId",
                schema: "tenants",
                table: "TenantRolePermissions",
                column: "TenantRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoles_IsActive",
                schema: "tenants",
                table: "TenantRoles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoles_TenantId",
                schema: "tenants",
                table: "TenantRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantRoles_TenantId_Code",
                schema: "tenants",
                table: "TenantRoles",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                schema: "tenants",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Status",
                schema: "tenants",
                table: "Tenants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Type",
                schema: "tenants",
                table: "Tenants",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_TenantId",
                schema: "tenants",
                table: "TenantUserRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_TenantId_TenantRoleId_UserId",
                schema: "tenants",
                table: "TenantUserRoles",
                columns: new[] { "TenantId", "TenantRoleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_TenantRoleId",
                schema: "tenants",
                table: "TenantUserRoles",
                column: "TenantRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_UserId",
                schema: "tenants",
                table: "TenantUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId",
                schema: "tenants",
                table: "TenantUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_UserId",
                schema: "tenants",
                table: "TenantUsers",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_UserId",
                schema: "tenants",
                table: "TenantUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainCars_TripId_CarNumber",
                schema: "train",
                table: "TrainCars",
                columns: new[] { "TripId", "CarNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainCars_TripId_SortOrder",
                schema: "train",
                table: "TrainCars",
                columns: new[] { "TripId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainCarSeats_CarId_CompartmentCode",
                schema: "train",
                table: "TrainCarSeats",
                columns: new[] { "CarId", "CompartmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainCarSeats_CarId_SeatNumber",
                schema: "train",
                table: "TrainCarSeats",
                columns: new[] { "CarId", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ProviderId",
                schema: "bus",
                table: "Trips",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteId",
                schema: "bus",
                table: "Trips",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_Code",
                schema: "bus",
                table: "Trips",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_DepartureAt",
                schema: "bus",
                table: "Trips",
                columns: new[] { "TenantId", "DepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_ProviderId",
                schema: "bus",
                table: "Trips",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_RouteId",
                schema: "bus",
                table: "Trips",
                columns: new[] { "TenantId", "RouteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_VehicleId",
                schema: "bus",
                table: "Trips",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteId",
                schema: "train",
                table: "Trips",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_Code",
                schema: "train",
                table: "Trips",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TenantId_DepartureAt",
                schema: "train",
                table: "Trips",
                columns: new[] { "TenantId", "DepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_FromTripStopTimeId",
                schema: "bus",
                table: "TripSeatHolds",
                column: "FromTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "bus",
                table: "TripSeatHolds",
                column: "HoldToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_SeatId",
                schema: "bus",
                table: "TripSeatHolds",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_ToTripStopTimeId",
                schema: "bus",
                table: "TripSeatHolds",
                column: "ToTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TripId_SeatId",
                schema: "bus",
                table: "TripSeatHolds",
                columns: new[] { "TripId", "SeatId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TripId_Status_HoldExpiresAt",
                schema: "bus",
                table: "TripSeatHolds",
                columns: new[] { "TripId", "Status", "HoldExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_FromTripStopTimeId",
                schema: "train",
                table: "TripSeatHolds",
                column: "FromTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_HoldToken",
                schema: "train",
                table: "TripSeatHolds",
                column: "HoldToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_ToTripStopTimeId",
                schema: "train",
                table: "TripSeatHolds",
                column: "ToTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TrainCarSeatId",
                schema: "train",
                table: "TripSeatHolds",
                column: "TrainCarSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TripId_Status_HoldExpiresAt",
                schema: "train",
                table: "TripSeatHolds",
                columns: new[] { "TripId", "Status", "HoldExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSeatHolds_TripId_TrainCarSeatId",
                schema: "train",
                table: "TripSeatHolds",
                columns: new[] { "TripId", "TrainCarSeatId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_FromTripStopTimeId",
                schema: "bus",
                table: "TripSegmentPrices",
                column: "FromTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_ToTripStopTimeId",
                schema: "bus",
                table: "TripSegmentPrices",
                column: "ToTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_TripId_FromStopIndex_ToStopIndex",
                schema: "bus",
                table: "TripSegmentPrices",
                columns: new[] { "TripId", "FromStopIndex", "ToStopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_TripId_IsActive",
                schema: "bus",
                table: "TripSegmentPrices",
                columns: new[] { "TripId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_FromTripStopTimeId",
                schema: "train",
                table: "TripSegmentPrices",
                column: "FromTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_ToTripStopTimeId",
                schema: "train",
                table: "TripSegmentPrices",
                column: "ToTripStopTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_TripId_FromStopIndex_ToStopIndex",
                schema: "train",
                table: "TripSegmentPrices",
                columns: new[] { "TripId", "FromStopIndex", "ToStopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripSegmentPrices_TripId_IsActive",
                schema: "train",
                table: "TripSegmentPrices",
                columns: new[] { "TripId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopDropoffPoints_TripStopTimeId_IsDefault",
                schema: "bus",
                table: "TripStopDropoffPoints",
                columns: new[] { "TripStopTimeId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopDropoffPoints_TripStopTimeId_SortOrder",
                schema: "bus",
                table: "TripStopDropoffPoints",
                columns: new[] { "TripStopTimeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopPickupPoints_TripStopTimeId_IsDefault",
                schema: "bus",
                table: "TripStopPickupPoints",
                columns: new[] { "TripStopTimeId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopPickupPoints_TripStopTimeId_SortOrder",
                schema: "bus",
                table: "TripStopPickupPoints",
                columns: new[] { "TripStopTimeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_StopPointId",
                schema: "bus",
                table: "TripStopTimes",
                column: "StopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_TripId_StopIndex",
                schema: "bus",
                table: "TripStopTimes",
                columns: new[] { "TripId", "StopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_TripId_StopPointId",
                schema: "bus",
                table: "TripStopTimes",
                columns: new[] { "TripId", "StopPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_StopPointId",
                schema: "train",
                table: "TripStopTimes",
                column: "StopPointId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_TripId_StopIndex",
                schema: "train",
                table: "TripStopTimes",
                columns: new[] { "TripId", "StopIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripStopTimes_TripId_StopPointId",
                schema: "train",
                table: "TripStopTimes",
                columns: new[] { "TripId", "StopPointId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                schema: "auth",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_TenantId",
                schema: "auth",
                table: "UserPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                schema: "auth",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PermissionId",
                schema: "auth",
                table: "UserPermissions",
                columns: new[] { "UserId", "PermissionId" },
                unique: true,
                filter: "[TenantId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PermissionId_TenantId",
                schema: "auth",
                table: "UserPermissions",
                columns: new[] { "UserId", "PermissionId", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_TenantId_Manufacturer_ModelName",
                schema: "fleet",
                table: "VehicleModels",
                columns: new[] { "TenantId", "Manufacturer", "ModelName" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_TenantId_VehicleType_IsActive",
                schema: "fleet",
                table: "VehicleModels",
                columns: new[] { "TenantId", "VehicleType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ProviderId",
                schema: "fleet",
                table: "Vehicles",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_SeatMapId",
                schema: "fleet",
                table: "Vehicles",
                column: "SeatMapId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_ProviderId",
                schema: "fleet",
                table: "Vehicles",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_VehicleType_Code",
                schema: "fleet",
                table: "Vehicles",
                columns: new[] { "TenantId", "VehicleType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_VehicleType_IsActive",
                schema: "fleet",
                table: "Vehicles",
                columns: new[] { "TenantId", "VehicleType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleModelId",
                schema: "fleet",
                table: "Vehicles",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Code",
                schema: "geo",
                table: "Wards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DistrictId",
                schema: "geo",
                table: "Wards",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Name",
                schema: "geo",
                table: "Wards",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AncillaryDefinitions",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BusVehicleDetails",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "CabinSeats",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "CancellationPolicyRules",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "DailyRates",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "ExtraServicePrices",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "FareRules",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "GeoSyncLogs",
                schema: "geo");

            migrationBuilder.DropTable(
                name: "HotelAmenityLinks",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "HotelContacts",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "HotelImages",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "HotelReviews",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "InventoryHolds",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "NewsPostCategories",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "NewsPostRevisions",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "NewsPostTags",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "NewsRedirects",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "OfferSegments",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "OfferTaxFeeLines",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "PromoRateOverrides",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RatePlanPolicies",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "RoomAmenityLinks",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypeBeds",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypeImages",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypeInventories",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypeMealPlans",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypeOccupancyRules",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypePolicies",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RouteStops",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "RouteStops",
                schema: "train");

            migrationBuilder.DropTable(
                name: "SiteSettings",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "TenantRolePermissions",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "TenantUserRoles",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "TenantUsers",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "TripSeatHolds",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "TripSeatHolds",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TripSegmentPrices",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "TripSegmentPrices",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TripStopDropoffPoints",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "TripStopPickupPoints",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "UserPermissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "CabinSeatMaps",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "ExtraServices",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "HotelAmenities",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "NewsCategories",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "NewsPosts",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "NewsTags",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "Offers",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "RatePlanRoomTypes",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "RoomAmenities",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "BedTypes",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "MealPlans",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "TenantRoles",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "Seats",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "TrainCarSeats",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TripStopTimes",
                schema: "train");

            migrationBuilder.DropTable(
                name: "TripStopTimes",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "MediaAssets",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "FareClasses",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "Flights",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "RatePlans",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "RoomTypes",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenants");

            migrationBuilder.DropTable(
                name: "TrainCars",
                schema: "train");

            migrationBuilder.DropTable(
                name: "Trips",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "Aircrafts",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "Airports",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "CancellationPolicies",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "CheckInOutRules",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "PropertyPolicies",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "Trips",
                schema: "train");

            migrationBuilder.DropTable(
                name: "Routes",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "AircraftModels",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "Airlines",
                schema: "flight");

            migrationBuilder.DropTable(
                name: "Hotels",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "Routes",
                schema: "train");

            migrationBuilder.DropTable(
                name: "StopPoints",
                schema: "bus");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "SeatMaps",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "VehicleModels",
                schema: "fleet");

            migrationBuilder.DropTable(
                name: "StopPoints",
                schema: "train");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Wards",
                schema: "geo");

            migrationBuilder.DropTable(
                name: "Districts",
                schema: "geo");

            migrationBuilder.DropTable(
                name: "Provinces",
                schema: "geo");
        }
    }
}
