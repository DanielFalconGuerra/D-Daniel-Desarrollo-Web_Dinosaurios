namespace Web_Dinosaurios.Models;

/// <summary>
/// Representa un dinosaurio del catálogo. Las propiedades coinciden una a una
/// con las columnas de la tabla <c>Dinosaurios</c> descritas en
/// <c>docs/dinosaurios-semilla.md</c> y en <c>docs/pbdb-importacion.md</c>.
/// </summary>
public class Dinosaurio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public string Antiguedad { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Dieta { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string ImagenUrl { get; set; } = string.Empty;

    public string CreditoImagen { get; set; } = string.Empty;

    public string FuenteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del género en la Paleobiology Database. Es <c>null</c> en los
    /// 8 registros semilla; el índice único <c>IX_Dinosaurios_PbdbId</c> evita
    /// importar dos veces el mismo género.
    /// </summary>
    public int? PbdbId { get; set; }

    /// <summary>
    /// Edad máxima en millones de años, usada para ordenar el catálogo. En los
    /// registros semilla es el primer número de <see cref="Antiguedad"/>.
    /// </summary>
    public double? EdadMaxMa { get; set; }
}
