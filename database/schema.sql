-- Creates the PokemonCacheDb database (if it doesn't already exist) and the
-- single table the API reads and writes with plain SQL (no ORM).

IF DB_ID('PokemonCacheDb') IS NULL
BEGIN
    CREATE DATABASE PokemonCacheDb;
END
GO

USE PokemonCacheDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Pokemon' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Pokemon
    (
        Id                 INT             NOT NULL PRIMARY KEY,  -- same id PokeAPI uses, so it's a natural key
        Name               NVARCHAR(100)   NOT NULL,
        HeightDecimetres   INT             NULL,
        WeightHectograms   INT             NULL,
        Types              NVARCHAR(200)   NULL,                  -- comma-separated, e.g. "grass,poison"
        ImageUrl           NVARCHAR(500)   NULL,
        CreatedAtUtc       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_Pokemon_Name ON dbo.Pokemon (Name);
END
GO
