using Web_Dinosaurios.Models;

namespace Web_Dinosaurios.Repositories;

/// <summary>
/// Acceso a los datos del catálogo de dinosaurios.
/// </summary>
public interface IDinosaurioRepository
{
    /// <summary>
    /// Devuelve todos los dinosaurios del catálogo ordenados por <see cref="Dinosaurio.Nombre"/>.
    /// </summary>
    Task<IReadOnlyList<Dinosaurio>> ObtenerTodosAsync();

    /// <summary>
    /// Devuelve el dinosaurio con el identificador indicado o <c>null</c> si no existe.
    /// </summary>
    /// <param name="id">Identificador del dinosaurio.</param>
    Task<Dinosaurio?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Busca dinosaurios por coincidencia parcial de nombre y por periodo exacto.
    /// Los filtros son opcionales: si llegan <c>null</c> o vacíos se ignoran.
    /// </summary>
    /// <param name="nombre">Texto a buscar dentro del nombre (coincidencia parcial).</param>
    /// <param name="periodo">Periodo geológico exacto por el que filtrar.</param>
    /// <returns>Lista de dinosaurios ordenada por <see cref="Dinosaurio.Nombre"/>.</returns>
    Task<IReadOnlyList<Dinosaurio>> BuscarAsync(string? nombre, string? periodo);
}
