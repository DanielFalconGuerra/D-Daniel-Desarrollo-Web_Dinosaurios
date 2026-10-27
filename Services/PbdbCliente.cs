using System.Text.Json;
using System.Text.Json.Serialization;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Cliente de la Paleobiology Database (PBDB). Solo se usa desde el importador
/// (<c>dotnet run -- importar-pbdb</c>), nunca al arrancar la web.
/// </summary>
public sealed class PbdbCliente
{
    /// <summary>Géneros válidos de Dinosauria (sin aves ni huevos fósiles), ~1 775 registros.</summary>
    private const string UrlGeneros =
        "https://paleobiodb.org/data1.2/taxa/list.json?base_name=Dinosauria%5EAves&rank=genus&taxon_status=valid&pres=regular&show=app,class,ecospace";

    /// <summary>Ocurrencias fósiles con la localización, ~7 500 registros.</summary>
    private const string UrlOcurrencias =
        "https://paleobiodb.org/data1.2/occs/list.json?base_name=Dinosauria%5EAves&pres=regular&idreso=genus&show=class,loc";

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
        // PBDB a veces devuelve las edades como cadena ("72.1"); aceptamos ambos formatos.
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _http;
    private readonly ILogger<PbdbCliente> _logger;

    public PbdbCliente(HttpClient http, ILogger<PbdbCliente> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve los géneros de Dinosauria (sin aves) de <c>taxa/list.json</c>.
    /// Lanza la excepción de la llamada si PBDB no responde o responde con error.
    /// </summary>
    public async Task<IReadOnlyList<PbdbTaxon>> ObtenerGenerosAsync(CancellationToken cancellationToken = default)
    {
        var respuesta = await ObtenerAsync<PbdbRespuestaTaxones>(UrlGeneros, cancellationToken);

        if (respuesta.Records is null)
        {
            _logger.LogWarning("PBDB no devolvió el arreglo 'records' al consultar los géneros.");
            return Array.Empty<PbdbTaxon>();
        }

        _logger.LogInformation("PBDB devolvió {Total} géneros.", respuesta.Records.Count);
        return respuesta.Records;
    }

    /// <summary>
    /// Devuelve, por género, el número de ocurrencias fósiles de cada país:
    /// <c>género → código de país (cc2) → ocurrencias</c>.
    /// </summary>
    public async Task<Dictionary<string, Dictionary<string, int>>> ObtenerPaisesPorGeneroAsync(
        CancellationToken cancellationToken = default)
    {
        var respuesta = await ObtenerAsync<PbdbRespuestaOcurrencias>(UrlOcurrencias, cancellationToken);

        var paisesPorGenero = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var ocurrencia in respuesta.Records ?? [])
        {
            if (string.IsNullOrWhiteSpace(ocurrencia.Gnl) || string.IsNullOrWhiteSpace(ocurrencia.Cc2))
            {
                continue;
            }

            if (!paisesPorGenero.TryGetValue(ocurrencia.Gnl, out var paises))
            {
                paises = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                paisesPorGenero[ocurrencia.Gnl] = paises;
            }

            paises[ocurrencia.Cc2] = paises.TryGetValue(ocurrencia.Cc2, out var total) ? total + 1 : 1;
        }

        _logger.LogInformation(
            "PBDB devolvió {Ocurrencias} ocurrencias para {Generos} géneros.",
            respuesta.Records?.Count ?? 0,
            paisesPorGenero.Count);

        return paisesPorGenero;
    }

    /// <summary>Descarga y deserializa un JSON de PBDB; cualquier fallo se propaga al llamador.</summary>
    private async Task<T> ObtenerAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        using var respuesta = await _http.GetAsync(url, cancellationToken);
        respuesta.EnsureSuccessStatusCode();

        await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
        var datos = await JsonSerializer.DeserializeAsync<T>(flujo, OpcionesJson, cancellationToken);

        if (datos is null)
        {
            throw new InvalidOperationException($"PBDB devolvió una respuesta vacía o ilegible: {url}");
        }

        return datos;
    }
}
