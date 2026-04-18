using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_Tours_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tours");

            migrationBuilder.CreateTable(
                name: "Tours",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    DurationNights = table.Column<int>(type: "int", nullable: false),
                    MinGuests = table.Column<int>(type: "int", nullable: true),
                    MaxGuests = table.Column<int>(type: "int", nullable: true),
                    MinAge = table.Column<int>(type: "int", nullable: true),
                    MaxAge = table.Column<int>(type: "int", nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsFeaturedOnHome = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivateTourSupported = table.Column<bool>(type: "bit", nullable: false),
                    IsInstantConfirm = table.Column<bool>(type: "bit", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MeetingPointSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighlightsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExcludesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourAddons",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsPerPerson = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowQuantitySelection = table.Column<bool>(type: "bit", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: true),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true),
                    IsDefaultSelected = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourAddons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourAddons_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourContacts",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactType = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourContacts_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourDropoffPoints",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    District = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DropoffTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourDropoffPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourDropoffPoints_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourFaqs",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AnswerMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourFaqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourFaqs_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourImages",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourImages_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourItineraryDays",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EndLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AccommodationName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IncludesBreakfast = table.Column<bool>(type: "bit", nullable: false),
                    IncludesLunch = table.Column<bool>(type: "bit", nullable: false),
                    IncludesDinner = table.Column<bool>(type: "bit", nullable: false),
                    TransportationSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourItineraryDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourItineraryDays_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPickupPoints",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    District = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PickupTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourPickupPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPickupPoints_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourPolicies",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPolicies_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourReviews",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReviewerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    ModerationNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReplyContent = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReplyAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplyByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_TourReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourReviews_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourSchedules",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DepartureTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ReturnTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BookingOpenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BookingCutoffAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MeetingPointSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PickupSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DropoffSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancellationNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsGuaranteedDeparture = table.Column<bool>(type: "bit", nullable: false),
                    IsInstantConfirm = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    MinGuestsToOperate = table.Column<int>(type: "int", nullable: true),
                    MaxGuests = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_TourSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourSchedules_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourItineraryItems",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourItineraryDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransportationMode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IncludesTicket = table.Column<bool>(type: "bit", nullable: false),
                    IncludesMeal = table.Column<bool>(type: "bit", nullable: false),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAdditionalFee = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourItineraryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourItineraryItems_TourItineraryDays_TourItineraryDayId",
                        column: x => x.TourItineraryDayId,
                        principalSchema: "tours",
                        principalTable: "TourItineraryDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourScheduleAddonPrices",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourAddonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsPerPerson = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultSelected = table.Column<bool>(type: "bit", nullable: false),
                    AllowQuantitySelection = table.Column<bool>(type: "bit", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: true),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TourScheduleAddonPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourScheduleAddonPrices_TourAddons_TourAddonId",
                        column: x => x.TourAddonId,
                        principalSchema: "tours",
                        principalTable: "TourAddons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TourScheduleAddonPrices_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourScheduleCapacities",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalSlots = table.Column<int>(type: "int", nullable: false),
                    SoldSlots = table.Column<int>(type: "int", nullable: false),
                    HeldSlots = table.Column<int>(type: "int", nullable: false),
                    BlockedSlots = table.Column<int>(type: "int", nullable: false),
                    MinGuestsToOperate = table.Column<int>(type: "int", nullable: true),
                    MaxGuestsPerBooking = table.Column<int>(type: "int", nullable: true),
                    WarningThreshold = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AllowWaitlist = table.Column<bool>(type: "bit", nullable: false),
                    AutoCloseWhenFull = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourScheduleCapacities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourScheduleCapacities_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourSchedulePrices",
                schema: "tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceType = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Taxes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Fees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinAge = table.Column<int>(type: "int", nullable: true),
                    MaxAge = table.Column<int>(type: "int", nullable: true),
                    MinQuantity = table.Column<int>(type: "int", nullable: true),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedTax = table.Column<bool>(type: "bit", nullable: false),
                    IsIncludedFee = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_TourSchedulePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourSchedulePrices_TourSchedules_TourScheduleId",
                        column: x => x.TourScheduleId,
                        principalSchema: "tours",
                        principalTable: "TourSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourAddons_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourAddons",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourAddons_TenantId_TourId_Code",
                schema: "tours",
                table: "TourAddons",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourAddons_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourAddons",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourAddons_TenantId_TourId_Type",
                schema: "tours",
                table: "TourAddons",
                columns: new[] { "TenantId", "TourId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TourAddons_TourId",
                schema: "tours",
                table: "TourAddons",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourContacts_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourContacts",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourContacts_TenantId_TourId_ContactType",
                schema: "tours",
                table: "TourContacts",
                columns: new[] { "TenantId", "TourId", "ContactType" });

            migrationBuilder.CreateIndex(
                name: "IX_TourContacts_TenantId_TourId_IsPrimary",
                schema: "tours",
                table: "TourContacts",
                columns: new[] { "TenantId", "TourId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_TourContacts_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourContacts",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourContacts_TourId",
                schema: "tours",
                table: "TourContacts",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourDropoffPoints_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourDropoffPoints",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourDropoffPoints_TenantId_TourId_Code",
                schema: "tours",
                table: "TourDropoffPoints",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourDropoffPoints_TenantId_TourId_IsDefault",
                schema: "tours",
                table: "TourDropoffPoints",
                columns: new[] { "TenantId", "TourId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TourDropoffPoints_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourDropoffPoints",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourDropoffPoints_TourId",
                schema: "tours",
                table: "TourDropoffPoints",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourFaqs_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourFaqs",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourFaqs_TenantId_TourId_IsHighlighted",
                schema: "tours",
                table: "TourFaqs",
                columns: new[] { "TenantId", "TourId", "IsHighlighted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourFaqs_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourFaqs",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourFaqs_TenantId_TourId_Type",
                schema: "tours",
                table: "TourFaqs",
                columns: new[] { "TenantId", "TourId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TourFaqs_TourId",
                schema: "tours",
                table: "TourFaqs",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourImages",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TenantId_TourId_IsCover",
                schema: "tours",
                table: "TourImages",
                columns: new[] { "TenantId", "TourId", "IsCover" });

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TenantId_TourId_IsPrimary",
                schema: "tours",
                table: "TourImages",
                columns: new[] { "TenantId", "TourId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourImages",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourImages_TourId",
                schema: "tours",
                table: "TourImages",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryDays_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourItineraryDays",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryDays_TenantId_TourId_DayNumber",
                schema: "tours",
                table: "TourItineraryDays",
                columns: new[] { "TenantId", "TourId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryDays_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourItineraryDays",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryDays_TourId",
                schema: "tours",
                table: "TourItineraryDays",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryItems_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourItineraryItems",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryItems_TenantId_TourItineraryDayId_SortOrder",
                schema: "tours",
                table: "TourItineraryItems",
                columns: new[] { "TenantId", "TourItineraryDayId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryItems_TenantId_TourItineraryDayId_Type",
                schema: "tours",
                table: "TourItineraryItems",
                columns: new[] { "TenantId", "TourItineraryDayId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraryItems_TourItineraryDayId",
                schema: "tours",
                table: "TourItineraryItems",
                column: "TourItineraryDayId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPickupPoints_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPickupPoints",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPickupPoints_TenantId_TourId_Code",
                schema: "tours",
                table: "TourPickupPoints",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPickupPoints_TenantId_TourId_IsDefault",
                schema: "tours",
                table: "TourPickupPoints",
                columns: new[] { "TenantId", "TourId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPickupPoints_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourPickupPoints",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPickupPoints_TourId",
                schema: "tours",
                table: "TourPickupPoints",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPolicies_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourPolicies",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPolicies_TenantId_TourId_Code",
                schema: "tours",
                table: "TourPolicies",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPolicies_TenantId_TourId_SortOrder",
                schema: "tours",
                table: "TourPolicies",
                columns: new[] { "TenantId", "TourId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPolicies_TenantId_TourId_Type",
                schema: "tours",
                table: "TourPolicies",
                columns: new[] { "TenantId", "TourId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TourPolicies_TourId",
                schema: "tours",
                table: "TourPolicies",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourReviews_TenantId_PublishedAt",
                schema: "tours",
                table: "TourReviews",
                columns: new[] { "TenantId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourReviews_TenantId_TourId_IsApproved_IsPublic",
                schema: "tours",
                table: "TourReviews",
                columns: new[] { "TenantId", "TourId", "IsApproved", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_TourReviews_TenantId_TourId_IsDeleted",
                schema: "tours",
                table: "TourReviews",
                columns: new[] { "TenantId", "TourId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourReviews_TenantId_TourId_Status",
                schema: "tours",
                table: "TourReviews",
                columns: new[] { "TenantId", "TourId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourReviews_TourId",
                schema: "tours",
                table: "TourReviews",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_Code",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_IsFeatured_IsFeaturedOnHome",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "IsFeatured", "IsFeaturedOnHome" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_PrimaryLocationId",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "PrimaryLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_ProviderId",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "ProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_Slug",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_TenantId_Status_IsActive_IsDeleted",
                schema: "tours",
                table: "Tours",
                columns: new[] { "TenantId", "Status", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleAddonPrices_TenantId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourScheduleAddonPrices",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleAddonPrices_TenantId_TourScheduleId_SortOrder",
                schema: "tours",
                table: "TourScheduleAddonPrices",
                columns: new[] { "TenantId", "TourScheduleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleAddonPrices_TenantId_TourScheduleId_TourAddonId",
                schema: "tours",
                table: "TourScheduleAddonPrices",
                columns: new[] { "TenantId", "TourScheduleId", "TourAddonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleAddonPrices_TourAddonId",
                schema: "tours",
                table: "TourScheduleAddonPrices",
                column: "TourAddonId");

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleAddonPrices_TourScheduleId",
                schema: "tours",
                table: "TourScheduleAddonPrices",
                column: "TourScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleCapacities_TenantId_Status_IsActive_IsDeleted",
                schema: "tours",
                table: "TourScheduleCapacities",
                columns: new[] { "TenantId", "Status", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleCapacities_TenantId_TourScheduleId",
                schema: "tours",
                table: "TourScheduleCapacities",
                columns: new[] { "TenantId", "TourScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourScheduleCapacities_TourScheduleId",
                schema: "tours",
                table: "TourScheduleCapacities",
                column: "TourScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_IsActive_IsDeleted",
                schema: "tours",
                table: "TourSchedulePrices",
                columns: new[] { "TenantId", "TourScheduleId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_IsDefault",
                schema: "tours",
                table: "TourSchedulePrices",
                columns: new[] { "TenantId", "TourScheduleId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType",
                schema: "tours",
                table: "TourSchedulePrices",
                columns: new[] { "TenantId", "TourScheduleId", "PriceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedulePrices_TourScheduleId",
                schema: "tours",
                table: "TourSchedulePrices",
                column: "TourScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TenantId_BookingCutoffAt",
                schema: "tours",
                table: "TourSchedules",
                columns: new[] { "TenantId", "BookingCutoffAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TenantId_Status_IsActive_IsDeleted",
                schema: "tours",
                table: "TourSchedules",
                columns: new[] { "TenantId", "Status", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TenantId_TourId_Code",
                schema: "tours",
                table: "TourSchedules",
                columns: new[] { "TenantId", "TourId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TenantId_TourId_DepartureDate",
                schema: "tours",
                table: "TourSchedules",
                columns: new[] { "TenantId", "TourId", "DepartureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TourId",
                schema: "tours",
                table: "TourSchedules",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourContacts",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourDropoffPoints",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourFaqs",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourImages",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourItineraryItems",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPickupPoints",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourPolicies",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourReviews",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourScheduleAddonPrices",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourScheduleCapacities",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourSchedulePrices",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourItineraryDays",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourAddons",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "TourSchedules",
                schema: "tours");

            migrationBuilder.DropTable(
                name: "Tours",
                schema: "tours");
        }
    }
}
