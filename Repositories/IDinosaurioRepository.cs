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
}
