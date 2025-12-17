-- Create Database
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ProjectOrganizer')
BEGIN
    CREATE DATABASE ProjectOrganizer;
END
GO

USE ProjectOrganizer;
GO
