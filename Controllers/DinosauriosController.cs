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
}
