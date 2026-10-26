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
    /// Periodos geológicos admitidos por el filtro del catálogo.
    /// </summary>
    private static readonly string[] PeriodosValidos = { "Jurásico", "Cretácico" };

    private readonly IDinosaurioRepository _repository;

    public DinosauriosController(IDinosaurioRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// GET: /Dinosaurios?nombre=...&amp;periodo=...
    /// Muestra el listado del catálogo aplicando los filtros opcionales.
    /// </summary>
    /// <param name="nombre">Texto de búsqueda por nombre; se recorta y se ignora si está vacío.</param>
    /// <param name="periodo">Periodo geológico; solo se aceptan «Jurásico» y «Cretácico».</param>
    public async Task<IActionResult> Index(string? nombre, string? periodo)
    {
        nombre = NormalizarTexto(nombre);
        periodo = NormalizarPeriodo(periodo);

        IReadOnlyList<Dinosaurio> dinosaurios = await _repository.BuscarAsync(nombre, periodo);

        ViewData["NombreActual"] = nombre;
        ViewData["PeriodoActual"] = periodo;
        ViewData["PeriodosValidos"] = PeriodosValidos;

        return View(dinosaurios);
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
    /// Valida el periodo contra la lista admitida y devuelve su forma canónica.
    /// Cualquier otro valor se ignora (<c>null</c>).
    /// </summary>
    private static string? NormalizarPeriodo(string? periodo)
    {
        string? recortado = NormalizarTexto(periodo);
        if (recortado is null)
        {
            return null;
        }

        foreach (string valido in PeriodosValidos)
        {
            if (string.Equals(valido, recortado, StringComparison.OrdinalIgnoreCase))
            {
                return valido;
            }
        }

        return null;
    }
}
