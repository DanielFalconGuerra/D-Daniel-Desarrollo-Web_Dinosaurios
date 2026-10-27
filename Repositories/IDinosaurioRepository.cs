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
    /// Busca dinosaurios por coincidencia parcial de nombre, por periodo (comparado por prefijo),
    /// por dieta exacta y opcionalmente solo aquellos con ilustración propia, devolviendo una
    /// página del resultado junto con el total de coincidencias.
    /// Los filtros son opcionales: si llegan <c>null</c> o vacíos se ignoran.
    /// </summary>
    /// <param name="nombre">Texto a buscar dentro del nombre (coincidencia parcial).</param>
    /// <param name="periodo">Periodo geológico por el que filtrar (prefijo, p. ej. «Cretácico»).</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamano">Cantidad de elementos por página.</param>
    /// <param name="dieta">Dieta exacta por la que filtrar (p. ej. «Carnívoro»).</param>
    /// <param name="soloConImagen">Si es <c>true</c>, excluye los géneros que usan el icono por defecto.</param>
    /// <returns>
    /// <c>Elementos</c> con la página solicitada ordenada por <see cref="Dinosaurio.EdadMaxMa"/>
    /// ascendente (los <c>null</c> al final) y luego por <see cref="Dinosaurio.Nombre"/>;
    /// <c>Total</c> con el número de coincidencias de los filtros, sin paginar.
    /// </returns>
    Task<(IReadOnlyList<Dinosaurio> Elementos, int Total)> BuscarAsync(
        string? nombre,
        string? periodo,
        int pagina,
        int tamano,
        string? dieta = null,
        bool soloConImagen = false);
}
