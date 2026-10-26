using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Web_Dinosaurios.Data;

/// <summary>
/// Prepara la base de datos SQLite al arrancar la aplicación: crea la carpeta
/// <c>App_Data</c>, aplica <c>Data/schema.sql</c> y, si la tabla <c>Dinosaurios</c>
/// está vacía, carga <c>Data/seed.sql</c>.
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

        var total = await conexion.ExecuteScalarAsync<long>("SELECT COUNT(1) FROM Dinosaurios;");
        if (total == 0)
        {
            await EjecutarScriptAsync(conexion, "seed.sql");
        }
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
}
