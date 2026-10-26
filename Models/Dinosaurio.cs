namespace Web_Dinosaurios.Models;

/// <summary>
/// Representa un dinosaurio del catálogo. Las propiedades coinciden una a una
/// con las columnas de la tabla <c>Dinosaurios</c> descritas en
/// <c>docs/dinosaurios-semilla.md</c>.
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
}
