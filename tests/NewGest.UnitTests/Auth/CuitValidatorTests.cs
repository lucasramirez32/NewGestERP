using FluentAssertions;
using NewGest.Domain.Services;

namespace NewGest.UnitTests.Auth;

public class CuitValidatorTests
{
    [Theory]
    // CUITs inválidos
    [InlineData("20-12345678-9", false)] // DV incorrecto (correcto sería -6)
    [InlineData("20123456789", false)]   // 11 dígitos pero DV incorrecto
    [InlineData("", false)]              // vacío
    [InlineData(null, false)]            // nulo
    [InlineData("12-34567-890", false)]  // longitud incorrecta
    [InlineData("AB-12345678-9", false)] // letras no permitidas
    // CUITs válidos — calculados con algoritmo módulo 11
    [InlineData("20-12345678-6", true)]  // persona física
    [InlineData("23-87654321-4", true)]  // CUIL con prefijo 23
    [InlineData("27-11223344-5", true)]  // CUIL con prefijo 27 (femenino)
    [InlineData("30-55667788-9", true)]  // empresa (prefijo 30)
    [InlineData("33-99887766-6", true)]  // empresa (prefijo 33)
    public void EsValido_retorna_resultado_correcto(string? cuit, bool esperado)
    {
        var resultado = CuitValidator.EsValido(cuit);
        resultado.Should().Be(esperado, $"CUIT '{cuit}' debería ser {(esperado ? "válido" : "inválido")}");
    }

    [Theory]
    [InlineData("20123456786", "20-12345678-6")]
    [InlineData("30556677889", "30-55667788-9")]
    [InlineData("20-12345678-6", "20-12345678-6")] // ya formateado
    public void Formatear_retorna_formato_con_guiones(string cuit, string esperado)
    {
        var resultado = CuitValidator.Formatear(cuit);
        resultado.Should().Be(esperado);
    }

    [Fact]
    public void Formatear_cuit_invalido_retorna_mismo_valor()
    {
        CuitValidator.Formatear("123").Should().Be("123");
    }

    [Fact]
    public void Formatear_null_retorna_string_vacio()
    {
        CuitValidator.Formatear(null).Should().BeEmpty();
    }
}
