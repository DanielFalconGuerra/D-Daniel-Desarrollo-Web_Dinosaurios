-- Esquema de la base de datos de Web_Dinosaurios
-- Contrato de datos: docs/dinosaurios-semilla.md
-- Ampliación PBDB: docs/pbdb-importacion.md (sección 3)
-- Base: App_Data/dinosaurios.db (SQLite, no versionada)

CREATE TABLE IF NOT EXISTS Dinosaurios (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre        TEXT    NOT NULL UNIQUE,
    Periodo       TEXT    NOT NULL,
    Antiguedad    TEXT    NOT NULL,
    Region        TEXT    NOT NULL,
    Dieta         TEXT    NOT NULL,
    Descripcion   TEXT    NOT NULL,
    ImagenUrl     TEXT    NOT NULL,
    CreditoImagen TEXT    NOT NULL,
    FuenteUrl     TEXT    NOT NULL,
    PbdbId        INTEGER NULL,
    EdadMaxMa     REAL    NULL
);

-- El índice único IX_Dinosaurios_PbdbId NO se crea en este script.
-- En una base ya existente, CREATE TABLE IF NOT EXISTS no agrega la columna
-- PbdbId y un CREATE INDEX aquí fallaría con «no such column: PbdbId»,
-- interrumpiendo el arranque. Lo crea DatabaseInitializer.AplicarColumnasPbdbAsync
-- después del ALTER TABLE correspondiente (también en bases nuevas).
