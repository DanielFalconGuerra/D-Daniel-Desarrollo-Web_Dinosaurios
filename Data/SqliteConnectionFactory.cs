using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Web_Dinosaurios.Data;

/// <summary>
/// Implementación de <see cref="IDbConnectionFactory"/> para SQLite.
/// Lee la cadena de conexión de <c>ConnectionStrings:Dinosaurios</c>.
/// </summary>
public class SqliteConnectionFactory : IDbConnectionFactory
{
    private const string NombreCadenaConexion = "Dinosaurios";

    private readonly string _cadenaConexion;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                $"No se encontró la cadena de conexión 'ConnectionStrings:{NombreCadenaConexion}' en la configuración.");
    }

    public IDbConnection CrearConexion()
    {
        var conexion = new SqliteConnection(_cadenaConexion);
        conexion.Open();
        return conexion;
    }
}
