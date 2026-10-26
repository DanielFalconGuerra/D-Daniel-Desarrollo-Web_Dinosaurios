using System.Data;

namespace Web_Dinosaurios.Data;

/// <summary>
/// Fábrica de conexiones a la base de datos de dinosaurios.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Crea (y abre) una nueva conexión a la base de datos.
    /// </summary>
    IDbConnection CrearConexion();
}
