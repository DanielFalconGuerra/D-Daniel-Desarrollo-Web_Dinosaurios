using System.Text.Json.Serialization;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Raíz de la respuesta de <c>taxa/list.json</c> de la Paleobiology Database.
/// </summary>
public sealed record PbdbRespuestaTaxones
{
    /// <summary>Géneros devueltos; puede faltar si la consulta no encuentra nada.</summary>
    [JsonPropertyName("records")]
    public List<PbdbTaxon>? Records { get; init; }
}

/// <summary>
/// Un género de <c>taxa/list.json</c> con los campos abreviados que usa el importador.
/// Todos los campos son opcionales: PBDB omite los que no conoce.
/// </summary>
public sealed record PbdbTaxon
{
    /// <summary>Identificador con prefijo, p. ej. <c>"txn:38613"</c>.</summary>
    [JsonPropertyName("oid")]
    public string? Oid { get; init; }

    /// <summary>Nombre del género, p. ej. <c>"Tyrannosaurus"</c>.</summary>
    [JsonPropertyName("nam")]
    public string? Nam { get; init; }

    /// <summary>Edad máxima (primera aparición) en millones de años, p. ej. <c>72.1</c>.</summary>
    [JsonPropertyName("fea")]
    public double? Fea { get; init; }

    /// <summary>Edad mínima (última aparición) en millones de años, p. ej. <c>66</c>.</summary>
    [JsonPropertyName("lla")]
    public double? Lla { get; init; }

    /// <summary>Familia, p. ej. <c>"Tyrannosauridae"</c>; vale <c>"NO_FAMILY_SPECIFIED"</c> si no está asignada.</summary>
    [JsonPropertyName("fml")]
    public string? Fml { get; init; }

    /// <summary>Dieta en inglés, p. ej. <c>"carnivore"</c>.</summary>
    [JsonPropertyName("jdt")]
    public string? Jdt { get; init; }
}

/// <summary>
/// Raíz de la respuesta de <c>occs/list.json</c> de la Paleobiology Database.
/// </summary>
public sealed record PbdbRespuestaOcurrencias
{
    /// <summary>Ocurrencias fósiles devueltas; puede faltar si la consulta no encuentra nada.</summary>
    [JsonPropertyName("records")]
    public List<PbdbOcurrencia>? Records { get; init; }
}

/// <summary>
/// Una ocurrencia fósil de <c>occs/list.json</c> con los campos abreviados que usa el importador.
/// Todos los campos son opcionales: PBDB omite los que no conoce.
/// </summary>
public sealed record PbdbOcurrencia
{
    /// <summary>Nombre del género al que pertenece la ocurrencia, p. ej. <c>"Tyrannosaurus"</c>.</summary>
    [JsonPropertyName("gnl")]
    public string? Gnl { get; init; }

    /// <summary>Código ISO de dos letras del país, p. ej. <c>"US"</c> o <c>"CN"</c>.</summary>
    [JsonPropertyName("cc2")]
    public string? Cc2 { get; init; }
}
