-- Add PIB and MaticniBroj columns to Klijenti table
ALTER TABLE Klijenti
ADD Pib NVARCHAR(20) NULL,
    MaticniBroj NVARCHAR(20) NULL;
GO
