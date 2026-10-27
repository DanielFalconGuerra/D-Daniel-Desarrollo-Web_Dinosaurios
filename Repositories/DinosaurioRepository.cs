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

    // El periodo se guarda con su subdivisión («Cretácico Superior», «Jurásico Superior»).
    // Se compara por prefijo en SQL (@Periodo || '%') para que «Cretácico» encuentre
    // «Cretácico Superior», sin concatenar nunca texto del usuario en la sentencia.
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
          AND (@Periodo IS NULL OR Periodo LIKE @Periodo || '%')
        ORDER BY EdadMaxMa IS NULL,
                 EdadMaxMa ASC,
                 Nombre ASC
        LIMIT @Tamano OFFSET @Desplazamiento;
        """;

    // Mismo WHERE que SqlBuscar para que el total coincida con la página devuelta.
    private const string SqlContar = """
        SELECT COUNT(*)
        FROM Dinosaurios
        WHERE (@Nombre IS NULL OR Nombre LIKE '%' || @Nombre || '%')
          AND (@Periodo IS NULL OR Periodo LIKE @Periodo || '%');
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
    public async Task<(IReadOnlyList<Dinosaurio> Elementos, int Total)> BuscarAsync(
        string? nombre,
        string? periodo,
        int pagina,
        int tamano)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (tamano < 1)
        {
            tamano = 1;
        }

        using var connection = _connectionFactory.CrearConexion();

        var parametros = new
        {
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
            Periodo = string.IsNullOrWhiteSpace(periodo) ? null : periodo.Trim(),
            Tamano = tamano,
            Desplazamiento = (long)(pagina - 1) * tamano
        };

        int total = await connection.ExecuteScalarAsync<int>(SqlContar, parametros);

        var dinosaurios = await connection.QueryAsync<Dinosaurio>(SqlBuscar, parametros);

        return (dinosaurios.AsList(), total);
    }
}
