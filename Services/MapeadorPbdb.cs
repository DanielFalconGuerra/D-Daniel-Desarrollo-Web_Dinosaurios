using System.Globalization;
using System.Text.Json;
using Web_Dinosaurios.Models;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Convierte un género de la Paleobiology Database, sus países y su resumen de
/// Wikipedia en un <see cref="Dinosaurio"/> aplicando las reglas de la sección 3 de
/// <c>docs/pbdb-importacion.md</c>. Es una clase estática y pura: no hace llamadas de
/// red ni escribe en la base de datos, de modo que el importador puede probarla con
/// datos fijos.
/// </summary>
public static class MapeadorPbdb
{
    /// <summary>Icono que comparten todos los géneros importados (lo dibuja el diseñador).</summary>
    public const string RutaIcono = "/images/dinosaurios/icono.svg";

    /// <summary>Crédito que acompaña al icono.</summary>
    public const string CreditoIcono = "Icono de Web_Dinosaurios";

    /// <summary>Valor usado cuando la fuente no aporta el dato.</summary>
    public const string SinDatos = "Sin datos";

    private const int MaximoPaises = 3;
    private const string PrefijoTaxon = "txn:";
    private const string FamiliaSinEspecificar = "NO_FAMILY_SPECIFIED";
    private const string EspacioInvisible = "\u200B";

    /// <summary>Ruta del archivo con los nombres de países en español.</summary>
    public static string RutaPaisesEs => Path.Combine(AppContext.BaseDirectory, "Data", "paises-es.json");

    private static readonly Lazy<IReadOnlyDictionary<string, string>> PaisesEsCache =
        new(() => CargarPaisesEs(RutaPaisesEs));

    /// <summary>
    /// Nombres de países en español indexados por código ISO de dos letras. Se cargan
    /// una sola vez desde <see cref="RutaPaisesEs"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string> PaisesEs => PaisesEsCache.Value;

    /// <summary>
    /// Construye el <see cref="Dinosaurio"/> de un género.
    /// </summary>
    /// <param name="taxon">Registro de <c>taxa/list.json</c> ya deserializado.</param>
    /// <param name="ocurrenciasPorPais">
    /// Código ISO de dos letras → número de ocurrencias fósiles del género en ese país.
    /// Solo se usan los tres países con más ocurrencias.
    /// </param>
    /// <param name="resumen">Extracto de Wikipedia; <c>null</c> o vacío si no hay artículo.</param>
    /// <param name="tituloWikipedia">
    /// Título final del artículo tras redirecciones y normalizaciones; si falta se usa
    /// el nombre del género.
    /// </param>
    /// <param name="paisesEs">
    /// Diccionario con los nombres de países en español; si falta se usa <see cref="PaisesEs"/>.
    /// </param>
    public static Dinosaurio Mapear(
        PbdbTaxon taxon,
        IReadOnlyDictionary<string, int>? ocurrenciasPorPais,
        string? resumen,
        string? tituloWikipedia = null,
        IReadOnlyDictionary<string, string>? paisesEs = null)
    {
        ArgumentNullException.ThrowIfNull(taxon);

        var descripcion = LimpiarTexto(resumen);
        var pbdbId = LeerPbdbId(taxon.Oid);
        var tieneResumen = descripcion.Length > 0;

        return new Dinosaurio
        {
            Nombre = LimpiarTexto(taxon.Nam),
            Periodo = ObtenerPeriodo(taxon.Fea),
            Antiguedad = FormatearAntiguedad(taxon.Fea, taxon.Lla),
            Region = FormatearRegion(ocurrenciasPorPais, paisesEs),
            Dieta = TraducirDieta(taxon.Jdt),
            Descripcion = tieneResumen ? descripcion : DescripcionDeRespaldo(taxon.Fml),
            ImagenUrl = RutaIcono,
            CreditoImagen = CreditoIcono,
            FuenteUrl = tieneResumen ? UrlWikipedia(tituloWikipedia ?? taxon.Nam) : UrlPbdb(pbdbId),
            PbdbId = pbdbId,
            EdadMaxMa = taxon.Fea
        };
    }

    /// <summary>
    /// Periodo de la Carta Cronoestratigráfica Internacional según la edad máxima
    /// (primera aparición) en millones de años. Devuelve <see cref="SinDatos"/> si la
    /// edad falta o queda fuera del Mesozoico.
    /// </summary>
    public static string ObtenerPeriodo(double? edadMaxMa)
    {
        if (edadMaxMa is not double fea)
        {
            return SinDatos;
        }

        if (fea > 247.2)
        {
            return "Triásico Inferior";
        }

        if (fea > 237)
        {
            return "Triásico Medio";
        }

        if (fea > 201.4)
        {
            return "Triásico Superior";
        }

        if (fea > 174.7)
        {
            return "Jurásico Inferior";
        }

        if (fea > 161.5)
        {
            return "Jurásico Medio";
        }

        if (fea > 145)
        {
            return "Jurásico Superior";
        }

        if (fea > 100.5)
        {
            return "Cretácico Inferior";
        }

        if (fea > 66)
        {
            return "Cretácico Superior";
        }

        return SinDatos;
    }

    /// <summary>
    /// Texto de <see cref="Dinosaurio.Antiguedad"/>: <c>"72,1–66 millones de años"</c>,
    /// con un decimal como máximo, coma decimal y raya «–». Si ambas edades coinciden
    /// (o falta la mínima) devuelve <c>"66 millones de años"</c>.
    /// </summary>
    public static string FormatearAntiguedad(double? edadMaxMa, double? edadMinMa)
    {
        if (edadMaxMa is not double fea)
        {
            return SinDatos;
        }

        var maxima = FormatearMillones(fea);
        if (edadMinMa is not double lla || lla == fea)
        {
            return $"{maxima} millones de años";
        }

        return $"{maxima}–{FormatearMillones(lla)} millones de años";
    }

    /// <summary>
    /// Nombres de los países con más ocurrencias (máximo tres) unidos con <c>", "</c> y
    /// <c>" y "</c> antes del último. Devuelve <see cref="SinDatos"/> si no hay ninguno.
    /// </summary>
    public static string FormatearRegion(
        IReadOnlyDictionary<string, int>? ocurrenciasPorPais,
        IReadOnlyDictionary<string, string>? paisesEs = null)
    {
        if (ocurrenciasPorPais is null || ocurrenciasPorPais.Count == 0)
        {
            return SinDatos;
        }

        var paises = paisesEs ?? PaisesEs;
        var nombres = new List<string>(MaximoPaises);

        foreach (var ocurrencia in ocurrenciasPorPais
                     .Where(par => !string.IsNullOrWhiteSpace(par.Key))
                     .OrderByDescending(par => par.Value)
                     .ThenBy(par => par.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!paises.TryGetValue(ocurrencia.Key.Trim(), out var nombre) || string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            nombre = nombre.Trim();
            if (nombres.Contains(nombre, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            nombres.Add(nombre);
            if (nombres.Count == MaximoPaises)
            {
                break;
            }
        }

        return nombres.Count switch
        {
            0 => SinDatos,
            1 => nombres[0],
            _ => string.Join(", ", nombres.Take(nombres.Count - 1)) + " y " + nombres[^1]
        };
    }

    /// <summary>
    /// Traduce el primer término de <c>jdt</c> («herbivore», «carnivore», …) al español.
    /// Cualquier otro valor devuelve <see cref="SinDatos"/>.
    /// </summary>
    public static string TraducirDieta(string? dieta)
    {
        var termino = PrimerTermino(dieta).ToLowerInvariant();

        return termino switch
        {
            "herbivore" => "Herbívoro",
            "carnivore" => "Carnívoro",
            "omnivore" => "Omnívoro",
            "granivore" => "Granívoro",
            "insectivore" => "Insectívoro",
            "piscivore" => "Piscívoro",
            _ => SinDatos
        };
    }

    /// <summary>
    /// Descripción para los géneros sin artículo en Wikipedia, con la familia cuando
    /// PBDB la aporta.
    /// </summary>
    public static string DescripcionDeRespaldo(string? familia)
    {
        var limpia = LimpiarTexto(familia);
        if (limpia.Length == 0 || string.Equals(limpia, FamiliaSinEspecificar, StringComparison.OrdinalIgnoreCase))
        {
            return "Género de dinosaurio registrado en la Paleobiology Database.";
        }

        return $"Género de dinosaurio de la familia {limpia} registrado en la Paleobiology Database.";
    }

    /// <summary>URL del artículo de Wikipedia en español, con los espacios como <c>_</c>.</summary>
    public static string UrlWikipedia(string? titulo)
    {
        var limpio = LimpiarTexto(titulo);
        return limpio.Length == 0
            ? "https://es.wikipedia.org/wiki/"
            : "https://es.wikipedia.org/wiki/" + limpio.Replace(' ', '_');
    }

    /// <summary>URL de la ficha del género en la Paleobiology Database.</summary>
    public static string UrlPbdb(int? pbdbId) =>
        pbdbId is int id
            ? "https://paleobiodb.org/classic/basicTaxonInfo?taxon_no=" + id.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    /// <summary>Convierte <c>"txn:38613"</c> en <c>38613</c>; <c>null</c> si no se puede leer.</summary>
    public static int? LeerPbdbId(string? oid)
    {
        if (string.IsNullOrWhiteSpace(oid))
        {
            return null;
        }

        var limpio = oid.Trim();
        if (limpio.StartsWith(PrefijoTaxon, StringComparison.OrdinalIgnoreCase))
        {
            limpio = limpio[PrefijoTaxon.Length..];
        }

        return int.TryParse(limpio, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    /// <summary>
    /// Quita los caracteres invisibles y los espacios repetidos de un texto de la API
    /// y recorta los extremos. Un texto nulo o vacío devuelve cadena vacía.
    /// </summary>
    public static string LimpiarTexto(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var limpio = texto.Replace(EspacioInvisible, string.Empty, StringComparison.Ordinal);
        return string.Join(' ', limpio.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Carga los nombres de países en español desde un JSON con forma
    /// <c>{ "AR": "Argentina", … }</c>; también acepta el objeto envuelto en otra
    /// propiedad (por ejemplo <c>{ "paises": { … } }</c>). Si el archivo no existe
    /// devuelve un diccionario vacío.
    /// </summary>
    public static IReadOnlyDictionary<string, string> CargarPaisesEs(string? ruta = null)
    {
        var paises = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var archivo = string.IsNullOrWhiteSpace(ruta) ? RutaPaisesEs : ruta;
        if (!File.Exists(archivo))
        {
            return paises;
        }

        using var documento = JsonDocument.Parse(File.ReadAllText(archivo));
        foreach (var (codigo, nombre) in LeerPares(documento.RootElement))
        {
            var clave = codigo.Trim();
            if (clave.Length == 0 || string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            paises[clave] = nombre.Trim();
        }

        return paises;
    }

    private static IEnumerable<(string Codigo, string Nombre)> LeerPares(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var propiedad in raiz.EnumerateObject())
        {
            if (propiedad.Value.ValueKind == JsonValueKind.String)
            {
                yield return (propiedad.Name, propiedad.Value.GetString() ?? string.Empty);
            }
            else if (propiedad.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var par in LeerPares(propiedad.Value))
                {
                    yield return par;
                }
            }
        }
    }

    private static string FormatearMillones(double valor) =>
        valor.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string PrimerTermino(string? valor)
    {
        var limpio = LimpiarTexto(valor);
        if (limpio.Length == 0)
        {
            return string.Empty;
        }

        var fin = limpio.IndexOfAny([' ', ',', ';', '|']);
        return fin < 0 ? limpio : limpio[..fin];
    }
}
