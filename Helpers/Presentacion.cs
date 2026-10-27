namespace Web_Dinosaurios.Helpers;

/// <summary>
/// Clases CSS y textos de presentación que comparten el catálogo y la ficha.
/// Contrato visual: <c>docs/guia-ui.md</c> §2, §3 y §5.
/// </summary>
public static class Presentacion
{
    /// <summary>Icono que usan los géneros importados de la PBDB, que no tienen ilustración.</summary>
    public const string UrlIcono = "/images/dinosaurios/icono.svg";

    // Época → (clase CSS de la época, límites de la Carta Cronoestratigráfica Internacional)
    private static readonly Dictionary<string, (string Clase, string Limites)> Epocas = new()
    {
        ["Cretácico Superior"] = ("dino-cretacico-superior", "100,5–66 Ma"),
        ["Cretácico Inferior"] = ("dino-cretacico-inferior", "145–100,5 Ma"),
        ["Jurásico Superior"] = ("dino-jurasico-superior", "161,5–145 Ma"),
        ["Jurásico Medio"] = ("dino-jurasico-medio", "174,7–161,5 Ma"),
        ["Jurásico Inferior"] = ("dino-jurasico-inferior", "201,4–174,7 Ma"),
        ["Triásico Superior"] = ("dino-triasico-superior", "237–201,4 Ma"),
        ["Triásico Medio"] = ("dino-triasico-medio", "247,2–237 Ma"),
        ["Triásico Inferior"] = ("dino-triasico-inferior", "251,9–247,2 Ma"),
    };

    /// <summary>Clases del periodo (banda) y de la época (encabezado), p. ej. «dino-cretacico dino-cretacico-superior».</summary>
    public static string ClasesPeriodo(string periodo)
    {
        var clasePeriodo = periodo.StartsWith("Triásico") ? "dino-triasico"
            : periodo.StartsWith("Jurásico") ? "dino-jurasico"
            : periodo.StartsWith("Cretácico") ? "dino-cretacico"
            : "dino-periodo-otro";

        return Epocas.TryGetValue(periodo, out var epoca) ? $"{clasePeriodo} {epoca.Clase}" : clasePeriodo;
    }

    /// <summary>Límites de la época en millones de años, o cadena vacía si el periodo no es una época conocida.</summary>
    public static string LimitesEpoca(string periodo) =>
        Epocas.TryGetValue(periodo, out var epoca) ? epoca.Limites : string.Empty;

    public static bool EsIcono(string? imagenUrl) =>
        string.Equals(imagenUrl, UrlIcono, StringComparison.OrdinalIgnoreCase);

    /// <summary>«Carnívoro, Argentina» sin los «Sin datos» que deja la importación.</summary>
    public static string DatosBreves(string dieta, string region)
    {
        var partes = new[] { dieta, region }.Where(p => !string.IsNullOrWhiteSpace(p) && p != "Sin datos").ToList();
        return partes.Count == 0 ? "Dieta y región sin datos" : string.Join(", ", partes);
    }

    public static string TextoFuente(string fuenteUrl) =>
        fuenteUrl.Contains("wikipedia.org") ? "Leer en Wikipedia"
        : fuenteUrl.Contains("paleobiodb.org") ? "Ver en la Paleobiology Database"
        : "Leer la fuente";
}
