using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContaCorrente.Api.Infrastructure.Converters;

/// <summary>
/// Persiste valores monetários como inteiro de centavos.
/// <para>
/// O SQLite não tem tipo decimal nativo — o provider cairia em REAL (ponto flutuante),
/// o que quebra comparações e somas de dinheiro. Guardando centavos em INTEGER, toda
/// aritmética no banco é exata e o domínio continua trabalhando com <c>decimal</c>.
/// </para>
/// </summary>
public sealed class CentavosConverter : ValueConverter<decimal, long>
{
    public CentavosConverter()
        : base(
            valor => (long)Math.Round(valor * 100m, MidpointRounding.AwayFromZero),
            centavos => centavos / 100m)
    {
    }
}
