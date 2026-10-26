using Dapper;
using Web_Dinosaurios.Data;
using Web_Dinosaurios.Models;

namespace Web_Dinosaurios.Repositories;

/// <summary>
/// Implementación de <see cref="IDinosaurioRepository"/> con Dapper sobre SQLite.
/// </summary>
public class DinosaurioRepository : IDinosaurioRepository
{
    private const string SqlObtenerTodos = """
        SELECT Id,
               Nombre,
               Periodo,
               Antiguedad,
               Region,
               Dieta,
               Descripcion,
               ImagenUrl,
               CreditoImagen,
               FuenteUrl
        FROM Dinosaurios
        ORDER BY Nombre;
        """;

    private const string SqlObtenerPorId = """
        SELECT Id,
               Nombre,
               Periodo,
               Antiguedad,
               Region,
               Dieta,
               Descripcion,
               ImagenUrl,
               CreditoImagen,
               FuenteUrl
        FROM Dinosaurios
        WHERE Id = @Id;
        """;

    private const string SqlBuscar = """
        SELECT Id,
               Nombre,
               Periodo,
               Antiguedad,
               Region,
               Dieta,
               Descripcion,
               ImagenUrl,
               CreditoImagen,
               FuenteUrl
        FROM Dinosaurios
        WHERE (@Nombre IS NULL OR Nombre LIKE '%' || @Nombre || '%')
          AND (@Periodo IS NULL OR Periodo = @Periodo)
        ORDER BY Nombre;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public DinosaurioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dinosaurio>> ObtenerTodosAsync()
    {
        using var connection = _connectionFactory.CrearConexion();

        var dinosaurios = await connection.QueryAsync<Dinosaurio>(SqlObtenerTodos);

        return dinosaurios.AsList();
    }

    /// <inheritdoc />
    public async Task<Dinosaurio?> ObtenerPorIdAsync(int id)
    {
        using var connection = _connectionFactory.CrearConexion();

        var dinosaurio = await connection.QuerySingleOrDefaultAsync<Dinosaurio>(
            SqlObtenerPorId,
            new { Id = id });

        return dinosaurio;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dinosaurio>> BuscarAsync(string? nombre, string? periodo)
    {
        using var connection = _connectionFactory.CrearConexion();

        var parametros = new
        {
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
            Periodo = string.IsNullOrWhiteSpace(periodo) ? null : periodo.Trim()
        };

        var dinosaurios = await connection.QueryAsync<Dinosaurio>(SqlBuscar, parametros);

        return dinosaurios.AsList();
    }
}
