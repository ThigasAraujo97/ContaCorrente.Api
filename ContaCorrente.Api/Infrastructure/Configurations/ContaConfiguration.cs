using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContaCorrente.Api.Infrastructure.Configurations;

public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("Contas");

        builder.HasKey(c => c.Id);

        // A conta também gera o próprio Guid. Ver MovimentacaoConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Documento)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Documento).IsUnique();

        builder.Property(c => c.Saldo)
            .HasConversion(new CentavosConverter())
            .IsRequired();

        builder.Property(c => c.CriadaEm).IsRequired();
        builder.Property(c => c.AtualizadaEm).IsRequired();

        // Concorrência otimista: o UPDATE carrega "WHERE Versao = <valor lido>".
        // Se outra transação movimentou a conta nesse meio tempo, zero linhas são
        // afetadas e o EF lança DbUpdateConcurrencyException.
        builder.Property(c => c.Versao)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasMany(c => c.Movimentacoes)
            .WithOne()
            .HasForeignKey(m => m.ContaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Conta.Movimentacoes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
