using System.Data;
using Dapper;
using Web_Dinosaurios.Data;
using Web_Dinosaurios.Models;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Importa el catálogo de dinosaurios desde la Paleobiology Database (PBDB) y
/// Wikipedia. Solo se invoca desde <c>dotnet run -- importar-pbdb</c>; nunca al
/// arrancar la web. Implementa las reglas de la sección 4 de
/// <c>docs/pbdb-importacion.md</c>.
/// </summary>
/// <remarks>
/// Orden de operaciones:
/// <list type="number">
///   <item>Pedir los géneros a PBDB (una llamada).</item>
///   <item>Descartar los registros sin <c>fea</c> o con <c>fea &lt;= 66</c> (fuera del Mesozoico).</item>
///   <item>Pedir los países por género a PBDB.</item>
///   <item>Pedir los resúmenes a Wikipedia en lotes de 20.</item>
///   <item>Mapear cada candidato a <see cref="Dinosaurio"/> con <see cref="MapeadorPbdb"/>.</item>
///   <item>Insertar todo en una sola transacción, omitiendo los géneros ya presentes.</item>
/// </list>
/// Si una API falla antes de abrir la transacción no se inserta nada; si falla
/// dentro de la transacción se hace rollback y se re-lanza la excepción.
/// </remarks>
public sealed class ImportadorPbdb
{
    private readonly PbdbCliente _pbdb;
    private readonly WikipediaCliente _wikipedia;
    private readonly IDbConnectionFactory _conexiones;

    public ImportadorPbdb(
        PbdbCliente pbdb,
        WikipediaCliente wikipedia,
        IDbConnectionFactory conexiones)
    {
        _pbdb = pbdb ?? throw new ArgumentNullException(nameof(pbdb));
        _wikipedia = wikipedia ?? throw new ArgumentNullException(nameof(wikipedia));
        _conexiones = conexiones ?? throw new ArgumentNullException(nameof(conexiones));
    }

    /// <summary>
    /// Ejecuta la importación completa. Devuelve el número de filas insertadas.
    /// </summary>
    public async Task<int> ImportarAsync(CancellationToken cancelacion = default)
    {
        // 1. Géneros de PBDB.
        Console.WriteLine("Consultando la Paleobiology Database (taxa/list)…");
        var generos = await _pbdb.ObtenerGenerosAsync(cancelacion);
        var recibidos = generos.Count;
        Console.WriteLine($"PBDB devolvió {recibidos} géneros.");

        // 2. Descartar los registros sin 'fea' o con 'fea <= 66' (fuera del Mesozoico).
        //    También se descartan los que no traen nombre utilizable.
        var candidatos = generos
            .Where(t => t.Fea is double fea && fea > 66 && !string.IsNullOrWhiteSpace(t.Nam))
            .ToList();
        var descartados = recibidos - candidatos.Count;
        Console.WriteLine($"Candidatos dentro del Mesozoico: {candidatos.Count} " +
                          $"({descartados} descartados).");

        // 3. Países por género (una sola llamada, corpus completo de ocurrencias).
        Console.WriteLine("Consultando los países de los fósiles (occs/list)…");
        var paisesPorGenero = await _pbdb.ObtenerPaisesPorGeneroAsync(cancelacion);

        // 4. Resúmenes de Wikipedia en lotes de 20 títulos.
        Console.WriteLine("Descargando resúmenes introductorios de Wikipedia…");
        var nombres = candidatos
            .Select(t => t.Nam)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resumenes = await _wikipedia.ObtenerResumenesAsync(nombres, cancelacion);

        // 5. Mapear cada candidato a Dinosaurio (reglas de docs/pbdb-importacion.md §3).
        var nuevos = new List<Dinosaurio>(candidatos.Count);
        var nombresConWikipedia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var taxon in candidatos)
        {
            paisesPorGenero.TryGetValue(taxon.Nam, out var paises);

            if (resumenes.TryGetValue(taxon.Nam, out var resumen))
            {
                nombresConWikipedia.Add(taxon.Nam);
                nuevos.Add(MapeadorPbdb.Mapear(taxon, paises, resumen.Extracto, resumen.Titulo));
            }
            else
            {
                nuevos.Add(MapeadorPbdb.Mapear(taxon, paises, resumen: null, tituloWikipedia: null));
            }
        }

        // 6. Insertar en una sola transacción, omitiendo los géneros ya presentes.
        var omitidos = 0;
        var insertados = 0;
        var insertadosConWikipedia = 0;

        using var conexion = _conexiones.CrearConexion();
        if (conexion.State != ConnectionState.Open)
        {
            conexion.Open();
        }

        using var transaccion = conexion.BeginTransaction();
        try
        {
            // Se omite un género si ya existe una fila con su nombre exacto
            // o con un nombre que empiece por él seguido de un espacio
            // (p. ej. 'Tyrannosaurus rex' impide insertar 'Tyrannosaurus').
            const string sqlExiste =
                "SELECT COUNT(1) FROM Dinosaurios " +
                "WHERE Nombre = @Nombre OR Nombre LIKE @Nombre || ' %'";

            const string sqlInsertar = """
                INSERT OR IGNORE INTO Dinosaurios
                    (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion,
                     ImagenUrl, CreditoImagen, FuenteUrl, PbdbId, EdadMaxMa)
                VALUES
                    (@Nombre, @Periodo, @Antiguedad, @Region, @Dieta, @Descripcion,
                     @ImagenUrl, @CreditoImagen, @FuenteUrl, @PbdbId, @EdadMaxMa)
                """;

            foreach (var dino in nuevos)
            {
                var existentes = await conexion.ExecuteScalarAsync<long>(
                    sqlExiste, new { dino.Nombre }, transaccion);

                if (existentes > 0)
                {
                    omitidos++;
                    continue;
                }

                // INSERT OR IGNORE protege además contra los índices únicos
                // (PbdbId y Nombre): cualquier colisión se cuenta como omitido.
                var filas = await conexion.ExecuteAsync(sqlInsertar, dino, transaccion);

                if (filas > 0)
                {
                    insertados++;
                    if (nombresConWikipedia.Contains(dino.Nombre))
                    {
                        insertadosConWikipedia++;
                    }
                }
                else
                {
                    omitidos++;
                }
            }

            transaccion.Commit();
        }
        catch
        {
            try
            {
                transaccion.Rollback();
            }
            catch
            {
                // El error de la importación es más importante que el del rollback;
                // se re-lanza el original desde el throw de abajo.
            }

            throw;
        }

        // 7. Resumen de la sección 4 del contrato.
        Console.WriteLine();
        Console.WriteLine("=== Resumen de la importación PBDB ===");
        Console.WriteLine($"Géneros recibidos:                          {recibidos}");
        Console.WriteLine($"Descartados (sin fea o fea <= 66):          {descartados}");
        Console.WriteLine($"Omitidos por ya existir:                     {omitidos}");
        Console.WriteLine($"Insertados:                                 {insertados}");
        Console.WriteLine($"Con descripción de Wikipedia:               {insertadosConWikipedia}");
        Console.WriteLine();

        return insertados;
    }
}
