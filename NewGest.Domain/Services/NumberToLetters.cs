namespace NewGest.Domain.Services;

/// <summary>
/// Convierte un monto decimal a texto en español (Argentina).
/// Migrado de FUNCION.PRG:329–417 — algoritmo idéntico al VFP para uso en cheques.
/// CRÍTICO: cualquier divergencia con el output VFP es un error legal.
/// Mínimo 50 casos validados contra VFP antes del go-live (ver NumberToLettersTests).
/// </summary>
public static class NumberToLetters
{
    private static readonly string[] Unidades =
        ["", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
         "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE",
         "DIECIOCHO", "DIECINUEVE"];

    private static readonly string[] Decenas =
        ["", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA",
         "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"];

    private static readonly string[] Centenas =
        ["", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
         "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"];

    public static string Convert(decimal monto, string moneda = "PESOS")
    {
        if (monto < 0) throw new ArgumentOutOfRangeException(nameof(monto), "El monto no puede ser negativo.");
        if (monto == 0) return $"CERO {moneda}";

        var entero    = (long)Math.Floor(monto);
        var centavos  = (int)Math.Round((monto - entero) * 100);

        if (entero == 0 && centavos > 0)
        {
            return $"{ConvertirMenorCien(centavos)} CENTAVO{(centavos == 1 ? "" : "S")}";
        }

        var parteEntera = ConvertirEntero(entero, esMillon: false);
        
        string conectorMoneda = (entero % 1_000_000 == 0) ? " DE " : " ";
        var resultado = $"{parteEntera}{conectorMoneda}{Pluralizar(entero, moneda)}";

        if (centavos > 0)
            resultado += $" CON {ConvertirMenorCien(centavos)} CENTAVO{(centavos == 1 ? "" : "S")}";

        return resultado.Trim();
    }

    private static string ConvertirEntero(long n, bool esMillon)
    {
        if (n == 0) return "";
        if (n == 1 && esMillon) return "UN";

        if (n < 20)   return Unidades[n];
        if (n < 100)  return ConvertirMenorCien((int)n);
        if (n < 1000) return ConvertirMenorMil((int)n);

        if (n < 1_000_000)
        {
            var miles   = n / 1000;
            var resto   = n % 1000;
            var prefijo = miles == 1 ? "MIL" : $"{ConvertirEntero(miles, false)} MIL";
            return resto == 0 ? prefijo : $"{prefijo} {ConvertirMenorMil((int)resto)}";
        }

        if (n < 1_000_000_000)
        {
            var millones = n / 1_000_000;
            var resto    = n % 1_000_000;
            var prefijo  = millones == 1
                ? "UN MILLÓN"
                : $"{ConvertirEntero(millones, true)} MILLONES";
            return resto == 0 ? prefijo : $"{prefijo} {ConvertirEntero(resto, false)}";
        }

        // Máximo soportado: 999.999.999,99
        throw new ArgumentOutOfRangeException(nameof(n), "Monto máximo soportado: 999.999.999,99");
    }

    private static string ConvertirMenorCien(int n)
    {
        if (n < 20) return Unidades[n];
        var dec  = n / 10;
        var unit = n % 10;
        if (unit == 0) return Decenas[dec];
        return dec == 2
            ? $"VEINTI{Unidades[unit]}"   // VEINTIUNO, VEINTIDÓS, etc.
            : $"{Decenas[dec]} Y {Unidades[unit]}";
    }

    private static string ConvertirMenorMil(int n)
    {
        if (n == 100) return "CIEN";
        var cent  = n / 100;
        var resto = n % 100;
        if (cent == 0) return ConvertirMenorCien(resto);
        return resto == 0
            ? Centenas[cent]
            : $"{Centenas[cent]} {ConvertirMenorCien(resto)}";
    }

    private static string Pluralizar(long entero, string moneda) =>
        entero == 1 ? moneda.TrimEnd('S') : moneda;  // PESO / PESOS
}
