IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'flight') IS NULL EXEC(N'CREATE SCHEMA [flight];');
GO

IF SCHEMA_ID(N'hotels') IS NULL EXEC(N'CREATE SCHEMA [hotels];');
GO

IF SCHEMA_ID(N'fleet') IS NULL EXEC(N'CREATE SCHEMA [fleet];');
GO

IF SCHEMA_ID(N'geo') IS NULL EXEC(N'CREATE SCHEMA [geo];');
GO

IF SCHEMA_ID(N'catalog') IS NULL EXEC(N'CREATE SCHEMA [catalog];');
GO

IF SCHEMA_ID(N'cms') IS NULL EXEC(N'CREATE SCHEMA [cms];');
GO

IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
GO

IF SCHEMA_ID(N'bus') IS NULL EXEC(N'CREATE SCHEMA [bus];');
GO

IF SCHEMA_ID(N'train') IS NULL EXEC(N'CREATE SCHEMA [train];');
GO

IF SCHEMA_ID(N'tenants') IS NULL EXEC(N'CREATE SCHEMA [tenants];');
GO

CREATE TABLE [flight].[AircraftModels] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Manufacturer] nvarchar(100) NOT NULL,
    [Model] nvarchar(100) NOT NULL,
    [TypicalSeatCapacity] int NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_AircraftModels] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [flight].[Airlines] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IataCode] nvarchar(8) NULL,
    [IcaoCode] nvarchar(8) NULL,
    [LogoUrl] nvarchar(1000) NULL,
    [WebsiteUrl] nvarchar(1000) NULL,
    [SupportPhone] nvarchar(50) NULL,
    [SupportEmail] nvarchar(200) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Airlines] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [AvatarUrl] nvarchar(500) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [hotels].[BedTypes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_BedTypes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [geo].[GeoSyncLogs] (
    [Id] uniqueidentifier NOT NULL,
    [Source] nvarchar(100) NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [Depth] int NOT NULL,
    [IsSuccess] bit NOT NULL,
    [HttpStatus] int NOT NULL,
    [ProvincesInserted] int NOT NULL,
    [ProvincesUpdated] int NOT NULL,
    [DistrictsInserted] int NOT NULL,
    [DistrictsUpdated] int NOT NULL,
    [WardsInserted] int NOT NULL,
    [WardsUpdated] int NOT NULL,
    [ErrorMessage] nvarchar(1000) NULL,
    [ErrorDetail] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_GeoSyncLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [hotels].[HotelAmenities] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Scope] int NOT NULL,
    [Kind] int NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [SortOrder] int NOT NULL,
    [IconKey] nvarchar(200) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HotelAmenities] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [hotels].[Hotels] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Slug] nvarchar(300) NULL,
    [LocationId] uniqueidentifier NULL,
    [AddressLine] nvarchar(500) NULL,
    [City] nvarchar(200) NULL,
    [Province] nvarchar(200) NULL,
    [CountryCode] nvarchar(10) NULL,
    [Latitude] decimal(9,6) NULL,
    [Longitude] decimal(9,6) NULL,
    [TimeZone] nvarchar(100) NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [StarRating] int NOT NULL,
    [Status] int NOT NULL,
    [DefaultCheckInTime] time(0) NULL,
    [DefaultCheckOutTime] time(0) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [WebsiteUrl] nvarchar(1000) NULL,
    [SeoTitle] nvarchar(300) NULL,
    [SeoDescription] nvarchar(2000) NULL,
    [SeoKeywords] nvarchar(2000) NULL,
    [CanonicalUrl] nvarchar(1000) NULL,
    [Robots] nvarchar(200) NULL,
    [OgImageUrl] nvarchar(1000) NULL,
    [SchemaJsonLd] nvarchar(max) NULL,
    [CoverMediaAssetId] uniqueidentifier NULL,
    [CoverImageUrl] nvarchar(1000) NULL,
    [PoliciesJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Hotels] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [hotels].[MealPlans] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_MealPlans] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [cms].[MediaAssets] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [Title] nvarchar(200) NULL,
    [AltText] nvarchar(300) NULL,
    [StorageProvider] nvarchar(50) NOT NULL,
    [StorageKey] nvarchar(500) NOT NULL,
    [PublicUrl] nvarchar(1000) NULL,
    [MimeType] nvarchar(100) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Width] int NULL,
    [Height] int NULL,
    [ChecksumSha256] nvarchar(128) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [cms].[NewsCategories] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [cms].[NewsRedirects] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [FromPath] nvarchar(500) NOT NULL,
    [ToPath] nvarchar(1000) NOT NULL,
    [StatusCode] int NOT NULL,
    [IsRegex] bit NOT NULL,
    [Reason] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsRedirects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [cms].[NewsTags] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(200) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsTags] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [auth].[Permissions] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(200) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Category] nvarchar(100) NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [geo].[Provinces] (
    [Id] uniqueidentifier NOT NULL,
    [Code] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NameEn] nvarchar(200) NULL,
    [Slug] nvarchar(250) NULL,
    [Type] nvarchar(50) NULL,
    [RegionCode] int NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [hotels].[RoomAmenities] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Kind] int NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [SortOrder] int NOT NULL,
    [IconKey] nvarchar(200) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomAmenities] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [fleet].[SeatMaps] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehicleType] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [TotalRows] int NOT NULL,
    [TotalColumns] int NOT NULL,
    [DeckCount] int NOT NULL DEFAULT 1,
    [LayoutVersion] nvarchar(50) NULL,
    [SeatLabelScheme] nvarchar(50) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_SeatMaps] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [cms].[SiteSettings] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [SiteName] nvarchar(200) NOT NULL,
    [SiteUrl] nvarchar(1000) NULL,
    [DefaultRobots] nvarchar(200) NULL,
    [DefaultOgImageUrl] nvarchar(1000) NULL,
    [DefaultTwitterCard] nvarchar(50) NULL,
    [DefaultTwitterSite] nvarchar(100) NULL,
    [DefaultSchemaJsonLd] nvarchar(max) NULL,
    [BrandLogoUrl] nvarchar(1000) NULL,
    [SupportEmail] nvarchar(200) NULL,
    [SupportPhone] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [tenants].[Tenants] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(32) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Type] int NOT NULL,
    [Status] int NOT NULL,
    [HoldMinutes] int NOT NULL DEFAULT 5,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [fleet].[VehicleModels] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehicleType] int NOT NULL,
    [Manufacturer] nvarchar(100) NOT NULL,
    [ModelName] nvarchar(120) NOT NULL,
    [ModelYear] int NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [flight].[CabinSeatMaps] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AircraftModelId] uniqueidentifier NOT NULL,
    [CabinClass] int NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [TotalRows] int NOT NULL,
    [TotalColumns] int NOT NULL,
    [LayoutVersion] nvarchar(50) NULL,
    [SeatLabelScheme] nvarchar(50) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CabinSeatMaps] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_flight_CabinSeatMaps_Cols] CHECK ([TotalColumns] > 0),
    CONSTRAINT [CK_flight_CabinSeatMaps_Rows] CHECK ([TotalRows] > 0),
    CONSTRAINT [FK_CabinSeatMaps_AircraftModels_AircraftModelId] FOREIGN KEY ([AircraftModelId]) REFERENCES [flight].[AircraftModels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[Aircrafts] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AircraftModelId] uniqueidentifier NOT NULL,
    [AirlineId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Registration] nvarchar(30) NULL,
    [Name] nvarchar(200) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Aircrafts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Aircrafts_AircraftModels_AircraftModelId] FOREIGN KEY ([AircraftModelId]) REFERENCES [flight].[AircraftModels] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Aircrafts_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [flight].[Airlines] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[AncillaryDefinitions] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AirlineId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Type] int NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [RulesJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_AncillaryDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AncillaryDefinitions_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [flight].[Airlines] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[FareClasses] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AirlineId] uniqueidentifier NOT NULL,
    [Code] nvarchar(10) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [CabinClass] int NOT NULL,
    [IsRefundable] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsChangeable] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_FareClasses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FareClasses_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [flight].[Airlines] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [hotels].[CancellationPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Type] int NOT NULL,
    [Description] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CancellationPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CancellationPolicies_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[CheckInOutRules] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [CheckInFrom] time(0) NOT NULL,
    [CheckInTo] time(0) NOT NULL,
    [CheckOutFrom] time(0) NOT NULL,
    [CheckOutTo] time(0) NOT NULL,
    [AllowsEarlyCheckIn] bit NOT NULL,
    [AllowsLateCheckOut] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CheckInOutRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CheckInOutRules_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[ExtraServices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Type] int NOT NULL,
    [Unit] int NOT NULL,
    [Taxable] bit NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExtraServices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExtraServices_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[HotelAmenityLinks] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [AmenityId] uniqueidentifier NOT NULL,
    [IsHighlighted] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HotelAmenityLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HotelAmenityLinks_HotelAmenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [hotels].[HotelAmenities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HotelAmenityLinks_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[HotelContacts] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [ContactName] nvarchar(200) NOT NULL,
    [RoleTitle] nvarchar(200) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [Notes] nvarchar(2000) NULL,
    [IsPrimary] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HotelContacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HotelContacts_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[HotelImages] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Kind] int NOT NULL,
    [SortOrder] int NOT NULL,
    [MediaAssetId] uniqueidentifier NULL,
    [ImageUrl] nvarchar(1000) NULL,
    [Title] nvarchar(200) NULL,
    [AltText] nvarchar(300) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HotelImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HotelImages_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[HotelReviews] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NULL,
    [CustomerUserId] uniqueidentifier NULL,
    [Rating] int NOT NULL,
    [Title] nvarchar(300) NULL,
    [Content] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [IsVerifiedStay] bit NOT NULL,
    [HelpfulCount] int NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_HotelReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HotelReviews_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[PropertyPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [PolicyJson] nvarchar(max) NULL,
    [Notes] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PropertyPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PropertyPolicies_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [AreaSquareMeters] int NULL,
    [HasBalcony] bit NULL,
    [HasWindow] bit NULL,
    [SmokingAllowed] bit NULL,
    [DefaultAdults] int NOT NULL,
    [DefaultChildren] int NOT NULL,
    [MaxAdults] int NOT NULL,
    [MaxChildren] int NOT NULL,
    [MaxGuests] int NOT NULL,
    [TotalUnits] int NOT NULL,
    [CoverMediaAssetId] uniqueidentifier NULL,
    [CoverImageUrl] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [IsActive] bit NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypes_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [cms].[NewsPosts] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Slug] nvarchar(300) NOT NULL,
    [Summary] nvarchar(2000) NULL,
    [ContentMarkdown] nvarchar(max) NOT NULL,
    [ContentHtml] nvarchar(max) NOT NULL,
    [CoverMediaAssetId] uniqueidentifier NULL,
    [CoverImageUrl] nvarchar(1000) NULL,
    [SeoTitle] nvarchar(300) NULL,
    [SeoDescription] nvarchar(2000) NULL,
    [SeoKeywords] nvarchar(2000) NULL,
    [CanonicalUrl] nvarchar(1000) NULL,
    [Robots] nvarchar(200) NULL,
    [OgTitle] nvarchar(300) NULL,
    [OgDescription] nvarchar(2000) NULL,
    [OgImageUrl] nvarchar(1000) NULL,
    [OgType] nvarchar(50) NULL,
    [TwitterCard] nvarchar(50) NULL,
    [TwitterSite] nvarchar(100) NULL,
    [TwitterCreator] nvarchar(100) NULL,
    [TwitterTitle] nvarchar(300) NULL,
    [TwitterDescription] nvarchar(2000) NULL,
    [TwitterImageUrl] nvarchar(1000) NULL,
    [SchemaJsonLd] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [ScheduledAt] datetimeoffset NULL,
    [PublishedAt] datetimeoffset NULL,
    [UnpublishedAt] datetimeoffset NULL,
    [AuthorUserId] uniqueidentifier NULL,
    [EditorUserId] uniqueidentifier NULL,
    [LastEditedAt] datetimeoffset NULL,
    [ViewCount] int NOT NULL,
    [WordCount] int NOT NULL,
    [ReadingTimeMinutes] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsPosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NewsPosts_AspNetUsers_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_NewsPosts_AspNetUsers_EditorUserId] FOREIGN KEY ([EditorUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_NewsPosts_MediaAssets_CoverMediaAssetId] FOREIGN KEY ([CoverMediaAssetId]) REFERENCES [cms].[MediaAssets] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [auth].[RolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [PermissionId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [auth].[Permissions] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [auth].[UserPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PermissionId] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NULL,
    [Effect] int NOT NULL,
    [Reason] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserPermissions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [auth].[Permissions] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [geo].[Districts] (
    [Id] uniqueidentifier NOT NULL,
    [Code] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NameEn] nvarchar(200) NULL,
    [Slug] nvarchar(250) NULL,
    [Type] nvarchar(50) NULL,
    [ProvinceId] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Districts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Districts_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [geo].[Provinces] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fleet].[Seats] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [SeatMapId] uniqueidentifier NOT NULL,
    [SeatNumber] nvarchar(20) NOT NULL,
    [RowIndex] int NOT NULL,
    [ColumnIndex] int NOT NULL,
    [DeckIndex] int NOT NULL,
    [SeatType] int NOT NULL,
    [SeatClass] int NOT NULL,
    [IsAisle] bit NOT NULL,
    [IsWindow] bit NOT NULL,
    [PriceModifier] decimal(18,2) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Seats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Seats_SeatMaps_SeatMapId] FOREIGN KEY ([SeatMapId]) REFERENCES [fleet].[SeatMaps] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tenants].[TenantRoles] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TenantRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantRoles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants].[Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tenants].[TenantUsers] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [IsOwner] bit NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TenantUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantUsers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants].[Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[CabinSeats] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [CabinSeatMapId] uniqueidentifier NOT NULL,
    [SeatNumber] nvarchar(10) NOT NULL,
    [RowIndex] int NOT NULL,
    [ColumnIndex] int NOT NULL,
    [IsAisle] bit NOT NULL,
    [IsWindow] bit NOT NULL,
    [SeatClass] nvarchar(50) NULL,
    [PriceModifier] decimal(18,2) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CabinSeats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CabinSeats_CabinSeatMaps_CabinSeatMapId] FOREIGN KEY ([CabinSeatMapId]) REFERENCES [flight].[CabinSeatMaps] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[FareRules] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [FareClassId] uniqueidentifier NOT NULL,
    [RulesJson] nvarchar(max) NOT NULL DEFAULT N'{}',
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_FareRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FareRules_FareClasses_FareClassId] FOREIGN KEY ([FareClassId]) REFERENCES [flight].[FareClasses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[CancellationPolicyRules] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CancellationPolicyId] uniqueidentifier NOT NULL,
    [CancelBeforeHours] int NULL,
    [CancelBeforeDays] int NULL,
    [ChargeType] int NOT NULL,
    [ChargeValue] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [Priority] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CancellationPolicyRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CancellationPolicyRules_CancellationPolicies_CancellationPolicyId] FOREIGN KEY ([CancellationPolicyId]) REFERENCES [hotels].[CancellationPolicies] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[ExtraServicePrices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ExtraServiceId] uniqueidentifier NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ExtraServicePrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExtraServicePrices_ExtraServices_ExtraServiceId] FOREIGN KEY ([ExtraServiceId]) REFERENCES [hotels].[ExtraServices] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RatePlans] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [HotelId] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Type] int NOT NULL,
    [Status] int NOT NULL,
    [CancellationPolicyId] uniqueidentifier NULL,
    [CheckInOutRuleId] uniqueidentifier NULL,
    [PropertyPolicyId] uniqueidentifier NULL,
    [Refundable] bit NOT NULL,
    [BreakfastIncluded] bit NOT NULL,
    [MinNights] int NULL,
    [MaxNights] int NULL,
    [MinAdvanceDays] int NULL,
    [MaxAdvanceDays] int NULL,
    [RequiresGuarantee] bit NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RatePlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RatePlans_CancellationPolicies_CancellationPolicyId] FOREIGN KEY ([CancellationPolicyId]) REFERENCES [hotels].[CancellationPolicies] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RatePlans_CheckInOutRules_CheckInOutRuleId] FOREIGN KEY ([CheckInOutRuleId]) REFERENCES [hotels].[CheckInOutRules] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RatePlans_Hotels_HotelId] FOREIGN KEY ([HotelId]) REFERENCES [hotels].[Hotels] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RatePlans_PropertyPolicies_PropertyPolicyId] FOREIGN KEY ([PropertyPolicyId]) REFERENCES [hotels].[PropertyPolicies] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [hotels].[InventoryHolds] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NULL,
    [BookingItemId] uniqueidentifier NULL,
    [CheckInDate] date NOT NULL,
    [CheckOutDate] date NOT NULL,
    [Units] int NOT NULL,
    [Status] int NOT NULL,
    [HoldExpiresAt] datetimeoffset NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [Notes] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_InventoryHolds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryHolds_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomAmenityLinks] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [AmenityId] uniqueidentifier NOT NULL,
    [IsHighlighted] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomAmenityLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomAmenityLinks_RoomAmenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [hotels].[RoomAmenities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoomAmenityLinks_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypeBeds] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [BedTypeId] uniqueidentifier NOT NULL,
    [Quantity] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypeBeds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypeBeds_BedTypes_BedTypeId] FOREIGN KEY ([BedTypeId]) REFERENCES [hotels].[BedTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoomTypeBeds_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypeImages] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [Kind] int NOT NULL,
    [SortOrder] int NOT NULL,
    [MediaAssetId] uniqueidentifier NULL,
    [ImageUrl] nvarchar(1000) NULL,
    [Title] nvarchar(200) NULL,
    [AltText] nvarchar(300) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypeImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypeImages_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypeInventories] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [TotalUnits] int NOT NULL,
    [SoldUnits] int NOT NULL,
    [HeldUnits] int NOT NULL,
    [Status] int NOT NULL,
    [MinNights] int NULL,
    [MaxNights] int NULL,
    [Notes] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypeInventories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypeInventories_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypeMealPlans] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [MealPlanId] uniqueidentifier NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypeMealPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypeMealPlans_MealPlans_MealPlanId] FOREIGN KEY ([MealPlanId]) REFERENCES [hotels].[MealPlans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoomTypeMealPlans_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypeOccupancyRules] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [MinAdults] int NOT NULL,
    [MaxAdults] int NOT NULL,
    [MinChildren] int NOT NULL,
    [MaxChildren] int NOT NULL,
    [MinGuests] int NOT NULL,
    [MaxGuests] int NOT NULL,
    [AllowsInfants] bit NOT NULL,
    [MaxInfants] int NULL,
    [Notes] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypeOccupancyRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypeOccupancyRules_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RoomTypePolicies] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [PolicyJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RoomTypePolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoomTypePolicies_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [cms].[NewsPostCategories] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [PostId] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsPostCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NewsPostCategories_NewsCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [cms].[NewsCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_NewsPostCategories_NewsPosts_PostId] FOREIGN KEY ([PostId]) REFERENCES [cms].[NewsPosts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [cms].[NewsPostRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [PostId] uniqueidentifier NOT NULL,
    [VersionNumber] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Summary] nvarchar(2000) NULL,
    [ContentMarkdown] nvarchar(max) NOT NULL,
    [ContentHtml] nvarchar(max) NOT NULL,
    [SeoTitle] nvarchar(300) NULL,
    [SeoDescription] nvarchar(2000) NULL,
    [CanonicalUrl] nvarchar(1000) NULL,
    [Robots] nvarchar(200) NULL,
    [OgTitle] nvarchar(300) NULL,
    [OgDescription] nvarchar(2000) NULL,
    [OgImageUrl] nvarchar(1000) NULL,
    [TwitterCard] nvarchar(50) NULL,
    [TwitterTitle] nvarchar(300) NULL,
    [TwitterDescription] nvarchar(2000) NULL,
    [TwitterImageUrl] nvarchar(1000) NULL,
    [SchemaJsonLd] nvarchar(max) NULL,
    [ChangeNote] nvarchar(1000) NULL,
    [EditorUserId] uniqueidentifier NULL,
    [EditedAt] datetimeoffset NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsPostRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NewsPostRevisions_AspNetUsers_EditorUserId] FOREIGN KEY ([EditorUserId]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK_NewsPostRevisions_NewsPosts_PostId] FOREIGN KEY ([PostId]) REFERENCES [cms].[NewsPosts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [cms].[NewsPostTags] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [PostId] uniqueidentifier NOT NULL,
    [TagId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NewsPostTags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NewsPostTags_NewsPosts_PostId] FOREIGN KEY ([PostId]) REFERENCES [cms].[NewsPosts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_NewsPostTags_NewsTags_TagId] FOREIGN KEY ([TagId]) REFERENCES [cms].[NewsTags] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [geo].[Wards] (
    [Id] uniqueidentifier NOT NULL,
    [Code] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NameEn] nvarchar(200) NULL,
    [Slug] nvarchar(250) NULL,
    [Type] nvarchar(50) NULL,
    [DistrictId] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Wards] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Wards_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [geo].[Districts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tenants].[TenantRolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TenantRoleId] uniqueidentifier NOT NULL,
    [PermissionId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TenantRolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantRolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [auth].[Permissions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantRolePermissions_TenantRoles_TenantRoleId] FOREIGN KEY ([TenantRoleId]) REFERENCES [tenants].[TenantRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantRolePermissions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants].[Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tenants].[TenantUserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TenantRoleId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TenantUserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantUserRoles_TenantRoles_TenantRoleId] FOREIGN KEY ([TenantRoleId]) REFERENCES [tenants].[TenantRoles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantUserRoles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants].[Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RatePlanPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PolicyJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RatePlanPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RatePlanPolicies_RatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [hotels].[RatePlans] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[RatePlanRoomTypes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [BasePrice] decimal(18,2) NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RatePlanRoomTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RatePlanRoomTypes_RatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [hotels].[RatePlans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RatePlanRoomTypes_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [hotels].[RoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [catalog].[Locations] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NormalizedName] nvarchar(250) NOT NULL,
    [ShortName] nvarchar(100) NULL,
    [Code] nvarchar(50) NULL,
    [AirportIataCode] nvarchar(10) NULL,
    [AirportIcaoCode] nvarchar(10) NULL,
    [TrainStationCode] nvarchar(50) NULL,
    [BusStationCode] nvarchar(50) NULL,
    [TimeZone] nvarchar(64) NULL,
    [AddressLine] nvarchar(300) NULL,
    [ProvinceId] uniqueidentifier NULL,
    [DistrictId] uniqueidentifier NULL,
    [WardId] uniqueidentifier NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Locations_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [geo].[Districts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Locations_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [geo].[Provinces] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Locations_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [geo].[Wards] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[DailyRates] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RatePlanRoomTypeId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [BasePrice] decimal(18,2) NULL,
    [Taxes] decimal(18,2) NULL,
    [Fees] decimal(18,2) NULL,
    [IsActive] bit NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_DailyRates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DailyRates_RatePlanRoomTypes_RatePlanRoomTypeId] FOREIGN KEY ([RatePlanRoomTypeId]) REFERENCES [hotels].[RatePlanRoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [hotels].[PromoRateOverrides] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RatePlanRoomTypeId] uniqueidentifier NOT NULL,
    [PromoCodeId] uniqueidentifier NULL,
    [PromoCode] nvarchar(80) NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [OverridePrice] decimal(18,2) NULL,
    [DiscountPercent] decimal(5,2) NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [ConditionsJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PromoRateOverrides] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PromoRateOverrides_RatePlanRoomTypes_RatePlanRoomTypeId] FOREIGN KEY ([RatePlanRoomTypeId]) REFERENCES [hotels].[RatePlanRoomTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[Airports] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [LocationId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [IataCode] nvarchar(8) NULL,
    [IcaoCode] nvarchar(8) NULL,
    [TimeZone] nvarchar(64) NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Airports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Airports_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [catalog].[Locations] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [catalog].[Providers] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(200) NOT NULL,
    [LegalName] nvarchar(250) NULL,
    [LogoUrl] nvarchar(500) NULL,
    [CoverUrl] nvarchar(500) NULL,
    [SupportPhone] nvarchar(50) NULL,
    [SupportEmail] nvarchar(200) NULL,
    [WebsiteUrl] nvarchar(300) NULL,
    [AddressLine] nvarchar(300) NULL,
    [LocationId] uniqueidentifier NULL,
    [ProvinceId] uniqueidentifier NULL,
    [DistrictId] uniqueidentifier NULL,
    [WardId] uniqueidentifier NULL,
    [RatingAverage] decimal(3,2) NULL,
    [RatingCount] int NOT NULL DEFAULT 0,
    [Description] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Providers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Providers_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [geo].[Districts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Providers_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [catalog].[Locations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Providers_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [geo].[Provinces] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Providers_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [geo].[Wards] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[StopPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [LocationId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [AddressLine] nvarchar(300) NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [Notes] nvarchar(max) NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_StopPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StopPoints_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [catalog].[Locations] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[StopPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [LocationId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [AddressLine] nvarchar(300) NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [Notes] nvarchar(max) NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_StopPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StopPoints_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [catalog].[Locations] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[Flights] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AirlineId] uniqueidentifier NOT NULL,
    [AircraftId] uniqueidentifier NOT NULL,
    [FromAirportId] uniqueidentifier NOT NULL,
    [ToAirportId] uniqueidentifier NOT NULL,
    [FlightNumber] nvarchar(20) NOT NULL,
    [DepartureAt] datetimeoffset NOT NULL,
    [ArrivalAt] datetimeoffset NOT NULL,
    [Status] int NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [Notes] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Flights] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Flights_Aircrafts_AircraftId] FOREIGN KEY ([AircraftId]) REFERENCES [flight].[Aircrafts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Flights_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [flight].[Airlines] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Flights_Airports_FromAirportId] FOREIGN KEY ([FromAirportId]) REFERENCES [flight].[Airports] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Flights_Airports_ToAirportId] FOREIGN KEY ([ToAirportId]) REFERENCES [flight].[Airports] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fleet].[Vehicles] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehicleType] int NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [VehicleModelId] uniqueidentifier NULL,
    [SeatMapId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [PlateNumber] nvarchar(50) NULL,
    [RegistrationNumber] nvarchar(50) NULL,
    [SeatCapacity] int NOT NULL,
    [InServiceFrom] datetimeoffset NULL,
    [InServiceTo] datetimeoffset NULL,
    [Status] nvarchar(50) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vehicles_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [catalog].[Providers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Vehicles_SeatMaps_SeatMapId] FOREIGN KEY ([SeatMapId]) REFERENCES [fleet].[SeatMaps] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Vehicles_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [fleet].[VehicleModels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[Routes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [FromStopPointId] uniqueidentifier NOT NULL,
    [ToStopPointId] uniqueidentifier NOT NULL,
    [EstimatedMinutes] int NOT NULL DEFAULT 0,
    [DistanceKm] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Routes_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [catalog].[Providers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Routes_StopPoints_FromStopPointId] FOREIGN KEY ([FromStopPointId]) REFERENCES [bus].[StopPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Routes_StopPoints_ToStopPointId] FOREIGN KEY ([ToStopPointId]) REFERENCES [bus].[StopPoints] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[Routes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [FromStopPointId] uniqueidentifier NOT NULL,
    [ToStopPointId] uniqueidentifier NOT NULL,
    [EstimatedMinutes] int NOT NULL DEFAULT 0,
    [DistanceKm] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Routes_StopPoints_FromStopPointId] FOREIGN KEY ([FromStopPointId]) REFERENCES [train].[StopPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Routes_StopPoints_ToStopPointId] FOREIGN KEY ([ToStopPointId]) REFERENCES [train].[StopPoints] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[Offers] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [AirlineId] uniqueidentifier NOT NULL,
    [FlightId] uniqueidentifier NOT NULL,
    [FareClassId] uniqueidentifier NOT NULL,
    [Status] int NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [BaseFare] decimal(18,2) NOT NULL,
    [TaxesFees] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [SeatsAvailable] int NOT NULL DEFAULT 9,
    [RequestedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [ConditionsJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_flight_Offers_ExpiresAt] CHECK ([ExpiresAt] > [RequestedAt]),
    CONSTRAINT [CK_flight_Offers_TotalPrice] CHECK ([TotalPrice] >= 0),
    CONSTRAINT [FK_Offers_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [flight].[Airlines] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_FareClasses_FareClassId] FOREIGN KEY ([FareClassId]) REFERENCES [flight].[FareClasses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_Flights_FlightId] FOREIGN KEY ([FlightId]) REFERENCES [flight].[Flights] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fleet].[BusVehicleDetails] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehicleId] uniqueidentifier NOT NULL,
    [BusType] nvarchar(100) NULL,
    [AmenitiesJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_BusVehicleDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BusVehicleDetails_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [fleet].[Vehicles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[RouteStops] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [StopPointId] uniqueidentifier NOT NULL,
    [StopIndex] int NOT NULL,
    [DistanceFromStartKm] int NULL,
    [MinutesFromStart] int NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_RouteStops] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RouteStops_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [bus].[Routes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RouteStops_StopPoints_StopPointId] FOREIGN KEY ([StopPointId]) REFERENCES [bus].[StopPoints] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[Trips] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [VehicleId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Status] int NOT NULL,
    [DepartureAt] datetimeoffset NOT NULL,
    [ArrivalAt] datetimeoffset NOT NULL,
    [FareRulesJson] nvarchar(max) NULL,
    [BaggagePolicyJson] nvarchar(max) NULL,
    [BoardingPolicyJson] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Trips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trips_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [catalog].[Providers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Trips_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [bus].[Routes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Trips_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [fleet].[Vehicles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[RouteStops] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [StopPointId] uniqueidentifier NOT NULL,
    [StopIndex] int NOT NULL,
    [DistanceFromStartKm] int NULL,
    [MinutesFromStart] int NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_RouteStops] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RouteStops_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [train].[Routes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RouteStops_StopPoints_StopPointId] FOREIGN KEY ([StopPointId]) REFERENCES [train].[StopPoints] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[Trips] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProviderId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [TrainNumber] nvarchar(20) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Status] int NOT NULL,
    [DepartureAt] datetimeoffset NOT NULL,
    [ArrivalAt] datetimeoffset NOT NULL,
    [FareRulesJson] nvarchar(max) NULL,
    [BaggagePolicyJson] nvarchar(max) NULL,
    [BoardingPolicyJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_Trips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trips_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [train].[Routes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[OfferSegments] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [OfferId] uniqueidentifier NOT NULL,
    [SegmentIndex] int NOT NULL,
    [FromAirportId] uniqueidentifier NOT NULL,
    [ToAirportId] uniqueidentifier NOT NULL,
    [DepartureAt] datetimeoffset NOT NULL,
    [ArrivalAt] datetimeoffset NOT NULL,
    [FlightNumber] nvarchar(20) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_OfferSegments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OfferSegments_Airports_FromAirportId] FOREIGN KEY ([FromAirportId]) REFERENCES [flight].[Airports] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OfferSegments_Airports_ToAirportId] FOREIGN KEY ([ToAirportId]) REFERENCES [flight].[Airports] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OfferSegments_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [flight].[Offers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [flight].[OfferTaxFeeLines] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [TenantId] uniqueidentifier NOT NULL,
    [OfferId] uniqueidentifier NOT NULL,
    [LineType] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [CurrencyCode] char(3) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')),
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_OfferTaxFeeLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OfferTaxFeeLines_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [flight].[Offers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[TripStopTimes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [StopPointId] uniqueidentifier NOT NULL,
    [StopIndex] int NOT NULL,
    [ArriveAt] datetimeoffset NULL,
    [DepartAt] datetimeoffset NULL,
    [MinutesFromStart] int NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripStopTimes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TripStopTimes_StopPoints_StopPointId] FOREIGN KEY ([StopPointId]) REFERENCES [bus].[StopPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripStopTimes_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [bus].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[TrainCars] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [CarNumber] nvarchar(20) NOT NULL,
    [CarType] int NOT NULL,
    [CabinClass] nvarchar(50) NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TrainCars] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrainCars_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [train].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[TripStopTimes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [StopPointId] uniqueidentifier NOT NULL,
    [StopIndex] int NOT NULL,
    [ArriveAt] datetimeoffset NULL,
    [DepartAt] datetimeoffset NULL,
    [MinutesFromStart] int NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripStopTimes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TripStopTimes_StopPoints_StopPointId] FOREIGN KEY ([StopPointId]) REFERENCES [train].[StopPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripStopTimes_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [train].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[TripSeatHolds] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [SeatId] uniqueidentifier NOT NULL,
    [FromTripStopTimeId] uniqueidentifier NOT NULL,
    [ToTripStopTimeId] uniqueidentifier NOT NULL,
    [FromStopIndex] int NOT NULL,
    [ToStopIndex] int NOT NULL,
    [Status] int NOT NULL,
    [UserId] uniqueidentifier NULL,
    [BookingId] uniqueidentifier NULL,
    [HoldToken] nvarchar(100) NOT NULL,
    [HoldExpiresAt] datetimeoffset NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripSeatHolds] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_bus_TripSeatHolds_StopIndex] CHECK ([FromStopIndex] < [ToStopIndex]),
    CONSTRAINT [FK_TripSeatHolds_Seats_SeatId] FOREIGN KEY ([SeatId]) REFERENCES [fleet].[Seats] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_TripStopTimes_FromTripStopTimeId] FOREIGN KEY ([FromTripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_TripStopTimes_ToTripStopTimeId] FOREIGN KEY ([ToTripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [bus].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[TripSegmentPrices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [FromTripStopTimeId] uniqueidentifier NOT NULL,
    [ToTripStopTimeId] uniqueidentifier NOT NULL,
    [FromStopIndex] int NOT NULL,
    [ToStopIndex] int NOT NULL,
    [CurrencyCode] nvarchar(3) NOT NULL,
    [BaseFare] decimal(18,2) NOT NULL,
    [TaxesFees] decimal(18,2) NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripSegmentPrices] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_bus_TripSegmentPrices_StopIndex] CHECK ([FromStopIndex] < [ToStopIndex]),
    CONSTRAINT [FK_TripSegmentPrices_TripStopTimes_FromTripStopTimeId] FOREIGN KEY ([FromTripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSegmentPrices_TripStopTimes_ToTripStopTimeId] FOREIGN KEY ([ToTripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSegmentPrices_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [bus].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[TripStopDropoffPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripStopTimeId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [AddressLine] nvarchar(300) NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripStopDropoffPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TripStopDropoffPoints_TripStopTimes_TripStopTimeId] FOREIGN KEY ([TripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [bus].[TripStopPickupPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripStopTimeId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [AddressLine] nvarchar(300) NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripStopPickupPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TripStopPickupPoints_TripStopTimes_TripStopTimeId] FOREIGN KEY ([TripStopTimeId]) REFERENCES [bus].[TripStopTimes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[TrainCarSeats] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [CarId] uniqueidentifier NOT NULL,
    [SeatNumber] nvarchar(30) NOT NULL,
    [SeatType] int NOT NULL,
    [CompartmentCode] nvarchar(20) NULL,
    [CompartmentIndex] int NULL,
    [RowIndex] int NOT NULL,
    [ColumnIndex] int NOT NULL,
    [IsWindow] bit NOT NULL,
    [IsAisle] bit NOT NULL,
    [SeatClass] nvarchar(50) NULL,
    [PriceModifier] decimal(18,2) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TrainCarSeats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrainCarSeats_TrainCars_CarId] FOREIGN KEY ([CarId]) REFERENCES [train].[TrainCars] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[TripSegmentPrices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [FromTripStopTimeId] uniqueidentifier NOT NULL,
    [ToTripStopTimeId] uniqueidentifier NOT NULL,
    [FromStopIndex] int NOT NULL,
    [ToStopIndex] int NOT NULL,
    [CurrencyCode] nvarchar(3) NOT NULL,
    [BaseFare] decimal(18,2) NOT NULL,
    [TaxesFees] decimal(18,2) NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripSegmentPrices] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_train_TripSegmentPrices_StopIndex] CHECK ([FromStopIndex] < [ToStopIndex]),
    CONSTRAINT [FK_TripSegmentPrices_TripStopTimes_FromTripStopTimeId] FOREIGN KEY ([FromTripStopTimeId]) REFERENCES [train].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSegmentPrices_TripStopTimes_ToTripStopTimeId] FOREIGN KEY ([ToTripStopTimeId]) REFERENCES [train].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSegmentPrices_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [train].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [train].[TripSeatHolds] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TripId] uniqueidentifier NOT NULL,
    [TrainCarSeatId] uniqueidentifier NOT NULL,
    [FromTripStopTimeId] uniqueidentifier NOT NULL,
    [ToTripStopTimeId] uniqueidentifier NOT NULL,
    [FromStopIndex] int NOT NULL,
    [ToStopIndex] int NOT NULL,
    [Status] int NOT NULL,
    [UserId] uniqueidentifier NULL,
    [BookingId] uniqueidentifier NULL,
    [HoldToken] nvarchar(100) NOT NULL,
    [HoldExpiresAt] datetimeoffset NOT NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NOT NULL,
    CONSTRAINT [PK_TripSeatHolds] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_train_TripSeatHolds_StopIndex] CHECK ([FromStopIndex] < [ToStopIndex]),
    CONSTRAINT [FK_TripSeatHolds_TrainCarSeats_TrainCarSeatId] FOREIGN KEY ([TrainCarSeatId]) REFERENCES [train].[TrainCarSeats] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_TripStopTimes_FromTripStopTimeId] FOREIGN KEY ([FromTripStopTimeId]) REFERENCES [train].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_TripStopTimes_ToTripStopTimeId] FOREIGN KEY ([ToTripStopTimeId]) REFERENCES [train].[TripStopTimes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TripSeatHolds_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [train].[Trips] ([Id]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_AircraftModels_TenantId_Code] ON [flight].[AircraftModels] ([TenantId], [Code]);
GO

CREATE INDEX [IX_AircraftModels_TenantId_IsActive] ON [flight].[AircraftModels] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Aircrafts_AircraftModelId] ON [flight].[Aircrafts] ([AircraftModelId]);
GO

CREATE INDEX [IX_Aircrafts_AirlineId] ON [flight].[Aircrafts] ([AirlineId]);
GO

CREATE UNIQUE INDEX [IX_Aircrafts_TenantId_Code] ON [flight].[Aircrafts] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Aircrafts_TenantId_IsActive] ON [flight].[Aircrafts] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Aircrafts_TenantId_Registration] ON [flight].[Aircrafts] ([TenantId], [Registration]);
GO

CREATE UNIQUE INDEX [IX_Airlines_TenantId_Code] ON [flight].[Airlines] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Airlines_TenantId_IsActive] ON [flight].[Airlines] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Airports_LocationId] ON [flight].[Airports] ([LocationId]);
GO

CREATE UNIQUE INDEX [IX_Airports_TenantId_Code] ON [flight].[Airports] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Airports_TenantId_IataCode] ON [flight].[Airports] ([TenantId], [IataCode]);
GO

CREATE INDEX [IX_Airports_TenantId_IsActive] ON [flight].[Airports] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_AncillaryDefinitions_AirlineId] ON [flight].[AncillaryDefinitions] ([AirlineId]);
GO

CREATE UNIQUE INDEX [IX_AncillaryDefinitions_TenantId_AirlineId_Code] ON [flight].[AncillaryDefinitions] ([TenantId], [AirlineId], [Code]);
GO

CREATE INDEX [IX_AncillaryDefinitions_TenantId_IsActive] ON [flight].[AncillaryDefinitions] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE INDEX [IX_AspNetUsers_IsActive] ON [AspNetUsers] ([IsActive]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_BedTypes_TenantId_Code] ON [hotels].[BedTypes] ([TenantId], [Code]);
GO

CREATE INDEX [IX_BedTypes_TenantId_IsActive] ON [hotels].[BedTypes] ([TenantId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_BusVehicleDetails_TenantId_VehicleId] ON [fleet].[BusVehicleDetails] ([TenantId], [VehicleId]);
GO

CREATE INDEX [IX_BusVehicleDetails_VehicleId] ON [fleet].[BusVehicleDetails] ([VehicleId]);
GO

CREATE INDEX [IX_CabinSeatMaps_AircraftModelId] ON [flight].[CabinSeatMaps] ([AircraftModelId]);
GO

CREATE INDEX [IX_CabinSeatMaps_TenantId_AircraftModelId_CabinClass] ON [flight].[CabinSeatMaps] ([TenantId], [AircraftModelId], [CabinClass]);
GO

CREATE UNIQUE INDEX [IX_CabinSeatMaps_TenantId_Code] ON [flight].[CabinSeatMaps] ([TenantId], [Code]);
GO

CREATE INDEX [IX_CabinSeatMaps_TenantId_IsActive] ON [flight].[CabinSeatMaps] ([TenantId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_CabinSeats_CabinSeatMapId_SeatNumber] ON [flight].[CabinSeats] ([CabinSeatMapId], [SeatNumber]);
GO

CREATE INDEX [IX_CabinSeats_TenantId_IsActive] ON [flight].[CabinSeats] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_CancellationPolicies_HotelId] ON [hotels].[CancellationPolicies] ([HotelId]);
GO

CREATE UNIQUE INDEX [IX_CancellationPolicies_TenantId_HotelId_Code] ON [hotels].[CancellationPolicies] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_CancellationPolicies_TenantId_HotelId_IsActive] ON [hotels].[CancellationPolicies] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_CancellationPolicies_TenantId_HotelId_Type] ON [hotels].[CancellationPolicies] ([TenantId], [HotelId], [Type]);
GO

CREATE INDEX [IX_CancellationPolicyRules_CancellationPolicyId] ON [hotels].[CancellationPolicyRules] ([CancellationPolicyId]);
GO

CREATE INDEX [IX_CancellationPolicyRules_TenantId_CancellationPolicyId_IsActive] ON [hotels].[CancellationPolicyRules] ([TenantId], [CancellationPolicyId], [IsActive]);
GO

CREATE INDEX [IX_CancellationPolicyRules_TenantId_CancellationPolicyId_Priority] ON [hotels].[CancellationPolicyRules] ([TenantId], [CancellationPolicyId], [Priority]);
GO

CREATE INDEX [IX_CheckInOutRules_HotelId] ON [hotels].[CheckInOutRules] ([HotelId]);
GO

CREATE UNIQUE INDEX [IX_CheckInOutRules_TenantId_HotelId_Code] ON [hotels].[CheckInOutRules] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_CheckInOutRules_TenantId_HotelId_IsActive] ON [hotels].[CheckInOutRules] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_DailyRates_RatePlanRoomTypeId] ON [hotels].[DailyRates] ([RatePlanRoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_DailyRates_TenantId_RatePlanRoomTypeId_Date] ON [hotels].[DailyRates] ([TenantId], [RatePlanRoomTypeId], [Date]);
GO

CREATE INDEX [IX_DailyRates_TenantId_RatePlanRoomTypeId_IsActive] ON [hotels].[DailyRates] ([TenantId], [RatePlanRoomTypeId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_Districts_Code] ON [geo].[Districts] ([Code]);
GO

CREATE INDEX [IX_Districts_Name] ON [geo].[Districts] ([Name]);
GO

CREATE INDEX [IX_Districts_ProvinceId] ON [geo].[Districts] ([ProvinceId]);
GO

CREATE INDEX [IX_ExtraServicePrices_ExtraServiceId] ON [hotels].[ExtraServicePrices] ([ExtraServiceId]);
GO

CREATE INDEX [IX_ExtraServicePrices_TenantId_ExtraServiceId_StartDate_EndDate] ON [hotels].[ExtraServicePrices] ([TenantId], [ExtraServiceId], [StartDate], [EndDate]);
GO

CREATE INDEX [IX_ExtraServices_HotelId] ON [hotels].[ExtraServices] ([HotelId]);
GO

CREATE UNIQUE INDEX [IX_ExtraServices_TenantId_HotelId_Code] ON [hotels].[ExtraServices] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_ExtraServices_TenantId_HotelId_IsActive] ON [hotels].[ExtraServices] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_ExtraServices_TenantId_HotelId_Type] ON [hotels].[ExtraServices] ([TenantId], [HotelId], [Type]);
GO

CREATE INDEX [IX_FareClasses_AirlineId] ON [flight].[FareClasses] ([AirlineId]);
GO

CREATE UNIQUE INDEX [IX_FareClasses_TenantId_AirlineId_Code] ON [flight].[FareClasses] ([TenantId], [AirlineId], [Code]);
GO

CREATE INDEX [IX_FareClasses_TenantId_IsActive] ON [flight].[FareClasses] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_FareRules_FareClassId] ON [flight].[FareRules] ([FareClassId]);
GO

CREATE UNIQUE INDEX [IX_FareRules_TenantId_FareClassId] ON [flight].[FareRules] ([TenantId], [FareClassId]);
GO

CREATE INDEX [IX_FareRules_TenantId_IsActive] ON [flight].[FareRules] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Flights_AircraftId] ON [flight].[Flights] ([AircraftId]);
GO

CREATE INDEX [IX_Flights_AirlineId] ON [flight].[Flights] ([AirlineId]);
GO

CREATE INDEX [IX_Flights_FromAirportId] ON [flight].[Flights] ([FromAirportId]);
GO

CREATE INDEX [IX_Flights_TenantId_AirlineId_DepartureAt] ON [flight].[Flights] ([TenantId], [AirlineId], [DepartureAt]);
GO

CREATE UNIQUE INDEX [IX_Flights_TenantId_FlightNumber_DepartureAt] ON [flight].[Flights] ([TenantId], [FlightNumber], [DepartureAt]);
GO

CREATE INDEX [IX_Flights_TenantId_IsActive] ON [flight].[Flights] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Flights_ToAirportId] ON [flight].[Flights] ([ToAirportId]);
GO

CREATE INDEX [IX_GeoSyncLogs_CreatedAt] ON [geo].[GeoSyncLogs] ([CreatedAt]);
GO

CREATE INDEX [IX_GeoSyncLogs_Source_Depth] ON [geo].[GeoSyncLogs] ([Source], [Depth]);
GO

CREATE UNIQUE INDEX [IX_HotelAmenities_TenantId_Scope_Code] ON [hotels].[HotelAmenities] ([TenantId], [Scope], [Code]);
GO

CREATE INDEX [IX_HotelAmenities_TenantId_Scope_IsActive] ON [hotels].[HotelAmenities] ([TenantId], [Scope], [IsActive]);
GO

CREATE INDEX [IX_HotelAmenities_TenantId_Scope_SortOrder] ON [hotels].[HotelAmenities] ([TenantId], [Scope], [SortOrder]);
GO

CREATE INDEX [IX_HotelAmenityLinks_AmenityId] ON [hotels].[HotelAmenityLinks] ([AmenityId]);
GO

CREATE INDEX [IX_HotelAmenityLinks_HotelId] ON [hotels].[HotelAmenityLinks] ([HotelId]);
GO

CREATE INDEX [IX_HotelAmenityLinks_TenantId_HotelId] ON [hotels].[HotelAmenityLinks] ([TenantId], [HotelId]);
GO

CREATE UNIQUE INDEX [IX_HotelAmenityLinks_TenantId_HotelId_AmenityId] ON [hotels].[HotelAmenityLinks] ([TenantId], [HotelId], [AmenityId]);
GO

CREATE INDEX [IX_HotelContacts_HotelId] ON [hotels].[HotelContacts] ([HotelId]);
GO

CREATE INDEX [IX_HotelContacts_TenantId_HotelId_IsActive] ON [hotels].[HotelContacts] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_HotelContacts_TenantId_HotelId_IsPrimary] ON [hotels].[HotelContacts] ([TenantId], [HotelId], [IsPrimary]);
GO

CREATE INDEX [IX_HotelImages_HotelId] ON [hotels].[HotelImages] ([HotelId]);
GO

CREATE INDEX [IX_HotelImages_TenantId_HotelId_Kind] ON [hotels].[HotelImages] ([TenantId], [HotelId], [Kind]);
GO

CREATE INDEX [IX_HotelImages_TenantId_HotelId_SortOrder] ON [hotels].[HotelImages] ([TenantId], [HotelId], [SortOrder]);
GO

CREATE INDEX [IX_HotelReviews_HotelId] ON [hotels].[HotelReviews] ([HotelId]);
GO

CREATE INDEX [IX_HotelReviews_TenantId_BookingId] ON [hotels].[HotelReviews] ([TenantId], [BookingId]);
GO

CREATE INDEX [IX_HotelReviews_TenantId_HotelId_Rating] ON [hotels].[HotelReviews] ([TenantId], [HotelId], [Rating]);
GO

CREATE INDEX [IX_HotelReviews_TenantId_HotelId_Status] ON [hotels].[HotelReviews] ([TenantId], [HotelId], [Status]);
GO

CREATE UNIQUE INDEX [IX_Hotels_TenantId_Code] ON [hotels].[Hotels] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Hotels_TenantId_IsActive] ON [hotels].[Hotels] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Hotels_TenantId_LocationId] ON [hotels].[Hotels] ([TenantId], [LocationId]);
GO

CREATE UNIQUE INDEX [IX_Hotels_TenantId_Slug] ON [hotels].[Hotels] ([TenantId], [Slug]) WHERE [Slug] IS NOT NULL;
GO

CREATE INDEX [IX_Hotels_TenantId_Status] ON [hotels].[Hotels] ([TenantId], [Status]);
GO

CREATE INDEX [IX_InventoryHolds_RoomTypeId] ON [hotels].[InventoryHolds] ([RoomTypeId]);
GO

CREATE INDEX [IX_InventoryHolds_TenantId_BookingId] ON [hotels].[InventoryHolds] ([TenantId], [BookingId]);
GO

CREATE INDEX [IX_InventoryHolds_TenantId_RoomTypeId_HoldExpiresAt] ON [hotels].[InventoryHolds] ([TenantId], [RoomTypeId], [HoldExpiresAt]);
GO

CREATE INDEX [IX_InventoryHolds_TenantId_RoomTypeId_Status] ON [hotels].[InventoryHolds] ([TenantId], [RoomTypeId], [Status]);
GO

CREATE INDEX [IX_Locations_DistrictId] ON [catalog].[Locations] ([DistrictId]);
GO

CREATE INDEX [IX_Locations_ProvinceId] ON [catalog].[Locations] ([ProvinceId]);
GO

CREATE INDEX [IX_Locations_TenantId_AirportIataCode] ON [catalog].[Locations] ([TenantId], [AirportIataCode]);
GO

CREATE INDEX [IX_Locations_TenantId_BusStationCode] ON [catalog].[Locations] ([TenantId], [BusStationCode]);
GO

CREATE INDEX [IX_Locations_TenantId_Code] ON [catalog].[Locations] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Locations_TenantId_NormalizedName] ON [catalog].[Locations] ([TenantId], [NormalizedName]);
GO

CREATE INDEX [IX_Locations_TenantId_TrainStationCode] ON [catalog].[Locations] ([TenantId], [TrainStationCode]);
GO

CREATE INDEX [IX_Locations_TenantId_Type_IsActive] ON [catalog].[Locations] ([TenantId], [Type], [IsActive]);
GO

CREATE INDEX [IX_Locations_WardId] ON [catalog].[Locations] ([WardId]);
GO

CREATE UNIQUE INDEX [IX_MealPlans_TenantId_Code] ON [hotels].[MealPlans] ([TenantId], [Code]);
GO

CREATE INDEX [IX_MealPlans_TenantId_IsActive] ON [hotels].[MealPlans] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_MediaAssets_TenantId_IsActive] ON [cms].[MediaAssets] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_MediaAssets_TenantId_Type] ON [cms].[MediaAssets] ([TenantId], [Type]);
GO

CREATE INDEX [IX_NewsCategories_TenantId_IsActive] ON [cms].[NewsCategories] ([TenantId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_NewsCategories_TenantId_Slug] ON [cms].[NewsCategories] ([TenantId], [Slug]);
GO

CREATE INDEX [IX_NewsCategories_TenantId_SortOrder] ON [cms].[NewsCategories] ([TenantId], [SortOrder]);
GO

CREATE INDEX [IX_NewsPostCategories_CategoryId] ON [cms].[NewsPostCategories] ([CategoryId]);
GO

CREATE INDEX [IX_NewsPostCategories_PostId] ON [cms].[NewsPostCategories] ([PostId]);
GO

CREATE UNIQUE INDEX [IX_NewsPostCategories_TenantId_PostId_CategoryId] ON [cms].[NewsPostCategories] ([TenantId], [PostId], [CategoryId]);
GO

CREATE INDEX [IX_NewsPostRevisions_EditorUserId] ON [cms].[NewsPostRevisions] ([EditorUserId]);
GO

CREATE INDEX [IX_NewsPostRevisions_PostId] ON [cms].[NewsPostRevisions] ([PostId]);
GO

CREATE INDEX [IX_NewsPostRevisions_TenantId_PostId_EditedAt] ON [cms].[NewsPostRevisions] ([TenantId], [PostId], [EditedAt]);
GO

CREATE UNIQUE INDEX [IX_NewsPostRevisions_TenantId_PostId_VersionNumber] ON [cms].[NewsPostRevisions] ([TenantId], [PostId], [VersionNumber]);
GO

CREATE INDEX [IX_NewsPosts_AuthorUserId] ON [cms].[NewsPosts] ([AuthorUserId]);
GO

CREATE INDEX [IX_NewsPosts_CoverMediaAssetId] ON [cms].[NewsPosts] ([CoverMediaAssetId]);
GO

CREATE INDEX [IX_NewsPosts_EditorUserId] ON [cms].[NewsPosts] ([EditorUserId]);
GO

CREATE INDEX [IX_NewsPosts_TenantId_PublishedAt] ON [cms].[NewsPosts] ([TenantId], [PublishedAt]);
GO

CREATE INDEX [IX_NewsPosts_TenantId_ScheduledAt] ON [cms].[NewsPosts] ([TenantId], [ScheduledAt]);
GO

CREATE UNIQUE INDEX [IX_NewsPosts_TenantId_Slug] ON [cms].[NewsPosts] ([TenantId], [Slug]);
GO

CREATE INDEX [IX_NewsPosts_TenantId_Status] ON [cms].[NewsPosts] ([TenantId], [Status]);
GO

CREATE INDEX [IX_NewsPostTags_PostId] ON [cms].[NewsPostTags] ([PostId]);
GO

CREATE INDEX [IX_NewsPostTags_TagId] ON [cms].[NewsPostTags] ([TagId]);
GO

CREATE UNIQUE INDEX [IX_NewsPostTags_TenantId_PostId_TagId] ON [cms].[NewsPostTags] ([TenantId], [PostId], [TagId]);
GO

CREATE UNIQUE INDEX [IX_NewsRedirects_TenantId_FromPath] ON [cms].[NewsRedirects] ([TenantId], [FromPath]);
GO

CREATE INDEX [IX_NewsRedirects_TenantId_IsActive] ON [cms].[NewsRedirects] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_NewsTags_TenantId_IsActive] ON [cms].[NewsTags] ([TenantId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_NewsTags_TenantId_Slug] ON [cms].[NewsTags] ([TenantId], [Slug]);
GO

CREATE INDEX [IX_Offers_AirlineId] ON [flight].[Offers] ([AirlineId]);
GO

CREATE INDEX [IX_Offers_FareClassId] ON [flight].[Offers] ([FareClassId]);
GO

CREATE INDEX [IX_Offers_FlightId] ON [flight].[Offers] ([FlightId]);
GO

CREATE INDEX [IX_Offers_TenantId_ExpiresAt] ON [flight].[Offers] ([TenantId], [ExpiresAt]);
GO

CREATE INDEX [IX_Offers_TenantId_FlightId] ON [flight].[Offers] ([TenantId], [FlightId]);
GO

CREATE INDEX [IX_Offers_TenantId_Status] ON [flight].[Offers] ([TenantId], [Status]);
GO

CREATE INDEX [IX_OfferSegments_FromAirportId] ON [flight].[OfferSegments] ([FromAirportId]);
GO

CREATE UNIQUE INDEX [IX_OfferSegments_OfferId_SegmentIndex] ON [flight].[OfferSegments] ([OfferId], [SegmentIndex]);
GO

CREATE INDEX [IX_OfferSegments_ToAirportId] ON [flight].[OfferSegments] ([ToAirportId]);
GO

CREATE UNIQUE INDEX [IX_OfferTaxFeeLines_OfferId_SortOrder_Code] ON [flight].[OfferTaxFeeLines] ([OfferId], [SortOrder], [Code]);
GO

CREATE INDEX [IX_Permissions_Category] ON [auth].[Permissions] ([Category]);
GO

CREATE UNIQUE INDEX [IX_Permissions_Code] ON [auth].[Permissions] ([Code]);
GO

CREATE INDEX [IX_Permissions_IsActive] ON [auth].[Permissions] ([IsActive]);
GO

CREATE INDEX [IX_PromoRateOverrides_RatePlanRoomTypeId] ON [hotels].[PromoRateOverrides] ([RatePlanRoomTypeId]);
GO

CREATE INDEX [IX_PromoRateOverrides_TenantId_PromoCodeId] ON [hotels].[PromoRateOverrides] ([TenantId], [PromoCodeId]);
GO

CREATE INDEX [IX_PromoRateOverrides_TenantId_RatePlanRoomTypeId_StartDate_EndDate] ON [hotels].[PromoRateOverrides] ([TenantId], [RatePlanRoomTypeId], [StartDate], [EndDate]);
GO

CREATE INDEX [IX_PropertyPolicies_HotelId] ON [hotels].[PropertyPolicies] ([HotelId]);
GO

CREATE UNIQUE INDEX [IX_PropertyPolicies_TenantId_HotelId_Code] ON [hotels].[PropertyPolicies] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_PropertyPolicies_TenantId_HotelId_IsActive] ON [hotels].[PropertyPolicies] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_Providers_DistrictId] ON [catalog].[Providers] ([DistrictId]);
GO

CREATE INDEX [IX_Providers_LocationId] ON [catalog].[Providers] ([LocationId]);
GO

CREATE INDEX [IX_Providers_ProvinceId] ON [catalog].[Providers] ([ProvinceId]);
GO

CREATE UNIQUE INDEX [IX_Providers_TenantId_Code] ON [catalog].[Providers] ([TenantId], [Code]);
GO

CREATE UNIQUE INDEX [IX_Providers_TenantId_Slug] ON [catalog].[Providers] ([TenantId], [Slug]);
GO

CREATE INDEX [IX_Providers_TenantId_Type_IsActive] ON [catalog].[Providers] ([TenantId], [Type], [IsActive]);
GO

CREATE INDEX [IX_Providers_WardId] ON [catalog].[Providers] ([WardId]);
GO

CREATE UNIQUE INDEX [IX_Provinces_Code] ON [geo].[Provinces] ([Code]);
GO

CREATE INDEX [IX_Provinces_Name] ON [geo].[Provinces] ([Name]);
GO

CREATE INDEX [IX_RatePlanPolicies_RatePlanId] ON [hotels].[RatePlanPolicies] ([RatePlanId]);
GO

CREATE UNIQUE INDEX [IX_RatePlanPolicies_TenantId_RatePlanId] ON [hotels].[RatePlanPolicies] ([TenantId], [RatePlanId]);
GO

CREATE INDEX [IX_RatePlanRoomTypes_RatePlanId] ON [hotels].[RatePlanRoomTypes] ([RatePlanId]);
GO

CREATE INDEX [IX_RatePlanRoomTypes_RoomTypeId] ON [hotels].[RatePlanRoomTypes] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_RatePlanRoomTypes_TenantId_RatePlanId_RoomTypeId] ON [hotels].[RatePlanRoomTypes] ([TenantId], [RatePlanId], [RoomTypeId]);
GO

CREATE INDEX [IX_RatePlanRoomTypes_TenantId_RoomTypeId_IsActive] ON [hotels].[RatePlanRoomTypes] ([TenantId], [RoomTypeId], [IsActive]);
GO

CREATE INDEX [IX_RatePlans_CancellationPolicyId] ON [hotels].[RatePlans] ([CancellationPolicyId]);
GO

CREATE INDEX [IX_RatePlans_CheckInOutRuleId] ON [hotels].[RatePlans] ([CheckInOutRuleId]);
GO

CREATE INDEX [IX_RatePlans_HotelId] ON [hotels].[RatePlans] ([HotelId]);
GO

CREATE INDEX [IX_RatePlans_PropertyPolicyId] ON [hotels].[RatePlans] ([PropertyPolicyId]);
GO

CREATE UNIQUE INDEX [IX_RatePlans_TenantId_HotelId_Code] ON [hotels].[RatePlans] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_RatePlans_TenantId_HotelId_IsActive] ON [hotels].[RatePlans] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_RatePlans_TenantId_HotelId_Status] ON [hotels].[RatePlans] ([TenantId], [HotelId], [Status]);
GO

CREATE INDEX [IX_RatePlans_TenantId_HotelId_Type] ON [hotels].[RatePlans] ([TenantId], [HotelId], [Type]);
GO

CREATE INDEX [IX_RolePermissions_PermissionId] ON [auth].[RolePermissions] ([PermissionId]);
GO

CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [auth].[RolePermissions] ([RoleId], [PermissionId]);
GO

CREATE UNIQUE INDEX [IX_RoomAmenities_TenantId_Code] ON [hotels].[RoomAmenities] ([TenantId], [Code]);
GO

CREATE INDEX [IX_RoomAmenities_TenantId_IsActive] ON [hotels].[RoomAmenities] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_RoomAmenities_TenantId_SortOrder] ON [hotels].[RoomAmenities] ([TenantId], [SortOrder]);
GO

CREATE INDEX [IX_RoomAmenityLinks_AmenityId] ON [hotels].[RoomAmenityLinks] ([AmenityId]);
GO

CREATE INDEX [IX_RoomAmenityLinks_RoomTypeId] ON [hotels].[RoomAmenityLinks] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_RoomAmenityLinks_TenantId_RoomTypeId_AmenityId] ON [hotels].[RoomAmenityLinks] ([TenantId], [RoomTypeId], [AmenityId]);
GO

CREATE INDEX [IX_RoomTypeBeds_BedTypeId] ON [hotels].[RoomTypeBeds] ([BedTypeId]);
GO

CREATE INDEX [IX_RoomTypeBeds_RoomTypeId] ON [hotels].[RoomTypeBeds] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_RoomTypeBeds_TenantId_RoomTypeId_BedTypeId] ON [hotels].[RoomTypeBeds] ([TenantId], [RoomTypeId], [BedTypeId]);
GO

CREATE INDEX [IX_RoomTypeImages_RoomTypeId] ON [hotels].[RoomTypeImages] ([RoomTypeId]);
GO

CREATE INDEX [IX_RoomTypeImages_TenantId_RoomTypeId_Kind] ON [hotels].[RoomTypeImages] ([TenantId], [RoomTypeId], [Kind]);
GO

CREATE INDEX [IX_RoomTypeImages_TenantId_RoomTypeId_SortOrder] ON [hotels].[RoomTypeImages] ([TenantId], [RoomTypeId], [SortOrder]);
GO

CREATE INDEX [IX_RoomTypeInventories_RoomTypeId] ON [hotels].[RoomTypeInventories] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_RoomTypeInventories_TenantId_RoomTypeId_Date] ON [hotels].[RoomTypeInventories] ([TenantId], [RoomTypeId], [Date]);
GO

CREATE INDEX [IX_RoomTypeInventories_TenantId_RoomTypeId_Status] ON [hotels].[RoomTypeInventories] ([TenantId], [RoomTypeId], [Status]);
GO

CREATE INDEX [IX_RoomTypeMealPlans_MealPlanId] ON [hotels].[RoomTypeMealPlans] ([MealPlanId]);
GO

CREATE INDEX [IX_RoomTypeMealPlans_RoomTypeId] ON [hotels].[RoomTypeMealPlans] ([RoomTypeId]);
GO

CREATE INDEX [IX_RoomTypeMealPlans_TenantId_RoomTypeId_IsActive] ON [hotels].[RoomTypeMealPlans] ([TenantId], [RoomTypeId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_RoomTypeMealPlans_TenantId_RoomTypeId_MealPlanId] ON [hotels].[RoomTypeMealPlans] ([TenantId], [RoomTypeId], [MealPlanId]);
GO

CREATE INDEX [IX_RoomTypeOccupancyRules_RoomTypeId] ON [hotels].[RoomTypeOccupancyRules] ([RoomTypeId]);
GO

CREATE INDEX [IX_RoomTypeOccupancyRules_TenantId_RoomTypeId_IsActive] ON [hotels].[RoomTypeOccupancyRules] ([TenantId], [RoomTypeId], [IsActive]);
GO

CREATE INDEX [IX_RoomTypePolicies_RoomTypeId] ON [hotels].[RoomTypePolicies] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [IX_RoomTypePolicies_TenantId_RoomTypeId] ON [hotels].[RoomTypePolicies] ([TenantId], [RoomTypeId]);
GO

CREATE INDEX [IX_RoomTypes_HotelId] ON [hotels].[RoomTypes] ([HotelId]);
GO

CREATE UNIQUE INDEX [IX_RoomTypes_TenantId_HotelId_Code] ON [hotels].[RoomTypes] ([TenantId], [HotelId], [Code]);
GO

CREATE INDEX [IX_RoomTypes_TenantId_HotelId_IsActive] ON [hotels].[RoomTypes] ([TenantId], [HotelId], [IsActive]);
GO

CREATE INDEX [IX_RoomTypes_TenantId_HotelId_SortOrder] ON [hotels].[RoomTypes] ([TenantId], [HotelId], [SortOrder]);
GO

CREATE INDEX [IX_RoomTypes_TenantId_HotelId_Status] ON [hotels].[RoomTypes] ([TenantId], [HotelId], [Status]);
GO

CREATE INDEX [IX_Routes_FromStopPointId] ON [bus].[Routes] ([FromStopPointId]);
GO

CREATE INDEX [IX_Routes_ProviderId] ON [bus].[Routes] ([ProviderId]);
GO

CREATE UNIQUE INDEX [IX_Routes_TenantId_Code] ON [bus].[Routes] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Routes_TenantId_IsActive] ON [bus].[Routes] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_Routes_TenantId_ProviderId] ON [bus].[Routes] ([TenantId], [ProviderId]);
GO

CREATE INDEX [IX_Routes_ToStopPointId] ON [bus].[Routes] ([ToStopPointId]);
GO

CREATE INDEX [IX_Routes_FromStopPointId] ON [train].[Routes] ([FromStopPointId]);
GO

CREATE UNIQUE INDEX [IX_Routes_TenantId_Code] ON [train].[Routes] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Routes_TenantId_ProviderId] ON [train].[Routes] ([TenantId], [ProviderId]);
GO

CREATE INDEX [IX_Routes_ToStopPointId] ON [train].[Routes] ([ToStopPointId]);
GO

CREATE UNIQUE INDEX [IX_RouteStops_RouteId_StopIndex] ON [bus].[RouteStops] ([RouteId], [StopIndex]);
GO

CREATE INDEX [IX_RouteStops_RouteId_StopPointId] ON [bus].[RouteStops] ([RouteId], [StopPointId]);
GO

CREATE INDEX [IX_RouteStops_StopPointId] ON [bus].[RouteStops] ([StopPointId]);
GO

CREATE UNIQUE INDEX [IX_RouteStops_RouteId_StopIndex] ON [train].[RouteStops] ([RouteId], [StopIndex]);
GO

CREATE INDEX [IX_RouteStops_RouteId_StopPointId] ON [train].[RouteStops] ([RouteId], [StopPointId]);
GO

CREATE INDEX [IX_RouteStops_StopPointId] ON [train].[RouteStops] ([StopPointId]);
GO

CREATE UNIQUE INDEX [IX_SeatMaps_TenantId_VehicleType_Code] ON [fleet].[SeatMaps] ([TenantId], [VehicleType], [Code]);
GO

CREATE INDEX [IX_SeatMaps_TenantId_VehicleType_IsActive] ON [fleet].[SeatMaps] ([TenantId], [VehicleType], [IsActive]);
GO

CREATE INDEX [IX_Seats_SeatMapId] ON [fleet].[Seats] ([SeatMapId]);
GO

CREATE UNIQUE INDEX [IX_Seats_SeatMapId_SeatNumber] ON [fleet].[Seats] ([SeatMapId], [SeatNumber]);
GO

CREATE UNIQUE INDEX [IX_SiteSettings_TenantId] ON [cms].[SiteSettings] ([TenantId]);
GO

CREATE INDEX [IX_SiteSettings_TenantId_IsActive] ON [cms].[SiteSettings] ([TenantId], [IsActive]);
GO

CREATE INDEX [IX_StopPoints_LocationId] ON [bus].[StopPoints] ([LocationId]);
GO

CREATE INDEX [IX_StopPoints_TenantId_LocationId] ON [bus].[StopPoints] ([TenantId], [LocationId]);
GO

CREATE INDEX [IX_StopPoints_TenantId_Type_IsActive] ON [bus].[StopPoints] ([TenantId], [Type], [IsActive]);
GO

CREATE INDEX [IX_StopPoints_LocationId] ON [train].[StopPoints] ([LocationId]);
GO

CREATE INDEX [IX_StopPoints_TenantId_LocationId] ON [train].[StopPoints] ([TenantId], [LocationId]);
GO

CREATE INDEX [IX_StopPoints_TenantId_Type_IsActive] ON [train].[StopPoints] ([TenantId], [Type], [IsActive]);
GO

CREATE INDEX [IX_TenantRolePermissions_PermissionId] ON [tenants].[TenantRolePermissions] ([PermissionId]);
GO

CREATE INDEX [IX_TenantRolePermissions_TenantId] ON [tenants].[TenantRolePermissions] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_TenantRolePermissions_TenantId_TenantRoleId_PermissionId] ON [tenants].[TenantRolePermissions] ([TenantId], [TenantRoleId], [PermissionId]);
GO

CREATE INDEX [IX_TenantRolePermissions_TenantRoleId] ON [tenants].[TenantRolePermissions] ([TenantRoleId]);
GO

CREATE INDEX [IX_TenantRoles_IsActive] ON [tenants].[TenantRoles] ([IsActive]);
GO

CREATE INDEX [IX_TenantRoles_TenantId] ON [tenants].[TenantRoles] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_TenantRoles_TenantId_Code] ON [tenants].[TenantRoles] ([TenantId], [Code]);
GO

CREATE UNIQUE INDEX [IX_Tenants_Code] ON [tenants].[Tenants] ([Code]);
GO

CREATE INDEX [IX_Tenants_Status] ON [tenants].[Tenants] ([Status]);
GO

CREATE INDEX [IX_Tenants_Type] ON [tenants].[Tenants] ([Type]);
GO

CREATE INDEX [IX_TenantUserRoles_TenantId] ON [tenants].[TenantUserRoles] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_TenantUserRoles_TenantId_TenantRoleId_UserId] ON [tenants].[TenantUserRoles] ([TenantId], [TenantRoleId], [UserId]);
GO

CREATE INDEX [IX_TenantUserRoles_TenantRoleId] ON [tenants].[TenantUserRoles] ([TenantRoleId]);
GO

CREATE INDEX [IX_TenantUserRoles_UserId] ON [tenants].[TenantUserRoles] ([UserId]);
GO

CREATE INDEX [IX_TenantUsers_TenantId] ON [tenants].[TenantUsers] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_TenantUsers_TenantId_UserId] ON [tenants].[TenantUsers] ([TenantId], [UserId]);
GO

CREATE INDEX [IX_TenantUsers_UserId] ON [tenants].[TenantUsers] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_TrainCars_TripId_CarNumber] ON [train].[TrainCars] ([TripId], [CarNumber]);
GO

CREATE INDEX [IX_TrainCars_TripId_SortOrder] ON [train].[TrainCars] ([TripId], [SortOrder]);
GO

CREATE INDEX [IX_TrainCarSeats_CarId_CompartmentCode] ON [train].[TrainCarSeats] ([CarId], [CompartmentCode]);
GO

CREATE UNIQUE INDEX [IX_TrainCarSeats_CarId_SeatNumber] ON [train].[TrainCarSeats] ([CarId], [SeatNumber]);
GO

CREATE INDEX [IX_Trips_ProviderId] ON [bus].[Trips] ([ProviderId]);
GO

CREATE INDEX [IX_Trips_RouteId] ON [bus].[Trips] ([RouteId]);
GO

CREATE UNIQUE INDEX [IX_Trips_TenantId_Code] ON [bus].[Trips] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Trips_TenantId_DepartureAt] ON [bus].[Trips] ([TenantId], [DepartureAt]);
GO

CREATE INDEX [IX_Trips_TenantId_ProviderId] ON [bus].[Trips] ([TenantId], [ProviderId]);
GO

CREATE INDEX [IX_Trips_TenantId_RouteId] ON [bus].[Trips] ([TenantId], [RouteId]);
GO

CREATE INDEX [IX_Trips_VehicleId] ON [bus].[Trips] ([VehicleId]);
GO

CREATE INDEX [IX_Trips_RouteId] ON [train].[Trips] ([RouteId]);
GO

CREATE UNIQUE INDEX [IX_Trips_TenantId_Code] ON [train].[Trips] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Trips_TenantId_DepartureAt] ON [train].[Trips] ([TenantId], [DepartureAt]);
GO

CREATE INDEX [IX_TripSeatHolds_FromTripStopTimeId] ON [bus].[TripSeatHolds] ([FromTripStopTimeId]);
GO

CREATE UNIQUE INDEX [IX_TripSeatHolds_HoldToken] ON [bus].[TripSeatHolds] ([HoldToken]);
GO

CREATE INDEX [IX_TripSeatHolds_SeatId] ON [bus].[TripSeatHolds] ([SeatId]);
GO

CREATE INDEX [IX_TripSeatHolds_ToTripStopTimeId] ON [bus].[TripSeatHolds] ([ToTripStopTimeId]);
GO

CREATE INDEX [IX_TripSeatHolds_TripId_SeatId] ON [bus].[TripSeatHolds] ([TripId], [SeatId]);
GO

CREATE INDEX [IX_TripSeatHolds_TripId_Status_HoldExpiresAt] ON [bus].[TripSeatHolds] ([TripId], [Status], [HoldExpiresAt]);
GO

CREATE INDEX [IX_TripSeatHolds_FromTripStopTimeId] ON [train].[TripSeatHolds] ([FromTripStopTimeId]);
GO

CREATE UNIQUE INDEX [IX_TripSeatHolds_HoldToken] ON [train].[TripSeatHolds] ([HoldToken]);
GO

CREATE INDEX [IX_TripSeatHolds_ToTripStopTimeId] ON [train].[TripSeatHolds] ([ToTripStopTimeId]);
GO

CREATE INDEX [IX_TripSeatHolds_TrainCarSeatId] ON [train].[TripSeatHolds] ([TrainCarSeatId]);
GO

CREATE INDEX [IX_TripSeatHolds_TripId_Status_HoldExpiresAt] ON [train].[TripSeatHolds] ([TripId], [Status], [HoldExpiresAt]);
GO

CREATE INDEX [IX_TripSeatHolds_TripId_TrainCarSeatId] ON [train].[TripSeatHolds] ([TripId], [TrainCarSeatId]);
GO

CREATE INDEX [IX_TripSegmentPrices_FromTripStopTimeId] ON [bus].[TripSegmentPrices] ([FromTripStopTimeId]);
GO

CREATE INDEX [IX_TripSegmentPrices_ToTripStopTimeId] ON [bus].[TripSegmentPrices] ([ToTripStopTimeId]);
GO

CREATE UNIQUE INDEX [IX_TripSegmentPrices_TripId_FromStopIndex_ToStopIndex] ON [bus].[TripSegmentPrices] ([TripId], [FromStopIndex], [ToStopIndex]);
GO

CREATE INDEX [IX_TripSegmentPrices_TripId_IsActive] ON [bus].[TripSegmentPrices] ([TripId], [IsActive]);
GO

CREATE INDEX [IX_TripSegmentPrices_FromTripStopTimeId] ON [train].[TripSegmentPrices] ([FromTripStopTimeId]);
GO

CREATE INDEX [IX_TripSegmentPrices_ToTripStopTimeId] ON [train].[TripSegmentPrices] ([ToTripStopTimeId]);
GO

CREATE UNIQUE INDEX [IX_TripSegmentPrices_TripId_FromStopIndex_ToStopIndex] ON [train].[TripSegmentPrices] ([TripId], [FromStopIndex], [ToStopIndex]);
GO

CREATE INDEX [IX_TripSegmentPrices_TripId_IsActive] ON [train].[TripSegmentPrices] ([TripId], [IsActive]);
GO

CREATE INDEX [IX_TripStopDropoffPoints_TripStopTimeId_IsDefault] ON [bus].[TripStopDropoffPoints] ([TripStopTimeId], [IsDefault]);
GO

CREATE INDEX [IX_TripStopDropoffPoints_TripStopTimeId_SortOrder] ON [bus].[TripStopDropoffPoints] ([TripStopTimeId], [SortOrder]);
GO

CREATE INDEX [IX_TripStopPickupPoints_TripStopTimeId_IsDefault] ON [bus].[TripStopPickupPoints] ([TripStopTimeId], [IsDefault]);
GO

CREATE INDEX [IX_TripStopPickupPoints_TripStopTimeId_SortOrder] ON [bus].[TripStopPickupPoints] ([TripStopTimeId], [SortOrder]);
GO

CREATE INDEX [IX_TripStopTimes_StopPointId] ON [bus].[TripStopTimes] ([StopPointId]);
GO

CREATE UNIQUE INDEX [IX_TripStopTimes_TripId_StopIndex] ON [bus].[TripStopTimes] ([TripId], [StopIndex]);
GO

CREATE INDEX [IX_TripStopTimes_TripId_StopPointId] ON [bus].[TripStopTimes] ([TripId], [StopPointId]);
GO

CREATE INDEX [IX_TripStopTimes_StopPointId] ON [train].[TripStopTimes] ([StopPointId]);
GO

CREATE UNIQUE INDEX [IX_TripStopTimes_TripId_StopIndex] ON [train].[TripStopTimes] ([TripId], [StopIndex]);
GO

CREATE INDEX [IX_TripStopTimes_TripId_StopPointId] ON [train].[TripStopTimes] ([TripId], [StopPointId]);
GO

CREATE INDEX [IX_UserPermissions_PermissionId] ON [auth].[UserPermissions] ([PermissionId]);
GO

CREATE INDEX [IX_UserPermissions_TenantId] ON [auth].[UserPermissions] ([TenantId]);
GO

CREATE INDEX [IX_UserPermissions_UserId] ON [auth].[UserPermissions] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionId] ON [auth].[UserPermissions] ([UserId], [PermissionId]) WHERE [TenantId] IS NULL;
GO

CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionId_TenantId] ON [auth].[UserPermissions] ([UserId], [PermissionId], [TenantId]) WHERE [TenantId] IS NOT NULL;
GO

CREATE INDEX [IX_VehicleModels_TenantId_Manufacturer_ModelName] ON [fleet].[VehicleModels] ([TenantId], [Manufacturer], [ModelName]);
GO

CREATE INDEX [IX_VehicleModels_TenantId_VehicleType_IsActive] ON [fleet].[VehicleModels] ([TenantId], [VehicleType], [IsActive]);
GO

CREATE INDEX [IX_Vehicles_ProviderId] ON [fleet].[Vehicles] ([ProviderId]);
GO

CREATE INDEX [IX_Vehicles_SeatMapId] ON [fleet].[Vehicles] ([SeatMapId]);
GO

CREATE INDEX [IX_Vehicles_TenantId_ProviderId] ON [fleet].[Vehicles] ([TenantId], [ProviderId]);
GO

CREATE UNIQUE INDEX [IX_Vehicles_TenantId_VehicleType_Code] ON [fleet].[Vehicles] ([TenantId], [VehicleType], [Code]);
GO

CREATE INDEX [IX_Vehicles_TenantId_VehicleType_IsActive] ON [fleet].[Vehicles] ([TenantId], [VehicleType], [IsActive]);
GO

CREATE INDEX [IX_Vehicles_VehicleModelId] ON [fleet].[Vehicles] ([VehicleModelId]);
GO

CREATE UNIQUE INDEX [IX_Wards_Code] ON [geo].[Wards] ([Code]);
GO

CREATE INDEX [IX_Wards_DistrictId] ON [geo].[Wards] ([DistrictId]);
GO

CREATE INDEX [IX_Wards_Name] ON [geo].[Wards] ([Name]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305032838_Phase11_Hotels_Init', N'8.0.24');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'tours') IS NULL EXEC(N'CREATE SCHEMA [tours];');
GO

CREATE TABLE [tours].[Tours] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProviderId] uniqueidentifier NULL,
    [PrimaryLocationId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Slug] nvarchar(300) NOT NULL,
    [Type] int NOT NULL,
    [Status] int NOT NULL,
    [Difficulty] int NOT NULL,
    [DurationDays] int NOT NULL,
    [DurationNights] int NOT NULL,
    [MinGuests] int NULL,
    [MaxGuests] int NULL,
    [MinAge] int NULL,
    [MaxAge] int NULL,
    [IsFeatured] bit NOT NULL,
    [IsFeaturedOnHome] bit NOT NULL,
    [IsPrivateTourSupported] bit NOT NULL,
    [IsInstantConfirm] bit NOT NULL,
    [CountryCode] nvarchar(10) NULL,
    [Province] nvarchar(200) NULL,
    [City] nvarchar(200) NULL,
    [MeetingPointSummary] nvarchar(1000) NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [HighlightsJson] nvarchar(max) NULL,
    [IncludesJson] nvarchar(max) NULL,
    [ExcludesJson] nvarchar(max) NULL,
    [TermsJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CoverImageUrl] nvarchar(1000) NULL,
    [CoverMediaAssetId] uniqueidentifier NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Tours] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [tours].[TourAddons] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Type] int NOT NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [BasePrice] decimal(18,2) NOT NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [IsPerPerson] bit NOT NULL,
    [IsRequired] bit NOT NULL,
    [AllowQuantitySelection] bit NOT NULL,
    [MinQuantity] int NULL,
    [MaxQuantity] int NULL,
    [IsDefaultSelected] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourAddons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourAddons_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourContacts] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Title] nvarchar(200) NULL,
    [Department] nvarchar(200) NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [ContactType] int NOT NULL,
    [IsPrimary] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourContacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourContacts_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourDropoffPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [AddressLine] nvarchar(500) NULL,
    [Ward] nvarchar(200) NULL,
    [District] nvarchar(200) NULL,
    [Province] nvarchar(200) NULL,
    [CountryCode] nvarchar(10) NULL,
    [Latitude] decimal(18,6) NULL,
    [Longitude] decimal(18,6) NULL,
    [DropoffTime] time NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourDropoffPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourDropoffPoints_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourFaqs] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Question] nvarchar(1000) NOT NULL,
    [AnswerMarkdown] nvarchar(max) NOT NULL,
    [AnswerHtml] nvarchar(max) NULL,
    [Type] int NOT NULL,
    [IsHighlighted] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourFaqs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourFaqs_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourImages] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [MediaAssetId] uniqueidentifier NULL,
    [ImageUrl] nvarchar(1000) NULL,
    [Caption] nvarchar(500) NULL,
    [AltText] nvarchar(500) NULL,
    [Title] nvarchar(500) NULL,
    [IsPrimary] bit NOT NULL,
    [IsCover] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourImages_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourItineraryDays] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [DayNumber] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [StartLocation] nvarchar(300) NULL,
    [EndLocation] nvarchar(300) NULL,
    [AccommodationName] nvarchar(300) NULL,
    [IncludesBreakfast] bit NOT NULL,
    [IncludesLunch] bit NOT NULL,
    [IncludesDinner] bit NOT NULL,
    [TransportationSummary] nvarchar(1000) NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourItineraryDays] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourItineraryDays_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourPickupPoints] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [AddressLine] nvarchar(500) NULL,
    [Ward] nvarchar(200) NULL,
    [District] nvarchar(200) NULL,
    [Province] nvarchar(200) NULL,
    [CountryCode] nvarchar(10) NULL,
    [Latitude] decimal(18,6) NULL,
    [Longitude] decimal(18,6) NULL,
    [PickupTime] time NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourPickupPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourPickupPoints_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [Type] int NOT NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [PolicyJson] nvarchar(max) NULL,
    [IsHighlighted] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourPolicies_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourReviews] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Rating] decimal(4,2) NOT NULL,
    [Title] nvarchar(300) NULL,
    [Content] nvarchar(4000) NULL,
    [ReviewerName] nvarchar(200) NULL,
    [Status] int NOT NULL,
    [IsApproved] bit NOT NULL,
    [IsPublic] bit NOT NULL,
    [ModerationNote] nvarchar(2000) NULL,
    [ReplyContent] nvarchar(4000) NULL,
    [ReplyAt] datetimeoffset NULL,
    [ReplyByUserId] uniqueidentifier NULL,
    [PublishedAt] datetimeoffset NULL,
    [ApprovedAt] datetimeoffset NULL,
    [ApprovedByUserId] uniqueidentifier NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourReviews_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(300) NULL,
    [DepartureDate] date NOT NULL,
    [ReturnDate] date NOT NULL,
    [DepartureTime] time NULL,
    [ReturnTime] time NULL,
    [BookingOpenAt] datetimeoffset NULL,
    [BookingCutoffAt] datetimeoffset NULL,
    [MeetingPointSummary] nvarchar(1000) NULL,
    [PickupSummary] nvarchar(1000) NULL,
    [DropoffSummary] nvarchar(1000) NULL,
    [Notes] nvarchar(4000) NULL,
    [InternalNotes] nvarchar(4000) NULL,
    [CancellationNotes] nvarchar(4000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [IsGuaranteedDeparture] bit NOT NULL,
    [IsInstantConfirm] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    [MinGuestsToOperate] int NULL,
    [MaxGuests] int NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourSchedules_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [tours].[Tours] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourItineraryItems] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourItineraryDayId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [ShortDescription] nvarchar(2000) NULL,
    [DescriptionMarkdown] nvarchar(max) NULL,
    [DescriptionHtml] nvarchar(max) NULL,
    [StartTime] time NULL,
    [EndTime] time NULL,
    [LocationName] nvarchar(300) NULL,
    [AddressLine] nvarchar(500) NULL,
    [TransportationMode] nvarchar(100) NULL,
    [IncludesTicket] bit NOT NULL,
    [IncludesMeal] bit NOT NULL,
    [IsOptional] bit NOT NULL,
    [RequiresAdditionalFee] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourItineraryItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourItineraryItems_TourItineraryDays_TourItineraryDayId] FOREIGN KEY ([TourItineraryDayId]) REFERENCES [tours].[TourItineraryDays] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourScheduleAddonPrices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourScheduleId] uniqueidentifier NOT NULL,
    [TourAddonId] uniqueidentifier NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [Price] decimal(18,2) NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [IsPerPerson] bit NOT NULL,
    [IsRequired] bit NOT NULL,
    [IsDefaultSelected] bit NOT NULL,
    [AllowQuantitySelection] bit NOT NULL,
    [MinQuantity] int NULL,
    [MaxQuantity] int NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourScheduleAddonPrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourScheduleAddonPrices_TourAddons_TourAddonId] FOREIGN KEY ([TourAddonId]) REFERENCES [tours].[TourAddons] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TourScheduleAddonPrices_TourSchedules_TourScheduleId] FOREIGN KEY ([TourScheduleId]) REFERENCES [tours].[TourSchedules] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourScheduleCapacities] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourScheduleId] uniqueidentifier NOT NULL,
    [TotalSlots] int NOT NULL,
    [SoldSlots] int NOT NULL,
    [HeldSlots] int NOT NULL,
    [BlockedSlots] int NOT NULL,
    [MinGuestsToOperate] int NULL,
    [MaxGuestsPerBooking] int NULL,
    [WarningThreshold] int NULL,
    [Status] int NOT NULL,
    [AllowWaitlist] bit NOT NULL,
    [AutoCloseWhenFull] bit NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourScheduleCapacities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourScheduleCapacities_TourSchedules_TourScheduleId] FOREIGN KEY ([TourScheduleId]) REFERENCES [tours].[TourSchedules] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tours].[TourSchedulePrices] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [TourScheduleId] uniqueidentifier NOT NULL,
    [PriceType] int NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [Taxes] decimal(18,2) NULL,
    [Fees] decimal(18,2) NULL,
    [MinAge] int NULL,
    [MaxAge] int NULL,
    [MinQuantity] int NULL,
    [MaxQuantity] int NULL,
    [IsDefault] bit NOT NULL,
    [IsIncludedTax] bit NOT NULL,
    [IsIncludedFee] bit NOT NULL,
    [Label] nvarchar(200) NULL,
    [Notes] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TourSchedulePrices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TourSchedulePrices_TourSchedules_TourScheduleId] FOREIGN KEY ([TourScheduleId]) REFERENCES [tours].[TourSchedules] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_TourAddons_TenantId_IsActive_IsDeleted] ON [tours].[TourAddons] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourAddons_TenantId_TourId_Code] ON [tours].[TourAddons] ([TenantId], [TourId], [Code]);
GO

CREATE INDEX [IX_TourAddons_TenantId_TourId_SortOrder] ON [tours].[TourAddons] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourAddons_TenantId_TourId_Type] ON [tours].[TourAddons] ([TenantId], [TourId], [Type]);
GO

CREATE INDEX [IX_TourAddons_TourId] ON [tours].[TourAddons] ([TourId]);
GO

CREATE INDEX [IX_TourContacts_TenantId_IsActive_IsDeleted] ON [tours].[TourContacts] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourContacts_TenantId_TourId_ContactType] ON [tours].[TourContacts] ([TenantId], [TourId], [ContactType]);
GO

CREATE INDEX [IX_TourContacts_TenantId_TourId_IsPrimary] ON [tours].[TourContacts] ([TenantId], [TourId], [IsPrimary]);
GO

CREATE INDEX [IX_TourContacts_TenantId_TourId_SortOrder] ON [tours].[TourContacts] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourContacts_TourId] ON [tours].[TourContacts] ([TourId]);
GO

CREATE INDEX [IX_TourDropoffPoints_TenantId_IsActive_IsDeleted] ON [tours].[TourDropoffPoints] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourDropoffPoints_TenantId_TourId_Code] ON [tours].[TourDropoffPoints] ([TenantId], [TourId], [Code]);
GO

CREATE INDEX [IX_TourDropoffPoints_TenantId_TourId_IsDefault] ON [tours].[TourDropoffPoints] ([TenantId], [TourId], [IsDefault]);
GO

CREATE INDEX [IX_TourDropoffPoints_TenantId_TourId_SortOrder] ON [tours].[TourDropoffPoints] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourDropoffPoints_TourId] ON [tours].[TourDropoffPoints] ([TourId]);
GO

CREATE INDEX [IX_TourFaqs_TenantId_IsActive_IsDeleted] ON [tours].[TourFaqs] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourFaqs_TenantId_TourId_IsHighlighted] ON [tours].[TourFaqs] ([TenantId], [TourId], [IsHighlighted]);
GO

CREATE INDEX [IX_TourFaqs_TenantId_TourId_SortOrder] ON [tours].[TourFaqs] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourFaqs_TenantId_TourId_Type] ON [tours].[TourFaqs] ([TenantId], [TourId], [Type]);
GO

CREATE INDEX [IX_TourFaqs_TourId] ON [tours].[TourFaqs] ([TourId]);
GO

CREATE INDEX [IX_TourImages_TenantId_IsActive_IsDeleted] ON [tours].[TourImages] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourImages_TenantId_TourId_IsCover] ON [tours].[TourImages] ([TenantId], [TourId], [IsCover]);
GO

CREATE INDEX [IX_TourImages_TenantId_TourId_IsPrimary] ON [tours].[TourImages] ([TenantId], [TourId], [IsPrimary]);
GO

CREATE INDEX [IX_TourImages_TenantId_TourId_SortOrder] ON [tours].[TourImages] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourImages_TourId] ON [tours].[TourImages] ([TourId]);
GO

CREATE INDEX [IX_TourItineraryDays_TenantId_IsActive_IsDeleted] ON [tours].[TourItineraryDays] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourItineraryDays_TenantId_TourId_DayNumber] ON [tours].[TourItineraryDays] ([TenantId], [TourId], [DayNumber]);
GO

CREATE INDEX [IX_TourItineraryDays_TenantId_TourId_SortOrder] ON [tours].[TourItineraryDays] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourItineraryDays_TourId] ON [tours].[TourItineraryDays] ([TourId]);
GO

CREATE INDEX [IX_TourItineraryItems_TenantId_IsActive_IsDeleted] ON [tours].[TourItineraryItems] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourItineraryItems_TenantId_TourItineraryDayId_SortOrder] ON [tours].[TourItineraryItems] ([TenantId], [TourItineraryDayId], [SortOrder]);
GO

CREATE INDEX [IX_TourItineraryItems_TenantId_TourItineraryDayId_Type] ON [tours].[TourItineraryItems] ([TenantId], [TourItineraryDayId], [Type]);
GO

CREATE INDEX [IX_TourItineraryItems_TourItineraryDayId] ON [tours].[TourItineraryItems] ([TourItineraryDayId]);
GO

CREATE INDEX [IX_TourPickupPoints_TenantId_IsActive_IsDeleted] ON [tours].[TourPickupPoints] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourPickupPoints_TenantId_TourId_Code] ON [tours].[TourPickupPoints] ([TenantId], [TourId], [Code]);
GO

CREATE INDEX [IX_TourPickupPoints_TenantId_TourId_IsDefault] ON [tours].[TourPickupPoints] ([TenantId], [TourId], [IsDefault]);
GO

CREATE INDEX [IX_TourPickupPoints_TenantId_TourId_SortOrder] ON [tours].[TourPickupPoints] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourPickupPoints_TourId] ON [tours].[TourPickupPoints] ([TourId]);
GO

CREATE INDEX [IX_TourPolicies_TenantId_IsActive_IsDeleted] ON [tours].[TourPolicies] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourPolicies_TenantId_TourId_Code] ON [tours].[TourPolicies] ([TenantId], [TourId], [Code]);
GO

CREATE INDEX [IX_TourPolicies_TenantId_TourId_SortOrder] ON [tours].[TourPolicies] ([TenantId], [TourId], [SortOrder]);
GO

CREATE INDEX [IX_TourPolicies_TenantId_TourId_Type] ON [tours].[TourPolicies] ([TenantId], [TourId], [Type]);
GO

CREATE INDEX [IX_TourPolicies_TourId] ON [tours].[TourPolicies] ([TourId]);
GO

CREATE INDEX [IX_TourReviews_TenantId_PublishedAt] ON [tours].[TourReviews] ([TenantId], [PublishedAt]);
GO

CREATE INDEX [IX_TourReviews_TenantId_TourId_IsApproved_IsPublic] ON [tours].[TourReviews] ([TenantId], [TourId], [IsApproved], [IsPublic]);
GO

CREATE INDEX [IX_TourReviews_TenantId_TourId_IsDeleted] ON [tours].[TourReviews] ([TenantId], [TourId], [IsDeleted]);
GO

CREATE INDEX [IX_TourReviews_TenantId_TourId_Status] ON [tours].[TourReviews] ([TenantId], [TourId], [Status]);
GO

CREATE INDEX [IX_TourReviews_TourId] ON [tours].[TourReviews] ([TourId]);
GO

CREATE UNIQUE INDEX [IX_Tours_TenantId_Code] ON [tours].[Tours] ([TenantId], [Code]);
GO

CREATE INDEX [IX_Tours_TenantId_IsFeatured_IsFeaturedOnHome] ON [tours].[Tours] ([TenantId], [IsFeatured], [IsFeaturedOnHome]);
GO

CREATE INDEX [IX_Tours_TenantId_PrimaryLocationId] ON [tours].[Tours] ([TenantId], [PrimaryLocationId]);
GO

CREATE INDEX [IX_Tours_TenantId_ProviderId] ON [tours].[Tours] ([TenantId], [ProviderId]);
GO

CREATE UNIQUE INDEX [IX_Tours_TenantId_Slug] ON [tours].[Tours] ([TenantId], [Slug]);
GO

CREATE INDEX [IX_Tours_TenantId_Status_IsActive_IsDeleted] ON [tours].[Tours] ([TenantId], [Status], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourScheduleAddonPrices_TenantId_IsActive_IsDeleted] ON [tours].[TourScheduleAddonPrices] ([TenantId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourScheduleAddonPrices_TenantId_TourScheduleId_SortOrder] ON [tours].[TourScheduleAddonPrices] ([TenantId], [TourScheduleId], [SortOrder]);
GO

CREATE UNIQUE INDEX [IX_TourScheduleAddonPrices_TenantId_TourScheduleId_TourAddonId] ON [tours].[TourScheduleAddonPrices] ([TenantId], [TourScheduleId], [TourAddonId]);
GO

CREATE INDEX [IX_TourScheduleAddonPrices_TourAddonId] ON [tours].[TourScheduleAddonPrices] ([TourAddonId]);
GO

CREATE INDEX [IX_TourScheduleAddonPrices_TourScheduleId] ON [tours].[TourScheduleAddonPrices] ([TourScheduleId]);
GO

CREATE INDEX [IX_TourScheduleCapacities_TenantId_Status_IsActive_IsDeleted] ON [tours].[TourScheduleCapacities] ([TenantId], [Status], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourScheduleCapacities_TenantId_TourScheduleId] ON [tours].[TourScheduleCapacities] ([TenantId], [TourScheduleId]);
GO

CREATE INDEX [IX_TourScheduleCapacities_TourScheduleId] ON [tours].[TourScheduleCapacities] ([TourScheduleId]);
GO

CREATE INDEX [IX_TourSchedulePrices_TenantId_TourScheduleId_IsActive_IsDeleted] ON [tours].[TourSchedulePrices] ([TenantId], [TourScheduleId], [IsActive], [IsDeleted]);
GO

CREATE INDEX [IX_TourSchedulePrices_TenantId_TourScheduleId_IsDefault] ON [tours].[TourSchedulePrices] ([TenantId], [TourScheduleId], [IsDefault]);
GO

CREATE UNIQUE INDEX [IX_TourSchedulePrices_TenantId_TourScheduleId_PriceType] ON [tours].[TourSchedulePrices] ([TenantId], [TourScheduleId], [PriceType]);
GO

CREATE INDEX [IX_TourSchedulePrices_TourScheduleId] ON [tours].[TourSchedulePrices] ([TourScheduleId]);
GO

CREATE INDEX [IX_TourSchedules_TenantId_BookingCutoffAt] ON [tours].[TourSchedules] ([TenantId], [BookingCutoffAt]);
GO

CREATE INDEX [IX_TourSchedules_TenantId_Status_IsActive_IsDeleted] ON [tours].[TourSchedules] ([TenantId], [Status], [IsActive], [IsDeleted]);
GO

CREATE UNIQUE INDEX [IX_TourSchedules_TenantId_TourId_Code] ON [tours].[TourSchedules] ([TenantId], [TourId], [Code]);
GO

CREATE INDEX [IX_TourSchedules_TenantId_TourId_DepartureDate] ON [tours].[TourSchedules] ([TenantId], [TourId], [DepartureDate]);
GO

CREATE INDEX [IX_TourSchedules_TourId] ON [tours].[TourSchedules] ([TourId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310144602_Phase11_Tours_Init', N'8.0.24');
GO

COMMIT;
GO

