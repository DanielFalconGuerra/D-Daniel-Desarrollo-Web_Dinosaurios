using Microsoft.AspNetCore.Mvc;
using Web_Dinosaurios.Models;
using Web_Dinosaurios.Repositories;

namespace Web_Dinosaurios.Controllers;

/// <summary>
/// Controlador del catálogo de dinosaurios.
/// </summary>
public class DinosauriosController : Controller
{
    private readonly IDinosaurioRepository _repository;

    public DinosauriosController(IDinosaurioRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// GET: /Dinosaurios
    /// Muestra el listado completo del catálogo.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<Dinosaurio> dinosaurios = await _repository.ObtenerTodosAsync();
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
}
