using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Web_Dinosaurios.Data;

/// <summary>
/// Prepara la base de datos SQLite al arrancar la aplicación: crea la carpeta
/// <c>App_Data</c>, aplica <c>Data/schema.sql</c>, agrega las columnas de la
/// importación PBDB si faltan (<c>PbdbId</c> y <c>EdadMaxMa</c>), carga
/// <c>Data/seed.sql</c> cuando la tabla <c>Dinosaurios</c> está vacía y rellena
/// la edad máxima de los registros semilla.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Nombre de la cadena de conexión. Es exactamente la misma que usa
    /// <see cref="SqliteConnectionFactory"/>, para no acabar sembrando una base de datos
    /// distinta de la que luego lee el repositorio.
    /// </summary>
    public const string NombreCadenaConexion = "Dinosaurios";

    /// <summary>
    /// Primer número de un texto: entero o decimal con coma o punto
    /// («68–66 millones de años» → 68; «72,1–66 millones de años» → 72,1).
    /// </summary>
    private static readonly Regex PatronPrimerNumero = new(
        @"\d+(?:[.,]\d+)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Inicializa la base de datos de forma idempotente. Se invoca una sola vez,
    /// después de construir la aplicación y antes de <c>app.Run()</c>.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <exception cref="InvalidOperationException">
    /// Si falta la cadena de conexión <c>Dinosaurios</c>.
    /// </exception>
    public static async Task InicializarAsync(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Sin respaldo: si la cadena no existe, fallamos igual que la fábrica de conexiones
        // en lugar de inventarnos una ruta por defecto.
        var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}' (ConnectionStrings:{NombreCadenaConexion}).");
        }

        CrearDirectorioDeDatos(cadenaConexion);

        await using var conexion = new SqliteConnection(cadenaConexion);
        await conexion.OpenAsync();

        await EjecutarScriptAsync(conexion, "schema.sql");

        // Las columnas y el índice de la importación PBDB se aplican desde C#:
        // en bases ya creadas la tabla existe sin ellas y schema.sql no las añade.
        await AplicarColumnasPbdbAsync(conexion);

        var total = await conexion.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM Dinosaurios;");
        if (total == 0)
        {
            await EjecutarScriptAsync(conexion, "seed.sql");
        }

        await RellenarEdadMaxMaSemillaAsync(conexion);
    }

    /// <summary>
    /// Agrega <c>PbdbId</c> y <c>EdadMaxMa</c> a las bases creadas antes de la
    /// importación PBDB y garantiza el índice único <c>IX_Dinosaurios_PbdbId</c>.
    /// </summary>
    /// <remarks>
    /// El índice se crea aquí y no en <c>schema.sql</c> porque, en una base ya
    /// existente, <c>CREATE TABLE IF NOT EXISTS</c> no añade la columna <c>PbdbId</c>:
    /// crearlo antes del <c>ALTER TABLE</c> fallaría con «no such column: PbdbId».
    /// SQLite no admite <c>ADD COLUMN ... UNIQUE</c>; la unicidad va en este índice,
    /// que además permite varios <c>NULL</c> (los 8 registros semilla).
    /// </remarks>
    private static async Task AplicarColumnasPbdbAsync(IDbConnection conexion)
    {
        var columnas = (await conexion.QueryAsync<string>(new CommandDefinition(
                "SELECT name FROM pragma_table_info('Dinosaurios');")))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!columnas.Contains("PbdbId"))
        {
            await conexion.ExecuteAsync(new CommandDefinition(
                "ALTER TABLE Dinosaurios ADD COLUMN PbdbId INTEGER NULL;"));
        }

        if (!columnas.Contains("EdadMaxMa"))
        {
            await conexion.ExecuteAsync(new CommandDefinition(
                "ALTER TABLE Dinosaurios ADD COLUMN EdadMaxMa REAL NULL;"));
        }

        await conexion.ExecuteAsync(new CommandDefinition(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Dinosaurios_PbdbId ON Dinosaurios(PbdbId);"));
    }

    /// <summary>
    /// Rellena <c>EdadMaxMa</c> de los registros semilla con el primer número de
    /// <c>Antiguedad</c> (sección 3 de <c>docs/pbdb-importacion.md</c>).
    /// Solo toca filas sin <c>PbdbId</c> (las 8 semilla) que aún no tengan
    /// <c>EdadMaxMa</c>, así que se puede ejecutar en cada arranque.
    /// </summary>
    private static async Task RellenarEdadMaxMaSemillaAsync(IDbConnection conexion)
    {
        var filas = await conexion.QueryAsync<FilaSemilla>(new CommandDefinition(
            "SELECT Id, Antiguedad FROM Dinosaurios WHERE PbdbId IS NULL AND EdadMaxMa IS NULL;"));

        foreach (var fila in filas)
        {
            if (!TryObtenerPrimerNumero(fila.Antiguedad, out var edadMaxima))
            {
                continue;
            }

            await conexion.ExecuteAsync(new CommandDefinition(
                "UPDATE Dinosaurios SET EdadMaxMa = @EdadMaxima WHERE Id = @Id;",
                new { EdadMaxima = edadMaxima, fila.Id }));
        }
    }

    /// <summary>
    /// Extrae el primer número de un texto como «68–66 millones de años» o
    /// «72,1–66 millones de años», aceptando coma o punto como separador decimal.
    /// </summary>
    private static bool TryObtenerPrimerNumero(string? texto, out double valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var coincidencia = PatronPrimerNumero.Match(texto);
        if (!coincidencia.Success)
        {
            return false;
        }

        var numero = coincidencia.Value.Replace(',', '.');
        return double.TryParse(numero, NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
    }

    /// <summary>
    /// Crea la carpeta donde vive el archivo <c>.db</c> (App_Data no está versionada).
    /// Las rutas relativas se resuelven igual que lo hace SQLite: contra el directorio actual.
    /// </summary>
    private static void CrearDirectorioDeDatos(string cadenaConexion)
    {
        var origen = new SqliteConnectionStringBuilder(cadenaConexion).DataSource;
        if (string.IsNullOrWhiteSpace(origen) || origen == ":memory:")
        {
            return;
        }

        var rutaCompleta = Path.GetFullPath(origen);
        var directorio = Path.GetDirectoryName(rutaCompleta);
        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
        }
    }

    /// <summary>
    /// Ejecuta un script SQL copiado junto al ensamblado, en <c>Data/</c>.
    /// </summary>
    private static async Task EjecutarScriptAsync(IDbConnection conexion, string nombreArchivo)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Data", nombreArchivo);
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                $"No se encontró el script SQL de inicialización '{nombreArchivo}'.", ruta);
        }

        var sql = await File.ReadAllTextAsync(ruta);
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        await conexion.ExecuteAsync(new CommandDefinition(sql));
    }

    /// <summary>
    /// Proyección mínima de una fila semilla para rellenar <c>EdadMaxMa</c>.
    /// </summary>
    private sealed class FilaSemilla
    {
        public long Id { get; set; }

        public string Antiguedad { get; set; } = string.Empty;
    }
}
