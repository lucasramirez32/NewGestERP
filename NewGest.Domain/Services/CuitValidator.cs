namespace NewGest.Domain.Services;

/// <summary>
/// Valida CUIT/CUIL argentino usando algoritmo módulo 11.
/// Migrado de FUNCION.PRG líneas 595-664.
/// </summary>
public static class CuitValidator
{
    private static readonly int[] Multiplicadores = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    public static bool EsValido(string? cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit)) return false;

        cuit = cuit.Replace("-", "").Replace(" ", "");

        if (cuit.Length != 11 || !cuit.All(char.IsDigit)) return false;

        int suma = Multiplicadores.Select((m, i) => m * (cuit[i] - '0')).Sum();
        int resto = suma % 11;
        int digitoVerif = resto == 0 ? 0 : resto == 1 ? 9 : 11 - resto;

        return digitoVerif == (cuit[10] - '0');
    }

    /// <summary>
    /// Formatea un CUIT de 11 dígitos como XX-XXXXXXXX-X.
    /// Si el string no tiene exactamente 11 dígitos lo retorna sin cambios.
    /// </summary>
    public static string Formatear(string? cuit)
    {
        if (string.IsNullOrEmpty(cuit)) return string.Empty;
        var digits = cuit.Replace("-", "").Replace(" ", "");
        if (digits.Length != 11) return cuit;
        return $"{digits[..2]}-{digits[2..10]}-{digits[10]}";
    }
}
