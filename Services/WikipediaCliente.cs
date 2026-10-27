using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Cliente de la API de Wikipedia en español que obtiene el extracto introductorio
/// (dos frases) de los géneros importados desde la Paleobiology Database.
/// Contrato: <c>docs/pbdb-importacion.md</c> §2 y §3.
/// </summary>
/// <remarks>
/// Reglas del contrato:
/// <list type="bullet">
///   <item>Lotes de 20 títulos por llamada (<c>exlimit=20</c>).</item>
///   <item>250 ms de espera entre lotes.</item>
///   <item>Cabecera <c>User-Agent</c> obligatoria (sin ella Wikipedia rechaza la llamada).</item>
///   <item>Se resuelven <c>query.normalized</c> y <c>query.redirects</c> para volver del título final al género pedido.</item>
///   <item>Se eliminan los caracteres invisibles <c>\u200B</c> y los espacios repetidos.</item>
/// </list>
/// Solo se devuelven los géneros que tienen artículo con extracto no vacío, con clave igual
/// al género solicitado y valor <c>(Titulo final, Extracto limpio)</c>.
/// </remarks>
public sealed class WikipediaCliente
{
    private const string UrlApi = "https://es.wikipedia.org/w/api.php";
    private const string UserAgent = "Web_Dinosaurios/1.0 (proyecto de aprendizaje)";
    private const int TamanoLote = 20;

    private static readonly TimeSpan EsperaEntreLotes = TimeSpan.FromMilliseconds(250);

    /// <summary>Carácter invisible (U+200B) que la API devuelve dentro de algunos extractos.</summary>
    private const string CaracterInvisible = "\u200B";

    private static readonly Regex EspaciosRepetidos = new(@"\s+", RegexOptions.Compiled);

    private readonly HttpClient _http;

    public WikipediaCliente(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>
    /// Obtiene el resumen introductorio de cada género pedido.
    /// </summary>
    /// <param name="generos">Nombres de género tal como llegan de la Paleobiology Database (p. ej. <c>Tyrannosaurus</c>).</param>
    /// <param name="cancelacion">Token opcional de cancelación.</param>
    /// <returns>
    /// Diccionario indexado por el género solicitado; el valor es el título final del artículo
    /// y el extracto limpio. Los géneros sin artículo (o con extracto vacío) no aparecen.
    /// </returns>
    public async Task<Dictionary<string, (string Titulo, string Extracto)>> ObtenerResumenesAsync(
        IReadOnlyList<string> generos,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(generos);

        var resumenes = new Dictionary<string, (string Titulo, string Extracto)>(StringComparer.OrdinalIgnoreCase);
        if (generos.Count == 0)
        {
            return resumenes;
        }

        for (var inicio = 0; inicio < generos.Count; inicio += TamanoLote)
        {
            // Pausa de cortesía entre lotes (la primera llamada sale de inmediato).
            if (inicio > 0)
            {
                await Task.Delay(EsperaEntreLotes, cancelacion);
            }

            var lote = generos.Skip(inicio).Take(TamanoLote).ToList();
            var (alias, paginas) = await ConsultarLoteAsync(lote, cancelacion);

            foreach (var genero in lote)
            {
                var tituloFinal = ResolverTitulo(genero, alias);
                if (!paginas.TryGetValue(tituloFinal, out var pagina))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(pagina.Extracto))
                {
                    continue;
                }

                resumenes[genero] = (pagina.Titulo, pagina.Extracto);
            }
        }

        return resumenes;
    }

    /// <summary>
    /// Llama a la API para un lote de hasta 20 títulos y devuelve el mapa de alias
    /// (<c>normalized</c> + <c>redirects</c>) y las páginas con su extracto.
    /// </summary>
    private async Task<(Dictionary<string, string> Alias, Dictionary<string, ResumenWikipedia> Paginas)>
        ConsultarLoteAsync(IReadOnlyList<string> lote, CancellationToken cancelacion)
    {
        var titulos = Uri.EscapeDataString(string.Join("|", lote));
        var url = $"{UrlApi}?action=query&format=json&prop=extracts&exintro=1&explaintext=1" +
                  $"&exsentences=2&exlimit={TamanoLote}&redirects=1&titles={titulos}";

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, url);
        solicitud.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        solicitud.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var respuesta = await _http.SendAsync(solicitud, cancelacion);
        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);

        var alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var paginas = new Dictionary<string, ResumenWikipedia>(StringComparer.OrdinalIgnoreCase);

        using var documento = JsonDocument.Parse(cuerpo);
        if (!documento.RootElement.TryGetProperty("query", out var consulta) ||
            consulta.ValueKind != JsonValueKind.Object)
        {
            return (alias, paginas);
        }

        // normalized y redirects comparten forma { from, to }: se encadenan en un único mapa.
        foreach (var clave in new[] { "normalized", "redirects" })
        {
            if (!consulta.TryGetProperty(clave, out var cambios) || cambios.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var cambio in cambios.EnumerateArray())
            {
                var desde = LeerCadena(cambio, "from");
                var hacia = LeerCadena(cambio, "to");
                if (desde is null || hacia is null)
                {
                    continue;
                }

                alias[NormalizarTitulo(desde)] = NormalizarTitulo(hacia);
            }
        }

        if (consulta.TryGetProperty("pages", out var nodoPaginas))
        {
            foreach (var elemento in EnumerarPaginas(nodoPaginas))
            {
                var titulo = LeerCadena(elemento, "title");
                if (titulo is null)
                {
                    continue;
                }

                var extracto = LimpiarExtracto(LeerCadena(elemento, "extract"));
                paginas[NormalizarTitulo(titulo)] = new ResumenWikipedia(titulo.Replace('_', ' ').Trim(), extracto);
            }
        }

        return (alias, paginas);
    }

    /// <summary>
    /// Recorre <c>query.pages</c> admitiendo tanto el formato con clave por pageid (formato 1)
    /// como el formato de arreglo (formato 2).
    /// </summary>
    private static IEnumerable<JsonElement> EnumerarPaginas(JsonElement nodoPaginas)
    {
        switch (nodoPaginas.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var propiedad in nodoPaginas.EnumerateObject())
                {
                    yield return propiedad.Value;
                }
                break;

            case JsonValueKind.Array:
                foreach (var elemento in nodoPaginas.EnumerateArray())
                {
                    yield return elemento;
                }
                break;
        }
    }

    /// <summary>
    /// Sigue la cadena de normalizaciones/redirecciones hasta el título final.
    /// Un ciclo (no debería ocurrir) se corta devolviendo el último título visitado.
    /// </summary>
    private static string ResolverTitulo(string genero, Dictionary<string, string> alias)
    {
        var actual = NormalizarTitulo(genero);
        var visitados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (alias.TryGetValue(actual, out var siguiente) && visitados.Add(actual))
        {
            actual = siguiente;
        }

        return actual;
    }

    /// <summary>Los títulos de MediaWiki usan guiones bajos; la API los devuelve con espacios.</summary>
    private static string NormalizarTitulo(string titulo) => titulo.Replace('_', ' ').Trim();

    /// <summary>Quita los caracteres invisibles U+200B y colapsa los espacios repetidos.</summary>
    private static string LimpiarExtracto(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var limpio = texto.Replace(CaracterInvisible, string.Empty, StringComparison.Ordinal);
        return EspaciosRepetidos.Replace(limpio, " ").Trim();
    }

    private static string? LeerCadena(JsonElement elemento, string nombre)
    {
        if (elemento.ValueKind != JsonValueKind.Object ||
            !elemento.TryGetProperty(nombre, out var propiedad) ||
            propiedad.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return propiedad.GetString();
    }

    /// <summary>Página de Wikipedia ya normalizada (título final y extracto limpio).</summary>
    private readonly record struct ResumenWikipedia(string Titulo, string Extracto);
}
