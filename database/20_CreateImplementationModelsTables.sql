-- 20_CreateImplementationModelsTables.sql
-- Kreiranje tabela za modele implementacije i stavke

-- Tabela je već kreirana u bazi, ova skripta je za dokumentaciju
-- CREATE TABLE [dbo].[ImplementationModels](
-- 	[Id] [int] IDENTITY(1,1) NOT NULL,
-- 	[Naziv] [nvarchar](100) NULL,
-- 	[Opis] [nvarchar](400) NULL,
-- 	[Aktivan] [bit] NOT NULL,
--  CONSTRAINT [PK_ImplementationModels] PRIMARY KEY CLUSTERED ([Id] ASC)
-- )
-- 
-- ALTER TABLE [dbo].[ImplementationModels] ADD CONSTRAINT [DF_ImplementationModels_Aktivan] DEFAULT ((0)) FOR [Aktivan]

-- CREATE TABLE [dbo].[ImplementationItems](
-- 	[Id] [int] IDENTITY(1,1) NOT NULL,
-- 	[ImplementationModelId] [int] NOT NULL,
-- 	[Naziv] [nvarchar](100) NOT NULL,
-- 	[Detalji] [nvarchar](500) NULL,
--  CONSTRAINT [PK_ImplementationItems] PRIMARY KEY CLUSTERED ([Id] ASC)
-- )
-- 
-- ALTER TABLE [dbo].[ImplementationItems] ADD CONSTRAINT [FK_ImplementationItems_ImplementationModels]
--     FOREIGN KEY ([ImplementationModelId])
--     REFERENCES [dbo].[ImplementationModels] ([Id])
--     ON DELETE CASCADE

PRINT 'ImplementationModels tables already exist in database!';
