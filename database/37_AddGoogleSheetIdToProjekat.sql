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
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [CheckListItems] (
        [Id] int NOT NULL IDENTITY,
        [Opis] nvarchar(150) NOT NULL,
        [Kompleksnost] decimal(18,2) NULL,
        CONSTRAINT [PK_CheckListItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [CodebookEntities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CodebookEntities] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [DocumentNumbering] (
        [Id] int NOT NULL IDENTITY,
        [Year] int NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [LastNumber] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentNumbering] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ImplementationModels] (
        [Id] int NOT NULL IDENTITY,
        [Naziv] nvarchar(100) NULL,
        [Opis] nvarchar(400) NULL,
        [Aktivan] bit NOT NULL,
        CONSTRAINT [PK_ImplementationModels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Klijenti] (
        [Id] int NOT NULL IDENTITY,
        [Naziv] nvarchar(200) NOT NULL,
        [Pib] nvarchar(20) NULL,
        [MaticniBroj] nvarchar(20) NULL,
        [Adresa] nvarchar(300) NULL,
        [Grad] nvarchar(100) NULL,
        [Zemlja] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Klijenti] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Auth0Id] nvarchar(255) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Name] nvarchar(255) NULL,
        [Role] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastLogin] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [UserSettings] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(255) NOT NULL,
        [OpenAiApiKey] nvarchar(500) NULL,
        [OpenAiModel] nvarchar(50) NOT NULL,
        [DevOpsPersonalAccessToken] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Codebooks] (
        [Id] int NOT NULL IDENTITY,
        [EntityTypeId] int NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Value] nvarchar(200) NOT NULL,
        [OrderIndex] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Codebooks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Codebooks_CodebookEntities_EntityTypeId] FOREIGN KEY ([EntityTypeId]) REFERENCES [CodebookEntities] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ImplementationItems] (
        [Id] int NOT NULL IDENTITY,
        [ImplementationModelId] int NOT NULL,
        [Naziv] nvarchar(100) NOT NULL,
        [Detalji] nvarchar(500) NULL,
        CONSTRAINT [PK_ImplementationItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImplementationItems_ImplementationModels_ImplementationModelId] FOREIGN KEY ([ImplementationModelId]) REFERENCES [ImplementationModels] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Projekti] (
        [Id] int NOT NULL IDENTITY,
        [BrojProjekta] nvarchar(50) NOT NULL,
        [Datum] date NOT NULL,
        [Naziv] nvarchar(300) NOT NULL,
        [Aktivan] bit NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [KlijentId] int NOT NULL,
        [CreatedBy] int NULL,
        [ImplementationModelId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [DevOpsOrganization] nvarchar(200) NULL,
        [DevOpsProject] nvarchar(200) NULL,
        [DevOpsAreaPath] nvarchar(500) NULL,
        [DevOpsIterationPath] nvarchar(500) NULL,
        CONSTRAINT [PK_Projekti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Projekti_ImplementationModels_ImplementationModelId] FOREIGN KEY ([ImplementationModelId]) REFERENCES [ImplementationModels] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Projekti_Klijenti_KlijentId] FOREIGN KEY ([KlijentId]) REFERENCES [Klijenti] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projekti_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ImplementationItemCheckListItems] (
        [Id] int NOT NULL IDENTITY,
        [ImplementationItemId] int NOT NULL,
        [CheckListItemId] int NOT NULL,
        CONSTRAINT [PK_ImplementationItemCheckListItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImplementationItemCheckListItems_CheckListItems_CheckListItemId] FOREIGN KEY ([CheckListItemId]) REFERENCES [CheckListItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ImplementationItemCheckListItems_ImplementationItems_ImplementationItemId] FOREIGN KEY ([ImplementationItemId]) REFERENCES [ImplementationItems] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Dokumenti] (
        [Id] int NOT NULL IDENTITY,
        [ProjekatId] int NOT NULL,
        [NazivFajla] nvarchar(500) NOT NULL,
        [TipFajla] nvarchar(50) NOT NULL,
        [BlobUrl] nvarchar(1000) NOT NULL,
        [Velicina] bigint NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Dokumenti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Dokumenti_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Notes] (
        [Id] int NOT NULL IDENTITY,
        [ProjekatId] int NOT NULL,
        [Opis] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notes_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ProjectImplementationItems] (
        [Id] int NOT NULL IDENTITY,
        [ImplementationModelId] int NOT NULL,
        [ProjectId] int NOT NULL,
        [ImplementationItemId] int NOT NULL,
        [Napomena] nvarchar(150) NULL,
        [Zavrseno] bit NOT NULL,
        [ZavrsenoDatum] datetime2 NULL,
        [KlijentPotvrdio] bit NOT NULL,
        [KlijentPotvrdioDatum] datetime2 NULL,
        CONSTRAINT [PK_ProjectImplementationItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectImplementationItems_ImplementationItems_ImplementationItemId] FOREIGN KEY ([ImplementationItemId]) REFERENCES [ImplementationItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectImplementationItems_ImplementationModels_ImplementationModelId] FOREIGN KEY ([ImplementationModelId]) REFERENCES [ImplementationModels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectImplementationItems_Projekti_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projekti] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ProjectPermissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ProjekatId] int NOT NULL,
        [PermissionLevel] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        CONSTRAINT [PK_ProjectPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectPermissions_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [Aktivnosti] (
        [Id] int NOT NULL IDENTITY,
        [Opis] nvarchar(max) NOT NULL,
        [Detalji] nvarchar(4000) NOT NULL,
        [Datum] datetime2 NOT NULL,
        [StartUtc] datetime2 NULL,
        [EndUtc] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        [Vrsta] nvarchar(100) NOT NULL,
        [Bau] bit NOT NULL,
        [ProjekatId] int NULL,
        [ProjectImplementationItemId] int NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Aktivnosti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Aktivnosti_ProjectImplementationItems_ProjectImplementationItemId] FOREIGN KEY ([ProjectImplementationItemId]) REFERENCES [ProjectImplementationItems] ([Id]),
        CONSTRAINT [FK_Aktivnosti_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Aktivnosti_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [ProjectImplementationItemCheckLists] (
        [Id] int NOT NULL IDENTITY,
        [ProjectImplementationItemId] int NOT NULL,
        [CheckListItemId] int NOT NULL,
        [Zavrsen] bit NOT NULL,
        [ZavrsenDatum] datetime2 NULL,
        [Procenat] decimal(5,2) NULL,
        CONSTRAINT [PK_ProjectImplementationItemCheckLists] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectImplementationItemCheckLists_CheckListItems_CheckListItemId] FOREIGN KEY ([CheckListItemId]) REFERENCES [CheckListItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectImplementationItemCheckLists_ProjectImplementationItems_ProjectImplementationItemId] FOREIGN KEY ([ProjectImplementationItemId]) REFERENCES [ProjectImplementationItems] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE TABLE [DevOpsTasksCandidates] (
        [Id] int NOT NULL IDENTITY,
        [AktivnostId] int NOT NULL,
        [UserId] int NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NULL,
        [AcceptanceCriteria] nvarchar(max) NULL,
        [Priority] nvarchar(50) NULL,
        [Estimation] nvarchar(50) NULL,
        [OrderIndex] int NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DevOpsTasksCandidates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DevOpsTasksCandidates_Aktivnosti_AktivnostId] FOREIGN KEY ([AktivnostId]) REFERENCES [Aktivnosti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DevOpsTasksCandidates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Aktivnosti_CreatedBy] ON [Aktivnosti] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Aktivnosti_ProjectImplementationItemId] ON [Aktivnosti] ([ProjectImplementationItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Aktivnosti_ProjekatId] ON [Aktivnosti] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Aktivnosti_Status] ON [Aktivnosti] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CodebookEntities_Name] ON [CodebookEntities] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Codebooks_EntityTypeId] ON [Codebooks] ([EntityTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Codebooks_EntityTypeId_Code] ON [Codebooks] ([EntityTypeId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Codebooks_EntityTypeId_IsActive] ON [Codebooks] ([EntityTypeId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_DevOpsTasksCandidates_AktivnostId] ON [DevOpsTasksCandidates] ([AktivnostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_DevOpsTasksCandidates_UserId] ON [DevOpsTasksCandidates] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Dokumenti_ProjekatId] ON [Dokumenti] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ImplementationItemCheckListItems_CheckListItemId] ON [ImplementationItemCheckListItems] ([CheckListItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ImplementationItemCheckListItems_ImplementationItemId] ON [ImplementationItemCheckListItems] ([ImplementationItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ImplementationItems_ImplementationModelId] ON [ImplementationItems] ([ImplementationModelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ImplementationModels_Aktivan] ON [ImplementationModels] ([Aktivan]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Klijenti_Naziv] ON [Klijenti] ([Naziv]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Notes_ProjekatId] ON [Notes] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectImplementationItemCheckLists_CheckListItemId] ON [ProjectImplementationItemCheckLists] ([CheckListItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId] ON [ProjectImplementationItemCheckLists] ([ProjectImplementationItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectImplementationItems_ImplementationItemId] ON [ProjectImplementationItems] ([ImplementationItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectImplementationItems_ImplementationModelId] ON [ProjectImplementationItems] ([ImplementationModelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectImplementationItems_ProjectId] ON [ProjectImplementationItems] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectPermissions_ProjekatId] ON [ProjectPermissions] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_ProjectPermissions_UserId] ON [ProjectPermissions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projekti_BrojProjekta] ON [Projekti] ([BrojProjekta]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Projekti_CreatedBy] ON [Projekti] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Projekti_ImplementationModelId] ON [Projekti] ([ImplementationModelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Projekti_KlijentId] ON [Projekti] ([KlijentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    CREATE INDEX [IX_Projekti_Status] ON [Projekti] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220194217_ChangeDatumToDateOnly'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220194217_ChangeDatumToDateOnly', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227112438_AddPdvFieldsToKlijenti'
)
BEGIN
    ALTER TABLE [Klijenti] ADD [PdvRegistrationDate] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227112438_AddPdvFieldsToKlijenti'
)
BEGIN
    ALTER TABLE [Klijenti] ADD [PdvStatus] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260227112438_AddPdvFieldsToKlijenti'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260227112438_AddPdvFieldsToKlijenti', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    ALTER TABLE [Projekti] ADD [AIPracen] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    ALTER TABLE [Projekti] ADD [GoogleSheetId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE TABLE [AiReminders] (
        [Id] int NOT NULL IDENTITY,
        [AktivnostId] int NOT NULL,
        [ProjekatId] int NULL,
        [UserId] int NOT NULL,
        [RemindAt] datetime2 NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Sent] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AiReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiReminders_Aktivnosti_AktivnostId] FOREIGN KEY ([AktivnostId]) REFERENCES [Aktivnosti] ([Id]),
        CONSTRAINT [FK_AiReminders_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AiReminders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE TABLE [DailyTasks] (
        [Id] int NOT NULL IDENTITY,
        [Datum] date NOT NULL,
        [Opis] nvarchar(500) NOT NULL,
        [KorisnikKreirao] int NOT NULL,
        [Solved] bit NOT NULL,
        [Pinned] bit NOT NULL,
        CONSTRAINT [PK_DailyTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyTasks_Users_KorisnikKreirao] FOREIGN KEY ([KorisnikKreirao]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ProjekatId] int NULL,
        [Type] nvarchar(50) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [IsRead] bit NOT NULL,
        [Dismissed] bit NOT NULL,
        [AktivnostId] int NULL,
        [ReferenceKey] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Aktivnosti_AktivnostId] FOREIGN KEY ([AktivnostId]) REFERENCES [Aktivnosti] ([Id]),
        CONSTRAINT [FK_Notifications_Projekti_ProjekatId] FOREIGN KEY ([ProjekatId]) REFERENCES [Projekti] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_AiReminders_AktivnostId] ON [AiReminders] ([AktivnostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_AiReminders_ProjekatId] ON [AiReminders] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_AiReminders_RemindAt_Sent] ON [AiReminders] ([RemindAt], [Sent]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_AiReminders_UserId] ON [AiReminders] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_DailyTasks_KorisnikKreirao] ON [DailyTasks] ([KorisnikKreirao]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_DailyTasks_KorisnikKreirao_Datum] ON [DailyTasks] ([KorisnikKreirao], [Datum]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_Notifications_AktivnostId] ON [Notifications] ([AktivnostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_Notifications_ProjekatId] ON [Notifications] ([ProjekatId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Notifications_ReferenceKey] ON [Notifications] ([ReferenceKey]) WHERE [ReferenceKey] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401131759_AddGoogleSheetIdToProjekat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260401131759_AddGoogleSheetIdToProjekat', N'8.0.0');
END;
GO

COMMIT;
GO

