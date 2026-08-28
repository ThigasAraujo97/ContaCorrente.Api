using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContaCorrente.Api.Infrastructure.Configurations;

public sealed class MovimentacaoConfiguration : IEntityTypeConfiguration<Movimentacao>
{
    public void Configure(EntityTypeBuilder<Movimentacao> builder)
    {
        builder.ToTable("Movimentacoes");

        builder.HasKey(m => m.Id);

        // Obrigatório, não cosmético. O domínio gera o Guid no construtor. Sem esta
        // linha, o EF assume chave gerada por ele: ao descobrir a movimentação pela
        // coleção do agregado e ver a chave já preenchida, conclui "linha existente" e
        // emite UPDATE em vez de INSERT — que não afeta nenhuma linha e vira, de forma
        // enganosa, um DbUpdateConcurrencyException.
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ContaId).IsRequired();

        builder.Property(m => m.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Valor)
            .HasConversion(new CentavosConverter())
            .IsRequired();

        builder.Property(m => m.SaldoResultante)
            .HasConversion(new CentavosConverter())
            .IsRequired();

        builder.Property(m => m.Descricao).HasMaxLength(200);

        // Nullable: lançamentos anteriores à introdução do campo não têm forma de
        // pagamento, e o extrato é histórico — não se reescreve o passado.
        builder.Property(m => m.FormaPagamento).HasConversion<int?>();

        builder.Property(m => m.OcorridaEm).IsRequired();

        // Cobre a consulta de histórico: filtro por conta + ordenação/período por data.
        builder.HasIndex(m => new { m.ContaId, m.OcorridaEm });

        // Cobre o filtro por forma de pagamento dentro de uma conta.
        builder.HasIndex(m => new { m.ContaId, m.FormaPagamento });
    }
}
