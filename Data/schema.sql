-- Esquema de la base de datos de Web_Dinosaurios
-- Contrato de datos: docs/dinosaurios-semilla.md
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
    FuenteUrl     TEXT    NOT NULL
);
