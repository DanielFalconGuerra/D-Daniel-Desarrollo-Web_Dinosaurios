using System.Text;

namespace Web_Dinosaurios.Services;

/// <summary>
/// Utilidades de texto, sin red, para los extractos de Wikipedia.
/// Contrato: <c>docs/pbdb-importacion.md</c> §6 (calidad de las descripciones).
/// </summary>
/// <remarks>
/// <see cref="Recortar"/> reemplaza al parámetro <c>exsentences</c> de la API (que cortaba en abreviaturas):
/// un punto cierra una frase solo si no hay paréntesis ni comillas abiertos en ese punto y lo sigue
/// un espacio y una mayúscula, o el final del texto. Se conservan las dos primeras frases y, si pasan de
/// <see cref="MaxCaracteres"/>, se corta en el último espacio anterior y se termina con «…».
/// <see cref="TrataDeDinosaurio"/> descarta los artículos homónimos (personajes, lugares, folclore,
/// desambiguaciones) buscando raíces temáticas sin distinguir mayúsculas ni acentos.
/// </remarks>
public static class TextoWikipedia
{
    /// <summary>Longitud máxima del extracto recortado, sin contar los puntos suspensivos.</summary>
    public const int MaxCaracteres = 400;

    private const string PuntosSuspensivos = "…";

    /// <summary>Raíces temáticas que debe contener un extracto para aceptarse (§6, regla 2).</summary>
    private static readonly string[] RaicesDinosaurio =
    {
        "dinosaur",
        "teropod",
        "sauropod",
        "ornitopod",
        "ornitisqui",
        "saurisqui",
        "arcosaur",
        "fosil",
        "extint"
    };

    /// <summary>
    /// Recorta un extracto introductorio a dos frases y a <see cref="MaxCaracteres"/> caracteres.
    /// </summary>
    /// <param name="extracto">Texto devuelto por la API de Wikipedia (ya sin caracteres invisibles).</param>
    /// <returns>
    /// El extracto recortado; cadena vacía si la entrada está vacía o solo tiene espacios.
    /// Si no se detecta el cierre de la segunda frase, se devuelve el texto completo (sujeto al límite).
    /// </returns>
    public static string Recortar(string? extracto)
    {
        if (string.IsNullOrWhiteSpace(extracto))
        {
            return string.Empty;
        }

        var texto = extracto.Trim();
        var fin = BuscarFinDeSegundaFrase(texto);
        var recorte = (fin > 0 ? texto[..fin] : texto).TrimEnd();

        if (recorte.Length <= MaxCaracteres)
        {
            return recorte;
        }

        var corte = UltimoEspacioAntesDe(recorte, MaxCaracteres);
        return corte > 0
            ? recorte[..corte].TrimEnd() + PuntosSuspensivos
            : recorte[..MaxCaracteres].TrimEnd() + PuntosSuspensivos;
    }

    /// <summary>
    /// Indica si el extracto trata realmente de un dinosaurio (§6, regla 2).
    /// </summary>
    /// <param name="extracto">Texto del artículo; se compara sin mayúsculas ni acentos.</param>
    /// <returns>
    /// <c>true</c> si contiene alguna raíz temática (<c>dinosaur</c>, <c>teropod</c>, <c>sauropod</c>,
    /// <c>ornitopod</c>, <c>ornitisqui</c>, <c>saurisqui</c>, <c>arcosaur</c>, <c>fosil</c>, <c>extint</c>).
    /// </returns>
    public static bool TrataDeDinosaurio(string? extracto)
    {
        if (string.IsNullOrWhiteSpace(extracto))
        {
            return false;
        }

        var normalizado = Normalizar(extracto);
        foreach (var raiz in RaicesDinosaurio)
        {
            if (normalizado.Contains(raiz, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Recorre el texto y devuelve el índice inmediatamente posterior al punto que cierra la segunda
    /// frase, o <c>-1</c> si el texto no llega a tener dos frases cerradas.
    /// </summary>
    private static int BuscarFinDeSegundaFrase(string texto)
    {
        var parentesis = 0;
        var comillasAngulares = 0;
        var comillaDoble = false;
        var frases = 0;

        for (var i = 0; i < texto.Length; i++)
        {
            switch (texto[i])
            {
                case '(':
                    parentesis++;
                    break;

                case ')':
                    if (parentesis > 0)
                    {
                        parentesis--;
                    }
                    break;

                case '«':
                case '“':
                case '‹':
                    comillasAngulares++;
                    break;

                case '»':
                case '”':
                case '›':
                    if (comillasAngulares > 0)
                    {
                        comillasAngulares--;
                    }
                    break;

                case '"':
                    comillaDoble = !comillaDoble;
                    break;

                case '.':
                    if (parentesis == 0 && comillasAngulares == 0 && !comillaDoble && CierraFrase(texto, i))
                    {
                        frases++;
                        if (frases == 2)
                        {
                            return i + 1;
                        }
                    }
                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Un punto cierra frase si le sigue un espacio (o varios) y una mayúscula, o si es el final del texto.
    /// Los decimales (<c>72.1</c>), las abreviaturas (<c>gr.</c>) y los puntos suspensivos no cierran.
    /// </summary>
    private static bool CierraFrase(string texto, int posicion)
    {
        var i = posicion + 1;
        if (i >= texto.Length)
        {
            return true;
        }

        if (!char.IsWhiteSpace(texto[i]))
        {
            return false;
        }

        while (i < texto.Length && char.IsWhiteSpace(texto[i]))
        {
            i++;
        }

        return i >= texto.Length || char.IsUpper(texto[i]);
    }

    /// <summary>Índice del último espacio dentro de los primeros <paramref name="limite"/> caracteres, o <c>-1</c>.</summary>
    private static int UltimoEspacioAntesDe(string texto, int limite)
    {
        var tope = Math.Min(limite, texto.Length);
        for (var i = tope - 1; i > 0; i--)
        {
            if (char.IsWhiteSpace(texto[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Pasa el texto a minúsculas y le quita los acentos y la diéresis.</summary>
    private static string Normalizar(string texto)
    {
        var constructor = new StringBuilder(texto.Length);
        foreach (var caracter in texto)
        {
            constructor.Append(SinAcento(caracter));
        }

        return constructor.ToString().ToLowerInvariant();
    }

    private static char SinAcento(char caracter) => caracter switch
    {
        'á' or 'à' or 'ä' or 'â' or 'Á' or 'À' or 'Ä' or 'Â' => 'a',
        'é' or 'è' or 'ë' or 'ê' or 'É' or 'È' or 'Ë' or 'Ê' => 'e',
        'í' or 'ì' or 'ï' or 'î' or 'Í' or 'Ì' or 'Ï' or 'Î' => 'i',
        'ó' or 'ò' or 'ö' or 'ô' or 'Ó' or 'Ò' or 'Ö' or 'Ô' => 'o',
        'ú' or 'ù' or 'ü' or 'û' or 'Ú' or 'Ù' or 'Ü' or 'Û' => 'u',
        'ñ' or 'Ñ' => 'n',
        'ç' or 'Ç' => 'c',
        _ => caracter
    };
}
