using FluentAssertions;
using NewGest.Domain.Services;
using Xunit;

namespace NewGest.UnitTests.Contabilidad;

public class NumberToLettersTests
{
    [Theory]
    // 1. Cero y básicos
    [InlineData(0, "CERO PESOS")]
    [InlineData(1, "UN PESO")]
    [InlineData(2, "DOS PESOS")]
    [InlineData(5, "CINCO PESOS")]
    [InlineData(9, "NUEVE PESOS")]
    [InlineData(10, "DIEZ PESOS")]
    
    // 2. Números entre 11 y 19
    [InlineData(11, "ONCE PESOS")]
    [InlineData(15, "QUINCE PESOS")]
    [InlineData(16, "DIECISÉIS PESOS")]
    [InlineData(19, "DIECINUEVE PESOS")]
    
    // 3. Decenas y combinaciones
    [InlineData(20, "VEINTE PESOS")]
    [InlineData(21, "VEINTIUN PESOS")]
    [InlineData(22, "VEINTIDOS PESOS")]
    [InlineData(26, "VEINTISEIS PESOS")]
    [InlineData(30, "TREINTA PESOS")]
    [InlineData(31, "TREINTA Y UN PESOS")]
    [InlineData(45, "CUARENTA Y CINCO PESOS")]
    [InlineData(99, "NOVENTA Y NUEVE PESOS")]
    
    // 4. Centenas
    [InlineData(100, "CIEN PESOS")]
    [InlineData(101, "CIENTO UN PESOS")]
    [InlineData(120, "CIENTO VEINTE PESOS")]
    [InlineData(121, "CIENTO VEINTIUN PESOS")]
    [InlineData(200, "DOSCIENTOS PESOS")]
    [InlineData(500, "QUINIENTOS PESOS")]
    [InlineData(999, "NOVECIENTOS NOVENTA Y NUEVE PESOS")]
    
    // 5. Miles
    [InlineData(1000, "MIL PESOS")]
    [InlineData(1001, "MIL UN PESOS")]
    [InlineData(1021, "MIL VEINTIUN PESOS")]
    [InlineData(2000, "DOS MIL PESOS")]
    [InlineData(5843, "CINCO MIL OCHOCIENTOS CUARENTA Y TRES PESOS")]
    [InlineData(999999, "NOVECIENTOS NOVENTA Y NUEVE MIL NOVECIENTOS NOVENTA Y NUEVE PESOS")]
    
    // 6. Millones (exactos con "DE" y no exactos sin "DE")
    [InlineData(1000000, "UN MILLÓN DE PESOS")]
    [InlineData(2000000, "DOS MILLONES DE PESOS")]
    [InlineData(1000001, "UN MILLÓN UN PESOS")]
    [InlineData(1002000, "UN MILLÓN DOS MIL PESOS")]
    [InlineData(1500000, "UN MILLÓN QUINIENTOS MIL PESOS")]
    [InlineData(10000000, "DIEZ MILLONES DE PESOS")]
    [InlineData(999000000, "NOVECIENTOS NOVENTA Y NUEVE MILLONES DE PESOS")]
    
    // 7. Centavos únicamente (entero == 0)
    [InlineData(0.01, "UN CENTAVO")]
    [InlineData(0.02, "DOS CENTAVOS")]
    [InlineData(0.10, "DIEZ CENTAVOS")]
    [InlineData(0.25, "VEINTICINCO CENTAVOS")]
    [InlineData(0.50, "CINCUENTA CENTAVOS")]
    [InlineData(0.99, "NOVENTA Y NUEVE CENTAVOS")]
    
    // 8. Combinaciones con decimales (Pesos + Centavos)
    [InlineData(1.01, "UN PESO CON UN CENTAVO")]
    [InlineData(1.50, "UN PESO CON CINCUENTA CENTAVOS")]
    [InlineData(10.25, "DIEZ PESOS CON VEINTICINCO CENTAVOS")]
    [InlineData(100.05, "CIEN PESOS CON CINCO CENTAVOS")]
    [InlineData(1000.50, "MIL PESOS CON CINCUENTA CENTAVOS")]
    [InlineData(1000000.99, "UN MILLÓN DE PESOS CON NOVENTA Y NUEVE CENTAVOS")]
    [InlineData(999999999.99, "NOVECIENTOS NOVENTA Y NUEVE MILLONES NOVECIENTOS NOVENTA Y NUEVE MIL NOVECIENTOS NOVENTA Y NUEVE PESOS CON NOVENTA Y NUEVE CENTAVOS")]
    public void Convert_DebeRetornarTextoEsperado(decimal monto, string esperado)
    {
        var resultado = NumberToLetters.Convert(monto);
        resultado.Should().Be(esperado);
    }

    [Fact]
    public void Convert_ConMonedaDiferente_DebeUsarMonedaEspecificada()
    {
        NumberToLetters.Convert(1, "DÓLAR").Should().Be("UN DÓLAR");
        NumberToLetters.Convert(2, "DÓLARES").Should().Be("DOS DÓLARES");
        NumberToLetters.Convert(1000000, "DÓLARES").Should().Be("UN MILLÓN DE DÓLARES");
    }
}
