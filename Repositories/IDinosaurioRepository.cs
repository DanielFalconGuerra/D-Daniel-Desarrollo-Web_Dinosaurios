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
}
