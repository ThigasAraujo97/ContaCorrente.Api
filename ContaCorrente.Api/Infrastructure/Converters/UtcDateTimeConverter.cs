using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ContaCorrente.Api.Infrastructure.Converters;

/// <summary>
/// Garante que datas voltem do banco marcadas como UTC.
/// <para>
/// O SQLite não guarda o fuso: ao ler, o EF devolve <c>DateTimeKind.Unspecified</c>, e o
/// System.Text.Json serializa sem o sufixo <c>Z</c>. O JavaScript trata uma data sem
/// <c>Z</c> como horário local, o que deslocaria o extrato pelo offset do fuso do
/// navegador. Este conversor reancora o Kind na leitura.
/// </para>
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            aoGravar => aoGravar.ToUniversalTime(),
            aoLer => DateTime.SpecifyKind(aoLer, DateTimeKind.Utc))
    {
    }
}
