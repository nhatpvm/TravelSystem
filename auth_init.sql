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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(200) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [AvatarUrl] nvarchar(500) NULL,
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_IsActive] ON [AspNetUsers] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260225111520_Identity_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260225111520_Identity_Init', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226054153_Tenants_Init'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [HoldMinutes] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226054153_Tenants_Init'
)
BEGIN
    CREATE TABLE [TenantUsers] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(max) NOT NULL,
        [IsOwner] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] varbinary(max) NOT NULL,
        CONSTRAINT [PK_TenantUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226054153_Tenants_Init'
)
BEGIN

    IF SCHEMA_ID(N'tenants') IS NULL EXEC(N'CREATE SCHEMA [tenants]');
    IF OBJECT_ID(N'[dbo].[Tenants]', N'U') IS NOT NULL
        EXEC(N'ALTER SCHEMA [tenants] TRANSFER [dbo].[Tenants]');
    IF OBJECT_ID(N'[dbo].[TenantUsers]', N'U') IS NOT NULL
        EXEC(N'ALTER SCHEMA [tenants] TRANSFER [dbo].[TenantUsers]');

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226054153_Tenants_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226054153_Tenants_Init', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    IF SCHEMA_ID(N'tenants') IS NULL EXEC(N'CREATE SCHEMA [tenants];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    ALTER SCHEMA [tenants] TRANSFER [TenantUsers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    ALTER SCHEMA [tenants] TRANSFER [Tenants];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[TenantUsers]') AND [c].[name] = N'RowVersion');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[TenantUsers] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [tenants].[TenantUsers] ALTER COLUMN [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[TenantUsers]') AND [c].[name] = N'RoleName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[TenantUsers] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [tenants].[TenantUsers] ALTER COLUMN [RoleName] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[TenantUsers]') AND [c].[name] = N'IsDeleted');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[TenantUsers] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [tenants].[TenantUsers] ADD DEFAULT CAST(0 AS bit) FOR [IsDeleted];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[Tenants]') AND [c].[name] = N'RowVersion');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[Tenants] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [tenants].[Tenants] ALTER COLUMN [RowVersion] rowversion NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[Tenants]') AND [c].[name] = N'Name');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[Tenants] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [tenants].[Tenants] ALTER COLUMN [Name] nvarchar(200) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[Tenants]') AND [c].[name] = N'IsDeleted');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[Tenants] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [tenants].[Tenants] ADD DEFAULT CAST(0 AS bit) FOR [IsDeleted];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[Tenants]') AND [c].[name] = N'HoldMinutes');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[Tenants] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [tenants].[Tenants] ADD DEFAULT 5 FOR [HoldMinutes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[tenants].[Tenants]') AND [c].[name] = N'Code');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [tenants].[Tenants] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [tenants].[Tenants] ALTER COLUMN [Code] nvarchar(32) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantUsers_TenantId] ON [tenants].[TenantUsers] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantUsers_TenantId_UserId] ON [tenants].[TenantUsers] ([TenantId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantUsers_UserId] ON [tenants].[TenantUsers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Code] ON [tenants].[Tenants] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_Tenants_Status] ON [tenants].[Tenants] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_Tenants_Type] ON [tenants].[Tenants] ([Type]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_Permissions_Category] ON [auth].[Permissions] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [auth].[Permissions] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_Permissions_IsActive] ON [auth].[Permissions] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [auth].[RolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [auth].[RolePermissions] ([RoleId], [PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantRolePermissions_PermissionId] ON [tenants].[TenantRolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantRolePermissions_TenantId] ON [tenants].[TenantRolePermissions] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantRolePermissions_TenantId_TenantRoleId_PermissionId] ON [tenants].[TenantRolePermissions] ([TenantId], [TenantRoleId], [PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantRolePermissions_TenantRoleId] ON [tenants].[TenantRolePermissions] ([TenantRoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantRoles_IsActive] ON [tenants].[TenantRoles] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantRoles_TenantId] ON [tenants].[TenantRoles] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantRoles_TenantId_Code] ON [tenants].[TenantRoles] ([TenantId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantUserRoles_TenantId] ON [tenants].[TenantUserRoles] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantUserRoles_TenantId_TenantRoleId_UserId] ON [tenants].[TenantUserRoles] ([TenantId], [TenantRoleId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantUserRoles_TenantRoleId] ON [tenants].[TenantUserRoles] ([TenantRoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_TenantUserRoles_UserId] ON [tenants].[TenantUserRoles] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_PermissionId] ON [auth].[UserPermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_TenantId] ON [auth].[UserPermissions] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_UserId] ON [auth].[UserPermissions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionId_TenantId] ON [auth].[UserPermissions] ([UserId], [PermissionId], [TenantId]) WHERE [TenantId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    ALTER TABLE [tenants].[TenantUsers] ADD CONSTRAINT [FK_TenantUsers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    ALTER TABLE [tenants].[TenantUsers] ADD CONSTRAINT [FK_TenantUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tenants].[Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226153601_AuthPermissions_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226153601_AuthPermissions_Init', N'8.0.24');
END;
GO

COMMIT;
GO

