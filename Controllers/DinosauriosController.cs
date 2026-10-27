using Microsoft.AspNetCore.Mvc;
using Web_Dinosaurios.Models;
using Web_Dinosaurios.Repositories;

namespace Web_Dinosaurios.Controllers;

/// <summary>
/// Controlador del catálogo de dinosaurios.
/// </summary>
public class DinosauriosController : Controller
{
    /// <summary>
    /// Cantidad de dinosaurios que se muestran por página.
    /// </summary>
    private const int TamanoPagina = 24;

    /// <summary>
    /// Periodos geológicos admitidos por el filtro del catálogo.
    /// </summary>
    private static readonly string[] PeriodosValidos = { "Triásico", "Jurásico", "Cretácico" };

    /// <summary>
    /// Dietas admitidas por el filtro del catálogo.
    /// </summary>
    private static readonly string[] DietasValidas = { "Herbívoro", "Carnívoro", "Omnívoro", "Piscívoro" };

    private readonly IDinosaurioRepository _repository;

    public DinosauriosController(IDinosaurioRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// GET: /Dinosaurios?nombre=...&amp;periodo=...&amp;dieta=...&amp;conImagen=true&amp;pagina=2
    /// Muestra el listado del catálogo aplicando los filtros opcionales y la paginación.
    /// </summary>
    /// <param name="nombre">Texto de búsqueda por nombre; se recorta y se ignora si está vacío.</param>
    /// <param name="periodo">Periodo geológico; solo se aceptan «Triásico», «Jurásico» y «Cretácico».</param>
    /// <param name="dieta">Dieta de la especie; solo se aceptan los valores de <see cref="DietasValidas"/>.</param>
    /// <param name="conImagen">Si es <c>true</c>, muestra únicamente especies con ilustración propia.</param>
    /// <param name="pagina">Número de página solicitado, empezando en 1.</param>
    public async Task<IActionResult> Index(
        string? nombre,
        string? periodo,
        string? dieta,
        bool conImagen = false,
        int pagina = 1)
    {
        nombre = NormalizarTexto(nombre);
        periodo = NormalizarValor(periodo, PeriodosValidos);
        dieta = NormalizarValor(dieta, DietasValidas);

        if (pagina < 1)
        {
            pagina = 1;
        }

        (IReadOnlyList<Dinosaurio> elementos, int total) =
            await _repository.BuscarAsync(nombre, periodo, pagina, TamanoPagina, dieta, conImagen);

        int totalPaginas = total == 0
            ? 1
            : (int)Math.Ceiling(total / (double)TamanoPagina);

        ViewData["NombreActual"] = nombre;
        ViewData["PeriodoActual"] = periodo;
        ViewData["PeriodosValidos"] = PeriodosValidos;
        ViewData["DietaActual"] = dieta;
        ViewData["DietasValidas"] = DietasValidas;
        ViewData["ConImagenActual"] = conImagen;
        ViewData["Pagina"] = pagina;
        ViewData["TotalPaginas"] = totalPaginas;
        ViewData["Total"] = total;
        ViewData["TamanoPagina"] = TamanoPagina;

        return View(elementos);
    }

    /// <summary>
    /// GET: /Dinosaurios/Detalle/5
    /// Muestra la ficha completa de un dinosaurio o 404 si no existe.
    /// </summary>
    /// <param name="id">Identificador del dinosaurio.</param>
    public async Task<IActionResult> Detalle(int id)
    {
        Dinosaurio? dinosaurio = await _repository.ObtenerPorIdAsync(id);
        if (dinosaurio is null)
        {
            return NotFound();
        }

        return View(dinosaurio);
    }

    /// <summary>
    /// Recorta el texto y devuelve <c>null</c> cuando queda vacío.
    /// </summary>
    private static string? NormalizarTexto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        string recortado = valor.Trim();
        return recortado.Length == 0 ? null : recortado;
    }

    /// <summary>
    /// Valida el valor contra la lista admitida y devuelve su forma canónica.
    /// Cualquier otro valor se ignora (<c>null</c>).
    /// </summary>
    private static string? NormalizarValor(string? valor, string[] permitidos)
    {
        string? recortado = NormalizarTexto(valor);
        if (recortado is null)
        {
            return null;
        }

        foreach (string valido in permitidos)
        {
            if (string.Equals(valido, recortado, StringComparison.OrdinalIgnoreCase))
            {
                return valido;
            }
        }

        return null;
    }
}
