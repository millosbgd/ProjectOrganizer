-- Create Mailovi table for storing Office 365 emails

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Mailovi]') AND type = 'U')
BEGIN
    CREATE TABLE [Mailovi] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [UserId]              INT NOT NULL,
        [MessageId]           NVARCHAR(500) NOT NULL,
        [From]                NVARCHAR(500) NOT NULL DEFAULT '',
        [Cc]                  NVARCHAR(1000) NULL,
        [Subject]             NVARCHAR(998) NOT NULL DEFAULT '',
        [BodyText]            NVARCHAR(MAX) NULL,
        [BodyHtml]            NVARCHAR(MAX) NULL,
        [ReceivedDateTime]    DATETIME2 NOT NULL,
        [DatumUcitavanja]     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_Mailovi] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_Mailovi_UserId_MessageId] UNIQUE ([UserId], [MessageId]),
        CONSTRAINT [FK_Mailovi_Users] FOREIGN KEY ([UserId])
            REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Mailovi_UserId] ON [Mailovi] ([UserId]);
    CREATE INDEX [IX_Mailovi_ReceivedDateTime] ON [Mailovi] ([ReceivedDateTime] DESC);

    PRINT 'Table Mailovi created.';
END
ELSE
    PRINT 'Table Mailovi already exists.';
